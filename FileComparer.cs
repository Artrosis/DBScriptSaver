using System;

namespace DBScriptSaver
{
    internal class FileComparer
    {
        private static string _path;
        public static void SetPath(string path)
        {
            _path = path;
        }
        public static string GetPath()
        {
            return _path;
        }
    }
}