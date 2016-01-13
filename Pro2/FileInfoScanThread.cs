using System;
using System.Collections.Generic;
using System.Text;
using ocNet.Lib;
using System.IO;

namespace ocNet_Backup.core
{
	public class FileInfoScanThread : ThreadBase
	{
		string		basePath;
		FileInfo	fileInfo;
		LogWriter	log;

		public string BasePath { get { return basePath; } }
		public FileInfo FileInfo { get { return fileInfo; } }

		public FileInfoScanThread(string basePath, LogWriter log)
		{
			this.log = log;
			this.basePath = basePath;
		}

		protected override void PreStop()
		{
			if (fileInfo != null && !IsStopped())
				fileInfo.ScanStop();
		}

		protected override void HandleException(Exception ex)
		{
			log.Append("Exception at FileInfoScanThread: {0}", ex.ToString());
		}

		protected override bool DoWork()
		{
			fileInfo = new FileInfo(basePath);
			fileInfo.Scan();

			return false;
		}
	}
}
