/*
 * Created by SharpDevelop.
 * User: SoporteSEM
 * Date: 12/04/2017
 * Time: 9:37
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.PropertyGridInternal;
using Client_test;

namespace UI_Server_GatewaySMS
{
	/// <summary>
	/// Description of Logger.
	/// </summary>
	public class Logger
	{
		string path = Environment.CurrentDirectory + @"\Log\";
		//string path = Environment.CurrentDirectory;
		public FileStream fs;
		public string fileNameBase = "log";
		
		
		public Logger()
		{
			/*Si no existe lo crea y sino lo ignora*/
			System.IO.Directory.CreateDirectory(path);
		}
		
		public void logData(string data)
		{
			string fileName = Settings1.Default.logFileName;
			
			
			
			if (!File.Exists(path + @fileName + ".txt"))
			{
				fs=File.Create(path + @fileName + ".txt");
				fs.Close();
			}
			else
			{
				if(File.ReadAllBytes(path + @fileName + ".txt").Length >= 104857600) // (100mB) File to big? Create new
				{
					int lognumber = Int32.Parse(fileName.Substring(fileName.LastIndexOf("-")+1, fileName.Length-(fileName.LastIndexOf("-")+1))); //Get old number, Can cause exception if the last digits aren't numbers
					lognumber++; //Increment lognumber by 1
					fileName=fileNameBase + "-" + lognumber;
					Settings1.Default.logFileName=fileName; //Override filename
					Settings1.Default.Save();
					
					if (!File.Exists(path + @fileName + ".txt"))
					{
						fs=File.Create(path + @fileName + ".txt");
						fs.Close();
						
					}
				}
			}
			fs=null;
			
			System.IO.StreamWriter sw = System.IO.File.AppendText(
				path + @fileName + ".txt");
			try
			{
				string logLine = System.String.Format(
					"{0:G}: {1}", System.DateTime.Now, data);
				sw.WriteLine(logLine);
			}
			finally
			{
				sw.Close();
			}
		}
		
	}
}

