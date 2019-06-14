using PropertyChanged;

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

        public Project Project { get; set; }
    }
}