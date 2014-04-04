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
	/// Summary description for Service1.
	/// </summary>
	[WebService(Namespace = "http://b-oss.ufon.cz/internalapi")]
	public class WSTool : WSBase
	{

		protected override  void InitializeComponent()
		{
			_logCategory = "log.ToolSrv";
		}

		[WebMethod]
		public RcStruct SetSubsAttributes(AttrCommandInfo[]  cmd)
		{
			string connStr = getConnectionString ();
			logMessage ("setSubsAttributes begin");


			RcStruct objRc = new RcStruct ();
			objRc.rc = 0;

			using (OracleConnection objConn = new OracleConnection(connStr))
			{
				objConn.Open();
				OracleTransaction transaction;

				OracleCommand objCmd = new OracleCommand("WS_SET_ATTR.set_attr", objConn);
				objCmd.CommandType = CommandType.StoredProcedure;
				transaction = objConn.BeginTransaction ();

				try
				{
					objCmd.Transaction = transaction;

					int i;
					bool isOk = true;

					for (i=0; i< cmd.Length; i++)
					{
						AttrCommandInfo cmdItem = cmd[i];	

						objCmd.Parameters.Clear();
						//WS_SET_ATTR.set_attr (25995, 'STYPE', '1');
						logMessage (string.Format ("command input: MSSIDN={0}, ATTR_NAME={1}, ATTR_VALUE={2}", cmdItem.msisdn, cmdItem.attr_name,cmdItem.attr_value));
						objCmd.Parameters.Add("MSSIDN"    , OracleType.VarChar).Value = cmdItem.msisdn;
						objCmd.Parameters.Add("ATTR_NAME" , OracleType.VarChar).Value = cmdItem.attr_name;
						objCmd.Parameters.Add("ATTR_VALUE", OracleType.VarChar).Value = cmdItem.attr_value;
						objCmd.Parameters.Add("RC", OracleType.Number).Direction = ParameterDirection.Output;

						objCmd.ExecuteNonQuery();

						string rc = objCmd.Parameters["RC"].Value.ToString();
						logMessage (string.Format ("command output: RC={0}", rc));

						if ( rc != "0" )
						{
							isOk = false;
							objRc.rc  = Int32.Parse(rc);

							string errMsg;

							switch (objRc.rc)
							{
								case 1:
									errMsg = "subs id not found";
									break;

								case 2:
									errMsg = "invalid attribute name";
									break;

								case 3:
									errMsg = "invalid attribute value";
									break;

								default:
									errMsg = "Unexpected errorcode " + rc + ". See to stored procedure for more details.";
									break;
							}

							objRc.msg = "FAILED set '" + cmdItem.attr_name + "' to value '" + cmdItem.attr_value + "' on msisdn '" + cmdItem.msisdn + "'";
							break;
						}
					}

					if ( isOk )
					{
						transaction.Commit();
					}
					else
					{
						transaction.Rollback ();
					}
				}

				catch (Exception e)
				{
					transaction.Rollback();
					logError ("SetSubsAttributes failed", e);
				}

				finally
				{
					objConn.Close();
				}
			}

			
			logMessage ("setSubsAttributes end: " + objRc.ToString());
			return objRc;
		}

		[WebMethod]
		public RcStruct UpdateSubsAttributes(string msisdn, string sType, string sCode, string sapId, string recWho, string recMe, string recSCode )
		{
			logMessage (string.Format ("UpdateSubsAttributes begin: msisdn={0}, sType={1}, sCode={2}, sapId={3}, recWho={4}, recMe={5}, recSCode={6}", 
				                                                    msisdn,     sType,     sCode,     sapId,     recWho,     recMe,     recSCode));

			AttrCommandInfo cmd;
			ArrayList cmdList = new ArrayList ();

			if ( ! isEmpty (sType) )
			{
				cmd = new AttrCommandInfo ();
				cmd.msisdn = msisdn;
				cmd.attr_name  = "STYPE";
				cmd.attr_value = sType;
				cmdList.Add (cmd);
			}

			if ( ! isEmpty (sCode) )
			{
				cmd = new AttrCommandInfo ();
				cmd.msisdn = msisdn;
				cmd.attr_name  = "SCODE";
				cmd.attr_value = sCode;
				cmdList.Add (cmd);
			}

			if ( ! isEmpty (sapId) )
			{
				cmd = new AttrCommandInfo ();
				cmd.msisdn = msisdn;
				cmd.attr_name  = "SAPID";
				cmd.attr_value = sapId;
				cmdList.Add (cmd);
			}

			if ( ! isEmpty (recWho) )
			{
				cmd = new AttrCommandInfo ();
				cmd.msisdn = msisdn;
				cmd.attr_name  = "REC_WHO";
				cmd.attr_value = recWho;
				cmdList.Add (cmd);
			}

			if ( ! isEmpty (recMe) )
			{
				cmd = new AttrCommandInfo ();
				cmd.msisdn = msisdn;
				cmd.attr_name  = "REC_ME";
				cmd.attr_value = recMe;
				cmdList.Add (cmd);
			}

			if ( ! isEmpty (recSCode) )
			{
				cmd = new AttrCommandInfo ();
				cmd.msisdn = msisdn;
				cmd.attr_name  = "REC_SCODE";
				cmd.attr_value = recSCode;
				cmdList.Add (cmd);
			}

			RcStruct rc;
			if ( cmdList.Count > 0 )
			{
				AttrCommandInfo[] cmdArray = (AttrCommandInfo[]) cmdList.ToArray(typeof(AttrCommandInfo));
				rc = SetSubsAttributes (cmdArray);
			}
			else
			{
				rc = new RcStruct ();
				rc.rc = 0;
			}
			
			logMessage ("UpdateSubsAttributes end: " + rc.ToString());
			return rc;
		}

	}

	// attr update struce
	public class AttrCommandInfo
	{
		public string msisdn;
		public string attr_name;
		public string attr_value;
	}

}
