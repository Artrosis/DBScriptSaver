using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DBScriptSaver.Core
{
    internal class PostgreParser
    {
        internal static PGTableData ParseTable(string oldScript)
        {
            if (oldScript == null || oldScript.Length == 0)
            {
                throw new Exception(@"Пустой скрипт для разбора");
            }
            var tablescripts = oldScript.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            PGTableData result = new PGTableData();

            string head = tablescripts.First(s => !s.StartsWith("CREATE SEQUENCE"));

            if (head == null)
            {
                throw new Exception("Не найдена заголовочная строка");
            }

            Regex regex = new Regex(@"^CREATE TABLE ""(.*)""\.""(.*)""\($");

            MatchCollection matches = regex.Matches(head);

            if (matches.Count != 1)
            {
                throw new Exception($@"Неверная заголовочная строка: {head}");
            }

            result.Schema = matches[0].Groups[1].Value;
            result.Name = matches[0].Groups[2].Value;

            foreach (var item in tablescripts)
            {
                if (item.StartsWith("CREATE SEQUENCE"))
                {
                    result.последовательности.Add((item, ""));
                    continue;
                }

                if (item.StartsWith("ALTER SEQUENCE"))
                {
                    var последовательность = result.последовательности.FirstOrDefault();

                    if (result.последовательности.Count == 1)
                    {
                        result.последовательности[0] = (result.последовательности[0].createScript, item);
                    }
                    continue;
                }

                object obj = ParseItem(item);

                if (obj == null)
                {
                    continue;
                }

                if (obj is PGColumnData column)
                {
                    result.Columns.Add(column);
                }
                else if (obj is PGConstrainsData constraint)
                {
                    result.Constrains.Add(constraint);
                }
            }

            return result;
        }

        private static object ParseItem(string item)
        {
            string regexpName = @"""\w+""";
            //new Regex(@"""\w+""");
            //string regexpType = @"( \w+( ?\w+\(\d{1,4}\))?)( without time zone)?";
            string regexpType = @"( ((\w+( \w+\(\d{1,4}\))?)|character varying|(timestamp(\(\d\))? with(out)? time zone)))";
            //new Regex(@"( ((\w+( \w+\(\d{1,4}\))?)|character varying|(timestamp(\(\d\))? with(out)? time zone)))");
            string regexpCollation = @"( COLLATE ""\w*"".""\w*"")?";
            //new Regex(@"( COLLATE ""\w*"".""\w*"")?");
            string regexpNotNull = @"( NOT NULL)?";
            //new Regex(@"( NOT NULL)?");
            string regexpDefault = @"( DEFAULT? .*)?";
            //new Regex(@"( DEFAULT? .*)?");
            Regex regexColumn = new Regex($"^{regexpName}{regexpType}({regexpCollation}{regexpNotNull}{regexpDefault})?,?$");

            MatchCollection matches = regexColumn.Matches(item);
            if (matches.Count == 1)
            {
                var col = new PGColumnData();
                col.Script = item.Replace(",", "");
                return col;
            }

            Regex primayKeyConstraint = new Regex(@"^CONSTRAINT "".*"" PRIMARY KEY (.*),?$");

            matches = primayKeyConstraint.Matches(item);
            if (matches.Count == 1)
            {
                var primaryKey = new PGConstrainsData();
                primaryKey.Script = item.Replace(",", "");
                return primaryKey;
            }

            Regex regexConstraint = new Regex(@"^CONSTRAINT ""\w*"" FOREIGN KEY (""\w*"") REFERENCES ""\w*"".""\w*"" (""\w*"") (ON DELETE NO ACTION)?\s?(ON UPDATE NO ACTION)?,?$");

            matches = regexConstraint.Matches(item);
            if (matches.Count == 1)
            {
                return null;
            }
            return null;
        }
    }
}