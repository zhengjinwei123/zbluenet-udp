using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Net;

namespace MobaServer
{
	class Program
	{
		public static USocket uSocket;
		static void Main(string[] args)
		{
			Console.WriteLine("启动服务器....");
			NetSystemInit();
			while (true) {
				Console.ReadLine();
			}

			
		}


		static void NetSystemInit()
		{
			uSocket = new USocket(DispatchNetEvent);
		}

		private static void DispatchNetEvent(BufferEntity obj)
		{
			// 进行报文分发
		}
	}
}
