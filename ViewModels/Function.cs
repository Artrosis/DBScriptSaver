using PropertyChanged;
using System.Xml.Linq;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Function: DBObject
    {
        public Function(XElement f) : base(f) { }
        public Function(string Name) : base(Name) { }
        public Function(string SchemaName, string ProcedureName) : base(SchemaName, ProcedureName) { }
    }
}