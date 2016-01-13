using System;
using System.Collections.Generic;
using System.Text;
using ocNet.Lib;

namespace ocNet_Backup.core
{
	public sealed class BUComEvent : TCPComEPPEvent
	{
		public BUComEventID EventID
		{
			get
			{
				BUComEventID eventID;

				try
				{
					eventID = (BUComEventID)Enum.ToObject(typeof(BUComEventID), eventNum);
				}
				catch
				{
					eventID = BUComEventID.Unknown;
				}

				return eventID;
			}
		}

		/// <summary>
		/// Constructor for use on get of event from EPP
		/// </summary>
		/// <param name="eventID"></param>
		/// <param name="mf"></param>
		public BUComEvent(BUComEventID eventID, MemFile mf)
			: base((uint)eventID, mf)
		{
		}

		/// <summary>
		/// Constructor for use on get of event from EPP
		/// </summary>
		/// <param name="eventID"></param>
		/// <param name="mf"></param>
		public BUComEvent(uint eventNum, MemFile mf)
			: base(eventNum, mf)
		{
		}

		/// <summary>
		/// Constructor for use on build packet for standard payload
		/// </summary>
		/// <param name="eventID"></param>
		/// <param name="args"></param>
		public BUComEvent(BUComEventID eventID, params object[] args)
			: base((uint)eventID, args)
		{
		}

		protected override string[] GetPayloadCode(uint eventNum)
		{
			BUComEventID eventID;

			try
			{
				eventID = (BUComEventID)Enum.ToObject(typeof(BUComEventID), eventNum);
			}
			catch
			{
				return null;
			}

			switch (eventID)
			{
				case BUComEventID.ClientInfo:								return new string[] {"U4U4ssU4U4"};
				case BUComEventID.QuitTransmission:							return new string[] {""};
				case BUComEventID.FileInfoCreate:							return new string[] {"U4U4s"};
				case BUComEventID.FileInfoGet:								return new string[] {"U4U4"};
				case BUComEventID.FileInfoGetReturnOK:						return new string[] {"U4U4Ds"};
				case BUComEventID.FileInfoGetReturnError:					return new string[] {"U4U4U4"};

				case BUComEventID.FileTransferCollectionGetInfo:			return new string[] {"U4U4"};
				case BUComEventID.FileTransferCollectionGetInfoReturn:		return new string[] {"U4U4I8I8"};
				case BUComEventID.FileTransferCollectionGetErrors:			return new string[] {"U4U4"};
				case BUComEventID.FileTransferCollectionGetErrorsReturn:	return new string[] {"U4U4U4(U4s)"};
				case BUComEventID.FileTransferCollectionDelete:				return new string[] {"U4U4"};

				case BUComEventID.SetAgentFileOptions:						return new string[] { "U4U4U4U1U1I4" };
			}
			return null;
		}

		protected override bool DispatchSpecialPayload(uint eventNum)
		{
			if (eventNum == (uint)BUComEventID.GetFiles)
			{
				UInt32 cou;

				mf.GetUInt32(); // Version
				parameter = new List<object>();
				parameter.Add(mf.GetUInt32());
				cou = mf.GetUInt32();
				parameter.Add(cou);
				for (UInt32 i = 0; i < cou; i++)
				{
					parameter.Add(mf.GetUInt32());
					parameter.Add(mf.GetDynUTF8String());
				}
				return true;
			}

			return false;
		}
	}
}
