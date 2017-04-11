using System;
using System.Net.Sockets;
using System.Threading;

namespace SGCFGXFRService
{
	/// <summary>
	/// Summary description for SGCFGXFRClientSocketListener.
	/// </summary>
	public class SGCFGXFRClientSocketListener
	{
		private Socket m_clientSocket = null;
		public SGCFGXFRClientSocketListener(Socket clientSocket)
		{
			m_clientSocket = clientSocket;
		}

		public void StartClientListener()
		{
			Thread clientListenerThread = 
				new Thread(new ThreadStart(ClientListenerThreadStart));
		}

		private void ClientListenerThreadStart()
		{
			while (m_clientSocket.Connected)
			{

			}
		}

		public void StopClientListener()
		{
			if (m_clientSocket)
			{
				m_clientSocket.Close();
			}
		}
	}
}
