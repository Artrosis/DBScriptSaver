using PropertyChanged;
using System.Xml.Linq;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Tbl : DBObject
    {
        public Tbl(XElement f) : base(f) { }
        public Tbl(string Name) : base(Name) { }
    }
}