using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Ports;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Forms;

namespace UI_Server_GatewaySMS
{
	/// <summary>
	/// TCPServer is the Server class. When "StartServer" method is called
	/// this Server object tries to connect to a IP Address specified on a port
	/// configured. Then the server start listening for client socket requests.
	/// As soon as a requestcomes in from any client then a Client Socket
	/// Listening thread will be started. That thread is responsible for client
	/// communication.
	/// </summary>
	public class TCPServer
	{
		/// <summary>
		/// Default Constants.
		/// </summary>
		
		/*LocalHost*/
		//public static IPAddress DEFAULT_SERVER = IPAddress.Parse("127.0.0.1");
		
		/*Notebook Fede*/
		//public static IPAddress DEFAULT_SERVER = IPAddress.Parse("163.10.123.161");
		
		/*Notebook Monitoreo*/
		public static IPAddress DEFAULT_SERVER = IPAddress.Parse("163.10.123.181");
		public static int DEFAULT_PORT=31001;
		public static IPEndPoint DEFAULT_IP_END_POINT = new IPEndPoint(DEFAULT_SERVER, DEFAULT_PORT);
		public static BlockingQueue<Message> queue;
		public static SerialPort serialPort = new SerialPort();
		public static GSM_Module gsm_module = new GSM_Module(ref serialPort);
		public static byte[] HTTPresponse;
		
		private static readonly HttpClient client = new HttpClient();
		
		public static Logger logger = new Logger();
		
		/// <summary>
		/// Local Variables Declaration.
		/// </summary>
		private TcpListener m_server = null;
		private bool m_stopServer=false;
		private bool m_stopPurging=false;
		private bool m_stopProccesing = false;
		private Thread m_serverThread = null;
		private Thread m_purgingThread = null;
		private Thread m_processingThread = null;
		private ArrayList m_socketListenersList = null;
		/// <summary>
		/// Constructors.
		/// </summary>
		public TCPServer()
		{
			Init(DEFAULT_IP_END_POINT);
		}
		public TCPServer(IPAddress serverIP)
		{
			Init(new IPEndPoint(serverIP, DEFAULT_PORT));
		}

		public TCPServer(int port)
		{
			Init(new IPEndPoint(DEFAULT_SERVER, port));
		}

		public TCPServer(IPAddress serverIP, int port)
		{
			Init(new IPEndPoint(serverIP, port));
		}

