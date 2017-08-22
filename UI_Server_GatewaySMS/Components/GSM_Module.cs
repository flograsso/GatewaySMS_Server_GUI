/*
 * Created by SharpDevelop.
 * User: SoporteSEM
 * Date: 06/02/2017
 * Time: 16:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UI_Server_GatewaySMS
{
	/// <summary>
	/// Description of Class1.
	/// </summary>
	/*Para manejar el módulo SIM*/
	public class GSM_Module{
		
		private String receivedBuffer;
		private string IMEI;
		private string signal;
		private SerialPort serialPort;
		
		/*Constructor*/
		public GSM_Module(ref SerialPort serial){
			receivedBuffer="";
			IMEI="";
			signal = "";
			serialPort=serial;
			this.serialPort.BaudRate=115200;
			/*Agrego evento manejador del dataReceived del serial*/
			this.serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.SerialPort1DataReceived);
		}
		
		public String getSignal(){
			return signal;
		}
		
		public String getReceivedBuffer(){
			return receivedBuffer;
		}
		
		public void setReceivedBuffer(String receivedBuffer){
			this.receivedBuffer=receivedBuffer;
		}
		
		public String getIMEI(){
			return IMEI;
		}
		
		/*Espera hasta encontrar el string deseado dentro de el buffer*/
		public void waitForResult(String response){
			while((getReceivedBuffer().IndexOf(response)==-1)){
				Thread.Sleep(100);
			}
		}
		
		
		/*  Envio de comandos
		 * 		Return = null 	--> Comando falló
		 *		Return !=null 	--> Comando OK
		 */
		String sendCommand(String command, int delaySec, String response){
			
			string received = "";
			receivedBuffer="";
			
			/*Puede tirar exepcion si el puerto esta cerrado (se desenchufa el modulo y
			 * el puerto queda cerrado por mas quer reconecte*/
			try
			{
				/*Envio Comando*/
				serialPort.WriteLine((command+"\r\n"));
			}
			catch (Exception){}
			
			/*Espero hasta que pase el tiempo delaySec o encuentre el string response en la respuesta*/
			var task = Task.Factory.StartNew (() => waitForResult(response));
			if (task.Wait(TimeSpan.FromSeconds(delaySec))){
				/*Encontro string*/
				received=receivedBuffer;
				
			}
			else /*No Encontro string*/
			{
				
				received = null;
			}
			
			receivedBuffer="";
			
			return received;
		}
		
		/*  Envio de comandos
		 * 	Igual a sendCommand solo que usa write y no writeln sino se envia un enter luego del SMS
		 * 		Return = null 	--> Comando falló
		 *		Return !=null 	--> Comando OK
		 * 
		 */
		String sendCommandMessage(String command, int delaySec, String response){
			
			string received = "";
			receivedBuffer="";
			
			/*Puede tirar exepcion si el puerto esta cerrado (se desenchufa el modulo y
			 * el puerto queda cerrado por mas quer reconecte*/
			try
			{
				/*Envio Comando*/

				serialPort.Write((command));
			}
			catch (Exception){}
			
			/*Espero hasta que pase el tiempo delaySec o encuentre el string response en la respuesta*/
			var task = Task.Factory.StartNew (() => waitForResult(response));
			if (task.Wait(TimeSpan.FromSeconds(delaySec))){
				/*Encontro string*/
				received=receivedBuffer;
				
			}
			else /*No Encontro string*/
			{
				
				received = null;
			}
			
			receivedBuffer="";
			
			return received;
		}
		
		
		public bool enviarSMS(string numero, string mensaje){
			
			bool OK=true;
			
			
			if ((sendCommand("AT+CMGS=\""+numero+"\"",5,"ERROR")) != null){
				OK=false;
			}
			
			if ((sendCommandMessage(mensaje,7,"ERROR")) != null){
				OK=false;
			}
			
			if ((sendCommand("\x1A",30,"OK")) == null){
				OK=false;
			}
			
			
			return OK;

		}
		
		public bool connectSIM900(){
			
			String result = "";
			bool OK = true;
			
			if(OK && sendCommand("AT",2,"OK")==null){
				OK=false;
			}
			
			if (OK && sendCommand("ATE0",2,"OK")==null){
				OK = false;
			}
			
			if (OK && (result = sendCommand("AT+CREG?",2,"OK")) != null){
				if ((result.IndexOf("0,1")==-1)&&(result.IndexOf("1,1")==-1)){
					OK=false;
				}
			}
			else
			{
				OK=false;
			}

			
			if (OK && sendCommand("AT+CPIN?",2,"OK")==null){
				OK=false;
			}
			
			return OK;
		}
		
		
		public bool setIMEI(){
			
			string result="";
			bool OK = true;
			
			if ((result = sendCommand("AT+CGSN",2,"OK")) != null){
				IMEI=result.Substring(0,result.Length-3);
			}
			else
			{
				OK=false;
			}
			
			return OK;
			
		}

		
		
		public bool setSignal(){
			
			string result = "";
			bool OK = true;
			
			if ((result = sendCommand("AT+CSQ",2,"OK")) != null){
				
				int i =0;
				
				
				do{
					i++;
				}while ((result[i] != ':') && ( i<result.Length-1));
				
				if (Char.IsNumber(result[i+2]) && Char.IsNumber(result[i+3])){
					signal=(Convert.ToString(result[i+2]))+(Convert.ToString(result[i+3]));

				}
				else
					if (Char.IsNumber(result[i+2])){
					signal="0"+result[i+2];
					
				}

			}
			else
			{
				OK=false;
				
			}
			return OK;
			
		}
		
		public bool prepareSMS(){
			
			bool OK = true;
			
			if (OK && (sendCommand("AT+CMGF=1",2,"OK")) == null){
				OK=false;
			}
			
			if (OK && sendCommand("AT+CSCS=\"GSM\"",2,"OK") == null){
				OK=false;
			}
			
			
			return OK;
			
		}
		
		/*Data recibida por puerto serial. Lo voy almacenando*/
		void SerialPort1DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
		{
			try{
				TCPServer.gsm_module.setReceivedBuffer(TCPServer.gsm_module.getReceivedBuffer()+TCPServer.serialPort.ReadLine());
			}
			catch(Exception){}
		}

	}
}
