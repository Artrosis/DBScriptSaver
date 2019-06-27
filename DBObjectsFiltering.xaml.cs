using DBScriptSaver.ViewModels;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для DBObjectsFiltering.xaml
    /// </summary>
    
    public partial class DBObjectsFiltering : Window
    {
        private ProjectDataBase db => ((ProjectDataBase)DataContext);
        public DBObjectsFiltering(ProjectDataBase dB)
        {
            DataContext = dB;
            
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                dB.UpdateFilterDataFromConfig();

                InitializeComponent();

                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
                {
                    DataSource = dB.Project.Server,
                    InitialCatalog = dB.Name ?? @"master",
                    UserID = "Kobra_main",
                    Password = "Ggv123",
                    ConnectTimeout = 3
                };
                SqlConnection conn = new SqlConnection(builder.ConnectionString);

                Server server = new Server(new ServerConnection(conn));

                if (!HasConnection(builder.ConnectionString))
                {
                    MessageBox.Show($@"Не удалось подключиться к серверу: {dB.Project.Server}");
                    return;
                }

                Database dataBase = server.Databases.Cast<Database>().ToList().SingleOrDefault(d => d.Name == dB.Name);

                if (dataBase == null)
                {
                    MessageBox.Show($@"На сервере {dB.Project.Server} не найдена база данных: {dB.Name}");
                    return;
                }

                dataBase.Schemas.Cast<Schema>().ToList()
                    .Where(s => !s.IsSystemObject).ToList()
                    .ForEach(s =>
                    {
                        StackPanel ElementPanel = new StackPanel() { Orientation = Orientation.Horizontal, Tag = "Element" };
                        listShemas.Children.Add(ElementPanel);

                        string sName = $@"{s.Name}";

                        Sch sh = dB.Schemas.SingleOrDefault(sch => sch.ToString() == sName);

                        ElementPanel.Children.Add(new Label()
                        {
                            Width = 300,
                            HorizontalContentAlignment = HorizontalAlignment.Left,
                            Content = sName
                        });
                        ElementPanel.Children.Add(new CheckBox()
                        {
                            Width = 150,
                            IsChecked = (sh?.State == ObjectState.Отслеживаемый)
                        });
                        ElementPanel.Children.Add(new CheckBox()
                        {
                            Width = 150,
                            IsChecked = (sh?.State == ObjectState.Игнорируемый)
                        });
                    }
                    );

                dataBase.StoredProcedures.Cast<StoredProcedure>().ToList()
                    .Where(sp => sp.Schema != "sys").ToList()
                    .ForEach(sp =>
                    {
                        string spName = $@"{sp.Schema}.{sp.Name}";
                        Procedure proc = dB.Procedures.SingleOrDefault(s => s.ToString() == spName);

                        if (proc == null)
                        {
                            var NewProc = new Procedure(spName);
                            dB.Procedures.Add(NewProc);
                        }
                    }
                    );

                dataBase.UserDefinedFunctions.Cast<UserDefinedFunction>().ToList()
                    .Where(f => f.Schema != "sys").ToList()
                    .ForEach(f =>
                    {
                        string fnName = $@"{f.Schema}.{f.Name}";
                        Function fn = dB.Functions.SingleOrDefault(fun => fun.ToString() == fnName);

                        if (fn == null)
                        {
                            var NewFn = new Function(fnName);
                            dB.Functions.Add(NewFn);
                        }
                    }
                    );
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }            
        }

        private bool HasConnection(string connectionString)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    con.Open();
                    using (DbCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandTimeout = 3;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "select 1";
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(db.FilterFile))
            {
                File.Delete(db.FilterFile);
            }

            XElement DBObjects = new XElement("DBObjects");

            var XProcedures = db.Procedures.Where(s => s.State != ObjectState.Не_указан).Select(s =>
            {
                var sp = new XElement("Procedure", s);
                sp.Add(new XAttribute(XName.Get("State"), s.State.ToString()));
                return sp;
            });

            XElement spNames = new XElement("Procedures", XProcedures);
            DBObjects.Add(spNames);

            var XFunctions = db.Functions.Where(s => s.State != ObjectState.Не_указан).Select(s =>
            {
                var fn = new XElement("Function", s);
                fn.Add(new XAttribute(XName.Get("State"), s.State.ToString()));
                return fn;
            });

            XElement fnNames = new XElement("Functions", XFunctions);
            DBObjects.Add(fnNames);

            File.AppendAllText(db.FilterFile, DBObjects.ToString());

            DialogResult = true;
        }

        private void CheckFromSaved_Click(object sender, RoutedEventArgs e)
        {
            string SourceFolder = db.BaseFolder + @"source";

            DirectoryInfo dir = new DirectoryInfo(SourceFolder);

            List<string> SavedObjects = new List<string>();

            foreach (var proc in dir.GetFiles("*.sql", SearchOption.TopDirectoryOnly))
            {
                SavedObjects.Add(Path.GetFileNameWithoutExtension(proc.Name));
            }

            foreach (var sp in db.Procedures)
            {
                if (SavedObjects.Contains(sp.FullName))
                {
                    sp.IsTrace = true;
                }
            }

            foreach (var fn in db.Functions)
            {
                if (SavedObjects.Contains(fn.FullName))
                {
                    fn.IsTrace = true;
                }
            }
        }
    }
}
