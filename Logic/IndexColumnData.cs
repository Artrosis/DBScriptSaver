namespace DBScriptSaver.Logic
{
    internal class IndexColumnData
    {
        public string Name;
        public int Order;
        public bool IsDesc;
        public bool IsIncluded;

        public string Definition => "\t" + $@"[{Name}] {(IsDesc ? "DESC" : "ASC")}";

        public override string ToString()
        {
            return Name;
        }
    }
}