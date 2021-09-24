﻿using DBScriptSaver.Helpers;
using DBScriptSaver.Parse;
using DBScriptSaver.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;

namespace DBScriptSaver.Core
{
    internal class MSSQLMigrationMaker : IMigrationMaker
    {
        private DbConnection connection;
        private Server server;
        private Script script;

        public MSSQLMigrationMaker(DbConnection dbConnection, Script script)
        {
            this.script = script;
            connection = dbConnection;
            connection.Open();
            server = new Server(new ServerConnection((SqlConnection)connection));
        }

        public List<Migration> Make()
        {
            List<Migration> Migrations = new List<Migration>();

            if (script.ObjectType == @"Индекс")
            {
                Migrations.Add(MakeCreateIndexMigration());
            }
            else if (script.ObjectType == @"Таблица")
            {
                switch (script.ChangeState)
                {
                    case ChangeType.Новый:
                        Migrations.Add(MakeCreateTableMigration());
                        break;
                    case ChangeType.Изменённый:
                        Migrations.AddRange(MakeAlterTableMigration());
                        break;
                }
            }

            return Migrations;
        }

        private List<Migration> MakeAlterTableMigration()
        {
            if (!File.Exists(script.FullPath))
            {
                return new List<Migration>();
            }

            string oldScript = File.ReadAllText(script.FullPath);

            return TableComparer.GetChanges(server.GetScript(script.urn), oldScript);
        }

        private Migration MakeCreateTableMigration()
        {
            return new Migration()
            {
                Name = FileHelper.CreateMigrationName(script.objName),
                Script = server.GetScript(script.urn)
            };
        }

        private Migration MakeCreateIndexMigration()
        {
            return new Migration()
            {
                Name = FileHelper.CreateMigrationName(script.objName),
                Script = server.GetScript(script.urn)
            };
        }
    }
}