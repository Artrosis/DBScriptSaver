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
            var sourceColumns = sourceScript.ParseColumns().Select(c => c.column).ToList();

            var НовыеСтолбцы = DBColumns
                                .Where(db => !sourceColumns.Any(c => c.Name.Sql == db.column.Name.Sql))
                                .ToList();

            var Result = new List<Migration>();

            foreach (var col in НовыеСтолбцы)
            {
                var defaultStatement = DBScript.SearchDefaultStatement(col.column.Name.Sql);

                string script = col.column.MakeCreateColumnScript(defaultStatement, col.description);

                var fileName = $@"Add_{col.column.Name}";

                Result.Add(
                    new Migration()
                    {
                        Name = FileHelper.CreateMigrationName(fileName),
                        Script = script
                    });
            }

            return Result;
        }

        private static string MakeCreateColumnScript(this SqlColumnDefinition column, SqlCodeObject defaultStatement, string description)
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
    ALTER TABLE {tableName} ADD {column.Sql}";

            if ((description?.Length ?? 0) > 0)
            {
                script += Environment.NewLine + description;
            }

            script += Environment.NewLine + "END";

            if (defaultStatement != null)
            {
                script += Environment.NewLine;
                script += "GO";
                script += Environment.NewLine;
                script += defaultStatement.Sql;
            }

            return script;
        }

        private static List<(SqlColumnDefinition column, string description)> ParseColumns(this string dBScript)
        {
            var createTableScript = Microsoft.SqlServer.Management.SqlParser.Parser.Parser.Parse(dBScript).Script;

            var createTable = createTableScript.GetCreateTable();

            var ColumnDefinitions = createTable.Children
                            .Single(c => c is SqlTableDefinition).Children
                            .Where(c => c is SqlColumnDefinition)
                            .Cast<SqlColumnDefinition>()
                            .ToList();

            var descriptions = createTableScript.GetColumnDescriptions();

            return ColumnDefinitions.Select(c => (c, descriptions.ContainsKey(c.Name.Value) ? descriptions[c.Name.Value] : null)).ToList();
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

        private static Dictionary<string, string> GetColumnDescriptions(this SqlCodeObject sqlObject)
        {
            List<SqlExecuteModuleStatement> execs = sqlObject.GetExecuteStatements();

            return execs
                .Where(e => e.Module.Sql == "sys.sp_addextendedproperty" && e.Arguments.Any(a => a.Sql == @"@level2type=N'COLUMN'"))
                .ToDictionary(e => ((SqlLiteralExpression)e.Arguments.Single(a => a.Parameter.VariableName == @"@level2name").Children.Single(c => c is SqlLiteralExpression)).Value, e => e.Sql);
        }
        private static List<SqlExecuteModuleStatement> GetExecuteStatements(this SqlCodeObject sqlObject)
        {
            List<SqlExecuteModuleStatement> result = new List<SqlExecuteModuleStatement>();

            if (sqlObject is SqlExecuteModuleStatement s)
            {
                result.Add(s);
                return result;
            }            

            foreach (var sql in sqlObject.Children)
            {
                result.AddRange(GetExecuteStatements(sql));
            }

            return result;
        }
        private static string GetTableName(this SqlColumnDefinition column)
        {
            var t = column.Parent.Parent as SqlCreateTableStatement;

            return t.Name.Sql;
        }
    }
}