using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

using System.Threading;
using System.Text;
using System.IO;
using System.Windows.Forms;
using UI_Server_GatewaySMS.Components;
using ServiceStack.Text;


namespace UI_Server_GatewaySMS
{
	/// <summary>
	/// Summary description for TCPSocketListener.
	/// </summary>
	public class TCPSocketListener
	{
		public static String DEFAULT_FILE_STORE_LOC="C:\\TCP\\";
		
		public enum STATE{FILE_NAME_READ, DATA_READ, FILE_CLOSED};

		/// <summary>
		/// Variables that are accessed by other classes indirectly.
		/// </summary>
		private Socket m_clientSocket = null;
		private bool m_stopClient=false;
		private Thread m_clientListenerThread=null;
		private bool m_markedForDeletion=false;

		/// <summary>
		/// Working Variables.
		/// </summary>
		private StringBuilder m_oneLineBuf=new StringBuilder();
		private DateTime m_lastReceiveDateTime;
		private DateTime m_currentReceiveDateTime;
		
		//Dictionary<string, string> httpHeaders = new Dictionary<string, string>();
		
		
		/// <summary>
		/// Client Socket Listener Constructor.
		/// </summary>
		/// <param name="clientSocket"></param>
		public TCPSocketListener(Socket clientSocket)
		{
			m_clientSocket = clientSocket;
		}

		/// <summary>
		/// Client SocketListener Destructor.
		/// </summary>
		~TCPSocketListener()
		{
			StopSocketListener();
		}

		/// <summary>
		/// Method that starts SocketListener Thread.
		/// </summary>
		public void StartSocketListener()
		{
			if (m_clientSocket!= null)
			{
				m_clientListenerThread =
					new Thread(new ThreadStart(SocketListenerThreadStart));

				m_clientListenerThread.Start();
			}
		}

		/// <summary>
		/// Thread method that does the communication to the client. This
		/// thread tries to receive from client and if client sends any data
		/// then parses it and again wait for the client data to come in a
		/// loop. The recieve is an indefinite time receive.
		/// </summary>
		private void SocketListenerThreadStart()
		{
			int size=0;
			Byte [] byteBuffer = new Byte[3000];

			m_lastReceiveDateTime = DateTime.Now;
			m_currentReceiveDateTime = DateTime.Now;

			System.Threading.Timer t= new System.Threading.Timer(new TimerCallback(CheckClientCommInterval),
			                                                     null,15000,15000);

			while (!m_stopClient)
			{
				try
				{
					size = m_clientSocket.Receive(byteBuffer);
					m_currentReceiveDateTime=DateTime.Now;
					
					
					
					/*Cierro el Socket. Sino hago esto, el receptor del HTTP200 se queda esperando el ACK-RST
 					que recien se ejecuta cuando se ejecuta la funcion CheckClientCommInterval*/
					//m_clientSocket.Close();
					
					
					/*Convierto el paquete recibido a un objeto mensaje*/
					var mensaje =ParseReceiveBuffer(byteBuffer, size);
					if(mensaje!=null){
						Message message = new Message(mensaje.numero,mensaje.mensaje);
						TCPServer.queue.Enqueue(message);
					}
					
					/*Envio HTTP OK*/
					m_clientSocket.Send(TCPServer.HTTPresponse,TCPServer.HTTPresponse.Length,SocketFlags.None);
					this.StopSocketListener();

					
				}
				catch (SocketException e)
				{
					m_stopClient=true;
					m_markedForDeletion=true;
					TCPServer.logger.logData("EXEPCION: "+e);
				}
			}
			t.Change(Timeout.Infinite, Timeout.Infinite);
			t=null;
		}

		/// <summary>
		/// Method that stops Client SocketListening Thread.
		/// </summary>
		public void StopSocketListener()
		{
			if (m_clientSocket!= null)
			{
				m_stopClient=true;
				
				
				m_clientSocket.Close();

				// Wait for one second for the the thread to stop.
				m_clientListenerThread.Join(1000);
				
				// If still alive; Get rid of the thread.
				if (m_clientListenerThread.IsAlive)
				{
					m_clientListenerThread.Abort();
				}
				m_clientListenerThread=null;
				m_clientSocket=null;
				m_markedForDeletion=true;
			}
		}

