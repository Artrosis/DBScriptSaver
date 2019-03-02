namespace DBScriptSaver.ViewModels
{
    public class Project
    {
        public Project()
        {
        }

        public string Name { get; set; }
        public string Path { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
