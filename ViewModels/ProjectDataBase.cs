using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ProjectDataBase
    {
        public ProjectDataBase(Project Project)
        {
            this.Project = Project;
        }
        public string Name { get; set; }
        public string Path { get; set; }

        [JsonIgnoreAttribute]
        public Project Project { get; set; }

        public List<string> traceProcedures = new List<string>();

        public string BaseFolder => Project.Path + System.IO.Path.DirectorySeparatorChar + Path + System.IO.Path.DirectorySeparatorChar;
        public string SourceFolder => BaseFolder + "source" + System.IO.Path.DirectorySeparatorChar;
        public string FilterFile => BaseFolder + "ObjectsFilter.cfg";

        internal void UpdateTraceProcedures()
        {
            if (File.Exists(FilterFile))
            {
                XElement storedProcedures = XElement.Parse(File.ReadAllText(FilterFile));

                storedProcedures.Elements().ToList().ForEach(sp => traceProcedures.Add(sp.Value));
            }
        }
    }
}