		/// <summary>
		/// Method that returns the state of this object i.e. whether this
		/// object is marked for deletion or not.
		/// </summary>
		/// <returns></returns>
		public bool IsMarkedForDeletion()
		{
			return m_markedForDeletion;
		}

		/// <summary>
		/// Este metodo recibe el paquete HTTP y des serializa el JSON enviado en data
		/// en un nuevo objeto mensaje
		/// </summary>
		/// <param name="byteBuffer: Buffer de Bytes recibido por el socket"></param>
		/// <param name="size: tamaño del buffer"></param>
		public MensajeJson ParseReceiveBuffer(Byte [] byteBuffer, int size)
		{
			
			string data = Encoding.UTF8.GetString(byteBuffer,0, size);
			
			/*TO SHOW DATA RECEIVED*/
			//MessageBox.Show(data);
			
			/*Voy hasta el inicio del JSON*/
			//Si encontre el "{"
			if(data.IndexOf("{") != -1){
				string aux = data.Remove(0,data.IndexOf("{"));
				try{
					var mensaje=JsonSerializer.DeserializeFromString<MensajeJson>(aux);
					if(mensaje.numero!="" && mensaje.mensaje!=""){

						return mensaje;
					}
					else
					{
						TCPServer.logger.logData("Mensaje o Numero Vacio : Numero:"+mensaje.numero+" Mensaje:"+mensaje.mensaje);
						return null;

					}
				}
				catch(Exception e)
				{
					TCPServer.logger.logData("EXEPCION: "+e);
					return null;
				}
				
				
			}
			else
			{
				return null;
			}
			
		}
		
		
		
		
		
		
		
		/*
		 * OLD CODE
			// Check whether data from client has more than one line of
			// information, where each line of information ends with "CRLF"
			// ("\r\n"). If so break data into different lines and process
			// separately.
			int lineEndIndex=0;
			do
			{
				lineEndIndex =	data.IndexOf("\r\n");
				if(lineEndIndex != -1)
				{
					m_oneLineBuf=m_oneLineBuf.Append(data,0,lineEndIndex+2);
					
					//MessageBox.Show(m_oneLineBuf.ToString());
					
					readHeaders(m_oneLineBuf.ToString());
					m_oneLineBuf.Remove(0,m_oneLineBuf.Length);
					data = data.Substring(lineEndIndex+2,
					                      data.Length -lineEndIndex-2);
					

				}
				else
				{
					// Just append to the existing buffer.
					m_oneLineBuf=m_oneLineBuf.Append(data);
				}
			}while(lineEndIndex != -1);
			
		 */
		
		
		

		/*
		 * OLD CODE
		/// <summary>
		/// Busca los parametros numero y mensaje en el POST y los guarda en httpHeaders
		/// </summary>
		/// <param name="leido: string recibido (1 linea)"></param>
		public void readHeaders(string leido) {
			

			if (leido.Equals("")) {
				return;
			}
			
			int separator = leido.IndexOf(':');
			if (separator != -1) {
				
				
				String name = leido.Substring(0, separator);
				int pos = separator + 1;
				while ((pos < leido.Length) && (leido[pos] == ' ')) {
					pos++; // strip any spaces
				}
				
				string value = leido.Substring(pos, leido.Length - pos);
				Console.WriteLine("header: {0}:{1}",name,value);
				httpHeaders[name] = value;
			}
			
		}
		

		 */
		
		
		/// <summary>
		/// Method that checks whether there are any client calls for the
		/// last 15 seconds or not. If not this client SocketListener will
		/// be closed.
		/// </summary>
		/// <param name="o"></param>
		private void CheckClientCommInterval(object o)
		{
			if (m_lastReceiveDateTime.Equals(m_currentReceiveDateTime))
			{
				this.StopSocketListener();
			}
			else
			{
				m_lastReceiveDateTime = m_currentReceiveDateTime;
			}
		}
		

	}

}
