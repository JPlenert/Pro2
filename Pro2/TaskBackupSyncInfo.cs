using System;
using System.Collections.Generic;
using System.Text;
using ocNet.Lib;
using System.Data;

namespace ocNet_Backup.core
{
	public enum TaskBackupSyncStatus
	{
		Idle = 0,
		Start,

		GetFileInfoAgent,
		WaitFileInfoTicket,
		GetFileInfoServer,
		FileInfoCompare,
		CreateDirectories,
		BuildTickets,
		DeleteObjects,
		MonitorFileTransfer,
		WaitErrorList,

		CopyFiles,

		EndJob,
		Ready,
		FatalError
	};

	public enum JobType
	{
		SyncAgentJob = 0,
		ReportEMailJob,
		SyncSMBJob,
	}

	public class TaskBackupSyncInfo
	{
		Database			db;

		public int TaskID { get; set; }

		// Report values
		public DateTime StatTaskStart { get; set; }
		public DateTime StatTaskEnd { get; set; }

		public int	StartJobID { get; set; }

		public JobType Type { get; set; }

		public long StatFileCount { get; set; }
		public long StatFileCreateCount { get; set; }
		public long StatFileCopyCount { get; set; }
		public long StatFileCopyOKCount { get; set; }
		public long StatFileCopyErrorCount { get; set; }
		public long StatFileDeleteCount { get; set; }
		public long StatFileDeleteOKCount { get; set; }
		public long StatFileDeleteErrorCount { get; set; }

		public long StatDirCount { get; set; }
		public long StatDirCreateCount { get; set; }
		public long StatDirCreateOKCount { get; set; }
		public long StatDirCreateErrorCount { get; set; }
		public long StatDirDeleteCount { get; set; }
		public long StatDirDeleteOKCount { get; set; }
		public long StatDirDeleteErrorCount { get; set; }

		public long StatByteCopyCount { get; set; }
		public long StatByteCopyOKCount { get; set; }

		public long StatErrorCount { get; set; }

		public string SourcePath { get; set; }
		public string DestinationPath { get; set; }
		public string AgentMachineName { get; set; }
		DateTime lastDBWrite;

		public TaskBackupSyncInfo(Database db)
		{
			this.db = db;
			lastDBWrite = DateTime.MinValue;
		}

		public void WriteToDB(bool insert, bool forcenow)
		{
			// Do not write every single info to db
			if (!insert && !forcenow && (DateTime.UtcNow - lastDBWrite).TotalSeconds < 60)
				return;

			DatabaseCommandUpSert cmd = new DatabaseCommandUpSert(db, "TaskBackupSync");

			cmd.AddField("TaskID", TaskID);
			cmd.AddField("StartJobID", StartJobID);
			cmd.AddField("JobType", (int)Type);
			cmd.AddField("StartTS", StatTaskStart, true, true);
			cmd.AddField("EndTS", StatTaskEnd, true, true);
			cmd.AddField("AgentMachineName", AgentMachineName);
			cmd.AddField("SourcePath", SourcePath);
			cmd.AddField("DestinationPath", DestinationPath);
			cmd.AddField("FileCount", StatFileCount);
			cmd.AddField("FileCreateCount", StatFileCreateCount);
			cmd.AddField("FileCopyCount", StatFileCopyCount);
			cmd.AddField("FileCopyOKCount", StatFileCopyOKCount);
			cmd.AddField("FileCopyErrorCount", StatFileCopyErrorCount);
			cmd.AddField("FileDeleteCount", StatFileDeleteCount);
			cmd.AddField("FileDeleteOKCount", StatFileDeleteOKCount);
			cmd.AddField("FileDeleteErrorCount", StatFileDeleteErrorCount);
			cmd.AddField("DirCount", StatDirCount);
			cmd.AddField("DirCreateCount", StatDirCreateCount);
			cmd.AddField("DirCreateOKCount", StatDirCreateOKCount);
			cmd.AddField("DirCreateErrorCount", StatDirCreateErrorCount);
			cmd.AddField("DirDeleteCount", StatDirDeleteCount);
			cmd.AddField("DirDeleteOKCount", StatDirDeleteOKCount);
			cmd.AddField("DirDeleteErrorCount", StatDirDeleteErrorCount);
			cmd.AddField("ByteCopyCount", StatByteCopyCount);
			cmd.AddField("ByteCopyOKCount", StatByteCopyOKCount);
			cmd.AddField("ErrorCount", StatErrorCount);

			if (insert)
				cmd.ExecuteInsert(DatabaseCommandOptions.LogError | DatabaseCommandOptions.ThrowException);
			else
				cmd.ExecuteUpdate(DatabaseCommandOptions.LogError | DatabaseCommandOptions.ThrowException, String.Format("TaskID = {0}", TaskID));

			lastDBWrite = DateTime.UtcNow;
		}

