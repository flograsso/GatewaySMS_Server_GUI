using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;

namespace TCPService
{
	/// <summary>
	/// Summary description for TCPInstaller.
	/// </summary>
	[RunInstaller(true)]
	public class TCPInstaller : System.Configuration.Install.Installer
	{
		private System.ServiceProcess.ServiceProcessInstaller TCPServiceProcessInstaller;
		private System.ServiceProcess.ServiceInstaller TCPServiceInstaller;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public TCPInstaller()
		{
			// This call is required by the Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}


		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.TCPServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.TCPServiceInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// TCPServiceProcessInstaller
			// 
			this.TCPServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			this.TCPServiceProcessInstaller.Password = null;
			this.TCPServiceProcessInstaller.Username = null;
			// 
			// TCPServiceInstaller
			// 
			this.TCPServiceInstaller.DisplayName = "TCP";
			this.TCPServiceInstaller.ServiceName = "TCPService";
			// 
			// TCPInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
																					  this.TCPServiceProcessInstaller,
																					  this.TCPServiceInstaller});

		}
		#endregion
	}
}
