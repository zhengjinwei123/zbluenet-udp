using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Net;
using ProtoMsg;

namespace mobaClient
{
	class Program
	{

		static void Main(string[] args)
		{
			var client = new App();

			while (true) {
				var k = Console.ReadLine();
				if ("quit" == k) {
					client.Quit();
					break;
				}

				if ("s" == k) {
					TestSend();
				}
					
				
			}

			client.Wait();
		}

		private static void TestSend()
		{
			UserInfo userInfo = new UserInfo();
			userInfo.Account = "zjw123";
			userInfo.Password = "123456";

			UserRegisterC2S userRegisterC2S = new UserRegisterC2S();
			userRegisterC2S.UserInfo = userInfo;
			BufferEntity buffer = BufferFactory.CreateAndSendPackage(1001, userInfo);
		}

	
	}
}
