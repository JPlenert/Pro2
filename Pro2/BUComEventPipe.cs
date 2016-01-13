using System;
using System.Collections.Generic;
using System.Text;

namespace ocNet_Backup.core
{
	public sealed class BUComEventPipe
	{
		List<BUComEvent> outList;
		List<BUComEvent> inList;

		public BUComEventPipe()
		{
			outList = new List<BUComEvent>();
			inList = new List<BUComEvent>();
		}

		BUComEvent GetEvent(List<BUComEvent> list)
		{
			BUComEvent evt;

			lock (list)
			{
				if (list.Count == 0)
					return null;

				evt = list[0];
				list.RemoveAt(0);
				return evt;
			}
		}

		BUComEvent GetEvent(List<BUComEvent> list, BUComEventID eventID, Int32 taskID)
		{
			BUComEvent evt;

			lock (list)
			{
				if (list.Count == 0)
					return null;

				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].EventID == eventID)
					{
						// if taskOrCollectionID is != 0, we check the 2nd parameter for taskID
						if (taskID == 0 || Convert.ToInt32(list[i].Parameter[1]) == taskID)
						{
							evt = list[i];
							list.RemoveAt(i);
							return evt;
						}
					}
				}
			}
			return null;
		}

		void SetEvent(List<BUComEvent> list, BUComEvent evt)
		{
			lock (list)
			{
				list.Add(evt);
			}
		}

		public void SetOutEvent(BUComEvent comEvent)
		{
			SetEvent(outList, comEvent);
		}

		public BUComEvent GetOutEvent()
		{
			return GetEvent(outList);
		}

		public BUComEvent GetOutEvent(BUComEventID eventID)
		{
			return GetEvent(outList, eventID, 0);
		}

		public void SetInEvent(BUComEvent comEvent)
		{
			SetEvent(inList, comEvent);
		}

		public BUComEvent GetInEvent()
		{
			return GetEvent(inList);
		}

		public BUComEvent GetInEvent(BUComEventID eventID, Int32 taskID)
		{
			return GetEvent(inList, eventID, taskID);
		}
	}
}
