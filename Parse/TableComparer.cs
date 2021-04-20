using DBScriptSaver.Helpers;
using DBScriptSaver.ViewModels;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBScriptSaver.Parse
{
    public static class TableComparer
    {
        public static List<Migration> GetChanges(string DBScript, string sourceScript)
        {
            var DBColumns = DBScript.ParseColumns();
            var sourceColumns = sourceScript.ParseColumns();

            var НовыеСтолбцы = DBColumns
                                .Where(db => !sourceColumns.Any(c => c.Name.Sql == db.Name.Sql))
                                .ToList();

            var Result = new List<Migration>();

            foreach (var column in НовыеСтолбцы)
            {
                string tableName = column.GetTableName();
                var script = $@"
IF NOT EXISTS (
       SELECT NULL
       FROM   sys.[columns] AS c
       WHERE  c.[object_id] = OBJECT_ID(N'{tableName}')
              AND c.name = N'{column.Name.Sql}'
   )
BEGIN
    ALTER TABLE {tableName} ADD {column.Sql}
END";

                var defaultStatement = DBScript.SearchDefaultStatement(column.Name.Sql);

                if (defaultStatement != null)
                {
                    script += Environment.NewLine;
                    script += "GO";
                    script += Environment.NewLine;
                    script += defaultStatement.Sql;
                }

                var fileName = $@"Add_{column.Name}";

                Result.Add(
                    new Migration()
                    {
                        Name = FileHelper.CreateMigrationName(fileName),
                        Script = script
                    });
            }

            return Result;
        }

        private static List<SqlColumnDefinition> ParseColumns(this string dBScript)
        {
            var createTable = Microsoft.SqlServer.Management.SqlParser.Parser.Parser.Parse(dBScript).Script.GetCreateTable();

            return createTable.Children
                            .Single(c => c is SqlTableDefinition).Children
                            .Where(c => c is SqlColumnDefinition)
                            .Cast<SqlColumnDefinition>()
                            .ToList();
        }

        private static SqlCodeObject SearchDefaultStatement(this string dBScript, string columnName)
        {
            var script = Microsoft.SqlServer.Management.SqlParser.Parser.Parser.Parse(dBScript).Script;
            var createTable = script.GetCreateTable();

            var defStatement = script.GetDefaultStatement(sql => sql.StartsWith($@"ALTER TABLE {createTable.Name.Sql} ADD  CONSTRAINT") 
                                                                && sql.EndsWith($@"FOR {columnName}"));

            if (defStatement == null)
            {
                return null;
            }

            return defStatement.Parent.Parent;
        }

        private static SqlCodeObject GetDefaultStatement(this SqlCodeObject sql, System.Func<string, bool> predicat)
        {
            if (sql is SqlCreateTableStatement)
            {
                return null;
            }
            if (sql is SqlNullStatement n && predicat(n.Sql))
            {
                return n;
            }

            foreach (var s in sql.Children)
            {
                var statement = GetDefaultStatement(s, predicat);
                if (statement != null)
                {
                    return statement;
                }
            }

            return null;
        }

        private static SqlCreateTableStatement GetCreateTable(this SqlCodeObject sqlObject)
        {
            if (sqlObject is SqlCreateTableStatement s)
            {
                return s;
            }            

            foreach (var sql in sqlObject.Children)
            {
                var statement = GetCreateTable(sql);
                if (statement != null)
                {
                    return statement;
                }
            }

            return null;
        }
        private static string GetTableName(this SqlColumnDefinition column)
        {
            var t = column.Parent.Parent as SqlCreateTableStatement;

            return t.Name.Sql;
        }
    }
}