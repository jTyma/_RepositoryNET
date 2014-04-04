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
    /// Summary description for get_account_by_cert
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class SAP2BOSS : System.Web.Services.WebService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("gsmResourceInventory");
        //private static readonly string schemaPrefix = "gsm.";
        private static readonly string schemaPrefixSap = "sap.";   


        /// <summary>
        /// Kudy do datbaze
        /// </summary>
        /// <returns></returns>
        private static string getConnectionString()
        {
            return System.Configuration.ConfigurationManager.AppSettings["databaseConn"];
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

        protected static object dbNlv(object val)
        {
            return isEmpty(val) ? DBNull.Value : val;
        }

        [WebMethod]
        public void get_cust_id_by_acct_id(int acct_id, out int p_cust_id, out int rc, out string msg)
        {
            rc = -1;
            msg = null;
            p_cust_id = -1;

            string connStr = getConnectionString();

            try
            {

                log.Info(string.Format("get_cust_id_by_acct_id acct_id={0}", acct_id));

                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand(schemaPrefixSap + "crmws_functions.get_cust_id_by_acct_id", objConn);
                    objCmd.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        objCmd.Transaction = transaction;
                        objCmd.Parameters.Clear();
                        objCmd.Parameters.Add("p_acct_id", OracleType.Number).Value = dbNlv(acct_id);
                        objCmd.Parameters.Add("p_cust_id", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_msg", OracleType.VarChar, 2048).Direction = ParameterDirection.Output;
                        objCmd.ExecuteNonQuery();

                        rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());
                        msg = objCmd.Parameters["p_msg"].Value.ToString();
                        p_cust_id = int.Parse(objCmd.Parameters["p_cust_id"].Value.ToString());

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        rc = -3;
                        log.Error("get_cust_id_by_acct_id", e);
                    }
                    finally
                    {
                        objConn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("get_cust_id_by_acct_id", e);
                rc = -1;
            }        
        }

         [WebMethod]
        public void set_contact_email(int p_cust_id, string p_email, out int rc, out string msg)
        {
            rc = -1;
            msg = null;

            string connStr = getConnectionString();

            try
            {

                log.Info(string.Format("set_contact_email p_cust_id={0} , p_email={1}", p_cust_id, p_email));

                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand(schemaPrefixSap + "crmws_functions.set_contact_email", objConn);
                    objCmd.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        objCmd.Transaction = transaction;
                        objCmd.Parameters.Clear();
                        objCmd.Parameters.Add("p_cust_id", OracleType.Number).Value = dbNlv(p_cust_id);
                        objCmd.Parameters.Add("p_email", OracleType.VarChar).Value = dbNlv(p_email);
                        objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_msg", OracleType.VarChar, 2048).Direction = ParameterDirection.Output;
                        objCmd.ExecuteNonQuery();

                        rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());
                        msg = objCmd.Parameters["p_msg"].Value.ToString();

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        rc = -3;
                        log.Error("set_contact_email", e);
                    }
                    finally
                    {
                        objConn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("set_contact_email", e);
                rc = -1;
            }
        }



        [WebMethod]
        public void set_contact_psc(int cust_id, string psc, out int rc, out string msg)
        {
            rc = -1;
            msg = null;

            string connStr = getConnectionString();

            try
            {
                log.Info(string.Format("set_contact_psc cust_id={0}, psc={1}", cust_id.ToString(), psc));

                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand(schemaPrefixSap + "crmws_functions.set_contact_psc", objConn);
                    objCmd.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        objCmd.Transaction = transaction;
                        objCmd.Parameters.Clear();
                        objCmd.Parameters.Add("p_cust_id", OracleType.Number).Value = dbNlv(cust_id);
                        objCmd.Parameters.Add("p_psc", OracleType.VarChar).Value = dbNlv(psc);
                        objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_msg", OracleType.VarChar, 2048).Direction = ParameterDirection.Output;
                        //
                        objCmd.ExecuteNonQuery();

                        rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());
                        msg = objCmd.Parameters["p_msg"].Value.ToString();

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        rc = -3;
                        log.Error("set_contact_psc: ", e);
                    }
                    finally
                    {
                        objConn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("set_contact_psc: ", e);
                rc = -1;
            }
        }


        [WebMethod]
        public void set_cust_type(int cust_id, string cust_type, out int rc, out string msg)
        {
            rc = -1;
            msg = null;

            string connStr = getConnectionString();

            try
            {
                log.Info(string.Format("set_cust_type cust_id={0}, cust_type={1}", cust_id, cust_type));

                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand(schemaPrefixSap + "crmws_functions.set_cust_type", objConn);
                    objCmd.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        objCmd.Transaction = transaction;
                        objCmd.Parameters.Clear();
                        objCmd.Parameters.Add("p_cust_id", OracleType.Number).Value = dbNlv(cust_id);
                        objCmd.Parameters.Add("p_cust_type", OracleType.VarChar).Value = dbNlv(cust_type);
                        objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_msg", OracleType.VarChar, 2048).Direction = ParameterDirection.Output;
                        objCmd.ExecuteNonQuery();

                        rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());
                        msg = objCmd.Parameters["p_msg"].Value.ToString();

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        rc = -3;
                        log.Error("set_contact_type", e);
                    }
                    finally
                    {
                        objConn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("set_contact_type", e);
                rc = -1;
            }
        }


        [WebMethod]
        public void set_cust_cert_nbr(int p_cust_id, int p_cert_type, string p_cert_nbr, out int rc, out string msg)
        {
            rc = -1;
            msg = null;

            string connStr = getConnectionString();

            try
            {
                log.Info(string.Format("set_cust_cert_nbr p_cust_id={0} p_cert_type={0} p_cert_nbr={0}", p_cust_id, p_cert_type, p_cert_nbr));

                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand(schemaPrefixSap + "crmws_functions.update_cust_cert", objConn);
                    objCmd.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        objCmd.Transaction = transaction;
                        objCmd.Parameters.Clear();
                        objCmd.Parameters.Add("p_cust_id", OracleType.Number).Value = dbNlv(p_cust_id);
                        objCmd.Parameters.Add("p_cert_type", OracleType.Number).Value = dbNlv(p_cert_type);
                        objCmd.Parameters.Add("p_cert_nbr", OracleType.VarChar).Value = dbNlv(p_cert_nbr);
                        objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_msg", OracleType.VarChar, 2048).Direction = ParameterDirection.Output;
                        objCmd.ExecuteNonQuery();

                        rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());
                        msg = objCmd.Parameters["p_msg"].Value.ToString();

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        rc = -3;
                        log.Error("set_cust_cert_nbr", e);
                    }
                    finally
                    {
                        objConn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("get_cust_id_by_acct_id", e);
                rc = -1;
            }
        }
    }
}
