using System;
using System.Data;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;
using System.Text;

namespace SapWS2
{
    /// <summary>
    /// PwGen - generator hesel pro predplacene a jine karty, kde ma byt prednastavene
    /// nejake heslo.
    /// </summary>
    [WebService(Namespace = "http://b-oss.ufon.cz/internalapi/pwgen")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class PwGen : System.Web.Services.WebService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("pwGen");
        private static int PW_LENGTH = 4;
        private static int MAX_ATTEMPTS = 5000;

        // nahone cislo se inicializuje asi casem, tj. kdyz to bylo
        // primo ve funkci rndString, tak to generovalo stejne vysledky,
        // pokud to bylo volano tesne po sobe
        private static Random random = new Random();


        /// <summary>
        /// Generovani hesla pro prepaidove (a mozna casem i jine)
        /// nove zakladane ucty
        /// </summary>
        /// <param name="pwType"></param>
        /// <returns></returns>
        [WebMethod]
        public string passwdGen(int pwType)
        {
            int sich = 0;
            log.Info(string.Format("pwgen type={0}", pwType));
            string pwd = null;

            if (pwType != 1)
            {
                throw new Exception("Unknown pasword type");
            }

            while (true)
            {
                pwd = rndString(PW_LENGTH, true);

                if (passwordScore(pwd) > 0)
                {
                    break;
                }

                sich++;
                if (sich > MAX_ATTEMPTS)
                {
                    throw new Exception("Unable to generate good passwords, bad luck. Try again please");
                }
            }

            return pwd;
        }

        private string rndString (int size, bool numnerOnly)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                if (numnerOnly)
                {
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(10 * random.NextDouble() + 48)));
                }
                else
                {
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                }

                builder.Append(ch); 
            }

            return builder.ToString();
        }


        /// 
        /// vypocet sily hesla 0 ... slabe heslo, 1 ... prijatelne heslo (zatim)
        /// prevzato ze samoobsluhy, 30.10.2009, verze 1.8
        /// 
        private static int passwordScore(string pwd)
        {

            // zastoupeni cisel
            int[] numCount = new int[10];
            int lastCh = 0;
            int deltaSign = 0;
            bool isSequence = pwd.Length > 1;
            for (int i = 0; i < pwd.Length; i++)
            {
                try
                {
                    int ch = int.Parse(pwd[i].ToString());
                    numCount[ch]++;

                    // postupky
                    if ((i > 0) && isSequence)
                    {
                        int delta = ch - lastCh;

                        if (Math.Abs(delta) == 1)
                        {
                            // druha cifra urcuje smer postupky
                            if (i == 1)
                            {
                                deltaSign = delta;
                            }
                            else
                            {
                                if (delta != deltaSign)
                                {
                                    // neni to urcite postupka
                                    isSequence = false;
                                }
                            }
                        }
                        else
                        {
                            // neni to postupka
                            isSequence = false;
                        }

                    }

                    lastCh = ch;
                }
                catch
                {
                    // ignorovat
                }
            }

            // je to sekvence
            if (isSequence)
            {
                return 0;
            }

            // projet zastoupeni cifer a kdyz tak to zastavit
            int twoCount = 0;
            bool isGroupOk = true;
            for (int i = 0; i < 10; i++)
            {
                // vice jak trojicka
                if (numCount[i] > 2)
                {
                    isGroupOk = false;
                    break;
                }

                // druha dvojicka
                if (numCount[i] == 2)
                {
                    twoCount++;

                    if (twoCount > 1)
                    {
                        isGroupOk = false;
                        break;
                    }
                }

            }

            if (!isGroupOk)
            {
                return 0;
            }

            return 1;
        }
    }
}
