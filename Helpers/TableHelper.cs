using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBScriptSaver.Helpers
{
    public static class TableHelper
    {
        public static string GetScript(this Server server, Table tbl)
        {
            StringBuilder sb = new StringBuilder();
            Scripter createScrp = new Scripter(server);
            createScrp.Options.ScriptSchema = true;
            createScrp.Options.ScriptBatchTerminator = true;
            createScrp.Options.DriAll = true;
            createScrp.Options.IncludeIfNotExists = true;
            createScrp.Options.ExtendedProperties = true;

            foreach (string s in createScrp.EnumScript(new Urn[] { tbl.Urn }))
            {
                sb.AppendLine(s);
            }

            return sb.ToString();
        }
    }
}
