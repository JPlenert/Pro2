using System;
using System.Collections.Generic;
using System.Text;

namespace ocNet_Backup.core
{
	public enum FileInfoCompareType
	{
		FullSync,
		SingleMaster		// LeftToRight
	};

	public sealed class FileInfoCompare
	{
		public class FileInfoCompareItem
		{
			public FileInfo				fLeftAct, fLeftLast;
			public FileInfo				fRightAct, fRightLast;

			public bool 				fDiffer, fConflict;
			public bool 				fLeftCopy, fRightCopy;
			public bool 				fLeftDelete, fRightDelete;

			// Missing files
			public bool 				fLAMissing, fLLMissing;
			public bool 				fRAMissing, fRLMissing;

			// For use in CompareDlg
			public FileInfoCompareItem	parentDir;
			public bool					showItem;

			public bool IsDirectory
			{
				get
				{
					return !fLAMissing && fLeftAct.IsDirectory ||
						!fLLMissing && fLeftLast.IsDirectory ||
						!fRAMissing && fRightAct.IsDirectory ||
						!fRLMissing && fRightLast.IsDirectory;
				}
			}
		};

		FileInfoCompareType			compareType;
		FileInfo					fiLeftAct;
		FileInfo 					fiRightAct;
		FileInfo 					fiLeftLast;
		FileInfo 					fiRightLast;

		public List<FileInfoCompareItem> compareItems;

		public long StatDirCountL		{ get; private set; }
		public long StatDirCountR		{ get; private set; }
		public long StatDirsToDeleteL	{ get; private set; }
		public long StatDirsToDeleteR	{ get; private set; }
		public long StatDirsToCreateL	{ get; private set; }
		public long StatDirsToCreateR	{ get; private set; }

		public long StatFileCountL		{ get; private set; }
		public long StatFileCountR		{ get; private set; }
		public long StatFilesToCreateL	{ get; private set; }
		public long StatFilesToCreateR	{ get; private set; }
		public long StatFilesToCopyLToR	{ get; private set; }
		public long StatFilesToCopyRToL	{ get; private set; }
		public long StatBytesToCopyRToL	{ get; private set; }
		public long StatBytesToCopyLToR	{ get; private set; }
		public long StatFilesToDeleteL	{ get; private set; }
		public long StatFilesToDeleteR	{ get; private set; }

		public long StatFilesInConflict	{ get; private set; }
		public bool StatIsDifferent		{ get; private set; }

		public FileInfoCompare(FileInfo fiLeftAct, FileInfo fiLeftLast, FileInfo fiRightAct, FileInfo fiRightLast)
		{
			this.fiLeftAct = fiLeftAct;
			this.fiRightAct = fiRightAct;
			this.fiLeftLast = fiLeftLast;
			this.fiRightLast = fiRightLast;

			compareType = FileInfoCompareType.FullSync;
			compareItems = new List<FileInfoCompareItem>();
		}

		public FileInfoCompare(FileInfo fiSource, FileInfo fiDestination)
		{
			this.fiLeftAct = fiSource;
			this.fiRightAct = fiDestination;
			this.fiLeftLast = null;
			this.fiRightLast = null;

			compareType = FileInfoCompareType.SingleMaster;
			compareItems = new List<FileInfoCompareItem>();
		}

