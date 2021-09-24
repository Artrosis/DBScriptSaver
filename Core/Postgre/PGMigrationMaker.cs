using DBScriptSaver.ViewModels;
using System.Collections.Generic;
using System.Data.Common;

namespace DBScriptSaver.Core
{
    internal class PGMigrationMaker : IMigrationMaker
    {
        private DbConnection dbConnection;
        private Script script;

        public PGMigrationMaker(DbConnection dbConnection, Script script)
        {
            this.dbConnection = dbConnection;
            this.script = script;
        }

        public List<Migration> Make()
        {
            throw new System.NotImplementedException();
        }
    }
}