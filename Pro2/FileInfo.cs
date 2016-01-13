using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ocNet.Lib;
using System.Runtime.InteropServices;

namespace ocNet_Backup.core
{
	public delegate void FileInfoScanHandler(object source, int fileCount);

	public sealed class FileInfo : IComparable
	{
		string			basePath;                    // Only set at root
		string			relPath;                     // Only set at directories

		string			fileName;
		DateTime		fileTimeWrite;
		DateTime		fileTimeCreate;
		FileAttributes	fileAttr;
		long			fileLength;
		bool			isDirectory;
		FileInfo		parent;
		FileInfo		root;
		List<FileInfo>	files;
		List<FileInfo>	childs;
		bool			stopScan;			// Only used in root 

		public event FileInfoScanHandler infoScanCallback;

		public string			FileName		{ get { return fileName;	} }
		public DateTime			FileTimeWrite	{ get { return fileTimeWrite; } }
		public DateTime			FileTimeCreate	{ get { return fileTimeCreate; } }
		public FileAttributes	FileAttributes	{ get { return fileAttr; } }
		public long				FileLength		{ get { return fileLength; } }
		public List<FileInfo>	Files			{ get { return files;	} }
		public List<FileInfo>	Directories		{ get { return childs; } }
		public string			FullPath		{ get { return root.basePath + RelPath; } }
		public bool				IsDirectory		{ get { return isDirectory; } }

		public string RelPath
		{
			get
			{
				if (isDirectory)
					return relPath;
				else if (parent != null && parent.isDirectory)
					return parent.relPath;
				else
					return null;
			}
		}

		FileInfo()
		{
		}

		public FileInfo(string basePath)
		{
			if (basePath.EndsWith("\\"))
				this.basePath = basePath;
			else
				this.basePath = basePath + "\\";

			this.relPath = "";
			this.fileName = ".";
			this.isDirectory = true;
			root = this;
		}

		FileInfo(FileInfo parent, string directoryname, DateTime fileTimeCreate)
		{
			this.relPath = parent.relPath + directoryname + "\\";
			this.fileName = directoryname;
			this.parent = parent;
			this.isDirectory = true;
			this.root = parent.root;
			this.infoScanCallback = parent.infoScanCallback;
		}

		FileInfo(FileInfo parent, string fileName, DateTime fileTimeCreate, DateTime fileTimeWrite, FileAttributes fileAttr, long fileLength)
		{
			this.fileName = fileName;
			this.fileTimeCreate = fileTimeCreate;
			this.fileTimeWrite = fileTimeWrite;
			this.fileAttr = fileAttr;
			this.fileLength = fileLength;
			this.parent = parent;
			this.root = parent.root;
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
				return 1;

			if (!(obj is FileInfo))
				throw new ArgumentException();

			return fileName.CompareTo(((FileInfo)obj).fileName);
		}

		public void Delete()
		{
			if (isDirectory && (files != null && files.Count > 0 || childs != null && childs.Count > 0))
				throw new Exception("FileInfo.Delete: Can not delete an item that has subitems !");

			// Get the right array
			if (isDirectory)
				parent.childs.Remove(this);
			else
				parent.files.Remove(this);
		}

		public void Update(DateTime fileTimeCreate, DateTime fileTimeWrite, long fileLength)
		{
			this.fileTimeCreate = fileTimeCreate;
			if (!isDirectory)
			{
				this.fileTimeWrite = fileTimeWrite;
				this.fileLength = fileLength;
			}
		}

