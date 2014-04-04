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
	public class AcctInfo : WSBase
	{
		protected override  void InitializeComponent()
		{
			_logCategory = "log.AcctInfo";
		}

		[WebMethod]
		public RcAcctInfo getAcctInfo(string AcctID, string MSISDN)
		{
			string connStr = getConnectionString ();
			logMessage ("getAcctInfo begin");

			RcAcctInfo objRc = new RcAcctInfo ();
			objRc.rc = 0;

			using (OracleConnection objConn = new OracleConnection(connStr))
			{
				objConn.Open();
				OracleTransaction transaction;

				OracleCommand objCmd = new OracleCommand("WS_FUNCTIONS.getAcctInfo", objConn);
				objCmd.CommandType = CommandType.StoredProcedure;
				transaction = objConn.BeginTransaction ();

				try
				{
					objCmd.Transaction = transaction;
					logMessage (string.Format ("command input: CUST_ID={0}, MSISDN={1}", AcctID, MSISDN));
					objCmd.Parameters.Add("p_acct_id", OracleType.Number).Value = dbNlv(AcctID);
					objCmd.Parameters.Add("p_mssidn" , OracleType.VarChar).Value = dbNlv(MSISDN);

					objCmd.Parameters.Add("p_InvNbr" ,            OracleType.VarChar, 100).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_InvAmount" ,         OracleType.VarChar, 100).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_InvDueDate" ,        OracleType.VarChar, 100).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_ActualBalance" ,     OracleType.VarChar, 100).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_PaymentAmt" ,        OracleType.VarChar, 100).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_PaymentDate" ,       OracleType.VarChar, 100).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_BillCycleDateFrom" , OracleType.VarChar, 100).Direction = ParameterDirection.Output;
					objCmd.Parameters.Add("p_BillCycleDateTo" ,   OracleType.VarChar, 100).Direction = ParameterDirection.Output;


					objCmd.Parameters.Add("p_result" , OracleType.Number).Direction = ParameterDirection.Output;

					objCmd.ExecuteNonQuery();

					string rc = objCmd.Parameters["p_result"].Value.ToString();
					logMessage (string.Format ("command output: RC={0}", rc));

					if ( rc == "0" )
					{
						transaction.Commit();

						objRc.invNbr            = objCmd.Parameters["p_InvNbr"].Value.ToString();
						objRc.invAmount         = objCmd.Parameters["p_invAmount"].Value.ToString();
						objRc.invDueDate        = objCmd.Parameters["p_invDueDate"].Value.ToString();
						objRc.actualBalance     = objCmd.Parameters["p_actualBalance"].Value.ToString();
						objRc.paymentAmt        = objCmd.Parameters["p_paymentAmt"].Value.ToString();
						objRc.paymentDate       = objCmd.Parameters["p_paymentDate"].Value.ToString();
						objRc.billCycleDateFrom = objCmd.Parameters["p_billCycleDateFrom"].Value.ToString();
						objRc.billCycleDateTo   = objCmd.Parameters["p_billCycleDateTo"].Value.ToString();

					}
					else
					{
						transaction.Rollback ();

						objRc.rc  = Int32.Parse(rc);
						string errMsg;
						switch (objRc.rc)
						{
							case -1:
								errMsg = "customer not found";
								break;
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
					logError ("AcctInfo failed",e);
				}

				finally
				{
					objConn.Close();
				}
			}

			
			logMessage ("getAcctInfo end: " + objRc.ToString());
			return objRc;
		}

		public class RcAcctInfo  
		{
			public int rc;
			public string msg;
			public string invNbr;
			public string invAmount;
			public string invDueDate;
			public string actualBalance;
			public string paymentAmt;
			public string paymentDate;
			public string billCycleDateFrom;
			public string billCycleDateTo;
		}
	}
}
