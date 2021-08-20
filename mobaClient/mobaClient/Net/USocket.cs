using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Game.Net
{
	public class USocket
	{
		UdpClient udpClient;
		string ip = "10.235.200.243"; // 服务器主机
		int port = 8899;

		public static UClient local;// 客户端代理， 完成发送的逻辑和处理逻辑， 保证报文的顺序

		public static IPEndPoint server;
		ConcurrentQueue<UdpReceiveResult> awaitHandle = new ConcurrentQueue<UdpReceiveResult>();


		public USocket(Action<BufferEntity> dispatchNetEvent)
		{
			udpClient = new UdpClient(0);
			server = new IPEndPoint(IPAddress.Parse(ip), port);
			local = new UClient(this, server, 0, 0, 0, dispatchNetEvent);
			ReceiveTask(); // 启动接收消息的异步任务
		}

		public async void ReceiveTask()
		{
			while (udpClient != null) {
				try
				{
					UdpReceiveResult result = await udpClient.ReceiveAsync();
					awaitHandle.Enqueue(result);
					Debug.Log("接收到了消息");
				}
				catch (Exception e) {
					Debug.LogError(e.Message);
				}
			}
		}

		public void SendACK(BufferEntity bufferEntity)
		{
			Send(bufferEntity.buffer, server);
		}

		public async void Send(byte[] data, IPEndPoint endPoint)
		{
			if (udpClient != null) {
				try
				{
					int len = await udpClient.SendAsync(data, data.Length, ip, port);
				}
				catch (Exception e) {
					Debug.LogError($"发送异常:{e.Message}");
				}
			}
		}

		// 外部调用
		public void Handle()
		{
			if (awaitHandle.Count > 0) {
				UdpReceiveResult data;
				if (awaitHandle.TryDequeue(out data)) {
					// 反序列化
					BufferEntity bufferEntity = new BufferEntity(data.RemoteEndPoint, data.Buffer);
					if (bufferEntity.isFull) {
						Debug.Log($"处理消息:id:{bufferEntity.messageID}, 序号: {bufferEntity.sn}");
						local.Handle(bufferEntity);
					}
				}
			}

		}

		public void Close()
		{
			if (local != null) {
				local = null;
			}

			if (udpClient != null) {
				udpClient.Close();
				udpClient = null;
			}
		}
	}
}
