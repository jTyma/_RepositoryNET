using System;
using System.Data;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;
using System.Data.OracleClient;

namespace SapWS2
{
    /// <summary>
    /// GSMResourceInventory - nejake funkce souvisejici s akci GSM pro Starlife
    /// urcene pro SPA formular
    /// </summary>
    [WebService(Namespace = "http://b-oss.ufon.cz/internalapi/gsmresourceinventory")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class GSMResourceInventory : System.Web.Services.WebService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("gsmResourceInventory");
        private static readonly string schemaPrefix = "gsm.";
        private static readonly string schemaPrefixSap = "sap.";        
        private static readonly string connStr = System.Configuration.ConfigurationManager.AppSettings["databaseConn"];

        [WebMethod]
        public void GetSimInfoByIccid(
            string IccdId, out string Esn, out string Msisdn, out string State,
            out int rc, out string msg
        )
        {
            rc = -1;
            msg = null;
            Msisdn = Esn = State = null;

            try
            {
                log.Info(string.Format(
                    "GetSimInfoByIccid IccdId={0}", IccdId));

                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand(schemaPrefix + "sl_resource_inventory.get_sim_info_by_iccid", objConn);
                            
                    objCmd.CommandType = CommandType.StoredProcedure;

                    objCmd.Transaction = transaction;
                    objCmd.Parameters.Clear();
                    objCmd.Parameters.Add("p_iccd_id", OracleType.VarChar).Value = dbNlv(IccdId);

                    objCmd.Parameters.Add("p_esn", OracleType.VarChar, 500).Direction = ParameterDirection.Output;
                    objCmd.Parameters.Add("p_msisdn", OracleType.VarChar, 500).Direction = ParameterDirection.Output;
                    objCmd.Parameters.Add("p_state", OracleType.Char, 1).Direction = ParameterDirection.Output;

                    objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                    objCmd.Parameters.Add("p_msg", OracleType.VarChar, 500).Direction = ParameterDirection.Output;

                    objCmd.ExecuteNonQuery();

                    rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());
                    msg = objCmd.Parameters["p_msg"].Value.ToString();

                    if (rc == 0)
                    {
                        Esn    = objCmd.Parameters["p_esn"].Value.ToString();
                        Msisdn = objCmd.Parameters["p_msisdn"].Value.ToString();
                        State  = objCmd.Parameters["p_state"].Value.ToString();
                    }

                    transaction.Commit();
                    objConn.Close();
                }

            }
            catch (Exception e)
            {
                rc = -2;
                msg = e.Message;
                log.Error("GetSimInfoByIccid exception", e);
            }

            log.Info(string.Format(
                "GetSimInfoByIccid finished: rc={0}, msg={1}, Esn={2}, Msisdn={3}, State={4}",
                rc, msg, Esn, Msisdn, State));
        }

        [WebMethod]
        public void GetSimInfoByMsisdn(
            string Msisdn, out string IccdId, out string Esn, out string State,
            out int rc, out string msg
        )
        {
            rc = -1;
            msg = null;
            IccdId = Esn = State = null;

            try
            {
                log.Info(string.Format(
                    "GetSimInfoByMsisdn Msisdn={0}",Msisdn));

                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand(schemaPrefix + "sl_resource_inventory.get_sim_info_by_msisdn", objConn);

                    objCmd.CommandType = CommandType.StoredProcedure;

                    objCmd.Transaction = transaction;
                    objCmd.Parameters.Clear();
                    objCmd.Parameters.Add("p_msisdn", OracleType.VarChar).Value = dbNlv(Msisdn);

                    objCmd.Parameters.Add("p_esn", OracleType.VarChar, 500).Direction = ParameterDirection.Output;
                    objCmd.Parameters.Add("p_iccid", OracleType.VarChar, 500).Direction = ParameterDirection.Output;
                    objCmd.Parameters.Add("p_state", OracleType.Char, 1).Direction = ParameterDirection.Output;

                    objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                    objCmd.Parameters.Add("p_msg", OracleType.VarChar, 500).Direction = ParameterDirection.Output;

                    objCmd.ExecuteNonQuery();

                    rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());
                    msg = objCmd.Parameters["p_msg"].Value.ToString();

                    if (rc == 0)
                    {
                        Esn = objCmd.Parameters["p_esn"].Value.ToString();
                        IccdId = objCmd.Parameters["p_iccid"].Value.ToString();
                        State = objCmd.Parameters["p_state"].Value.ToString();
                    }
                    transaction.Commit();
                    objConn.Close();
                }

            }
            catch (Exception e)
            {
                rc = -2;
                msg = e.Message;
                log.Error("GetSimInfoByMsisdn exception", e);
            }

            log.Info(string.Format(
                "GetSimInfoByMsisdn finished: rc={0}, msg={1}, Esn={2}, Iccid={3}, State={4}",
                rc, msg, Esn, IccdId, State));
        }

        [WebMethod]
        public void GetAccountByCert (
            string CertValue, string CertType,
            out string AcctId,
            out int rc, out string msg
        )
        {
            rc = -1;
            msg = null;
            AcctId = "";            

            try
            {
                log.Info(string.Format(
                    "GetAccountByCert CertValue={0} CertType={1}", CertValue, CertType));

                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand(schemaPrefixSap + "crmws_functions.get_account_by_cert", objConn);

                    objCmd.CommandType = CommandType.StoredProcedure;

                    objCmd.Transaction = transaction;
                    objCmd.Parameters.Clear();
                    objCmd.Parameters.Add("p_cert_value", OracleType.VarChar).Value = dbNlv(CertValue);
                    objCmd.Parameters.Add("p_cert_type", OracleType.VarChar).Value = dbNlv(CertType);

                    objCmd.Parameters.Add("p_acct_id", OracleType.VarChar, 200).Direction = ParameterDirection.Output;

                    objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                    objCmd.Parameters.Add("p_msg", OracleType.VarChar, 500).Direction = ParameterDirection.Output;

                    objCmd.ExecuteNonQuery();

                    rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());
                    msg = objCmd.Parameters["p_msg"].Value.ToString();
                    AcctId = objCmd.Parameters["p_acct_id"].Value.ToString();
                    transaction.Commit();
                    objConn.Close();
                }

            }
            catch (Exception e)
            {
                rc = -1;
                msg = e.Message;
                log.Error("GetAccountByCert exception", e);
            }

            log.Info(string.Format(
                "GetAccountByCert finished: rc={0}, msg={1}, AcctId={2}",
                rc, msg, AcctId));
        }


        // pomocne funkce

        protected static object dbNlv(object val)
        {
            return isEmpty(val) ? DBNull.Value : val;
        }

        protected static bool isEmpty(object val)
        {
            if (val == null)
            {
                return true;
            }

            if (val is string)
            {
                return (((string)val).Length == 0);
            }

            return false;
        }

    }
}
