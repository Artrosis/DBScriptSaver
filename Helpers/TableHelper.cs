using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBScriptSaver.Helpers
{
    public static class ServerHelper
    {
        public static string GetScript(this Server server, Table tbl)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string s in scripter(server).EnumScript(new Urn[] { tbl.Urn }))
            {
                sb.AppendLine(s);
            }

            return sb.ToString();
        }

        private static Scripter scripter(Server server)
        {
            var result = new Scripter(server);
            result.Options.ScriptSchema = true;
            result.Options.ScriptBatchTerminator = true;
            result.Options.DriAll = true;
            result.Options.Indexes = true;
            result.Options.IncludeIfNotExists = true;
            result.Options.ExtendedProperties = true;

            return result;
        }

        public static string GetScript(this Server server, Index index)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string s in scripter(server).EnumScript(new Urn[] { index.Urn }))
            {
                sb.AppendLine(s);
            }

            return sb.ToString();
        }
    }
}
