using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ude;

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для fmDataBasesEditor.xaml
    /// </summary>
    public partial class fmDataBasesEditor : Window
    {
        private Project project => DataContext as Project;
        public fmDataBasesEditor(Project proj)
        {
            InitializeComponent();

            DataContext = proj;
        }

        private ProjectDataBase SelectedBase => gcDataBases.SelectedItem as ProjectDataBase;

        private void EditDBObjects_Click(object sender, RoutedEventArgs e)
        {
            var DB = SelectedBase;

            if (DB == null)
            {
                return;
            }

            var fmEditor = new DBObjectsFiltering(DB) { Owner = this };
            fmEditor.ShowDialog();
        }

        private void Compare_Click(object sender, RoutedEventArgs e)
        {
            var DB = SelectedBase;
            DirectoryInfo d = new DirectoryInfo(DB.SourceFolder);
            Dictionary<string, Tuple<string, DateTime>> SourcesData = d.GetFiles(@"*.sql", SearchOption.TopDirectoryOnly)
                                    .OrderBy(f => f.LastWriteTime)
                                    .ToDictionary(f => f.Name, f => new Tuple<string, DateTime>(File.ReadAllText(f.FullName, GetEncoding(f.FullName)), f.LastWriteTime));

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            {
                DataSource = DB.Project.Server,
                InitialCatalog = DB.Name ?? @"master",
                UserID = "Kobra_main",
                Password = "Ggv123",
                ConnectTimeout = 3
            };

            using (SqlConnection conn = new SqlConnection(builder.ConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT s.[name] + N'.' + o.[name] AS ObjectName, sm.[definition], o.[TYPE]" + Environment.NewLine
                                + @"FROM   sys.sql_modules   AS sm" + Environment.NewLine
                                + @"       JOIN sys.objects  AS o" + Environment.NewLine
                                + @"            ON  o.[object_id] = sm.[object_id]" + Environment.NewLine
                                + @"       JOIN sys.schemas  AS s" + Environment.NewLine
                                + @"            ON  o.[schema_id] = s.[schema_id]" + Environment.NewLine
                                + $"WHERE  sm.object_id IN ({SourcesData.Keys.ToList().GetObjectIdString()})";

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string FileName = ((string)r["ObjectName"]) + ".sql";
                        string TextFromDB = ((string)r["definition"]);

                        string SourcesKey = SourcesData.Keys.Select(k => new { Value = k, Upper = k.ToUpper() }).SingleOrDefault(k => k.Upper == FileName.ToUpper())?.Value;
                        Tuple<string, DateTime> SavedText = SourcesData[SourcesKey];                        
                        string TextFromFile = SavedText?.Item1;

                        if ((TextFromFile == null) ||(TextFromFile != TextFromDB))
                        {
                            File.WriteAllText(DB.SourceFolder + FileName, TextFromDB);
                        }
                    }
                }
            }
        }
        private static Encoding GetEncoding(string FullFileName)
        {
            var detector = new CharsetDetector();
            byte[] bytes = File.ReadAllBytes(FullFileName);
            detector.Feed(bytes, 0, bytes.Length);
            detector.DataEnd();
            string encoding = detector.Charset;
            if (encoding == "windows-1255")
            {
                encoding = "windows-1251";
            }
            Encoding enc = Encoding.GetEncoding(encoding);
            return enc;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            project.DataBases.Add(new ProjectDataBase(project));
        }
    }
}
