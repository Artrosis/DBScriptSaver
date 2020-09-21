using DBScriptSaver.ViewModels;
using PropertyChanged;
using System.IO;

namespace DBScriptSaver
{

    [AddINotifyPropertyChangedInterface]
    internal class ScriptWrapper
    {
        private Script t;
        public Script getScript
        {
            get
            {
                Script result = new Script()
                {
                    FileName = t.FileName,
                    FullPath = t.FullPath,
                    ScriptText = t.ScriptText
                };

                if (!string.IsNullOrWhiteSpace(EditedFilePath) && File.Exists(EditedFilePath))
                {
                    result.ScriptText = File.ReadAllText(EditedFilePath);
                }
                return result;
            }
        }
        public ScriptWrapper(Script t)
        {
            this.t = t;
        }
        public string FileName => getScript.FileName;
        public string FullPath => getScript.FullPath;
        public string ScriptText => getScript.ScriptText;
        public string ObjectType => getScript.ObjectType;
        public string ChangeState => getScript.ChangeState;
        public bool Save { get; set; } = false;
        public string EditedFilePath { get; internal set; }
    }
}