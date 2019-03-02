using DBScriptSaver.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBScriptSaver.ViewModels
{
    public class DBScriptViewModel
    {
        public ObservableCollection<Project> Projects { get; set; }

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
            }
            else
            {
                Projects = new ObservableCollection<Project>(new List<Project>());
            }
            Projects.CollectionChanged += SaveProjects;
        }

        private void SaveProjects(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            File.WriteAllText(Settings, JsonConvert.SerializeObject(Projects));
        }

        public void AddProject()
        {
            Projects.Add(new Project() { Name = Resources.НовыйПроект });            
        }
    }
}
