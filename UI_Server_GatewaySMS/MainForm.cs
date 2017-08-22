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
				
				TCPServer.serialPort.PortName=textBox_serialport.Text;
				Settings1.Default.puertoCOM=textBox_serialport.Text;
				Settings1.Default.Save();
				while (i < 3 && !ok)
				{

					restartUSBController();
					
					if(!TCPServer.serialPort.IsOpen){
						TCPServer.serialPort.Open();
					}
					
					if(TCPServer.serialPort.IsOpen){
						TCPServer.logger.logData("CONECTADO AL PUERTO "+TCPServer.serialPort.PortName);
						if (TCPServer.gsm_module.connectSIM900() && TCPServer.gsm_module.setSignal() && TCPServer.gsm_module.prepareSMS())
						{
							ok=true;
							TCPServer.logger.logData("CONECTADO AL MODULO GSM");
						}
						else
						{
							TCPServer.logger.logData("ERROR : No se pudo conectar al módulo SIM");
						}
					}
					else
					{
						TCPServer.logger.logData("ERROR : No se pudo conectar al puerto COM");
						MessageBox.Show("No se pudo conectar al puerto COM","ERROR",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
					}
					
					
					i++;
					System.Threading.Thread.Sleep(1000);
				}
				
			}
			catch(Exception ex)
			{
				TCPServer.logger.logData("EXEPCION: "+ex);
			}
			

			if (ok){
				try{
					// Create the Server Object ans Start it.
					server = new TCPServer();
					server.StartServer();
					TCPServer.logger.logData("Servidor Iniciado");
				}
				catch(Exception e2){
					TCPServer.logger.logData("ERROR : Error de creacion del Server");
					TCPServer.logger.logData(e2.ToString());
					MessageBox.Show("Error de creacion del server");
				}
				button1.Enabled=false;
				textBox_serialport.Enabled=false;
				label3.Text="RUNNING...";
				TCPServer.sendErrorEmail(3);
				
			}
			else
			{
				label3.Text="DESCONECTADO";
				button1.Enabled=true;
				textBox_serialport.Enabled=true;
				TCPServer.sendErrorEmail(2);
				TCPServer.logger.logData("ERROR : No se pudo conectar con el modulo GSM");

			}
			
		}
		void MainFormFormClosed(object sender, FormClosedEventArgs e)
		{
			label3.Text="CERRANDO...";
			try{
				/*Si le hago el stop y no esta creado. Se queda ahi*/
				if (server != null && server.isServerRunning()){
					server.StopServer();
				}
				/*Cierro el serial*/
				
				if(TCPServer.serialPort.IsOpen){
					TCPServer.serialPort.Close();
				}
				
			}
			catch (Exception ex)
			{
				TCPServer.logger.logData("EXEPCION: "+ex);
			}
			finally
			{
				if(TCPServer.serialPort.IsOpen){
					TCPServer.serialPort.Close();
				}
			}
			
			
		}
		/*Boton salir*/
		void Button2Click(object sender, EventArgs e)
		{
			this.Close();
		}

		
		/*Ejecuta mediante cmd el programa devcon (administrador de dispositivos por consola)
		 *y reinicia el dispositivo. Esto lo hace llamando al programa y pasandole unos paramentros
		 * comando C:\.....\devcon.exe restart *Instance_ID*  donde Instance_ID es el identificador del dispositivo.
		 * Este identificador lo puedo ver desde el administrador de dispositivos. En los detalles del dispositivo
		 * viendo la "Ruta de Acceso de la Instancia del Dispositivo"
		 * El software devcon.exe es de Windows y debe estar en la misma carpeta del .exe del programa.
		 * El device ID es leido del archivo Settings1
		 */
		void restartUSBController()
		{
			//Create process
			System.Diagnostics.Process pProcess = new System.Diagnostics.Process();

			//strCommand is path and file name of command to run
			pProcess.StartInfo.FileName = @AppDomain.CurrentDomain.BaseDirectory.ToString()+"\\devcon.exe";

			//Execute comand "restart *deviceID*"
			pProcess.StartInfo.Arguments = " restart *"+Settings1.Default.deviceID+"*";

			pProcess.StartInfo.UseShellExecute = false;

			//Set output of program to be written to process output stream
			pProcess.StartInfo.RedirectStandardOutput = true;

			//Start the process
			pProcess.Start();

			//Get program output
			//string strOutput = pProcess.StandardOutput.ReadToEnd();

			//Wait for process to finish
			pProcess.WaitForExit();
			
			
			

		}

		
	}
}