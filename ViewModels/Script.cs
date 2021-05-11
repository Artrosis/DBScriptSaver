using DBScriptSaver.Helpers;
using DBScriptSaver.Parse;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBScriptSaver.ViewModels
{
    public class Script
    {
        public string FileName;
        public string FullPath;
        public string ScriptText;
        public string ObjectType;
        public ChangeType ChangeState;
        public Urn urn;
        public string objName;

        public List<Migration> MakeMigrations(Server server)
        {
            List<Migration> Migrations = new List<Migration>();

            if (ObjectType == @"Индекс")
            {
                Migrations.Add(MakeCreateIndexMigration(server));
            }
            else if (ObjectType == @"Таблица")
            {
                switch (ChangeState)
                {
                    case ChangeType.Новый:
                        Migrations.Add(MakeCreateTableMigration(server));
                        break;
                    case ChangeType.Изменённый:
                        Migrations.AddRange(MakeAlterTableMigration(server));
                        break;
                }
            }

            return Migrations;
        }

        private List<Migration> MakeAlterTableMigration(Server server)
        {
            if (!File.Exists(FullPath))
            {
                return new List<Migration>();
            }

            string oldScript = File.ReadAllText(FullPath);

            return TableComparer.GetChanges(server.GetScript(urn), oldScript);
        }

        private Migration MakeCreateTableMigration(Server server)
        {
            return new Migration()
            {
                Name = FileHelper.CreateMigrationName(objName),
                Script = server.GetScript(urn)
            };
        }

        private Migration MakeCreateIndexMigration(Server server)
        {
            return new Migration()
            {
                Name = FileHelper.CreateMigrationName(objName),
                Script = server.GetScript(urn)
            };
        }
    }
}