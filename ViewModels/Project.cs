using Newtonsoft.Json;
using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Security;
using System.Windows.Data;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Project
    {
        [JsonIgnoreAttribute]
        public DBScriptViewModel vm;
        public string DBLogin { get; set; }
        public SecureString DBPassword { get; set; }
        public Project(DBScriptViewModel vm) : base()
        {
            this.vm = vm;
            DataBases.CollectionChanged += DataBases_CollectionChanged;
        }

        private void DataBases_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (ProjectDataBase db in e.NewItems)
                {
                    db.Project = this;
                }
            }
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
