using System;
using System.Collections.Generic;
using System.Text;

namespace ocNet_Backup.core
{
	public enum BUComEventID : uint
	{
		//***********************************************************************************************
		//* Content directory
		//***********************************************************************************************

		Unknown = 0,

		//***********************************************************************************************
		// After the client has established the connection to the server and the auth was successfully
		// done, the client sends this event to deliver information about the client
		// Direction: Client->Server
		// Format	: 
		//				U4 - Version (1)
		//				U4 - ClientVersion
		//				s  - ClientAssemblyVersionNumber
		//				s  - Hostname
		//				U4 - Connect reason (0 = agent; 1 = ui)
		//				U4 - serial No (0 = unlicensed / invalid) // old always 0
		ClientInfo = 0x00001000,


		//***********************************************************************************************
		// Tells the server that the client is still alive.
		// This packet is only send if a connection is active and no packet has been send 
		// since the last two minutes.
		// The alive packet is also neccessary because of the timeout of the different NAT-Systems
		// Direction: Client->Server
		// Format	: 
		//				Empty
		Alive,

		//***********************************************************************************************
		// Tells the server that the client now quits the transmission.
		// Direction: Client->Server
		// Format	: 
		//				Empty
		QuitTransmission,

		//***********************************************************************************************
		// Tells the client to send a number file to the server
		// Direction: Server->Client
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID
		//				U4 - Count of files => a
		//			a*(	U4 - FileTicketID
		//				s  - FullPath)
		//				
		GetFiles,

		//***********************************************************************************************
		// Tells the client to create a new FileInfo info for a specific directory
		// Direction: Server->Client
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID
		//				s  - FullPath
		FileInfoCreate,

		//***********************************************************************************************
		// Requests a FileInfo
		// Direction: Server->Client
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID
		FileInfoGet,

		//***********************************************************************************************
		// Returns the a FileInfo
		// Direction: Client->Server
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID
		//				D  - TS of FileInfo
		//				s  - FileInfoPath on Agent
		FileInfoGetReturnOK,

		//***********************************************************************************************
		// Returns error information on the a FileInfo
		// Direction: Client->Server
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID
		//				U4 - ErrorCode (1 = Not Found, 2 = Running)
		FileInfoGetReturnError,

		//***********************************************************************************************
		// Requests the status of a FileTransferCollection
		// Direction: Server->Client
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID	
		FileTransferCollectionGetInfo,

		//***********************************************************************************************
		// Returns the status of a FileTransferCollection
		// Direction: Client->Server
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID
		//				I4 - Files to send (-1 if collection ticket was not found)	
		//				I4 - Files in error list (-1 if collection ticket was not found)
		FileTransferCollectionGetInfoReturn,

		//***********************************************************************************************
		// Requests the errors of a FileTransferCollection
		// Direction: Server->Client
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID
		FileTransferCollectionGetErrors,

		//***********************************************************************************************
		// Returns the error tickets of a FileTransferCollection
		// Direction: Client->Server
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID	
		//				U4 - Count of files
		//			a*(	U4 - FileTicketID
		//				s  - Error string)
		FileTransferCollectionGetErrorsReturn,

		//***********************************************************************************************
		// Requests the delete of a FileTransferCollection
		// Direction: Server->Client
		// Format	: 
		//				U4 - Version (1)
		//				U4 - TaskID	
		FileTransferCollectionDelete,

		//***********************************************************************************************
		// Sets the Agent file options
		// Direction: Server->Client
		// Format	: 
		//				U4 - Version (1)
		//				U4 - FileTRXTimeout
		//				U4 - FileTRXBufferSize
		//				U1 - FileCrypt
		//				U1 - FileCompress
		//				I4 - Bandwith
		SetAgentFileOptions,

	};
}
