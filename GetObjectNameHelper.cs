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
            var ss = s.Substring(s.IndexOf(@".") + 1);
            if (ss.IndexOf(@".") < 0)
            {
                return ss;
            }
            return ss.Substring(0, ss.IndexOf(@"."));
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
        public static string GetObjectIdStringSql(this List<string> lst)
        {
            return lst.Select(o => o + ".sql").ToList().GetObjectIdString();
        }
        public static string GetObjectsList(this List<string> lst)
        {
            string result = "";
            foreach (string ObjectName in lst)
            {
                if (result != "")
                {
                    result += ", ";
                }
                result += $"N'{ObjectName}'";
            }
            return result;
        }
    }
}
