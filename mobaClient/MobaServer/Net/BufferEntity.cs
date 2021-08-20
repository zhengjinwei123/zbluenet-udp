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

		public int protoSize; // 4
		public int session; // 会话ID 4
		public int sn; // 序号 4
		public int moduleID; // 模块ID 4
		public long time; // 发送时间 8
		public int messageType; // 协议类型 4
		public int messageID;// 协议ID 4
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
		public BufferEntity(IPEndPoint endPoint, int session, int sn, int moduleID, int messageType, int messageID, byte[] proto)
		{
			protoSize = proto.Length;

			this.endPoint = endPoint;
			this.session = session;
			this.sn = sn;
			this.moduleID = moduleID;
			this.messageType = messageType;
			this.messageID = messageID;
			this.proto = proto;
		}

		/// <summary>
		/// 编码的接口 
		/// </summary>
		/// <param name="isAck">确认报文 业务报文</param>
		/// <returns></returns>
		public byte[] Encoder(bool isAck)
		{
			byte[] data = new byte[32 + protoSize];
			if (isAck)
			{
				protoSize = 0;// 发送的业务报文的大小
			}

			byte[] _length = BitConverter.GetBytes(protoSize);
			byte[] _session = BitConverter.GetBytes(session);
			byte[] _sn = BitConverter.GetBytes(sn);
			byte[] _moduleId = BitConverter.GetBytes(moduleID);
			byte[] _time = BitConverter.GetBytes(time);
			byte[] _messageType = BitConverter.GetBytes(messageType);
			byte[] _messageID = BitConverter.GetBytes(messageID);

			// 将字节数组写入 data
			Array.Copy(_length, 0, data, 0, 4);
			Array.Copy(_session, 0, data, 4, 4);
			Array.Copy(_sn, 0, data, 8, 4);
			Array.Copy(_moduleId, 0, data, 12, 4);
			Array.Copy(_time, 0, data, 16, 8);
			Array.Copy(_messageType, 0, data, 24, 4);
			Array.Copy(_messageID, 0, data, 28, 4);

			if (false == isAck)
			{
				// 如果是业务报文
				Array.Copy(proto, 0, data, 32, proto.Length);
			}

			buffer = data;
			return data;
		}

		public BufferEntity(IPEndPoint endPoint, byte[] buffer)
		{
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
			if (buffer.Length >= 4)
			{
				protoSize = BitConverter.ToInt32(buffer, 0);// 从0的位置读取4个字节
				if (buffer.Length == protoSize + 32)
				{
					isFull = true;
				}
			}
			else
			{
				isFull = false;
				return;
			}

			session = BitConverter.ToInt32(buffer, 4);
			sn = BitConverter.ToInt32(buffer, 8);
			moduleID = BitConverter.ToInt32(buffer, 12);
			time = BitConverter.ToInt64(buffer, 16);
			messageType = BitConverter.ToInt32(buffer, 24);
			messageID = BitConverter.ToInt32(buffer, 28);

			if (messageType != 0)
			{
				proto = new byte[protoSize];

				Array.Copy(buffer, 32, proto, 0, protoSize);
			}
		}

		/// <summary>
		/// 创建一个ack 报文实体
		/// </summary>
		/// <param name="package">收到的报文实体</param>
		public BufferEntity(BufferEntity package)
		{
			protoSize = 0;
			this.endPoint = package.endPoint;
			this.session = package.session;
			this.sn = package.sn;
			this.moduleID = package.moduleID;
			this.time = 0;
			this.messageID = package.messageID;
			this.messageType = 0;

			buffer = Encoder(true);
		}
	}
}
