using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Net
{
	public class USocket
	{
		UdpClient socket;
		string ip = "10.235.200.243";
		int port = 8899;
		int sessionID = 1000;

		Action<BufferEntity> dispatchNetEvent;
		CancellationTokenSource ct = new CancellationTokenSource();

		ConcurrentQueue<UdpReceiveResult> awaitHandle = new ConcurrentQueue<UdpReceiveResult>();
		ConcurrentDictionary<int, UClient> clients = new ConcurrentDictionary<int, UClient>();

		public USocket(Action<BufferEntity> dispatchNetEvent)
		{
			this.dispatchNetEvent = dispatchNetEvent;
			socket = new UdpClient(port);
			Receive();

			Task.Run(Handle, ct.Token);
		}

		public async void Send(byte[] data, IPEndPoint endPoint)
		{
			if (socket != null) {
				try
				{
					int len = await socket.SendAsync(data, data.Length, endPoint);
					if (data.Length == len) {

					}
				}
				catch (Exception e) {
					Debug.LogError($"发送异常: {e.Message}");
					Close();
				}
			}
		}

		public void SendACK(BufferEntity ackPackage, IPEndPoint endPoint)
		{
			Debug.Log("回复客户端收到消息了");
			Send(ackPackage.buffer, endPoint);
		}

		// 接收消息的接口
		public async void Receive()
		{
			if (socket != null) {
				try
				{
					UdpReceiveResult result = await socket.ReceiveAsync();
					Debug.Log("接收到客户端的消息了");
					awaitHandle.Enqueue(result);
					Receive();
				}
				catch (Exception e) {
					Debug.LogError($"接收异常: {e.Message}");
					Close();
				}
			}
		}

		

		public void Close()
		{
			// 取消任务的信号
			ct.Cancel();

			foreach (var client in clients.Values) {
				client.Close();
			}
			clients.Clear();

			if (socket != null) {
				socket.Close();
				socket = null;
			}

			if (dispatchNetEvent != null) {
				dispatchNetEvent = null;
			}
		}

		async Task Handle()
		{
			while (!ct.IsCancellationRequested) {
				if (awaitHandle.Count > 0) {
					UdpReceiveResult data;

					if (awaitHandle.TryDequeue(out data)) {
						BufferEntity bufferEntity = new BufferEntity(data.RemoteEndPoint, data.Buffer);
						if (bufferEntity.isFull) {
							if (bufferEntity.session == 0) {
								sessionID += 1;
								bufferEntity.session = sessionID;
								CreateUClient(bufferEntity);
								Debug.Log($"创建客户端，会话ID: {sessionID}");
							}

							UClient targetClient;
							if (clients.TryGetValue(bufferEntity.session, out targetClient)) {
								targetClient.Handle(bufferEntity);
							}
						}
					}
				}
			}
		}

		private void CreateUClient(BufferEntity bufferEntity)
		{
			UClient client;
			if (!clients.TryGetValue(bufferEntity.session, out client)) {
				client = new UClient(this, bufferEntity.endPoint, 0, 0, bufferEntity.session, dispatchNetEvent);
				clients.TryAdd(bufferEntity.session, client);
			}
		}

		public void RemoveClient(int sessionId)
		{
			UClient client;
			if (clients.TryRemove(sessionId, out client)) {
				client.Close();
				client = null;
			}
		}

		public UClient GetClient(int sessionID) {
			UClient client;
			if (clients.TryGetValue(sessionID, out client)) {
				return client;
			}
			return null;
		}
	}
}
