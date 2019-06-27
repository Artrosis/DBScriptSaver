using PropertyChanged;
using System;
using System.Xml.Linq;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class DBObject
    {
        protected string Schema;
        protected string Name;

        public ObjectState State;

        public DBObject(XElement f)
        {
            Schema = GetSchema(f.Value);
            Name = GetName(f.Value);
            State = (ObjectState)Enum.Parse(typeof(ObjectState), f.Attribute(XName.Get("State")).Value);
        }

        public DBObject(string Name)
        {
            Schema = GetSchema(Name);
            this.Name = GetName(Name);
            State = ObjectState.Не_указан;
        }

        public override string ToString()
        {
            return $@"{Schema}.{Name}";
        }
        private static string GetSchema(string s)
        {
            return s.Substring(0, s.IndexOf(@"."));
        }
        private static string GetName(string s)
        {
            return s.Substring(s.IndexOf(@".") + 1);
        }

        public string FullName => $@"{Schema}.{Name}";

        [AlsoNotifyFor("IsIgnore")]
        public bool IsTrace
        {
            get
            {
                return State == ObjectState.Отслеживаемый;
            }

            set
            {
                if (value == true)
                {
                    State = ObjectState.Отслеживаемый;
                }
                else if (State == ObjectState.Отслеживаемый)
                {
                    State = ObjectState.Не_указан;
                }
            }
        }

        [AlsoNotifyFor("IsTrace")]
        public bool IsIgnore
        {
            get
            {
                return State == ObjectState.Игнорируемый;
            }

            set
            {
                if (value == true)
                {
                    State = ObjectState.Игнорируемый;
                }
                else if (State == ObjectState.Игнорируемый)
                {
                    State = ObjectState.Не_указан;
                }              
            }
        }
    }
}