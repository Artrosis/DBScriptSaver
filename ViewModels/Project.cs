using Newtonsoft.Json;
using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Project
    {
        public Project() : base()
        {
        }

        public string Name { get; set; }
        public string Path { get; set; }

        public string Server { get; set; }
        public ObservableCollection<ProjectDataBase> DataBases { get; } = new ObservableCollection<ProjectDataBase>();

        [JsonIgnoreAttribute]
        public ListCollectionView EditDataBases
        {
            get
            {
                return new ListCollectionView(DataBases);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
