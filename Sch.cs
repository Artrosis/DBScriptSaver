using System.Xml.Linq;

namespace DBScriptSaver.ViewModels
{
    public class Sch: DBObject
    {
        public Sch(XElement f) : base(f) { }
        public Sch(string Name) : base(Name) { }
    }
}