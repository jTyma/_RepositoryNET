using System;
using System.Data;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;
using System.Data.OracleClient;
using System.Globalization;

namespace SapWS2
{
    /// <summary>
    /// Rozhrani poskytujici udaje o zakaznikovi, zejmena o predpacenem zakaznikovi.
    /// Urceno pro datovy prepaid.
    /// 
    /// Ale nakonec nejen pro nej, nebot u postbaidu to rika zakaznikovi jaky je jeho 
    /// aktualni dluh. Uff.
    /// 
    /// </summary>
    [WebService(Namespace = "http://b-oss.ufon.cz/internalapi/prepaidinfo")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class PrepaidInfo : System.Web.Services.WebService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("pwGen");

        [WebMethod]
        public void getMSISDNProfile(string msisdn, string subsId, 
            out string acctType, out int acctId, out string acctDebtAfterDue,
            out string outMsisdn,
            out string subsState, out string tarifId, out string tarifDesc,
            out string actualCredit,
            out string lastCreditTransDate, out string lastCreditTransAmount,
            out string lastDebitTransDate, out string lastDebitTransAmount,
            out int subsUsed,
            out int rc, out string msg)
        {
            log.Info(string.Format("getMSISDNProfile msisdn={0}, subsId={1}", msisdn, subsId));
            msg = null;
            rc = getProfile(msisdn, subsId, out acctType, out acctId, out outMsisdn, out subsState, out tarifId, out tarifDesc,
                out actualCredit, out lastCreditTransDate, out lastCreditTransAmount, out lastDebitTransDate, out lastDebitTransAmount,
                out subsUsed);

            //rc = 0;

            // zalogovat vysledeks
            log.Debug(
                string.Format(
                "getMSISDNProfile result: acctType={0}, acctId={1}, outMsisdn={2}, subsState={3}, tarifId={4}, tarifDesc={5}, actualCredit={6}, lastCreditTransDate={7}, lastCreditTransAmount={8}, lastDebitTransDate={9}, lastDebitTransAmount={10}, subsUsed={11}",
                acctType, acctId, outMsisdn, subsState, tarifId, tarifDesc, actualCredit, lastCreditTransDate, lastCreditTransAmount, lastDebitTransDate, lastDebitTransAmount, subsUsed
                ));

            // pohhledat pripadny dluh u POST paidoveho zakaznika
            acctDebtAfterDue = null;
            if ((rc == 0) && ("POST".Equals(acctType)))
            {
                string subMsg = null;
                int subRc = getPrepaidAccountDue(acctId, out acctDebtAfterDue, out subMsg);
                if (subRc != 0)
                {
                    acctDebtAfterDue = null;
                }

            }

            log.Info(string.Format("getMSISDNProfile done rc={0}, msg={1}", rc, msg));
        }

        private int getProfile ( string msisdn, string subsId,    
            out string  acctType, out int acctId, out string outMsisdn, out string subsState,
            out string tarifId, out string tarifDesc, out string actualCredit,
            out string lastCreditTransDate, out string lastCreditTransAmount,
            out string lastDebitransDate, out string lastDebitTransAmount, out int subsUsed
        )
        {

            // resetace
            acctType = tarifId = outMsisdn = subsState = tarifDesc = lastCreditTransDate = lastDebitransDate = null;
            acctId = subsUsed = 0;
            actualCredit = lastCreditTransAmount = lastDebitTransAmount = null;

            int rc = -1;    

            string connStr = getConnectionString();

            try
            {
                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand("mon.WS_FUNCTIONS.GET_PROFILE", objConn);
                    objCmd.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        objCmd.Transaction = transaction;
                        objCmd.Parameters.Clear();
                        objCmd.Parameters.Add("in_MSISDN", OracleType.VarChar).Value = dbNlv(msisdn);
                        objCmd.Parameters.Add("in_SUBS_ID", OracleType.Number).Value = dbNlv(subsId);

                        objCmd.Parameters.Add("p_acct_type", OracleType.VarChar, 10).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_acct_id", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_msisdn", OracleType.VarChar, 15).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_subs_state", OracleType.VarChar, 8).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_tarif_id", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_tarif_desc", OracleType.VarChar, 55).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_actual_credit", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_last_credit_d", OracleType.DateTime).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_last_credit_a", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_last_debit_d", OracleType.DateTime).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_last_debit_a", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_subs_used", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;

                        objCmd.ExecuteNonQuery();

                        rc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());

                        // pokud se to povedlo, nagenerjeme autorizacni retezec
                        if (rc == 0)
                        {
                            acctId = int.Parse(objCmd.Parameters["p_acct_id"].Value.ToString());

                            tarifId = objCmd.Parameters["p_tarif_id"].Value.ToString();
                            actualCredit = objCmd.Parameters["p_actual_credit"].Value.ToString();
                            subsUsed = int.Parse(objCmd.Parameters["p_subs_used"].Value.ToString()); ;

                            acctType = objCmd.Parameters["p_acct_type"].Value.ToString().Trim();
                            outMsisdn = objCmd.Parameters["p_msisdn"].Value.ToString().Trim();
                            subsState = objCmd.Parameters["p_subs_state"].Value.ToString().Trim();
                            tarifDesc = objCmd.Parameters["p_tarif_desc"].Value.ToString().Trim();
                            
                            lastCreditTransDate = formatDate(objCmd.Parameters["p_last_credit_d"].Value);
                            lastDebitransDate = formatDate(objCmd.Parameters["p_last_debit_d"].Value);
                            lastCreditTransAmount = formatNumber(objCmd.Parameters["p_last_credit_a"].Value);
                            lastDebitTransAmount = formatNumber(objCmd.Parameters["p_last_debit_a"].Value);

                        }
                        else
                        {
                            // zalogovat error
                            log.Error("getProfile failed, rc="  + rc);
                        }

                        transaction.Commit();                            
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        rc = -3;
                        log.Error("getProfile", e);
                    }
                    finally
                    {
                        objConn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("getProfile", e);
                rc = -1;
            }

