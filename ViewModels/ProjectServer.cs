using Newtonsoft.Json;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Data;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ProjectServer
    {
        [JsonIgnore]
        public DBScriptViewModel vm;
        public string Name { get; set; }
        public string Path { get; set; }
        public string DBLogin { get; set; }
        public string DBPassword { get; set; }
        public ProjectServer(DBScriptViewModel vm) : base()
        {
            this.vm = vm;
        }  

        public override string ToString()
        {
            return Name;
        }
    }
}