using System;
using System.Collections.Generic;
using System.Text;

using SapWS2;
using NUnit.Framework;
using log4net;

namespace SapWS2.UnitTests
{
    [TestFixture()]
    public class WebServices
    {
        private SapWS2.Boss2XI appl = new SapWS2.Boss2XI();
        private int rc;
        private string msg;
        private string acctId = "256";
        private int prodId = 12367;

        [SetUp()]

        public void Init()
        {

            // TODO: dat do konfigurace aby to nebylo natvrdo
            //System.Configuration.ConfigurationManager.AppSettings["databaseConn"] = "Data Source=CC_TEST;User ID=crmws;Password=crmws;";
            Console.WriteLine("acctId: " + acctId);
        }

        [Test]
        public void Test_invoces_all()
        {
            int size;
            string label;
            Boss2XI.InvoiceInfo[] lst;

            Console.WriteLine("=== all data ===");
            appl.invoces(null, acctId, null, null, null, null, null, null, out rc, out msg, out size, out label, out lst);
            Assert.AreEqual(0, rc, msg);
            Assert.AreEqual(lst.Length, size, "size is not length");
            dumpData(lst);
        }

        [Test]
        public void Test_invoces_debt()
        {
            int size;
            string label;
            Boss2XI.InvoiceInfo[] lst;

            Console.WriteLine("=== debt data ===");
            appl.invoces(null, acctId, "D", null, null, null, null, null, out rc, out msg, out size, out label, out lst);
            Assert.AreEqual(0, rc, msg);
            Assert.AreNotEqual(label, null, "labe is not null");
            Assert.AreEqual(lst.Length, size, "size is not length");

            Console.WriteLine(label);
            dumpData(lst);
        }

        [Test]
        public void Test_invoces_pay()
        {
            int size;
            string label;
            Boss2XI.InvoiceInfo[] lst;

            Console.WriteLine("=== pay data ===");
            appl.invoces(null, acctId, "P", null, null, null, null, null, out rc, out msg, out size, out label, out lst);
            Assert.AreEqual(0, rc, msg);
            Assert.AreEqual(label, null, "labe is null");
            Assert.AreEqual(lst.Length, size, "size is not length");

            Console.WriteLine(label);
            dumpData(lst);
        }

        [Test]
        public void Test_invoces_cond1()
        {
            int size;
            string label;
            Boss2XI.InvoiceInfo[] lst0;
            Boss2XI.InvoiceInfo[] lst1;
            Boss2XI.InvoiceInfo[] lst2;
            Boss2XI.InvoiceInfo[] lst3;

            Console.WriteLine("=== filter cond ===");

            Console.WriteLine("Type");
            appl.invoces(null, acctId, null, null, null, null, null, null, out rc, out msg, out size, out label, out lst0);
            Assert.AreEqual(0, rc, msg);

            appl.invoces(null, acctId, "A", null, null, null, null, null, out rc, out msg, out size, out label, out lst1);
            Assert.AreEqual(0, rc, msg);

            Console.WriteLine(lst0.Length + " = " + lst1.Length);
            Assert.AreEqual(lst0.Length, lst1.Length , "all is nos as NULL");

            // dluzne a zaplacene
            appl.invoces(null, acctId, "D", null, null, null, null, null, out rc, out msg, out size, out label, out lst2);
            Assert.AreEqual(0, rc, msg);
            appl.invoces(null, acctId, "P", null, null, null, null, null, out rc, out msg, out size, out label, out lst3);
            Assert.AreEqual(0, rc, msg);

            Console.WriteLine(lst1.Length + " = " + lst2.Length + " + " + lst3.Length);
            Assert.AreEqual(lst1.Length, lst2.Length + lst3.Length);

            // filtr na datum
            Console.WriteLine("Date");

            appl.invoces(null, acctId, "A", null, null, null, null, null, out rc, out msg, out size, out label, out lst0);
            Assert.AreEqual(0, rc, msg);

            appl.invoces(null, acctId, "A", null, "2008-01-01", null, null, null, out rc, out msg, out size, out label, out lst1);
            Assert.AreEqual(0, rc, msg);
            appl.invoces(null, acctId, "A", "2008-01-01", "2008-05-01", null, null, null, out rc, out msg, out size, out label, out lst2);
            Assert.AreEqual(0, rc, msg);
            appl.invoces(null, acctId, "A", "2008-05-01", null, null, null, null, out rc, out msg, out size, out label, out lst3);
            Assert.AreEqual(0, rc, msg);


            Console.WriteLine(lst0.Length + " = " + lst1.Length + " + " + lst2.Length + " + " + lst3.Length);
            Assert.AreEqual(lst0.Length, lst1.Length + lst2.Length + lst3.Length);

            // filtr na limit
            Console.WriteLine("Limit");

            appl.invoces(null, acctId, "A", null, null, "5", null, null, out rc, out msg, out size, out label, out lst0);
            Assert.AreEqual(0, rc, msg);

            Console.WriteLine(lst0.Length + " = " + "5");
            Assert.AreEqual(lst0.Length, 5);

            // chybny vstup
            Console.WriteLine("Error input");
            appl.invoces(null, acctId, "A", null, "2008-05-01", "5", null, null, out rc, out msg, out size, out label, out lst0);
            Assert.AreEqual(-2, rc, msg);
        }

