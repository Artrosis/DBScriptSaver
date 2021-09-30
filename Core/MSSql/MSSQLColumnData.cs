using System;
using System.Collections.Generic;

namespace DBScriptSaver.Core
{
    public class MSSQLColumnData
    {
        public int Order;
        public string Name;
        public string Type;
        public short MaxLength;
        public byte Precision;
        public byte Scale;
        public bool IsIdentity;
        public long? SeedValue;
        public long? IncrementalValue;
        public string Collation;
        public bool Nullable;

        public string Definition {
            get
            {
                string result = "\t" + $@"[{Name}] ";

                result += MakeType();

                if (IsIdentity)
                {
                    result += $@" IDENTITY({SeedValue},{IncrementalValue})";
                }

                if ((Collation?.Length ?? 0) > 0)
                {
                    result += $@" COLLATE {Collation}";
                }

                if (!Nullable)
                {
                    result += $@" NOT";
                }
                result += $@" NULL";

                return result;
            } 
        }

        public bool IsTextImage()
        {
            if (TextImageColumns.Contains(Type))
            {
                return true;
            }
            if (MaxColumns.Contains(Type) && MaxLength == -1)
            {
                return true;
            }
            return false;
        }

        private static readonly List<string> TextImageColumns = new List<string>()
        {
            "text",
            "ntext",
            "image",
            "xml"
        };
        private static readonly List<string> MaxColumns = new List<string>()
        {
            "varchar",
            "nvarchar"
        };

        private string MakeType()
        {
            string result = "";

            result += $@"[{Type}]";

            if (ScalableColumns.Contains(Type))
            {
                var precision = $@"{Precision}";
                if (Scale > 0)
                {
                    precision += $@",{Scale}";
                }
                result += $@"({precision})";
            }

            if (LengthColumns.Contains(Type))
            {
                if (MaxLength == -1)
                {
                    result += $@"(max)";
                }
                else
                {
                    if (Type.StartsWith("n"))
                    {
                        result += $@"({MaxLength/2})";
                    }
                    else
                    {
                        result += $@"({MaxLength})";
                    }                    
                }
            }
            return result;
        }
        private static readonly List<string> ScalableColumns = new List<string>()
        {
            "numeric",
            "decimal"
        };
        private static readonly List<string> LengthColumns = new List<string>()
        {
            "char",
            "nchar",
            "varbinary",
            "varchar",
            "nvarchar"
        };
        public override string ToString()
        {
            return Name;
        }
    }
}