		void Insert(string[] dirs, int dirLevel, string fileName, DateTime fileTimeCreate, DateTime fileTimeWrite, FileAttributes fileAttr, long fileLength)
		{
			int i;

			if (dirs == null || dirLevel >= dirs.Length - 1)
			{
				// Directory was found .. care about files
				// Is file existing; Update (better use Update function directly)
				if (files != null)
				{
					for (i = 0; i < files.Count; i++)
					{
						if (string.Compare(files[i].fileName, fileName, true) == 0)
						{
							files[i].Update(fileTimeCreate, fileTimeWrite, fileLength);
							return;
						}
					}
				}
				files.Add(new FileInfo(this, fileName, fileTimeCreate, fileTimeWrite, fileAttr, fileLength));
				files.Sort();
			}
			else
			{
				// Try find directory
				if (childs != null)
				{
					for (i = 0; i < childs.Count; i++)
					{
						if (string.Compare(childs[i].fileName, dirs[dirLevel], true) == 0)
						{
							childs[i].Insert(dirs, dirLevel + 1, fileName, fileTimeCreate, fileTimeWrite, fileAttr, fileLength);
							return;
						}
					}
				}
				childs.Add(new FileInfo(this, dirs[dirLevel], fileTimeCreate));
				childs.Sort();
			}
		}

		public void Insert(string path, string fileName, DateTime fileTimeCreate, DateTime fileTimeWrite, FileAttributes fileAttr, long fileLength)
		{
			string[] dir;

			dir = path.Split('\\');
			Insert(dir, 0, fileName, fileTimeCreate, fileTimeWrite, fileAttr, fileLength);
		}

		[DllImport("ocNet.LibNative.dll", CharSet = CharSet.Auto)]
		static extern Int32 ScanDirectory(string directory, out IntPtr arrayPtr);

		[DllImport("ocNet.LibNative.dll")]
		static extern void ScanDirectoryFree(IntPtr arrayPtr);
		

