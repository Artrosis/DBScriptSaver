using DBScriptSaver.ViewModels;
using PropertyChanged;
using System.IO;

namespace DBScriptSaver
{

    [AddINotifyPropertyChangedInterface]
    internal class ScriptWrapper
    {
        private readonly IScript t;
        public IScript getScript
        {
            get
            {
                IScript result = t.Copy();

                if (!string.IsNullOrWhiteSpace(EditedFilePath) && File.Exists(EditedFilePath))
                {
                    result.ScriptText = File.ReadAllText(EditedFilePath);
                }
                return result;
            }
        }
        public ScriptWrapper(IScript t)
        {
            this.t = t;
        }
        public string FileName => t.FileName;
        public string FullPath => t.FullPath;
        public string ScriptText => getScript.ScriptText;
        public string ObjectType => t.ObjectType;
        public ChangeType ChangeState => t.ChangeState;
        public bool Save { get; set; } = false;
        public string EditedFilePath { get; internal set; }
    }
}