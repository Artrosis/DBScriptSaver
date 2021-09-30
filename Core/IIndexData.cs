using DBScriptSaver.ViewModels;

namespace DBScriptSaver.Core
{
    internal interface IIndexData
    {
        IScript GetScript();
    }
}