/*
 * Created by SharpDevelop.
 * User: SoporteSEM
 * Date: 04/04/2017
 * Time: 10:48
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.IO.Ports;

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


			
		}
		void Button1Click(object sender, EventArgs e)
		{
			label3.Text="INICIANDO...";
			this.Refresh();
			
			try{
				if(!TCPServer.serialPort.IsOpen){
					TCPServer.serialPort.Close();
					TCPServer.serialPort.PortName=textBox_serialport.Text;
					TCPServer.serialPort.Open();
					
					// Create the Server Object ans Start it.
					server = new TCPServer();
					server.StartServer();
					
				}
			}
			catch(Exception){
				MessageBox.Show("Puerto COM incorrecto","ERROR",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
			}
			
			
			if (TCPServer.gsm_module.connectSIM900() && TCPServer.gsm_module.setSignal() && TCPServer.gsm_module.prepareSMS()){
				label3.Text="RUNNING...";
			}

			else
			{
				label3.Text="DESCONECTADO";
			}
			
		}
		void MainFormFormClosed(object sender, FormClosedEventArgs e)
		{
			label3.Text="CERRANDO...";
			server.StopServer();
			/*Cierro el serial*/
			try{
				if(TCPServer.serialPort.IsOpen){
					TCPServer.serialPort.Close();
				}
				
			}
			catch (Exception){}
			
			
		}
		void Button2Click(object sender, EventArgs e)
		{
			this.Close();
		}
		

		
	}
}