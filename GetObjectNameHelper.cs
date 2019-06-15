using System;
using System.Collections.Generic;
using System.Linq;

namespace DBScriptSaver
{
    public static class GetObjectNameHelper
    {
        public static string GetSchema(this string s)
        {
            return s.Substring(0, s.IndexOf(@"."));
        }
        public static string GetName(this string s)
        {
            s = s.Substring(s.IndexOf(@".") + 1);
            return s.Substring(0, s.IndexOf(@"."));
        }
        public static string GetObjectIdString(this List<string> lst)
        {
            string result = "";
            foreach (string ObjectName in lst)
            {
                if (result != "")
                {
                    result += ", ";
                }
                result += $"OBJECT_ID(N'[{ObjectName.GetSchema()}].[{ObjectName.GetName()}]')";
            }
            return result;
        }
    }
}