        [Test]
        public void Test_prodHistory()
        {
            int size1, size2;
            Boss2XI.ProdStateInfo[] lst1;
            Boss2XI.ParamHisInfo[] lst2;

            Console.WriteLine(string.Format("=== prod hist (prodId={0})===", prodId));
            appl.prodHistory(prodId, null, null, out rc, out msg, out size1, out lst1, out size2, out lst2);
            Assert.AreEqual(0, rc, msg);
            Assert.AreEqual(lst1.Length, size1, "size1 is not length1");
            Assert.AreEqual(lst2.Length, size2, "size2 is not length2");

            Console.WriteLine("state");
            for (int i = 0; i < lst1.Length; i++)
            {
                Console.WriteLine(string.Format("state={0}, name={1}, from={2}, to={3}",
                    lst1[i].state, lst1[i].stateName, lst1[i].dateFrom, lst1[i].dateTo));
            }

            Console.WriteLine("attributes");
            for (int i = 0; i < lst2.Length; i++)
            {
                Console.WriteLine(string.Format("attrId={0}, name={1}, value={2}, from={3}, to={4}",
                    lst2[i].attrId, lst2[i].name, lst2[i].value, lst2[i].dateFrom, lst2[i].dateTo));
            }
        }



        private void dumpData (Boss2XI.InvoiceInfo[] lst)
        {
            for (int i = 0; i < lst.Length; i++)
            {                
                Console.WriteLine(string.Format("nbr={0}, date={1}, dueDate={2}, amount={3}, debtAmount={4}",
                    lst[i].billNbr, lst[i].invoiceDate, lst[i].dueDate, lst[i].amount, lst[i].debtAmount));
            }
        }
    }
    [TestFixture()]
    public class Pwgen
    {
        private SapWS2.PwGen pwg = new SapWS2.PwGen();

        [Test]
        public void passwdGen()
        {
            // nagenerovat par hesel
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine(pwg.passwdGen(1));
            }
        }

    }

    [TestFixture()]
    public class SetActivateParams
    {
        private log4net.Appender.ConsoleAppender appender;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("test NU");
        private SapWS2.ActivationParameters appl = new SapWS2.ActivationParameters();
        private int rc;
        private string msg;

        [SetUp()]

        public void Init()
        {
            appender = new log4net.Appender.ConsoleAppender();
            appender.Layout = new log4net.Layout.SimpleLayout();
            appender.Threshold = log4net.Core.Level.All;
            log4net.Config.BasicConfigurator.Configure(appender);

            // TODO: dat do konfigurace aby to nebylo natvrdo
            //System.Configuration.ConfigurationManager.AppSettings["databaseConn"] = "Data Source=CC_TEST;User ID=crmws;Password=crmws;";
            Console.WriteLine("acctId");
        }


        [Test]
        public void no_input()
        {
            appl.setActivationParameters("", "", "", null, out rc, out msg);
            Assert.AreEqual(0, rc, msg);
        }

        [Test]
        public void empty_params()
        {
            appl.setActivationParameters("", "", "", null, out rc, out msg);
            Assert.AreEqual(0, rc, msg);
        }

    }

}
