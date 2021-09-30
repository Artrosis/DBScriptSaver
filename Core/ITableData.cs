using DBScriptSaver.ViewModels;

namespace DBScriptSaver.Core
{
    internal interface ITableData
    {
        IScript GetScript();
    }
}