		public void ScanItem()
		{
			IntPtr arrayValue = IntPtr.Zero;
			IntPtr arrayValueInitial = IntPtr.Zero;
			Int32 count;
			Int32 itemSize;
			PInvoke.WIN32_FIND_DATA data;

			count = ScanDirectory(DirectoryNat.MakeUniversalPath(root.basePath + relPath + "*"), out arrayValueInitial);
			if (count == -1)
				throw new Exception("First filefind failed");
			arrayValue = arrayValueInitial;

			itemSize = Marshal.SizeOf(typeof(PInvoke.WIN32_FIND_DATA));
			for (int item = 0; item < count; item++)
			{
				DateTime creationTime;
				DateTime lastWriteTime;

				data = (PInvoke.WIN32_FIND_DATA)Marshal.PtrToStructure(arrayValue, typeof(PInvoke.WIN32_FIND_DATA));

				creationTime = DateTime.FromFileTimeUtc((((Int64)data.ftLastAccessTime.dwHighDateTime) << 32) + (Int64)data.ftLastAccessTime.dwLowDateTime);
				lastWriteTime = DateTime.FromFileTimeUtc((((Int64)data.ftLastWriteTime.dwHighDateTime) << 32) + (Int64)data.ftLastWriteTime.dwLowDateTime);

				if ((data.dwFileAttributes & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
				{
					if (data.cFileName != "." && data.cFileName != "..")
					{
						if (childs == null)
							childs = new List<FileInfo>();
						childs.Add(new FileInfo(this, data.cFileName, creationTime));
					}
				}
				else
				{
					if (files == null)
						files = new List<FileInfo>();
					files.Add(new FileInfo(this, data.cFileName, creationTime, lastWriteTime, data.dwFileAttributes, ((Int64)data.nFileSizeHigh << 32) | data.nFileSizeLow));
				}

				arrayValue = new IntPtr(arrayValue.ToInt64() + itemSize);
			}

			ScanDirectoryFree(arrayValueInitial);
		}

		public void ScanItemN()
		{
			FileFindNat find = new FileFindNat(DirectoryNat.MakeUniversalPath(root.basePath + relPath + "*"));
			bool findOK;

			findOK = find.Next();
			// Error if not found on first item
			if (!findOK)
				throw new Exception("First filefind failed");

			while (findOK)
			{
				if (find.IsDirectory)
				{
					if (find.Name != "." && find.Name != "..")
					{
						if (childs == null)
							childs = new List<FileInfo>();
						childs.Add(new FileInfo(this, find.Name, find.CreationTimeUtc));
					}
				}
				else
				{
					if (files == null)
						files = new List<FileInfo>();
					files.Add(new FileInfo(this, find.Name, find.CreationTimeUtc, find.LastWriteTimeUtc, find.Attributes, find.Length));
				}

				findOK = find.Next();
			}
			find.Dispose();
		}

		public void ScanStop()
		{
			stopScan = true;
			// ToDo: How can we see if the scan was complete ??
		}

		public void Scan()
		{
			root.stopScan = false;
			ScanSub();
		}

		void ScanSub()
		{
			if (root.stopScan)
				return;

			ScanItem();

			if (infoScanCallback != null)
				infoScanCallback(this, files == null ? 0 : files.Count);

			if (files != null)
				files.Sort();
			if (childs != null)
			{
				childs.Sort();

				foreach (FileInfo fi in childs)
					fi.ScanSub();
			}
		}

		public void DebugContent(StreamWriter stream)
		{
			if (isDirectory)
			{
				if (parent == null)
					stream.WriteLine("Full dir: {0}", root.basePath + relPath);
				else
					stream.WriteLine("Sub dir: {0} => {1}", fileName, root.basePath + relPath);

				if (files != null)
					foreach (FileInfo fi in files)
						fi.DebugContent(stream);

				if (childs != null)
					foreach (FileInfo fi in childs)
						fi.DebugContent(stream);
			}
			else
				stream.WriteLine(" {0} {1} {2} {3}", fileName, fileTimeCreate, fileTimeWrite, fileLength);
		}

		// Reads the file structure from file; uses the basePath for file name
		public void Read(MemFile mf)
		{
			if (mf.GetUInt32() != 0xDEADBEEF)
			{
				throw new ArgumentException("Magic Word");
			}
			if (mf.GetUInt32() != 1)
			{
				throw new ArgumentException("Unknown Version");
			}
			ReadItem(mf);
		}

		// Reads a file item
		void ReadItem(MemFile mf)
		{
			int len, cou;
			FileInfo fi;

			fileName = mf.GetDynUTF8String();
			fileTimeCreate = new DateTime(mf.GetInt64(), DateTimeKind.Utc);
			fileTimeWrite = new DateTime(mf.GetInt64(), DateTimeKind.Utc);
			fileLength = mf.GetInt64();
			isDirectory = mf.GetBool();
			if (isDirectory)
				relPath = mf.GetDynUTF8String();
			len = mf.GetInt32();
			if (len > 0)
			{
				files = new List<FileInfo>(len);
				for (cou = 0; cou < len; cou++)
				{
					fi = new FileInfo();
					files.Add(fi);
					fi.parent = this;
					fi.root = this.root;
					fi.ReadItem(mf);
				}
			}

			len = mf.GetInt32();
			if (len > 0)
			{
				childs = new List<FileInfo>(len);
				for (cou = 0; cou < len; cou++)
				{
					fi = new FileInfo();
					childs.Add(fi);
					fi.root = this.root;
					fi.ReadItem(mf);
				}
			}
		}

		// Writes the file structure into a file; uses the basePath for file name
		public void Write(MemFile mf)
		{
			mf.Set((UInt32)0xDEADBEEF);    // Magic word
			mf.Set((UInt32)1);             // Version;
			WriteItem(mf);
		}

		// Writes a file item
		void WriteItem(MemFile mf)
		{
			mf.SetDynUTF8String(fileName);
			mf.Set(fileTimeCreate.Ticks);
			mf.Set(fileTimeWrite.Ticks);
			mf.Set(fileLength);
			mf.Set(isDirectory);
			if (isDirectory)
				mf.SetDynUTF8String(relPath);
			if (files == null)
				mf.Set((Int32)0);
			else
			{
				mf.Set(files.Count);
				foreach (FileInfo fi in files)
					fi.WriteItem(mf);
			}

			if (childs == null)
				mf.Set((Int32)0);
			else
			{
				mf.Set(childs.Count);
				foreach (FileInfo fi in childs)
					fi.WriteItem(mf);
			}
		}
	}
}
