using Game.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mobaClient
{
	public class App
	{
		USocket uSocket;
		public App()
		{
			uSocket = new USocket(dispatchNetEvent);

			var task = Task.Factory.StartNew(Update);

			task.Wait();
		}

		private void Update()
		{
			if (uSocket != null) {
				while (true) {
					uSocket.Handle();
				}
			}
		}

		private void dispatchNetEvent(BufferEntity obj)
		{

		}
	}
}