		public bool ReadFromDB(int taskID)
		{
			DataTable table;
			DatabaseCommand cmd = new DatabaseCommand(db);

			cmd.AppendFormat("Select * from TaskBackupSync where taskid = {0}", taskID);
			table = cmd.ExecuteSelectToTable(DatabaseCommandOptions.ThrowException | DatabaseCommandOptions.LogError);
			if (table == null || table.Rows.Count == 0)
				return false;

			TaskID = Convert.ToInt32(table.Rows[0]["taskID"]);
			StartJobID = DatabaseFieldConvert.ToInt32(table.Rows[0]["StartJobID"]);
			Type = (JobType)DatabaseFieldConvert.ToInt32(table.Rows[0]["JobType"]);
			StatTaskStart = DatabaseFieldConvert.ToDateTime(table.Rows[0]["StartTS"]);
			StatTaskEnd = DatabaseFieldConvert.ToDateTime(table.Rows[0]["EndTS"]);
			AgentMachineName = table.Rows[0]["AgentMachineName"] as string;
			SourcePath = table.Rows[0]["SourcePath"] as string;
			DestinationPath = table.Rows[0]["DestinationPath"] as string;
			StatFileCount = Convert.ToInt64(table.Rows[0]["FileCount"]);
			StatFileCreateCount = Convert.ToInt64(table.Rows[0]["FileCreateCount"]);
			StatFileCopyCount = Convert.ToInt64(table.Rows[0]["FileCopyCount"]);
			StatFileCopyOKCount = Convert.ToInt64(table.Rows[0]["FileCopyOKCount"]);
			StatFileCopyErrorCount = Convert.ToInt64(table.Rows[0]["FileCopyErrorCount"]);
			StatFileDeleteCount = Convert.ToInt64(table.Rows[0]["FileDeleteCount"]);
			StatFileDeleteOKCount = Convert.ToInt64(table.Rows[0]["FileDeleteOKCount"]);
			StatFileDeleteErrorCount = Convert.ToInt64(table.Rows[0]["FileDeleteErrorCount"]);
			StatDirCount = Convert.ToInt64(table.Rows[0]["DirCount"]);
			StatDirCreateCount = Convert.ToInt64(table.Rows[0]["DirCreateCount"]);
			StatDirCreateOKCount = Convert.ToInt64(table.Rows[0]["DirCreateOKCount"]);
			StatDirCreateErrorCount = Convert.ToInt64(table.Rows[0]["DirCreateErrorCount"]);
			StatDirDeleteCount = Convert.ToInt64(table.Rows[0]["DirDeleteCount"]);
			StatDirDeleteOKCount = Convert.ToInt64(table.Rows[0]["DirDeleteOKCount"]);
			StatDirDeleteErrorCount = Convert.ToInt64(table.Rows[0]["DirDeleteErrorCount"]);
			StatByteCopyCount = Convert.ToInt64(table.Rows[0]["ByteCopyCount"]);
			StatByteCopyOKCount = Convert.ToInt64(table.Rows[0]["ByteCopyOKCount"]);
			StatErrorCount = Convert.ToInt64(table.Rows[0]["ErrorCount"]);

			return true;
		}
	}
}