		void CompareItems(List<FileInfo> leftAct, List<FileInfo> leftLast, List<FileInfo> rightAct, List<FileInfo> rightLast, FileInfoCompareItem parentDir)
		{
			int indexOnLA, indexOnLL, indexOnRA, indexOnRL, count;
			FileInfoCompareItem fici;
			string[] fileSortString;
			string refString;

			indexOnLL = indexOnLA = indexOnRA = indexOnRL = 0;

			while ((leftAct != null && indexOnLA < leftAct.Count) || (leftLast != null && indexOnLL < leftLast.Count) ||
				   (rightAct != null && indexOnRA < rightAct.Count) || (rightLast != null && indexOnRL < rightLast.Count))
			{
				fici = new FileInfoCompareItem();
				fici.parentDir = parentDir;

				// What files are missing ?
				fici.fLAMissing = (leftAct == null || indexOnLA == leftAct.Count);
				fici.fLLMissing = (leftLast == null || indexOnLL == leftLast.Count);
				fici.fRAMissing = (rightAct == null || indexOnRA == rightAct.Count);
				fici.fRLMissing = (rightLast == null || indexOnRL == rightLast.Count);

				// We need the first file => ref string!
				fileSortString = new string[] { fici.fLAMissing ? "" : leftAct[indexOnLA].FileName, fici.fLLMissing ? "" : leftLast[indexOnLL].FileName,
                    fici.fRAMissing ? "" : rightAct[indexOnRA].FileName, fici.fRLMissing ? "" : rightLast[indexOnRL].FileName};
				Array.Sort(fileSortString);

				refString = "";
				for (count = 0; count < 4; count++)
				{
					if (fileSortString[count].Length > 0)
					{
						refString = fileSortString[count];
						break;
					}
				}

				// Add the infos !
				if (!fici.fLAMissing && string.Compare(refString, leftAct[indexOnLA].FileName, true) == 0)
					fici.fLeftAct = leftAct[indexOnLA++];
				else
					fici.fLAMissing = true;
				if (!fici.fLLMissing && string.Compare(refString, leftLast[indexOnLL].FileName, true) == 0)
					fici.fLeftLast = leftLast[indexOnLL++];
				else
					fici.fLLMissing = true;
				if (!fici.fRAMissing && string.Compare(refString, rightAct[indexOnRA].FileName, true) == 0)
					fici.fRightAct = rightAct[indexOnRA++];
				else
					fici.fRAMissing = true;
				if (!fici.fRLMissing && string.Compare(refString, rightLast[indexOnRL].FileName, true) == 0)
					fici.fRightLast = rightLast[indexOnRL++];
				else
					fici.fRLMissing = true;

				// Left is not existing but on right side
				if (fici.fLeftAct == null && fici.fRightAct != null)
				{
					fici.fDiffer = true;
					if (compareType == FileInfoCompareType.FullSync)
					{
						// was the file there at the last sync
						if (fici.fLeftLast != null)
						{
							// yes, so we must delete the right file; if it was not altered since last sync
							if (fici.fLeftLast.FileTimeWrite == fici.fRightAct.FileTimeWrite &&
								fici.fLeftLast.FileLength == fici.fRightAct.FileLength)
								fici.fRightDelete = true;
							else
								// Else copy the right one
								fici.fRightCopy = true;
						}
						else
							// no, just copy right
							fici.fRightCopy = true;
					}
					else
						// On Left-to-Right, if right is existing, delete !
						fici.fRightDelete = true;
				}
				// Right is not existing but on the left side
				else if (fici.fRightAct == null && fici.fLeftAct != null)
				{
					fici.fDiffer = true;
					if (compareType == FileInfoCompareType.FullSync)
					{
						// was the file there at the last sync
						if (fici.fRightLast != null)
						{
							// yes, so we must delete the left file; if it was not altered since last sync
							if (fici.fRightLast.FileTimeWrite == fici.fLeftAct.FileTimeWrite &&
								fici.fRightLast.FileLength == fici.fLeftAct.FileLength)
								fici.fLeftDelete = true;
							else
								// Else copy the left one
								fici.fLeftCopy = true;
						}
						else
							// no, just copy left
							fici.fLeftCopy = true;
					}
					else
						// On Left-To-Right, if right is not existing, copy left
						fici.fLeftCopy = true;
				}
				// Both files are existing
				else if (fici.fRightAct != null && fici.fLeftAct != null)
				{
					// Do the files differ ?
					if (fici.fLeftAct.FileTimeWrite != fici.fRightAct.FileTimeWrite ||
						fici.fLeftAct.FileLength != fici.fRightAct.FileLength)
					{
						bool leftChanged, rightChanged;

						fici.fDiffer = true;

						if (compareType == FileInfoCompareType.FullSync)
						{
							leftChanged = !(fici.fLeftLast != null &&
											fici.fLeftAct.FileTimeWrite == fici.fLeftLast.FileTimeWrite &&
											fici.fLeftAct.FileLength == fici.fLeftLast.FileLength);
							rightChanged = !(fici.fRightLast != null &&
											fici.fRightAct.FileTimeWrite == fici.fRightLast.FileTimeWrite &&
											fici.fRightAct.FileLength == fici.fRightLast.FileLength);
							// Left changed and right unchanged, copy the left one !
							if (leftChanged && !rightChanged)
								fici.fLeftCopy = true;
							// Right changed and left unchanged, copy the right one !
							else if (!leftChanged && rightChanged)
								fici.fRightCopy = true;
							// We have a conflict !
							else
								fici.fConflict = true;
						}
						else
							// On Left-To-Right, if differ copy the left
							fici.fLeftCopy = true;
					}
				}

				compareItems.Add(fici);
			};
		}

