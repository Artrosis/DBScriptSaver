using PropertyChanged;
using System.IO;

namespace DBScriptSaver
{

    [AddINotifyPropertyChangedInterface]
    internal class ScriptWrapper
    {
        private (string FileName, string FullPath, string ScriptText) t;
        public (string FileName, string FullPath, string ScriptText) tuple
        {
            get
            {
                (string FileName, string FullPath, string ScriptText) result = (t.FileName, t.FullPath, t.ScriptText);

                if (!string.IsNullOrWhiteSpace(EditedFilePath) && File.Exists(EditedFilePath))
                {
                    result.ScriptText = File.ReadAllText(EditedFilePath);
                }
                return result;
            }
        }

        public ScriptWrapper((string FileName, string FullPath, string ScriptText) t)
        {
            this.t = t;
        }

        public string FileName => tuple.FileName;
        public string FullPath => tuple.FullPath;
        public string ScriptText => tuple.ScriptText;
        public bool Save { get; set; } = true;

        public string EditedFilePath { get; internal set; }
    }
}