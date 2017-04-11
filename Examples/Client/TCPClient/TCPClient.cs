using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;

namespace TCPClient
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class TCPClient
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			TCPClient client = null;
			client = new TCPClient("SatyaTest.cfg\r\n");
			client = new TCPClient("SatyaTest1.cfg\r\n");
			client = new TCPClient("SatyaTest2.cfg\r\n");
			client = new TCPClient("SatyaTest3.cfg\r\n");
			client = new TCPClient("SatyaTest4.cfg\r\n");
			client = new TCPClient("SatyaTest5.cfg\r\n");
		}

		private String m_fileName=null;
		public TCPClient(String fileName)
		{
			m_fileName=fileName;
			Thread t = new Thread(new ThreadStart(ClientThreadStart));
			t.Start();
		}

		private void ClientThreadStart()
		{
			Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			clientSocket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"),31001));

			// Send the file name.
			clientSocket.Send(Encoding.ASCII.GetBytes(m_fileName));
			
			// Receive the length of the filename.
			byte [] data = new byte[128];
			clientSocket.Receive(data);
			int length=BitConverter.ToInt32(data,0);

			clientSocket.Send(Encoding.ASCII.GetBytes(m_fileName+":"+"this is a test\r\n"));

			clientSocket.Send(Encoding.ASCII.GetBytes(m_fileName+":"+"THIS IS "));
			clientSocket.Send(Encoding.ASCII.GetBytes("ANOTHRER "));
			clientSocket.Send(Encoding.ASCII.GetBytes("TEST."));
			clientSocket.Send(Encoding.ASCII.GetBytes("\r\n"));
			clientSocket.Send(Encoding.ASCII.GetBytes(m_fileName+":"+"TEST.\r\n"+m_fileName+":"+"TEST AGAIN.\r\n"));
			clientSocket.Send(Encoding.ASCII.GetBytes("[EOF]\r\n"));

			// Get the total length
			clientSocket.Receive(data);
			length=BitConverter.ToInt32(data,0);
			clientSocket.Close();
		}

	}
}
