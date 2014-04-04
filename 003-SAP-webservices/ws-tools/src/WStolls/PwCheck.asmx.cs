using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Services;
using System.Data.OracleClient;



namespace mobilkom.internalapi.pwCheck
{
	/// <summary>
	/// Summary description for Service1.
	/// </summary>
	[WebService(Namespace = "http://b-oss.ufon.cz/internalapi")]
	public class PwCheck : WSBase
	{

		protected override  void InitializeComponent()
		{
			_logCategory = "log.PwdCheck";
		}

		public PwCheck() : base ()
		{
		}

		[WebMethod]
		public RcStruct checkPin(string AcctID, string MSISDN,  string pwd)
		{
			string connStr = getConnectionString ();
			logMessage ("checkPin begin");
			
			RcStruct objRc = new RcStruct ();
			objRc.rc = 0;

			using (OracleConnection objConn = new OracleConnection(connStr))
			{
				objConn.Open();
				OracleTransaction transaction;

				OracleCommand objCmd = new OracleCommand("ws_check_pwd.check_acct_pin", objConn);
				objCmd.CommandType = CommandType.StoredProcedure;
				transaction = objConn.BeginTransaction ();

				try
				{
					objCmd.Transaction = transaction;

					objCmd.Parameters.Clear();
					//WS_SET_ATTR.set_attr (25995, 'STYPE', '1');
					logMessage (string.Format ("input: p_acct_id={0}, MSSIDN={1}, pwd={2}", AcctID, MSISDN, "CENSORED"));
					objCmd.Parameters.Add("p_acct_id", OracleType.VarChar).Value = dbNlv(AcctID);
					objCmd.Parameters.Add("p_mssidn" , OracleType.VarChar).Value = dbNlv(MSISDN);
					objCmd.Parameters.Add("p_pin"    , OracleType.VarChar).Value = dbNlv(pwd);
					objCmd.Parameters.Add("p_result" , OracleType.Number).Direction = ParameterDirection.Output;

					objCmd.ExecuteNonQuery();

					string rc = objCmd.Parameters["p_result"].Value.ToString();
					logMessage (string.Format ("command output: p_result={0}", rc));

					objRc.rc  = Int32.Parse(rc);
					string errMsg;


					switch (objRc.rc)
					{
						case -1:
							errMsg = "no account for mssidn";
							break;

						case -2:
							errMsg = "invalid account id";
							break;

						case 1:
							errMsg = "password is ok";
							break;

						case 0:
							errMsg = "password is not ok";
							break;

						default:
							errMsg = "Unexpected errorcode " + rc + ". See to stored procedure for more details.";
							break;
					}
					objRc.msg = errMsg;

					transaction.Commit();
				}

				catch (Exception e)
				{
					transaction.Rollback();
					logError ("checkPin failed", e);
				}

				finally
				{
					objConn.Close();
				}
			}

			
			logMessage ("checkPin end: " + objRc.ToString());
			return objRc;
		}

		[WebMethod]
		public RcStruct checkPwdHash(string AcctID, string MSISDN,  string pwdHash, string salt)
		{
			string connStr = getConnectionString ();
			logMessage ("checkPwdHash begin");

			string hashSecret = System.Configuration.ConfigurationSettings.AppSettings["pwdHashSecret"];

			RcStruct objRc = new RcStruct ();
			objRc.rc = 0;

			using (OracleConnection objConn = new OracleConnection(connStr))
			{
				objConn.Open();
				OracleTransaction transaction;

				OracleCommand objCmd = new OracleCommand("ws_check_pwd.check_pw_hash", objConn);
				objCmd.CommandType = CommandType.StoredProcedure;
				transaction = objConn.BeginTransaction ();

				try
				{
					objCmd.Transaction = transaction;

					objCmd.Parameters.Clear();
					logMessage (string.Format ("input: p_acct_id={0}, MSSIDN={1}, pw_hash={2}, salt={3}", AcctID, MSISDN, "CENSORED", salt));
					objCmd.Parameters.Add("p_acct_id"   , OracleType.VarChar).Value = dbNlv(AcctID);
					objCmd.Parameters.Add("p_mssidn"    , OracleType.VarChar).Value = dbNlv(MSISDN);
					objCmd.Parameters.Add("p_test_hash" , OracleType.VarChar).Value = dbNlv(pwdHash);
					objCmd.Parameters.Add("p_salt"      , OracleType.VarChar).Value = dbNlv(salt);
					objCmd.Parameters.Add("p_secret"    , OracleType.VarChar).Value = dbNlv(hashSecret);
					objCmd.Parameters.Add("p_result"    , OracleType.Number).Direction = ParameterDirection.Output;

					objCmd.ExecuteNonQuery();

					string rc = objCmd.Parameters["p_result"].Value.ToString();
					logMessage (string.Format ("command output: p_result={0}", rc));

					objRc.rc  = Int32.Parse(rc);
					string errMsg;


					switch (objRc.rc)
					{
						case -1:
							errMsg = "no account for mssidn";
							break;

						case -2:
							errMsg = "invalid account id";
							break;

						case 1:
							errMsg = "password is ok";
							break;

						case 0:
							errMsg = "password is not ok";
							break;

						default:
							errMsg = "Unexpected errorcode " + rc + ". See to stored procedure for more details.";
							break;
					}
					objRc.msg = errMsg;

					transaction.Commit();
				}

				catch (Exception e)
				{
					transaction.Rollback();
					logError ("checkPwdHash failed" + objRc.ToString(), e);
				}

				finally
				{
					objConn.Close();
				}
			}

			
			logMessage ("checkPwdHash end: " + objRc.ToString());
			return objRc;
		}
	}
}
