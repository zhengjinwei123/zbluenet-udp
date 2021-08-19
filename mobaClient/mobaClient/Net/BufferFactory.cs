using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Net
{
	public class BufferFactory
	{
		enum MessageType {
			ACK = 0, // 确认报文
			Logic = 1, // 业务逻辑报文
		}

		public static BufferEntity CreateAndSendPackage(int messageID, IMessage message) {
			Debug.Log(messageID, message);

			BufferEntity buffer = new BufferEntity(USocket.local.endPoint, USocket.local.sessionID, 0, 0, MessageType.Logic.GetHashCode(),
				messageID, ProtobufHelper.ToBytes(message));
			USocket.local.Send(buffer);
			return buffer;
		}
	}
}
