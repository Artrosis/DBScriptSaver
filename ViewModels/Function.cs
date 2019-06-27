using PropertyChanged;
using System.Xml.Linq;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Function: DBObject
    {
        public Function(XElement f) : base(f) { }
        public Function(string Name) : base(Name) { }
    }
}