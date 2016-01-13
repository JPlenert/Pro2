using System;
using System.Collections.Generic;
using System.Text;
using ocNet.Lib;

namespace ocNet_Backup.core
{
	public sealed class TCPComEPPBU : TCPComEPP
	{
		public TCPComEPPBU(TCPComEP ep, string cryptPassword, Guid interfaceGUID, bool wantAck)
			: base(ep, cryptPassword, interfaceGUID, wantAck)
		{
		}

		public void SendPacket(BUComEvent buEvent)
		{
			base.SendPacket((uint)buEvent.EventID, buEvent.MFile);
		}

		public BUComEvent GetPacket()
		{
			uint eventNum;
			MemFile mf;

			if (!base.GetPacket(out eventNum, out mf))
				return null;
			return new BUComEvent(eventNum, mf);
		}
	}
}
