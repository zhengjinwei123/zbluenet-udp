﻿using System;
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
		public IPEndPoint endPoint;
		USocket uSocket;// 内部封装了发送的接口
		public int sessionID; // 会话ID
		public int sendSN = 0;// 发送序号
		public int handleSN = 0; // 处理的序号 为了保证报文的顺序

		private int overtime = 150;// 超时时间

		Action<BufferEntity> handleAction; // 处理报文的函数， 实际就是把分发报文给各个游戏模块

		// 缓存等待处理的报文
		ConcurrentDictionary<int, BufferEntity> waitHandle = new ConcurrentDictionary<int, BufferEntity>();
		// 缓存已经发送的报文
		ConcurrentDictionary<int, BufferEntity> sendPackage = new ConcurrentDictionary<int, BufferEntity>();

		public UClient(USocket uSocket, IPEndPoint endPoint, int sendSN, int handleSN, int sessionID, Action<BufferEntity> dispatchNetEvent)
		{
			this.uSocket = uSocket;
			this.endPoint = endPoint;
			this.sendSN = sendSN;
			this.handleSN = handleSN;
			this.sessionID = sessionID;
			this.handleAction = dispatchNetEvent;

			CheckOutTime(); // 超时检测
		}

		// 处理消息： 按照报文的序号进行顺序处理，如果是收到超过当前顺序+1 的报文， 先进行缓存
		public void Handle(BufferEntity buffer)
		{
			if (this.sessionID == 0 && buffer.session != 0) {
				this.sessionID = buffer.session;
				Debug.Log($"服务器发给我们的会话ID是:{buffer.session}");
			}

			switch (buffer.messageType) {
				case 0:// ack 确认报文
					BufferEntity bufferEntity;
					if (sendPackage.TryRemove(buffer.sn, out bufferEntity)) {
						Debug.Log($"收到ACK 确认报文，序号是:{buffer.sn}");
					}
					break;
				case 1: // 业务报文
					BufferEntity ackPacka = new BufferEntity(buffer);
					uSocket.SendACK(ackPacka); // 先告诉服务器 我已经收到这个报文

					// 再来处理业务报文
					HandleLogicPackage(buffer);
					break;
				default:
					break;
			}
		}

		private void HandleLogicPackage(BufferEntity buffer)
		{
			if (buffer.sn <= handleSN) {
				return;
			}

			// 已经收到的报文是错序的
			if (buffer.sn - handleSN > 1) {
				if (waitHandle.TryAdd(buffer.sn, buffer)) {
					Debug.Log($"收到错序的报文:{buffer.sn}");
				}
				return;
			}

			// 更新已处理的报文
			handleSN = buffer.sn;
			if (handleAction != null) {
				// 分发给游戏模块处理
				handleAction(buffer);
			}

			// 检测缓存的数据， 有没有包含下一条可以处理的数据
			BufferEntity nextBuffer;
			if (waitHandle.TryRemove(handleSN + 1, out nextBuffer)) {
				HandleLogicPackage(nextBuffer);
			}
		}

		// 发送的接口
		public void Send(BufferEntity package)
		{
			package.time = TimerHelper.Now();
			sendSN += 1;

			package.sn = sendSN;

			package.Encoder(false);
			if (sessionID != 0)
			{
				// 缓存起来 因为可能需要重发
				sendPackage.TryAdd(sendSN, package);
			}
			else {
				// 还没有和服务器建立连接， 所以不需要缓存
			}

			uSocket.Send(package.buffer, endPoint);
		}

		public async void CheckOutTime()
		{
			await Task.Delay(overtime);
			foreach (var package in sendPackage.Values) {
				// 确定是不是超过最大发送次数， 关闭socket
				if (package.recurCount >= 10) {
					Debug.Log($"重发次数超过10次， 关闭socket");
					OnDisconnect();
					return;
				}

				if (TimerHelper.Now() - package.time >= (package.recurCount + 1) * overtime) {
					package.recurCount += 1;
					Debug.Log($"超时重发，次数: {package.recurCount}");
					uSocket.Send(package.buffer, endPoint);
				}
			}
			CheckOutTime();
		}

		private void OnDisconnect()
		{
			handleAction = null;
			uSocket.Close();
		}
	}
}
