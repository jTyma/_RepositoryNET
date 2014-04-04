using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            bossApi.WSTool ws = new bossApi.WSTool();
            ws.Url = "http://192.168.1.20/intws/ToolSrv.asmx";
            Object rc = ws.UpdateSubsAttributes("420790369169", "1", "102666TR", "1111111112", null, null, null);
        }
    }
}
