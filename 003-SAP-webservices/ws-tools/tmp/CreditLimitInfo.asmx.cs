using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Services;
using System.Data.OracleClient;

namespace mobilkom.internalapi
{
	/// <summary>
	/// Summary description for AcctInfo.
	/// </summary>
	[WebService(Namespace = "http://b-oss.ufon.cz/internalapi")]
	public class CreditLimitInfo : WSBase
	{

		protected override  void InitializeComponent()
		{
			_logCategory = "log.CreditLimitInfo";
		}

		[WebMethod]
		public RcCreditLimitInfo getCreditLimitInfo (string MSISDN)
		{
			string connStr = getConnectionString ();
			logMessage ("getCreditLimitInfo begin");

			RcCreditLimitInfo objRc = new RcCreditLimitInfo ();
			objRc.rc = 0;

			using (OracleConnection objConn = new OracleConnection(connStr))
			{
				objConn.Open();
				OracleTransaction transaction;

				OracleCommand objCmd = new OracleCommand("WS_IVR.credit_limit_info", objConn);
				objCmd.CommandType = CommandType.StoredProcedure;
				transaction = objConn.BeginTransaction ();

				try
				{
					objCmd.Transaction = transaction;
					logMessage (string.Format ("command input: MSISDN={0}", MSISDN));

					objCmd.Parameters.Add("p_msisdn" , OracleType.VarChar).Value = dbNlv(MSISDN);
					objCmd.Parameters.Add("p_clp_charge" ,         OracleType.Number).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_enh_credit_limit" ,   OracleType.Number).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_spend_credit_limit" , OracleType.Number).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_result",              OracleType.Number).Direction = ParameterDirection.Output;

					objCmd.ExecuteNonQuery();

					string rc = objCmd.Parameters["p_result"].Value.ToString();
					logMessage (string.Format ("command output: RC={0}", rc));

					if ( rc == "0" )
					{
						transaction.Commit();

						objRc.clpCharge        = currencyNumber(objCmd.Parameters["p_clp_charge"].Value.ToString());
						objRc.enhCreditLimit   = currencyNumber(objCmd.Parameters["p_enh_credit_limit"].Value.ToString());
						objRc.spendCreditLimit = currencyNumber(objCmd.Parameters["p_spend_credit_limit"].Value.ToString());
					}
					else
					{
						transaction.Rollback ();

						objRc.rc  = Int32.Parse(rc);
						string errMsg;
						switch (objRc.rc)
						{
							default:
								errMsg = "Unexpected errorcode " + rc + ". See to stored procedure for more details.";
								break;
						}

						objRc.msg = errMsg;
					}
				}

				catch (Exception e)
				{
					transaction.Rollback();
					logError ("getCreditLimitInfo failed", e);
				}

				finally
				{
					objConn.Close();
				}
			}
			
			logMessage ("getCreditLimitInfo end: " + objRc.ToString());
			return objRc;
		}



		public class RcCreditLimitInfo  
		{
			public int rc;
			public string msg;
			public string clpCharge;
			public string enhCreditLimit;
			public string spendCreditLimit;

			public override string ToString()
			{
				return string.Format(
                    "rc={0}, msg={1}, clpCharge={2}, enhCreditLimit={3}, spendCreditLimit={4}",
					rc, msg, clpCharge, enhCreditLimit, spendCreditLimit
				);
			}
		}

		/// <summary>
		/// z castky v halirich to udela string, v cele castce (vydeli to 100)
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		protected string currencyNumber (string c)
		{

			decimal d = (decimal.Parse (c)) / 100;			
			return String.Format ("{0:0.00}", d);
		}
	}
}
