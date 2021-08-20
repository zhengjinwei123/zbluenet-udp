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
		Task task;
		private bool quit = false;
		public App()
		{
			uSocket = new USocket(dispatchNetEvent);
			task = Task.Factory.StartNew(Update);
		}

		public void Wait()
		{
			task.Wait();
		}

		public void Quit() {
			quit = true;
		}

		private void Update()
		{
			if (uSocket != null) {
				while (quit == false) {
					uSocket.Handle();
				}
			}
		}

		private void dispatchNetEvent(BufferEntity obj)
		{

		}
	}
}
