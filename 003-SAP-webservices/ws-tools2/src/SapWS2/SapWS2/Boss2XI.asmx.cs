using System;
using System.Data;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.Globalization;


namespace SapWS2
{
    /// <summary>
    /// Úkol z CEMFU:
    ///    Vytvoøení WS pro SAP - Invoices a historie služeb a parametrù
    ///    (asi ID 12746)
    /// 
    /// a asi se sem casem nabali dalsi veci
    /// </summary>
    [WebService(Namespace = "http://b-oss.ufon.cz/internalapi/boss2xi")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class Boss2XI : System.Web.Services.WebService
    {
        // logovadlo
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("SapWS2");

        private static readonly string schemaPrefixMain = "";

        private enum SelectMode { ALL, DEBT, PAY };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="crmId">id zakaznika v SAPu</param>
        /// <param name="acctId">accoutn id v BOSSu</param>
        /// <param name="listType">typ vypisu (A ... vsechny, D ... nezaplacene, P ... zaplacene) - default (kdyz null) = A</param>
        /// <param name="dateFrom">od kdy ve formatu YYYY-MM-DD</param>
        /// <param name="dateTo">od kdy ve formatu YYYY-MM-DD</param>
        /// <param name="limit">omezeni na pocet faktur ve vystupu</param>
        /// <param name="callAppl">volajici aplikace (autorizace)</param>
        /// <param name="auth">autorizacni retezec</param>
        /// <param name="rc">vysledek (0 Ok)</param>
        /// <param name="msg">popis pripadne chyby</param>
        /// <param name="resultSize">velikost vystupniho pole</param>
        /// <param name="resultLabel">pripadny popis vystupniho pole</param>
        /// <param name="invoiceList">vystupni pole s jednotlivymi udaji o fakture</param>
        [WebMethod]
        public void invoces(
            string crmId, string acctId, 
            string listType, string dateFrom, string dateTo, string limit,
            string callAppl, string auth,
            out int rc, out string msg,
            out int resultSize, out string resultLabel, out InvoiceInfo[] invoiceList
        )
        {
            log.Info(string.Format("invocesList begin, crmId={0}, acctId={1}, dateFrom={2}, dateTo={3}, limit={4}, callAppl={5}", crmId, acctId, dateFrom, dateTo, limit, callAppl));

            rc = 0;
            msg = null;
            resultSize = 0;
            resultLabel = null;
            invoiceList = null;

            SelectMode selMode;

            // jake faktury vybrat
            switch (listType)
            {
                case "A":
                    selMode = SelectMode.ALL;
                    break;

                case "D":
                    selMode = SelectMode.DEBT;
                    break;

                case "P":
                    selMode = SelectMode.PAY;
                    break;

                default:
                    if (! isEmpty(listType))
                    {
                        rc = -1;
                        msg = "invalid select mode";
                        return;
                    }
                    selMode = SelectMode.ALL;
                    break;
            }

            // omezeni na datumy
            string filterFrom = isEmpty(dateFrom) ? null : dateFrom;
            string filterTo   = isEmpty(dateTo) ? null : dateTo;

            // kolik jich vybrat
            int limitCount = -1;
            if ( ! isEmpty (limit))
            {
                limitCount = Convert.ToInt32(limit);
            }

            // nepripustna kombinace filtru
            if ( (limitCount >=0) && ((filterFrom != null) || (filterTo != null)) )
            {
                rc = -2;
                msg = "filter combinations";
                return;
            }


            string connStr = getConnectionString();

            try
            {
                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;

                    OracleCommand objCmd = new OracleCommand(schemaPrefixMain + "crmws_functions.get_customer_invoces", objConn);
                    objCmd.CommandType = CommandType.StoredProcedure;
                    transaction = objConn.BeginTransaction();

                    try
                    {
                        objCmd.Transaction = transaction;

                        objCmd.Parameters.Clear();
                        objCmd.Parameters.Add("p_acct_id", OracleType.Number).Value = dbNlv(acctId);
                        objCmd.Parameters.Add("p_crm_id", OracleType.VarChar).Value = dbNlv(crmId);
                        objCmd.Parameters.Add("p_xml_txt", OracleType.Clob).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_debt", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_msg", OracleType.VarChar, 500).Direction = ParameterDirection.Output;

                        objCmd.ExecuteNonQuery();

                        rc = Convert.ToInt32(objCmd.Parameters["p_rc"].Value);
                        msg = objCmd.Parameters["p_msg"].Value.ToString();

                        // povedlo se, precteme si data
                        if (rc == 0)
                        {
                            int debt = Convert.ToInt32(objCmd.Parameters["p_debt"].Value);
                            OracleLob orc = (OracleLob)objCmd.Parameters["p_xml_txt"].Value;
                            string xmlTxt = orc.Value.ToString();

                            // sichr prazdne XML je treba nahradit NULLem, jinak se to nenabinduje
                            if ((xmlTxt != null) && (xmlTxt.Length == 0))
                            {
                                xmlTxt = null;
                            }

                            System.IO.StringReader sReader = new System.IO.StringReader(xmlTxt);
                            System.Data.DataSet dSet = new DataSet();
                            dSet.ReadXml(sReader);

                            if ((dSet.Tables != null) && (dSet.Tables.Count > 0))
                            {
                                DataTable dt = dSet.Tables[0];

                                List<InvoiceInfo> lst = new List<InvoiceInfo>();
                                bool showIt;

                                // omezeni na pocet ve vystupu
                                int fromIdx = -1;
                                if (limitCount > 0)
                                {
                                    fromIdx = dt.Rows.Count - limitCount;
                                }

                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    object[] row = dt.Rows[i].ItemArray;
                                    InvoiceInfo item = new InvoiceInfo();

                                    // nakopirivat to
                                    item.billNbr = Convert.ToString(row[dt.Columns.IndexOf("nbr")]);
                                    decimal amount = Convert.ToDecimal(row[dt.Columns.IndexOf("due")]);
                                    decimal debtAmount = Convert.ToDecimal(row[dt.Columns.IndexOf("debt")]);
                                    item.invoiceDate = Convert.ToString(row[dt.Columns.IndexOf("bill_date")]);
                                    item.dueDate = Convert.ToString(row[dt.Columns.IndexOf("debt_date")]);

                                    // jestli ho tam davat
                                    showIt = true;
                                    int invDebt = Convert.ToInt32(item.debtAmount);
                                    if ( 
                                         ((debtAmount != 0) && (selMode == SelectMode.PAY )) ||
                                         ((debtAmount == 0) && (selMode == SelectMode.DEBT))
                                    )
                                    {
                                        showIt = false;
                                    }

                                    // pripadne omezeni na pocet faktur
                                    if (showIt && (fromIdx >= 0) && (i < fromIdx))
                                    {
                                        showIt = false;
                                    }

                                    // pripadne omezeni na datum
                                    if (showIt && (filterFrom != null) && (filterFrom.CompareTo(item.invoiceDate) > 0))
                                    {
                                        showIt = false;
                                    }

                                    if (showIt && (filterTo != null) && (filterTo.CompareTo(item.invoiceDate) < 0))
                                    {
                                        showIt = false;
                                    }


                                    // prevest z haliru na koruny
                                    amount      = amount / 100;
                                    debtAmount  = debtAmount / 100;

                                    // prevest na string do vystupu
                                    item.amount = amount.ToString("F", CultureInfo.InvariantCulture);
                                    item.debtAmount = debtAmount.ToString("F", CultureInfo.InvariantCulture);

                                    if (showIt)
                                    {
                                        lst.Add(item);
                                    }
                                }

                                // otocime to, aby byly nejcerstvejsi nahore
                                lst.Reverse();

                                invoiceList = lst.ToArray();
                                resultSize = lst.Count;

                                resultLabel = null;

                                // sestavit popis kolekce dle zadani
                                if (selMode == SelectMode.DEBT)
                                {
                                    decimal debtTotal = 0;
                                    int invCount = 0;
                                    List<string> nbrList = new List<string> ();
                                    foreach (InvoiceInfo inv in invoiceList)
                                    {
                                        debtTotal = debtTotal + Convert.ToDecimal(inv.debtAmount);
                                        invCount++;
                                        nbrList.Add(inv.billNbr);
                                    }

                                    string nbrs = string.Join(", ", nbrList.ToArray());

                                    // cislovky a cstina
                                    string txtLng = "nezaplacených faktur";
                                    if (invCount == 1) txtLng = "nezaplacená faktura";
                                    if ((invCount >= 2) && (invCount <= 4)) txtLng = "nezaplacené faktury";

                                    resultLabel = string.Format("{0} {1} ve výši celkem {2} Kè ({3})", invCount, txtLng, debtTotal, nbrs);
                                }
                            }
                            

                        }

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        log.Error(e);
                        rc = -2;
                        msg = "fail:" + e.Message;
                    }

                    finally
                    {
                        objConn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                rc = -3;
                log.Error(e);
            }

            log.Info(string.Format("invocesList end, rc={0},msg={1}", rc, msg));
        }

        /// <summary>
        ///   Popis jedne faktury
        /// </summary>
        public class InvoiceInfo
        {
            public string billNbr;        // cislo fakutury
            public string invoiceDate;    // datum vystaveni
            public string dueDate;        // datum splatnosti
            public string amount;        // castka
            public string debtAmount;    // dluzna castka
        }

        /// <summary>
        /// Historie produktu
        /// </summary>
        /// <param name="prodId">id produktu</param>
        /// <param name="callAppl">volajici aplikace (autorizace)</param>
        /// <param name="auth">autorizacni retezec</param>
        /// <param name="rc">vysledek (0 Ok)</param>
        /// <param name="msg">popis pripadne chyby</param>
        /// <param name="resultStateCount">kolik zaznamu o historii stavu se naslo</param>
        /// <param name="prodStateList">jednotlive zazvamy o hostorii stavu</param>
        /// <param name="resultParamHisCount">kolik parametru se naslo</param>
        /// <param name="paramHisList">jednotlive zaznamy o parametrech</param>
        [WebMethod]
        public void prodHistory (
            int prodId, 
            string callAppl, string auth,
            out int rc, out string msg,
            out int resultStateCount, out ProdStateInfo[] prodStateList,
            out int resultParamHisCount, out ParamHisInfo[] paramHisList
        )
        {
            rc = -1;
            msg = null;
            resultStateCount = 0;
            prodStateList = null;
            resultParamHisCount = 0;
            paramHisList = null;

            log.Info(string.Format("prodHistory begin, prodId={0}", prodId));

            string connStr = getConnectionString();

            try
            {
                using (OracleConnection objConn = new OracleConnection(connStr))
                {
                    objConn.Open();
                    OracleTransaction transaction;

                    OracleCommand objCmd = new OracleCommand(schemaPrefixMain + "crmws_functions.get_prod_history", objConn);
                    objCmd.CommandType = CommandType.StoredProcedure;
                    transaction = objConn.BeginTransaction();

                    try
                    {
                        objCmd.Transaction = transaction;

                        objCmd.Parameters.Clear();
                        objCmd.Parameters.Add("p_prod_id", OracleType.Number).Value = prodId;
                        objCmd.Parameters.Add("p_curr_state", OracleType.Cursor).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_curr_his", OracleType.Cursor).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_rc", OracleType.Number).Direction = ParameterDirection.Output;
                        objCmd.Parameters.Add("p_msg", OracleType.VarChar, 500).Direction = ParameterDirection.Output;

                        objCmd.ExecuteNonQuery();

                        rc = Convert.ToInt32(objCmd.Parameters["p_rc"].Value);
                        msg = objCmd.Parameters["p_msg"].Value.ToString();

                        // povedlo se, precteme si tabulky
                        if (rc == 0)
                        {
                            OracleDataAdapter adapter = new OracleDataAdapter(objCmd);
                            DataSet dSet = new DataSet();
                            adapter.Fill(dSet);

                            // prvni tabulka s cursorem - historie statusu
                            if ((dSet.Tables != null) && (dSet.Tables.Count > 0))
                            {
                                DataTable dt = dSet.Tables[0];

                                List<ProdStateInfo> lst = new List<ProdStateInfo>();
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    object[] row = dt.Rows[i].ItemArray;
                                    ProdStateInfo item = new ProdStateInfo();

                                    // nakopirivat to
                                    item.state     = Convert.ToString(row[dt.Columns.IndexOf("prod_state")]);
                                    item.stateName = Convert.ToString(row[dt.Columns.IndexOf("prod_state_name")]);
                                    item.dateFrom  = Convert.ToString(row[dt.Columns.IndexOf("date_from")]);
                                    item.dateTo    = Convert.ToString(row[dt.Columns.IndexOf("date_to")]);

                                    lst.Add(item);
                                }

                                prodStateList = lst.ToArray();
                                resultStateCount = lst.Count;
                            }

                            // druha tabulka s cursorem - historie produktu
                            if ((dSet.Tables != null) && (dSet.Tables.Count > 1))
                            {
                                DataTable dt = dSet.Tables[1];

                                List<ParamHisInfo> lst = new List<ParamHisInfo>();

                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    object[] row = dt.Rows[i].ItemArray;
                                    ParamHisInfo item = new ParamHisInfo();

                                    item.attrId   = Convert.ToInt32(row[dt.Columns.IndexOf("id")]);
                                    item.name     = Convert.ToString(row[dt.Columns.IndexOf("attr_name")]);
                                    item.value    = Convert.ToString(row[dt.Columns.IndexOf("text_value")]);
                                    item.dateFrom = Convert.ToString(row[dt.Columns.IndexOf("date_from")]);
                                    item.dateTo   = Convert.ToString(row[dt.Columns.IndexOf("date_to")]);

                                    lst.Add(item);
                                }

                                paramHisList = lst.ToArray();
                                resultParamHisCount = lst.Count;
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        log.Error(e);
                        rc = -2;
                        msg = "fail:" + e.Message;
                    }

                    finally
                    {
                        objConn.Close();
                    }
                }
            }
            catch (Exception e)
            {
                rc = -3;
                log.Error(e);
            }

            log.Info(string.Format("prodHistory end, rc={0},msg={1}", rc, msg));
        }        

        /// <summary>
        /// historie produktu
        /// </summary>
        public class ProdStateInfo
        {
            public string state;
            public string stateName;  // pridano
            public string dateFrom;
            public string dateTo;
        }

        /// <summary>
        /// historie paramtru produktu
        /// </summary>
        public class ParamHisInfo
        {
            public int attrId;   // pridano
            public string name;
            public string value;
            public string dateFrom;
            public string dateTo;
        }


        /// <summary>
        /// connection string do oraclu
        /// </summary>
        /// <returns></returns>
        protected string getConnectionString()
        {
            return System.Configuration.ConfigurationManager.AppSettings["databaseConn"];
        }

        /// <summary>
        /// osetreni NULL ve vstupu
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        protected object dbNlv(object val)
        {
            return isEmpty(val) ? DBNull.Value : val;
        }

        /// <summary>
        /// pomocna funkce na praznou hodnotu
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        protected bool isEmpty(object val)
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
