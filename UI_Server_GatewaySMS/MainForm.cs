/*
 * Created by SharpDevelop.
 * User: SoporteSEM
 * Date: 04/04/2017
 * Time: 10:48
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 * 
 * # UI_Server_GatewaySMS
	Servidor HTTP en C# el cual escucha constantemente un puerto dado y al recibir un POST, 
	crea un nuevo Thread que lo maneje. Este Thread recibe los parametros de un mensaje y lo encola. 
	Otro Thread se encarga de desencolar los mensajes y enviarlos por un modulo GSM mediante el 
	puerto serial

	Los parametros a configurar para su funcionamiento son:

	En el archivo TCPServer, hay que setear la IP local de la PC en la variable "DEFAULT_SERVER" y 
	el puerto en "DEFAULT_PORT"
	El BaudRate para comunicarse con el GSM esta configurado en 115200, se puede editar en la 
	clase GSM_Module, en la linea "this.serialPort.BaudRate=115200;"
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.IO.Ports;
using System.Windows.Forms.PropertyGridInternal;
using Client_test;


namespace UI_Server_GatewaySMS
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		private TCPServer server=null;

		

		public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			
			InitializeComponent();
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}
		void MainFormLoad(object sender, EventArgs e)
		{
			button1.Enabled=false;
			textBox_serialport.Text=Settings1.Default.puertoCOM;
			/*Llamo al Click de boton Conectar*/
			Button1Click(sender,e);
			
			
		}
		void Button1Click(object sender, EventArgs e)
		{
			int i = 0;
			bool ok = false;
			label3.Text="INICIANDO...";
			this.Refresh();
			
			try{
				while (i < 3 && !ok)
				{
					
					TCPServer.serialPort.PortName=textBox_serialport.Text;
					Settings1.Default.puertoCOM=textBox_serialport.Text;
					Settings1.Default.Save();
					
					if(!TCPServer.serialPort.IsOpen){
						TCPServer.serialPort.PortName=TCPServer.serialPort.PortName;
						TCPServer.serialPort.Open();
						TCPServer.logger.logData("CONECTADO AL PUERTO "+textBox_serialport.Text);
						
						if (TCPServer.gsm_module.connectSIM900() && TCPServer.gsm_module.setSignal() && TCPServer.gsm_module.prepareSMS())
						{
							ok=true;
						}
						i++;
					}
					
					System.Threading.Thread.Sleep(1000);
					

					
				}
			}
			catch(Exception){
				TCPServer.logger.logData("ERROR : Puerto COM Incorrecto");
				MessageBox.Show("Puerto COM incorrecto","ERROR",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
			}
			

			if (ok){
				// Create the Server Object ans Start it.
				server = new TCPServer();
				server.StartServer();
				button1.Enabled=false;
				textBox_serialport.Enabled=false;
				label3.Text="RUNNING...";
				TCPServer.logger.logData("CONECTADO AL MODULO GSM");
			}
			else
			{
				label3.Text="DESCONECTADO";
				button1.Enabled=true;
				textBox_serialport.Enabled=true;
				TCPServer.logger.logData("ERROR : No se pudo conectar con el modulo GSM");
			}
			
		}
		void MainFormFormClosed(object sender, FormClosedEventArgs e)
		{
			label3.Text="CERRANDO...";
			/*Si le hago el stop y no esta creado. Se queda ahi*/
			if (server != null && server.isServerRunning()){
				server.StopServer();
			}
			
			/*Cierro el serial*/
			try{
				if(TCPServer.serialPort.IsOpen){
					TCPServer.serialPort.Close();
				}
				
			}
			catch (Exception ex)
			{
				TCPServer.logger.logData("EXEPCION: "+ex);
			}
			
			
		}
		/*Boton salir*/
		void Button2Click(object sender, EventArgs e)
		{
			this.Close();
		}

		

		
	}
}