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
    /// Summary description for ActivationParameters
    /// </summary>
    [WebService(Namespace = "http://b-oss.ufon.cz/internalapi/acparams")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class ActivationParameters : System.Web.Services.WebService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("ACP");
        private static readonly string schemaPrefix = "gemini.";
        private static readonly string connStr = System.Configuration.ConfigurationManager.AppSettings["databaseConn"];
        private static readonly bool testOnly = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["setActivationParameters.testOnly"]) == 1;

        [WebMethod]
        public void setActivationParameters (
            string spaOrderId, string sapProductId, string ownHardware, ParamDef[] parameters, 
            out int rc, out string msg

        )
        {
            rc = -1;
            msg = null;

            // pouze testujeme
            if (testOnly)
            {
                log.Info(string.Format(
                    "setActivationParameters (fake mode, no database actions) spaOrderId={0}, sapProductId={1}, noHardware={2}, params={3}",
                    spaOrderId, sapProductId, ownHardware, parameters));

                rc = 0;
                msg = null;
                return;
            }

            try
            {
                log.Info(string.Format(
                    "setActivationParameters spaOrderId={0}, sapProductId={1}, noHardware={2}, params={3}",
                    spaOrderId, sapProductId, ownHardware, parameters));

                if ((parameters == null) || (parameters.Length == 0))
                {
                    throw new ErrException(10, "no parameters array specified");
                }

                bool isErr = false;
                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    // projedeme jednotlive parametry a zavolame funkci
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        try
                        {
                            //OracleCommand objCmd = new OracleCommand(schemaPrefix + "apply_clp_setting_wssap.create_activation_par_req", objConn);
                            OracleCommand objCmd = new OracleCommand(schemaPrefix + "apply_clp_setting_wssap.create_activation_par_req", objConn);
                            
                            objCmd.CommandType = CommandType.StoredProcedure;

                            objCmd.Transaction = transaction;
                            objCmd.Parameters.Clear();
                            objCmd.Parameters.Add("p_spa_order_nbr", OracleType.VarChar).Value = dbNlv(spaOrderId);
                            objCmd.Parameters.Add("p_product_id_sap", OracleType.VarChar).Value = dbNlv(sapProductId);
                            objCmd.Parameters.Add("p_own_hw", OracleType.VarChar).Value = dbNlv(ownHardware);
                            objCmd.Parameters.Add("p_par_type", OracleType.VarChar).Value = dbNlv(parameters[i].paramType);
                            objCmd.Parameters.Add("p_par_value", OracleType.VarChar).Value = dbNlv(parameters[i].paramValue);
                            objCmd.Parameters.Add("p_par_expiration", OracleType.DateTime).Value = dbNlv(parameters[i].paramExpiration);

                            objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                            objCmd.Parameters.Add("p_msg", OracleType.VarChar, 500).Direction = ParameterDirection.Output;

                            objCmd.ExecuteNonQuery();

                            rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());

                            // nepovedlo se to
                            if (rc != 0)
                            {
                                // zalogovat error
                                msg = objCmd.Parameters["p_msg"].Value.ToString();
                                log.Error(string.Format("create_activation_par_req, rc={0}, msg={1}", rc, msg));
                                isErr = true;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error("create_activation_par_req exception", e);
                            isErr = true;
                            msg = e.Message;
                            rc = -2;
                        }

                        // chyba, konec
                        if (isErr)
                        {
                            break;
                        }
                    }

                    if (isErr)
                    {
                        transaction.Rollback();
                    }
                    else
                    {
                        transaction.Commit();
                    }

                    objConn.Close();
                }

            }
            catch (Exception e)
            {
                if (e is ErrException)
                {
                    rc = ((ErrException)e).rc;
                    msg = ((ErrException)e).msg;

                    log.Error(string.Format(
                        "setActivationParameters error: {0}, {1}",
                        rc, e.Message));
                }
                else
                {
                    rc = -2;
                    msg = e.Message;
                    log.Error("setActivationParameters exception", e);
                }


            }

            log.Info(string.Format(
                "setActivationParameters finished: rc={0}, msg={1}",
                rc, msg));
        }

        private class ErrException: Exception
        {
            public int rc;
            public string msg;

            public ErrException (int rc, string msg): base(string.Format("rc={0}, msg={1}", rc, msg))
            {
                this.rc = rc;
                this.msg = msg;
            }
        }

        protected static object dbNlv(object val)
        {
            return isEmpty(val) ? DBNull.Value : val;
        }

        /// <summary>
        /// Pomocná funkce na testování prázdnosti
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
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

    public class ParamDef
    {
        public string paramType;
        public string paramValue;
        public DateTime paramExpiration;

        public string toString()
        {
            return string.Format("paramType={0}, paramValue={1}, paramExpiration={2}", this.paramType, this.paramValue, this.paramExpiration);
        }
    }
}
