using DBScriptSaver.Helpers;
using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;

namespace DBScriptSaver.Core
{
    internal class PGMigrationMaker : IMigrationMaker
    {
        private readonly PGScript script;

        public PGMigrationMaker(PGScript script)
        {
            this.script = script;
        }

        public List<Migration> Make()
        {
            List<Migration> Migrations = new List<Migration>();

            if (script.ObjectType == @"Индекс" && script.ChangeState == ChangeType.Новый)
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

        private IEnumerable<Migration> MakeAlterTableMigration()
        {
            if (!File.Exists(script.FullPath))
            {
                return new List<Migration>();
            }

            string oldScript = File.ReadAllText(script.FullPath);

            return script.PGObject.CreateAlterMirgrations(oldScript);
        }

        private Migration MakeCreateTableMigration()
        {
            return MakeCreateMigration();
        }

        private Migration MakeCreateIndexMigration()
        {
            return MakeCreateMigration();
        }

        private Migration MakeCreateMigration()
        {
            return new Migration()
            {
                Name = FileHelper.CreateMigrationName(script.ObjName),
                Script = script.PGObject.CreateMirgration()
            };
        }
    }
}