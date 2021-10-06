using DBScriptSaver.ViewModels;
using System.Collections.Generic;

namespace DBScriptSaver.Core
{
    internal class EmptyMigrationMaker : IMigrationMaker
    {
        public List<Migration> Make()
        {
            return new List<Migration>();
        }
    }
}