using DBScriptSaver.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DBScriptSaver.ViewModels
{
    public class DBScriptViewModel
    {
        public ObservableCollection<Project> Projects { get; }

        public ListCollectionView EditProjects
        {
            get
            {
                return new ListCollectionView(Projects);
            }
        }

        const string SettingsFileName = @"Settings.cfg";

        private string Settings = string.Empty;

        public DBScriptViewModel()
        {
            string SettingsDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}{Path.DirectorySeparatorChar}DBScriptHelper";
            
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }
            Settings = Path.Combine(SettingsDirectory, SettingsFileName);
            if (!File.Exists(Settings))
            {
                File.Create(Settings);
            }

            string ProjectsData = File.ReadAllText(Settings);

            if (ProjectsData != string.Empty)
            {
                Projects = new ObservableCollection<Project>(JsonConvert.DeserializeObject<List<Project>>(ProjectsData));
                Projects.ToList().ForEach(i => (i as INotifyPropertyChanged).PropertyChanged += Item_PropertyChanged);
                Projects.ToList().ForEach(p => p.vm = this);
                Projects.ToList().SelectMany(i => i.DataBases).ToList().ForEach(i => (i as INotifyPropertyChanged).PropertyChanged += Item_PropertyChanged);
            }
            else
            {
                Projects = new ObservableCollection<Project>(new List<Project>());
            }
            
            Projects.CollectionChanged += ContentCollectionChanged;
        }

        private void ContentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (Project proj in e.NewItems)
                {
                    proj.vm = this;
                }
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == @"DBPassword")
            {
                return;
            }
            SaveProjects();
        }
        public void SaveProjects()
        {
            File.WriteAllText(Settings, JsonConvert.SerializeObject(Projects));
            TaskbarIconHelper.UpdateContextMenu();
        }

        public void AddProject()
        {
            Projects.Add(new Project(this) { Name = Resources.НовыйПроект });
        }
    }
}
