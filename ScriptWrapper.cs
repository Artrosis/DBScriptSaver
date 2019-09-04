using PropertyChanged;

namespace DBScriptSaver
{

    [AddINotifyPropertyChangedInterface]
    internal class ScriptWrapper
    {
        public (string FileName, string FullPath, string ScriptText) t;

        public ScriptWrapper((string FileName, string FullPath, string ScriptText) t)
        {
            this.t = t;
        }

        public string FileName => t.FileName;
        public string FullPath => t.FullPath;
        public string ScriptText => t.ScriptText;


        private bool save = true;
        public bool Save
        {
            get => save;

            set
            {
                save = value;
            }
        }
    }
}