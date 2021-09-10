using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Game.Net
{
	public class BufferEntity
	{
		public int recurCount = 0;// 重发次数， 工程内部使用， 并非业务数据

		public IPEndPoint endPoint; // 发送的目标终端

		public UInt16 protoSize; // 2
		public int session; // 会话ID 4
		public int sn; // 序号 4
		//public int moduleID; // 模块ID 4
		public long time; // 发送时间 8
		public UInt16 messageType; // 协议类型 2
		public UInt16 messageID;// 协议ID 2
		public byte[] proto; // 业务报文

		public byte[] buffer; // 最终要发送的数据 或者是 收到的数据

		// 请求报文
		/// <summary>
		/// 构建请求报文
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="session"></param>
		/// <param name="sn"></param>
		/// <param name="moduleID"></param>
		/// <param name="messageType"></param>
		/// <param name="messageID"></param>
		/// <param name="proto"></param>
		public BufferEntity(IPEndPoint endPoint, int session, int sn, int moduleID, int messageType, int messageID, byte[] proto) {
			protoSize = (UInt16)proto.Length;

			this.endPoint = endPoint;
			this.session = session;
			this.sn = sn;
			//this.moduleID = moduleID;
			this.messageType = (UInt16)messageType;
			this.messageID = (UInt16)messageID;
			this.proto = proto;
		}

		/// <summary>
		/// 编码的接口 
		/// </summary>
		/// <param name="isAck">确认报文 业务报文</param>
		/// <returns></returns>
		public byte[] Encoder(bool isAck) {
			byte[] data = new byte[22 + protoSize];
			if (isAck) {
				protoSize = 0;// 发送的业务报文的大小
			}

			Debug.Log($"Encoder protosize:{protoSize}");

			byte[] _length = BitConverter.GetBytes(protoSize);
			byte[] _session = BitConverter.GetBytes(session);
			byte[] _sn = BitConverter.GetBytes(sn);
			//byte[] _moduleId = BitConverter.GetBytes(moduleID);
			byte[] _time = BitConverter.GetBytes(time);
			byte[] _messageType = BitConverter.GetBytes(messageType);
			byte[] _messageID = BitConverter.GetBytes(messageID);

			// 将字节数组写入 data
			Array.Copy(_length, 0, data, 0, 2);
			Array.Copy(_session, 0, data, 2, 4);
			Array.Copy(_sn, 0, data, 6, 4);
			//Array.Copy(_moduleId, 0, data, 12, 4);
			Array.Copy(_time, 0, data, 10, 8);
			Array.Copy(_messageType, 0, data, 18, 2);
			Array.Copy(_messageID, 0, data, 20, 2);

			if (false == isAck) {
				// 如果是业务报文
				Array.Copy(proto, 0, data, 22, proto.Length);
			}

			buffer = data;
			return data;
		}

		public BufferEntity(IPEndPoint endPoint, byte[] buffer) {
			this.endPoint = endPoint;
			this.buffer = buffer;
			Decoder();
		}

		public bool isFull = false;

		/// <summary>
		/// 报文反序列化
		/// </summary>
		private void Decoder()
		{
			isFull = false;
			if (buffer.Length >= 2)
			{
				protoSize = BitConverter.ToUInt16(buffer, 0);// 从0的位置读取2个字节
				if (buffer.Length == protoSize + 22)
				{
					isFull = true;
				}
			}
			else {
				isFull = false;
				return;
			}

			session = BitConverter.ToInt32(buffer, 2);
			sn = BitConverter.ToInt32(buffer, 6);
			//moduleID = BitConverter.ToInt32(buffer, 12);
			time = BitConverter.ToInt64(buffer, 10);
			messageType = BitConverter.ToUInt16(buffer, 18);
			messageID = BitConverter.ToUInt16(buffer, 20);

			if (messageType != 0) {
				proto = new byte[protoSize];

				Array.Copy(buffer, 22, proto, 0, protoSize);
			}
		}

		/// <summary>
		/// 创建一个ack 报文实体
		/// </summary>
		/// <param name="package">收到的报文实体</param>
		public BufferEntity(BufferEntity package) {
			protoSize = 0;
			this.endPoint = package.endPoint;
			this.session = package.session;
			this.sn = package.sn;
			//this.moduleID = package.moduleID;
			this.time = 0;
			this.messageID = package.messageID;
			this.messageType = 0;

			buffer = Encoder(true);
		}


	}
}
