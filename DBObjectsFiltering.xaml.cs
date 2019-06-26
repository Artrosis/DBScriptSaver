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
            InitializeComponent();

            DataContext = dB;
            
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                dB.UpdateFilterDataFromConfig();

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

                        ElementPanel.Children.Add(new Label()
                        {
                            Width = 300,
                            HorizontalContentAlignment = HorizontalAlignment.Left,
                            Content = sName
                        });
                        ElementPanel.Children.Add(new CheckBox()
                        {
                            Width = 150,
                        });
                        ElementPanel.Children.Add(new CheckBox()
                        {
                            Width = 150,
                        });
                    }
                    );

                dataBase.StoredProcedures.Cast<StoredProcedure>().ToList()
                    .Where(sp => sp.Schema != "sys").ToList()
                    .ForEach(sp =>
                    {
                        StackPanel ElementPanel = new StackPanel() { Orientation = Orientation.Horizontal, Tag = "Element" };
                        listProcedures.Children.Add(ElementPanel);

                        string spName = $@"{sp.Schema}.{sp.Name}";

                        ElementPanel.Children.Add(new Label()
                                                    {
                                                        Width = 300,
                                                        HorizontalContentAlignment = HorizontalAlignment.Left,
                                                        Content = spName
                        });
                        ElementPanel.Children.Add(new CheckBox()
                                                    {
                                                        Width = 150,
                                                        IsChecked = dB.traceProcedures.Contains(spName),
                                                        Tag = "Trace"
                        });
                        ElementPanel.Children.Add(new CheckBox()
                        {
                            Width = 150,
                            IsChecked = dB.IgnoreProcedures.Contains(spName),
                            Tag = "Ignore"
                        });

                        ElementPanel.Visibility = !(ShowIgnored.IsChecked ?? false) && dB.IgnoreProcedures.Contains(spName) ? Visibility.Collapsed : Visibility.Visible;
                    }
                    );
                dataBase.UserDefinedFunctions.Cast<UserDefinedFunction>().ToList()
                    .Where(f => f.Schema != "sys").ToList()
                    .ForEach(f =>
                    {
                        StackPanel ElementPanel = new StackPanel() { Orientation = Orientation.Horizontal, Tag = "Element" };
                        listFunctions.Children.Add(ElementPanel);

                        string fnName = $@"{f.Schema}.{f.Name}";

                        ElementPanel.Children.Add(new Label()
                        {
                            Width = 300,
                            HorizontalContentAlignment = HorizontalAlignment.Left,
                            Content = fnName
                        });
                        ElementPanel.Children.Add(new CheckBox()
                        {
                            Width = 150,
                            IsChecked = dB.traceFunctions.Contains(fnName)
                        });
                        ElementPanel.Children.Add(new CheckBox()
                        {
                            Width = 150,
                            IsChecked = dB.IgnoreFunctions.Contains(fnName),
                            Tag = "Ignore"
                        });

                        ElementPanel.Visibility = !(ShowIgnored.IsChecked ?? false) && dB.IgnoreFunctions.Contains(fnName) ? Visibility.Collapsed : Visibility.Visible;
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

            var lstProcedures = listProcedures.Children.Cast<UIElement>().Where(el => el is StackPanel && (string)((StackPanel)el).Tag == "Element").ToList();
            List<string> UsedProcedures = new List<string>();
            List<string> IgnoreProcedures = new List<string>();
            foreach (StackPanel panel in lstProcedures)
            {
                bool IsUse = ((CheckBox)panel.Children[1]).IsChecked ?? false;
                if (IsUse)
                {
                    string spName = (string)((Label)panel.Children[0]).Content;
                    UsedProcedures.Add(spName);
                }
                bool IsIgnore = ((CheckBox)panel.Children[2]).IsChecked ?? false;
                if (IsIgnore)
                {
                    string spName = (string)((Label)panel.Children[0]).Content;
                    IgnoreProcedures.Add(spName);
                }
            }

            XElement DBObjects = new XElement("DBObjects");
            XElement spNames = new XElement("StoredProcedures", UsedProcedures.Select(s => new XElement("StoredProcedure", s)));
            DBObjects.Add(spNames);
            XElement ispNames = new XElement("IgnoredProcedures", IgnoreProcedures.Select(s => new XElement("IgnoreStoredProcedure", s)));
            DBObjects.Add(ispNames);

            var lstFunctions = listFunctions.Children.Cast<UIElement>().Where(el => el is StackPanel && (string)((StackPanel)el).Tag == "Element").ToList();
            List<string> UsedFunctions = new List<string>();
            List<string> IgnoredFunctions = new List<string>();
            foreach (StackPanel panel in lstFunctions)
            {
                string fnName = (string)((Label)panel.Children[0]).Content;
                bool IsUse = ((CheckBox)panel.Children[1]).IsChecked ?? false;
                if (IsUse)
                {
                    UsedFunctions.Add(fnName);
                }
                bool IsIgnore = ((CheckBox)panel.Children[2]).IsChecked ?? false;
                if (IsIgnore)
                {
                    IgnoredFunctions.Add(fnName);
                }
            }

            XElement fnNames = new XElement("Functions", UsedFunctions.Select(f => new XElement("Function", f)));
            DBObjects.Add(fnNames);
            XElement ifnNames = new XElement("IgnoredFunctions", IgnoredFunctions.Select(f => new XElement("IgnoreFunction", f)));
            DBObjects.Add(ifnNames);

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
                SavedObjects.Add(System.IO.Path.GetFileNameWithoutExtension(proc.Name));
            }

            var lst = listProcedures.Children.Cast<UIElement>().Where(el => el is StackPanel && (string)((StackPanel)el).Tag == "Element").ToList();
            foreach (StackPanel panel in lst)
            {
                string spName = (string)((Label)panel.Children[0]).Content;
                if (SavedObjects.Contains(spName))
                {
                    ((CheckBox)panel.Children[1]).IsChecked = true;
                }
            }

            lst = listFunctions.Children.Cast<UIElement>().Where(el => el is StackPanel && (string)((StackPanel)el).Tag == "Element").ToList();
            foreach (StackPanel panel in lst)
            {
                string fnName = (string)((Label)panel.Children[0]).Content;
                if (SavedObjects.Contains(fnName))
                {
                    ((CheckBox)panel.Children[1]).IsChecked = true;
                }
            }
        }
    }
}
