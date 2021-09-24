using DBScriptSaver.ViewModels;
using System.Collections.Generic;

namespace DBScriptSaver.Core
{
    public interface IMigrationMaker
    {
        List<Migration> Make();
    }
}