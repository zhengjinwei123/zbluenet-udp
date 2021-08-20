using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Game.Net
{
	public class UClient
	{
		private USocket uSocket;
		private IPEndPoint endPoint;
		private int sendSN;
		private int handleSN;
		private int session;
		private Action<BufferEntity> dispatchNetEvent;

		public bool isConnect = true;// 是否处于连接的状态
		int overtime = 150;

		ConcurrentDictionary<int, BufferEntity> sendPackage = new ConcurrentDictionary<int, BufferEntity>();
		ConcurrentDictionary<int, BufferEntity> waitHandle = new ConcurrentDictionary<int, BufferEntity>();

		public UClient(USocket uSocket, IPEndPoint endPoint, int sendSN, int handleSN, int session, Action<BufferEntity> dispatchNetEvent)
		{
			this.uSocket = uSocket;
			this.endPoint = endPoint;
			this.sendSN = sendSN;
			this.handleSN = handleSN;
			this.session = session;
			this.dispatchNetEvent = dispatchNetEvent;

			CheckOutTime();
		}

		private async void CheckOutTime()
		{
			await Task.Delay(overtime);
			foreach (var package in sendPackage.Values) {
				if (package.recurCount >= 10) {
					Debug.LogError($"重发10次还是失败， 协议id: {package.messageID}");
					uSocket.RemoveClient(session);
					return;
				}
				if (TimerHelper.Now() - package.time >= (package.recurCount + 1) * overtime) {
					// 重发次数+1
					package.recurCount += 1;
					Debug.Log($"超时重发,序号是:{package.sn}");
					uSocket.Send(package.buffer, endPoint);
				}
			}
			CheckOutTime();
		}

		public void Handle(BufferEntity buffer)
		{
			switch (buffer.messageType) {
				case 0:
					BufferEntity buff;
					if (sendPackage.TryRemove(buffer.sn, out buff))
					{
						Debug.Log($"报文已确认,序号： {buffer.sn}");
					}
					else {
						Debug.Log($"要确认的报文不存在,序号:{buffer.sn}");
					}
					break;
				case 1:
					BufferEntity ackPackage = new BufferEntity(buffer);
					uSocket.SendACK(ackPackage, endPoint);
					Debug.Log("收到是业务报文");
					HandleLogicPackage(buffer);
					break;
			}
		}

		public void Send(BufferEntity package)
		{
			if (isConnect == false) {
				return;
			}
			package.time = TimerHelper.Now();
			sendSN += 1;
			package.sn = sendSN;

			// 序列化
			package.Encoder(false);
			uSocket.Send(package.buffer, endPoint);
			if (session != 0) {
				sendPackage.TryAdd(package.sn, package);
			}

		}

		// 处理业务逻辑
		private void HandleLogicPackage(BufferEntity buffer)
		{
			if (buffer.sn <= handleSN) {
				Debug.Log($"已经处理过的消息,序号: {buffer.sn}");
				return;
			}
			if (buffer.sn - handleSN > 1) {
				if (waitHandle.TryAdd(buffer.sn, buffer)) {
					Debug.Log($"错序的报文,进行缓存，序号是: {buffer.sn}");
				}
				return;
			}

			handleSN = buffer.sn;
			if (dispatchNetEvent != null) {
				Debug.Log("分发消息给游戏模块");
				dispatchNetEvent(buffer);
			}

			BufferEntity nextBuffer;
			if (waitHandle.TryRemove(handleSN + 1, out nextBuffer)) {
				HandleLogicPackage(nextBuffer);
			}
		}

		public void Close()
		{
			isConnect = false;
		}
	}
}
