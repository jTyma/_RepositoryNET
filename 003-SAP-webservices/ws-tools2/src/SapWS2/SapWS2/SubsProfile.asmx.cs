using System;
using System.Data;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;

namespace SapWS2
{
    /// <summary>
    /// Summary description for SubsProfile
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class SubsProfile : System.Web.Services.WebService
    {

        public void getSubsProfile(string MSISDN, int acctId, 
            out string subsType, out int tarifId, out string tarifLabel,
            out string subsStatus, out Credit creditInfo,
            out int rc, out string msg)
        {
            subsType = null;
            tarifId = 0;
            tarifLabel = null;
            subsStatus = null;

            creditInfo = null;

            msg = null;
            rc = 0;
        }

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        public class Credit
        {
            public string creditValue;
            public string lastCreditDate;
            public string lastCreditValue;
            public string lastUsageDate;
            public string lastUsageValue;
        }
    }
}
