using PropertyChanged;
using System.Xml.Linq;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Procedure : DBObject
    {
        public Procedure(XElement f): base(f) { }
        public Procedure(string Name) : base(Name) { }
        public Procedure(string SchemaName, string ProcedureName) : base(SchemaName, ProcedureName) { }
    }
}