            return rc;
        }

        private int getPrepaidAccountDue(int acctId, out string due, out string msg)
        {
            int rc = 0;
            due = null;
            msg = null;

            string connStr = getConnectionString();

            try
            {
                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;
                    transaction = objConn.BeginTransaction();

                    OracleCommand objCmd = new OracleCommand("mon.ws_functions.get_account_debt", objConn);
                    objCmd.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        objCmd.Transaction = transaction;
                        objCmd.Parameters.Clear();
                        objCmd.Parameters.Add("p_acct_id", OracleType.VarChar).Value = dbNlv(acctId);
                        objCmd.Parameters.Add("p_due", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_msg", OracleType.VarChar, 2048).Direction = ParameterDirection.Output;
                        objCmd.ExecuteNonQuery();

                        int subRc = int.Parse(objCmd.Parameters["p_rc"].Value.ToString());
                        if (rc == 0)
                        {
                            due = formatNumber(objCmd.Parameters["p_due"].Value);
                        }
                        else
                        {
                            string subMsg = objCmd.Parameters["p_msg"].Value.ToString();
                            log.Error(string.Format("getPrepaidAccountDue failed: rc={0}, msg={1}", subRc, subMsg));
                            due = null;
                        }

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        rc = -3;
                        log.Error("getPrepaidAccountDue", e);
                    }
                    finally
                    {
                        objConn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("getPrepaidAccountDue", e);
                rc = -1;
            }

            return rc;
        }
        

        /// <summary>
        /// Kudy do datbaze
        /// </summary>
        /// <returns></returns>
        private static string getConnectionString()
        {
            return System.Configuration.ConfigurationManager.AppSettings["databaseConn"];
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

        /// <summary>
        /// Formatovani cisla - pokud je tak ve formatu NNN.NN, pokud neni, tak vrati null
        /// </summary>
        /// <param name="nbr"></param>
        /// <returns></returns>
        public static string formatNumber(object nbr)
        {
            if ((nbr == null) || (nbr is DBNull))
            {
                return null;
            }

            if (nbr is Decimal)
            {
                return ((Decimal)nbr).ToString("F", CultureInfo.InvariantCulture);
            }

            // sichr
            if (nbr is int)
            {
                return ((int)nbr).ToString("F", CultureInfo.InvariantCulture);
            }

            return nbr.ToString();
        }

        /// <summary>
        /// Formatovani datumu
        /// </summary>
        /// <returns></returns>
        public static string formatDate (object date)
        {
            if ((date == null) || (date is DBNull))
            {
                return null;
            }


            if (date is DateTime)
            {
                return ((DateTime)date).ToString ("yyyy-MM-dd HH:mm:ss");
            }
            return date.ToString();
        }

    }
}
