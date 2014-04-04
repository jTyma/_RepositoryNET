using System;

namespace mobilkom.internalapi
{
	/// <summary>
	/// Z�kladn� trr�da pro webov� slu�by, aby to nemuselo b�t v�echno nahn�cane v jednom objeku
	/// </summary>
	public class WSBase: System.Web.Services.WebService
	{
		// logovani
		protected static readonly log4net.ILog log = log4net.LogManager.GetLogger("BOSS_API");

		protected string _logCategory;

		public WSBase() 
		{
			// defaults
			_logCategory = "log.Default";

			InitializeComponent();
		}

		protected virtual void  InitializeComponent()
		{
		}

		/// <summary>
		/// Pomocn� funkce na testov�n� pr�zdnosti
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		protected bool isEmpty (object val)
		{
			if (val==null)
			{
				return true;
			}

			if (val is string )
			{
				return (((string)val).Length == 0);
			}

			return false;
		}

		protected void logError (string msg, Exception e)
		{
			string logcat = getLogCategory();
			log.Error (string.Format("{0}: {1}", logcat, msg), e);
		}

		/// <summary>
		/// Zalogov�n� ud�lost
		/// </summary>
		/// <param name="msg"></param>
		protected void logMessage (string msg)
		{
			string logcat = getLogCategory();
			
			// log4net logovani
			log.Info (string.Format("{0}: {1}", logcat, msg));

			/* ====================================================================
			 * z nejakycj duvodu todle Microsofti trace neni schopno zvladat
			 * konkurencni pristup k logovacim souborum :-(((
			 * 
			 * a nebo nevim, jak ho to naucit
			 * 
			 * Dusledkem je, ze to pada a webservice nebezi
			 * ====================================================================
			string logMsg = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg;
			if (System.Diagnostics.Trace.Listeners[logcat] != null)
			{
				System.Diagnostics.Trace.Listeners[logcat].WriteLine(logMsg);
			}
			else
			{
				// sichr, ale todle pak zaloguje do vsech
				System.Diagnostics.Trace.WriteLine ( logMsg, logcat);
			}
			//System.Diagnostics.Trace.WriteLine ( System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg, logcat);
			*/
		}

		protected string getLogCategory()
		{
			return _logCategory;
		}

		// return struct value
		public class RcStruct
		{
			public int rc;
			public string msg;

			public override string ToString()
			{
				return string.Format ("rc={0}, msg={1}", rc, msg);
			}
		}

		/// <summary>
		/// pripojen� do datab�ze
		/// </summary>
		/// <returns></returns>
		protected string getConnectionString ()
		{
			return System.Configuration.ConfigurationSettings.AppSettings["databaseConn"];
		}

		/// <summary>
		/// Pokud je to prazdne, vrati se DBNull.Value
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		protected object dbNlv (object val)
		{
			return isEmpty (val) ? DBNull.Value : val;
		}

	}
}
