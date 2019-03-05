using PropertyChanged;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ProjectDataBase
    {
        public ProjectDataBase()
        {
        }
        public string Name { get; set; }
        public string Path { get; set; }
    }
}