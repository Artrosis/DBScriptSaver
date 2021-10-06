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

            string head = tablescripts[0];

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

            tablescripts.RemoveAt(0);

            foreach (var item in tablescripts)
            {
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
            Regex regexColumn = new Regex(@"^""\w*"" \w* NOT? NULL? (DEFAULT? .*)?,?$");

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