		void CompareDirectoryWithSub(FileInfo fiLeftAct, FileInfo fiLeftLast, FileInfo fiRightAct, FileInfo fiRightLast, FileInfoCompareItem parentDir)
		{
			int		lastRecFici = 0;

			CompareItems(fiLeftAct == null ? null : fiLeftAct.Files, fiLeftLast == null ? null : fiLeftLast.Files,
				fiRightAct == null ? null : fiRightAct.Files, fiRightLast == null ? null : fiRightLast.Files, parentDir);
			CompareItems(fiLeftAct == null ? null : fiLeftAct.Directories, fiLeftLast == null ? null : fiLeftLast.Directories,
				fiRightAct == null ? null : fiRightAct.Directories, fiRightLast == null ? null : fiRightLast.Directories, parentDir);

			while (compareItems.Count > lastRecFici)
			{
				if (compareItems[lastRecFici].IsDirectory)
				{
					CompareItems(compareItems[lastRecFici].fLeftAct == null ? null : compareItems[lastRecFici].fLeftAct.Files, 
						compareItems[lastRecFici].fLeftLast == null ? null : compareItems[lastRecFici].fLeftLast.Files,
						compareItems[lastRecFici].fRightAct == null ? null : compareItems[lastRecFici].fRightAct.Files, 
						compareItems[lastRecFici].fRightLast == null ? null : compareItems[lastRecFici].fRightLast.Files, parentDir);
					CompareItems(compareItems[lastRecFici].fLeftAct == null ? null : compareItems[lastRecFici].fLeftAct.Directories, 
						compareItems[lastRecFici].fLeftLast == null ? null : compareItems[lastRecFici].fLeftLast.Directories,
						compareItems[lastRecFici].fRightAct == null ? null : compareItems[lastRecFici].fRightAct.Directories, 
						compareItems[lastRecFici].fRightLast == null ? null : compareItems[lastRecFici].fRightLast.Directories, parentDir);
				}
				lastRecFici++;
			}
		}

		public void CompareWithSub()
		{
			CompareDirectoryWithSub(fiLeftAct, fiLeftLast, fiRightAct, fiRightLast, null);
			BuildStatistics();
		}

		public void CompareFlat()
		{
			CompareItems(fiLeftAct == null ? null : fiLeftAct.Files, fiLeftLast == null ? null : fiLeftLast.Files,
				fiRightAct == null ? null : fiRightAct.Files, fiRightLast == null ? null : fiRightLast.Files, null);
			CompareItems(fiLeftAct == null ? null : fiLeftAct.Directories, fiLeftLast == null ? null : fiLeftLast.Directories,
				fiRightAct == null ? null : fiRightAct.Directories, fiRightLast == null ? null : fiRightLast.Directories, null);
		}

		void BuildStatistics()
		{
			StatDirsToCreateR = StatFilesToCopyLToR = StatDirsToCreateL = StatBytesToCopyRToL =
				StatDirsToDeleteL = StatDirsToDeleteR = StatFilesInConflict = StatFileCountL = StatFileCountR =
				StatDirCountL = StatDirCountR = 0;

			foreach (FileInfoCompareItem fici in compareItems)
			{
				if (fici.fLeftAct != null)
					if (fici.fLeftAct.IsDirectory)
						StatDirCountL++;
					else
						StatFileCountL++;
				if (fici.fRightAct != null)
					if (fici.fRightAct.IsDirectory)
						StatDirCountR++;
					else
						StatFileCountR++;

				if (fici.fLeftCopy || fici.fRightCopy)
				{
					if (fici.fLeftCopy)
						if (fici.fLeftAct.IsDirectory)
							StatDirsToCreateR++;
						else
						{
							StatFilesToCopyLToR++;
							StatBytesToCopyLToR += fici.fLeftAct.FileLength;
							if (fici.fRAMissing)
								StatFilesToCreateR++;
						}

					if (fici.fRightCopy)
						if (fici.fRightAct.IsDirectory)
							StatDirsToCreateL++;
						else
						{
							StatFilesToCopyRToL++;
							StatBytesToCopyRToL += fici.fRightAct.FileLength;
							if (fici.fLAMissing)
								StatFilesToCreateL++;
						}
				}
				else if (fici.fLeftDelete || fici.fRightDelete)
				{
					if (fici.fLeftDelete)
						if (fici.fLeftAct.IsDirectory)
							StatDirsToDeleteL++;
						else
							StatFilesToDeleteL++;
					if (fici.fRightDelete)
						if (fici.fRightAct.IsDirectory)
							StatDirsToDeleteR++;
						else
							StatFilesToDeleteR++;
				}
				else if (fici.fConflict)
					StatFilesInConflict++;
			}

			StatIsDifferent = StatDirsToCreateR > 0 || StatDirsToCreateL > 0 || StatDirsToDeleteR > 0 || StatDirsToDeleteL > 0 || StatFilesToCopyRToL > 0 || StatFilesToCopyLToR > 0;
		}

	}
}
