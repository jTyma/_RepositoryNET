using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Services;

namespace WStolls
{
	/// <summary>
	/// Summary description for Service1.
	/// </summary>
	[WebService(Namespace = "http://b-oss.ufon.cz/internalapi")]
	public class Service1 : System.Web.Services.WebService
	{
		public Service1()
		{
			InitializeComponent();
		}

		#region Component Designer generated code
		
		//Required by the Web Services Designer 
		private IContainer components = null;
				
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if(disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);		
		}
		
		#endregion
//		[WebMethod]
//		public string HelloWorld()
//		{
//			return "Hello World";
//		}

		[WebMethod]
		public int SetSubsAttributes(AttrCommandInfo[]  cmd)
		{
			AttrCommandInfo x = new AttrCommandInfo ();
			return 0;
		}

		[WebMethod]
		public int UpdateSubsAttributes(int subsId, string sType, string sCode, string sapId, string recWho, string recMe, string recSCode )
		{
			//AttrCommandInfo x = new AttrCommandInfo ();
			//return "Hello World";
			return 0;
		}


		// attr update struce
		public class AttrCommandInfo
		{
			public int subsId;
			public string attr_name;
			public string attr_value;
		}
	}

}
