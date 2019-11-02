using System;

namespace DBScriptSaver.ViewModels
{
    public class DependenceObject
    {
        public string ObjectType
        {
            get;
            set;
        }
        public string ObjectName
        {
            get;
            set;
        }

        public override string ToString()
        {
            return ObjectName;
        }
    }
}