		public TCPServer(IPEndPoint ipNport)
		{
			Init(ipNport);
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~TCPServer()
		{
			StopServer();
		}

		/// <summary>
		/// Init method that create a server (TCP Listener) Object based on the
		/// IP Address and Port information that is passed in.
		/// </summary>
		/// <param name="ipNport"></param>
		private void Init(IPEndPoint ipNport)
		{
			try
			{
				m_server = new TcpListener(ipNport);

			}
			catch(Exception e)
			{
				m_server=null;
				logger.logData("EXEPCION: "+e);
			}
		}
		public bool isServerRunning(){
			return !m_stopServer;
		}
		/// <summary>
		/// Method that starts TCP/IP Server.
		/// </summary>
		public void StartServer()
		{
			
			if (m_server!=null)
			{
				
				/*Creo la rta HTTP que luego enviaran los Threads*/
				StringBuilder str = new StringBuilder();
				str.Append("HTTP/1.1 200 OK\r\n");
				str.Append("\r\n");
				HTTPresponse = Encoding.ASCII.GetBytes(str.ToString());
				
				// Create a ArrayList for storing SocketListeners before
				// starting the server.
				m_socketListenersList = new ArrayList();
				
				queue = new BlockingQueue<Message>();
				
				// Start the Server and start the thread to listen client
				// requests.
				m_server.Start();
				m_serverThread = new Thread(new ThreadStart(ServerThreadStart));
				m_serverThread.Start();

				// Create a low priority thread that checks and deletes client
				// SocktConnection objcts that are marked for deletion.
				m_purgingThread = new Thread(new ThreadStart(PurgingThreadStart));
				m_purgingThread.Priority=ThreadPriority.Lowest;
				m_purgingThread.Start();
				
				//Doy inicio al thread que procesa los SMS
				m_processingThread = new Thread (new ThreadStart(processingQueueThreadStart));
				m_processingThread.Start();
			}
		}

		/// <summary>
		/// Method that stops the TCP/IP Server.
		/// </summary>
		public void StopServer()
		{
			if (m_server!=null)
			{
				// It is important to Stop the server first before doing
				// any cleanup. If not so, clients might being added as
				// server is running, but supporting data structures
				// (such as m_socketListenersList) are cleared. This might
				// cause exceptions.

				// Stop the TCP/IP Server.
				m_stopServer=true;
				m_server.Stop();

				// Wait for one second for the the thread to stop.
				m_serverThread.Join(1000);
				
				// If still alive; Get rid of the thread.
				if (m_serverThread.IsAlive)
				{
					m_serverThread.Abort();
				}
				m_serverThread=null;
				
				m_stopPurging=true;
				m_purgingThread.Join(1000);
				if (m_purgingThread.IsAlive)
				{
					m_purgingThread.Abort();
				}
				m_purgingThread=null;
				
				m_stopProccesing=true;
				m_processingThread.Join(1000);
				if (m_processingThread.IsAlive)
				{
					m_processingThread.Abort();
				}
				m_processingThread=null;
				

				// Free Server Object.
				m_server = null;

				// Stop All clients.
				StopAllSocketListers();
			}
		}


		/// <summary>
		/// Method that stops all clients and clears the list.
		/// </summary>
		private void StopAllSocketListers()
		{
			foreach (TCPSocketListener socketListener
			         in m_socketListenersList)
			{
				socketListener.StopSocketListener();
			}
			// Remove all elements from the list.
			m_socketListenersList.Clear();
			m_socketListenersList=null;
		}

		/// <summary>
		/// TCP/IP Server Thread that is listening for clients.
		/// </summary>
		private void ServerThreadStart()
		{
			// Client Socket variable;
			Socket clientSocket = null;
			TCPSocketListener socketListener = null;
			
			while(!m_stopServer)
			{
				try
				{
					// Wait for any client requests and if there is any
					// request from any client accept it (Wait indefinitely).
					//Bloqueante. Espera hasta nueva peticion de cliente o un close.
					clientSocket = m_server.AcceptSocket();

					// Create a SocketListener object for the client.
					socketListener = new TCPSocketListener(clientSocket);

					// Add the socket listener to an array list in a thread
					// safe fashon.
					//Monitor.Enter(m_socketListenersList);
					lock(m_socketListenersList)
					{
						m_socketListenersList.Add(socketListener);
					}
					//Monitor.Exit(m_socketListenersList);

					// Start a communicating with the client in a different
					// thread.
					socketListener.StartSocketListener();
				}
				catch (SocketException e)
				{
					m_stopServer = true;
					logger.logData("EXEPCION: "+e);
				}
			}
		}

		/// <summary>
		/// Thread method for purging Client Listeneres that are marked for
		/// deletion (i.e. clients with socket connection closed). This thead
		/// is a low priority thread and sleeps for 10 seconds and then check
		/// for any client SocketConnection obects which are obselete and
		/// marked for deletion.
		/// </summary>
		private void PurgingThreadStart()
		{
			while (!m_stopPurging)
			{
				ArrayList deleteList = new ArrayList();

				// Check for any clients SocketListeners that are to be
				// deleted and put them in a separate list in a thread sage
				// fashon.
				//Monitor.Enter(m_socketListenersList);
				lock(m_socketListenersList)
				{
					foreach (TCPSocketListener socketListener
					         in m_socketListenersList)
					{
						if (socketListener.IsMarkedForDeletion())
						{
							deleteList.Add(socketListener);
							socketListener.StopSocketListener();
						}
					}

					// Delete all the client SocketConnection ojects which are
					// in marked for deletion and are in the delete list.
					for(int i=0; i<deleteList.Count;++i)
					{
						m_socketListenersList.Remove(deleteList[i]);
					}
				}
				//Monitor.Exit(m_socketListenersList);

				deleteList=null;
				Thread.Sleep(10000);
			}
		}
		
		
		/// <summary>
		///Thread que procesa los mensajes encolados
		/// </summary>
		private void processingQueueThreadStart(){
			
			Message aux;
			bool enviado;
			int errorCount = 0;

			while(!m_stopProccesing){
				
				enviado=false;
				errorCount=0;
				
				TCPServer.queue.TryDequeue(out aux);

				/*Elimino /r /n. Reemplazo caracteres acentuados, ñ y corto a 160 caracteres*/
				string mensaje = filtrarMensaje(aux.mensaje);
				
				/*Elimina espacios y devuelve "" si contiene caracteres no numericos*/
				string numero = filtrarNumero(aux.numero);
				
				/*Convierto a codificacion GSM7*/
				string mensajeGSMFormat=GSMChar(mensaje);
				
				/*Numero vacio*/
				if (numero == ""){
					enviado=true;
					logger.logData("ERROR - Numero Incorrecto. Numero: "+aux.numero+ " Mensaje: "+mensaje);
					
				}
				
				while (!enviado && (errorCount<3)){
					
					if (gsm_module.enviarSMS(numero,mensajeGSMFormat))
					{
						enviado = true;
						logger.logData("Mensaje Enviado. Numero: "+numero+ " Mensaje: "+mensaje);
					}
					else
					{
						logger.logData("ERROR (Intento "+(errorCount+1)+"/3) : Mensaje NO Enviado. Numero: "+numero+ " Mensaje: "+mensaje);
						
						try{
							if(!TCPServer.serialPort.IsOpen){
								TCPServer.serialPort.PortName=TCPServer.serialPort.PortName;
								TCPServer.serialPort.Open();
							}
						}
						catch(Exception e)
						{
							logger.logData("EXEPCION: "+e);
						}
						
						gsm_module.connectSIM900();
						gsm_module.setSignal();
						gsm_module.prepareSMS();
						
					}
					errorCount++;
					
				}
				
				if (!enviado){
					/*Aviso por mail que no pudo enviar*/
					sendErrorEmail();
				}
			}
			
		}
		
		/*
		 * NO USADA
		
		public static void sendHTTPResponse(){
			
			VERSION 1
			
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:31001");
			request.Method = "POST";
			request.ServicePoint.Expect100Continue = false;
			request.ContentType = "application/x-www-form-urlencoded";
			request.Timeout = 10000;

			request.Headers.Add("Param: prueba");

			string postData = "postData";
			ASCIIEncoding encoding = new ASCIIEncoding();
			byte[] byte1 = encoding.GetBytes(postData);
			request.ContentLength = byte1.Length;
			Stream reqStream = request.GetRequestStream();
			reqStream.Write(byte1, 0, byte1.Length);
			reqStream.Close();
			
			VERSION 2. MAS FACIL Y FUNCIONA
			
			try
			{
				var post = new NameValueCollection();
				post.Add("devid", "v1C08EE53692D300");

				using (var wc = new WebClient())
				{
					wc.UploadValues("http://api.pushingbox.com/pushingbox", post);
				}
				
				post = null;
			}
			catch (WebException we)
			{}
		}
		
		 */
		
		
		/// <summary>
		///Envio un request a la API de pushingbox quien me envia un mail
		/// </summary>
		public void sendErrorEmail()
		{
			try
			{
				var post = new NameValueCollection();
				post.Add("devid", "v1C08EE53692D300");

				using (var wc = new WebClient())
				{
					wc.UploadValues("http://api.pushingbox.com/pushingbox", post);
				}
				
				post = null;
			}
			catch (WebException e)
			{
				logger.logData("EXEPCION: "+e);
			}
		}
		
		
		/// <summary>
		///Elimina los caracteres acentuados y corta el string a max 160 caracteres
		/// </summary>
		public string filtrarMensaje(string mensaje){
			
			
			
			/*Elimino /n y /r*/
			mensaje= mensaje.Replace("\n", string.Empty).Replace("\r",string.Empty);
			
			/*
			 * 	Reemplazo caracteres con tilde por su equivalente sin tilde
			 *	Reemplazo ñ por n
			 */
			System.Text.RegularExpressions.Regex a = new Regex("[á|à|ä|â]", RegexOptions.Compiled);
			Regex e = new Regex("[é|è|ë|ê]", RegexOptions.Compiled);
			Regex i = new Regex("[í|ì|ï|î]", RegexOptions.Compiled);
			Regex o = new Regex("[ó|ò|ö|ô]", RegexOptions.Compiled);
			Regex u = new Regex("[ú|ù|ü|û]", RegexOptions.Compiled);
			Regex n = new Regex("[ñ|Ñ]", RegexOptions.Compiled);
			mensaje = a.Replace(mensaje, "a");
			mensaje = e.Replace(mensaje, "e");
			mensaje = i.Replace(mensaje, "i");
			mensaje = o.Replace(mensaje, "o");
			mensaje = u.Replace(mensaje, "u");
			mensaje = n.Replace(mensaje, "n");
			
			
			/*
			 *	Si tiene mas de 160 caracteres lo corto.
			 */
			if (mensaje.Length>159){
				mensaje=mensaje.Remove(160,mensaje.Length-160);
			}
			mensaje=mensaje.Replace("\n", string.Empty).Replace("\r",string.Empty);
			
			
			
			return mensaje;
			

		}
		
		
		/// <summary>
		/// Borra espacios en blanco y testea si el string es un numero
		/// </summary>
		public string filtrarNumero(string numero){
			
			string newNumber;
			
			/*Borro espacios*/
			newNumber=numero.Replace(" ",string.Empty);
			
			/*Testeo si es numero*/
			double i = 0;
			bool result = double.TryParse(newNumber, out i);
			result=StrIsInteger(newNumber);
			if(result){
				return newNumber;
			}
			else
			{
				return "";
			}
		}
		
		
		
		private bool StrIsInteger(string str)
		{
			Regex regex = new Regex(@"^[0-9]+$");
			
			try
			{
				if (String.IsNullOrWhiteSpace(str))
				{
					return false;
				}
				if(!regex.IsMatch(str))
				{
					return false;
				}
				
				return true;
				
			}
			catch(Exception e)
			{
				logger.logData("EXEPCION: "+e);
			}
			
			return false;
			
		}
		

		/// <summary>
		/// Codifica el string a otro string con codificacion GSM 03.38 (GSM 7 bit)
		/// http://www.developershome.com/sms/gsmAlphabet.asp
		/// </summary>
		/// <param name="PlainText: texto a convertir"></param>
		
		public static string GSMChar(string PlainText)
		{
			// ` is not a conversion, just a untranslatable letter
			string strGSMTable ="";
			strGSMTable += "@£$¥èéùìòÇ`Øø`Åå";
			strGSMTable += "Δ_ΦΓΛΩΠΨΣΘΞ`ÆæßÉ";
			strGSMTable += " !\"#¤%&'()*=,-./";
			strGSMTable += "0123456789:;<=>?";
			strGSMTable += "¡ABCDEFGHIJKLMNO";
			strGSMTable += "PQRSTUVWXYZÄÖÑÜ`";
			strGSMTable += "¿abcdefghijklmno";
			strGSMTable += "pqrstuvwxyzäöñüà";

			string strExtendedTable = "";
			strExtendedTable += "````````````````";
			strExtendedTable += "````^```````````";
			strExtendedTable += "````````{}`````\\";
			strExtendedTable += "````````````[~]`";
			strExtendedTable += "|```````````````";
			strExtendedTable += "````````````````";
			strExtendedTable += "`````€``````````";
			strExtendedTable += "````````````````";

			string strGSMOutput = "";
			int i = 0;
			
			/*Guardo el decimal correspondiente al caracter en UTF7 segun la tabla:
			 http://www.developershome.com/sms/gsmAlphabet.asp
			 Los caracteres normales coinciden pero el $ o _ por ejemplo, no coinciden
			 */
			ArrayList byteArray = new ArrayList();
			
			foreach(char cPlainText in PlainText.ToCharArray())
			{
				int intGSMTable = strGSMTable.IndexOf(cPlainText);
				if (intGSMTable != -1)
				{
					strGSMOutput += intGSMTable.ToString("X2");
					byteArray.Add(Convert.ToByte(intGSMTable.ToString("X2"), 16));
					i++;
					continue;
				}
				int intExtendedTable = strExtendedTable.IndexOf(cPlainText);
				if (intExtendedTable !=-1)
				{
					strGSMOutput += (27).ToString("X2");
					strGSMOutput += intExtendedTable.ToString("X2");
					byteArray.Add(Convert.ToByte((27).ToString("X2"), 16));
					byteArray.Add(Convert.ToByte(intExtendedTable.ToString("X2"), 16));
					i++;
				}
			}
			
			/*Convierto el ArrayList en un byte{[]*/
			byte[] byteArray1= (byte[])byteArray.ToArray(typeof(byte));
			
			/*Codifico en UTF7. Si lo imprimo como ASCII o UTF8 no voy a ver bien los caracteres pero en los SMS, si*/
			return Encoding.UTF7.GetString(byteArray1,0, byteArray1.Length);
		}
		

	}
}
