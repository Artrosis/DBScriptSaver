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

        public static string GetScript(this Server server, Urn urn)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string s in scripter(server).EnumScript(new Urn[] { urn }))
            {
                sb.AppendLine(s);
            }

            return sb.ToString();
        }
    }
}
