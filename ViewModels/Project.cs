using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Data;
using SysPath = System.IO.Path;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Project
    {
        [JsonIgnore]
        public DBScriptViewModel vm;
        public string Name { get; set; }
        public string Path { get; set; }
        public ProjectServer Server { get; set; }
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
        public ObservableCollection<ProjectDataBase> DataBases { get; } = new ObservableCollection<ProjectDataBase>();

        [JsonIgnore]
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
        [JsonIgnore]
        public List<string> DBPaths
        { 
            get
            {
                List<string> fullPaths = new List<string>();

                fullPaths.AddRange(Directory.GetDirectories(Path, "changes*", SearchOption.AllDirectories)
                                    .Concat(Directory.GetDirectories(Path, "source*", SearchOption.AllDirectories))
                                    .Concat(Directory.GetDirectories(Path, "tables*", SearchOption.AllDirectories)));

                return fullPaths
                           .Select(p => SysPath.GetDirectoryName(p))
                           .Distinct()
                           .Select(p => p.Replace(Path, ""))
                           .Where(p => p.Length > 0)
                           .ToList();
            }
        }
    }
}
