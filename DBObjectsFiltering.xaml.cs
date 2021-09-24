using DBScriptSaver.Core;
using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
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

        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        public DBObjectsFiltering(ProjectDataBase dB)
        {
            timer.Tick += ApplyTextFilter;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 400);

            DataContext = dB;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                dB.UpdateFilterDataFromConfig();

                InitializeComponent();

                if (!HasConnection(db.Project.Server.GetDBQueryHelper()))
                {
                    throw new Exception($@"Не удалось подключиться к серверу: {dB.Project.Server}");
                }

                if (!db.Project.Server.DBNames.Any(b => b == dB.Name))
                {
                    MessageBox.Show($@"На сервере {dB.Project.Server} не найдена база данных: {dB.Name}");
                    return;
                }

                foreach (string s in dB.GetSchemasFromDB())
                {
                    if (!dB.Schemas.Any(sh => sh.ToString() == s))
                    {
                        var NewSchema = new Sch(s);
                        dB.Schemas.Add(NewSchema);
                    }
                }

                foreach (Procedure sp in dB.GetStoredProceduresFromDB())
                {
                    string spName = $@"{sp.Schema}.{sp.Name}";

                    if (!dB.Procedures.Any(s => s.ToString() == spName))
                    {
                        dB.Procedures.Add(sp);
                    }
                }

                foreach (Function f in dB.GetFunctionsFromDB())
                {
                    string fnName = $@"{f.Schema}.{f.Name}";

                    if (!dB.Functions.Any(fun => fun.ToString() == fnName))
                    {
                        dB.Functions.Add(f);
                    }
                }

                foreach (Tbl t in dB.GetTablesFromDB())
                {
                    string tblName = $@"{t.Schema}.{t.Name}";

                    if (!dB.Tables.Any(tab => tab.ToString() == tblName))
                    {
                        dB.Tables.Add(t);
                    }
                }

                Filtering();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ApplyTextFilter(object sender, EventArgs e)
        {
            timer.Stop();
            Filtering();
        }

        private bool Filter(object obj)
        {
            var o = obj as DBObject;

            if (tbFilter.Text.Length > 0)
            {
                if (!o.FullName.ToUpper().Contains(tbFilter.Text.ToUpper()))
                {
                    return false;
                }
            }

            switch (o.State)
            {
                case ObjectState.Не_указан:
                    return ShowOnSchema(o.Schema);
                case ObjectState.Отслеживаемый:
                    return ShowTraced.IsChecked ?? false;
                case ObjectState.Игнорируемый:
                    return ShowIgnored.IsChecked ?? false;
                default:
                    return true;
            }
        }

        private bool ShowOnSchema(string schema)
        {
            switch (db.Schemas.Single(s => s.Name == schema).State)
            {
                case ObjectState.Не_указан:
                    return true;
                case ObjectState.Отслеживаемый:
                    return ShowTraced.IsChecked ?? false;
                case ObjectState.Игнорируемый:
                    return ShowIgnored.IsChecked ?? false;
                default:
                    return true;
            }
        }

        private bool HasConnection(IDBQueryHelper helper)
        {
            try
            {
                return helper.CheckConnection();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(db.FilterFile))
            {
                File.Delete(db.FilterFile);
            }

            XElement DBObjects = new XElement("DBObjects");

            var XSchemas = db.Schemas.Where(s => s.State != ObjectState.Не_указан).Select(s =>
            {
                var sch = new XElement("Schema", s);
                sch.Add(new XAttribute(XName.Get("State"), s.State.ToString()));
                return sch;
            });

            XElement schNames = new XElement("Schemas", XSchemas);
            DBObjects.Add(schNames);

            var XProcedures = db.Procedures
                                .Where(p => p.State != ObjectState.Не_указан)
                                .Where(p =>
                                {
                                    var schema = db.Schemas.SingleOrDefault(chs => chs.Name == p.Schema);
                                    return schema != null && schema.State != p.State;
                                })
                                .Select(p =>
                                {
                                    var sp = new XElement("Procedure", p);
                                    sp.Add(new XAttribute(XName.Get("State"), p.State.ToString()));
                                    return sp;
                                });

            XElement spNames = new XElement("Procedures", XProcedures);
            DBObjects.Add(spNames);

            var XFunctions = db.Functions
                                .Where(f => f.State != ObjectState.Не_указан)
                                .Where(f =>
                                {
                                    var schema = db.Schemas.SingleOrDefault(chs => chs.Name == f.Schema);
                                    return schema != null && schema.State != f.State;
                                })
                                .Select(f =>
                                {
                                    var fn = new XElement("Function", f);
                                    fn.Add(new XAttribute(XName.Get("State"), f.State.ToString()));
                                    return fn;
                                });

            XElement fnNames = new XElement("Functions", XFunctions);
            DBObjects.Add(fnNames);

            var XTables = db.Tables
                                .Where(t => t.State != ObjectState.Не_указан)
                                .Where(t =>
                                {
                                    var schema = db.Schemas.SingleOrDefault(chs => chs.Name == t.Schema);
                                    return schema != null && schema.State != t.State;
                                })
                                .Select(t =>
                                {
                                    var fn = new XElement("Table", t);
                                    fn.Add(new XAttribute(XName.Get("State"), t.State.ToString()));
                                    return fn;
                                });

            XElement tblNames = new XElement("Tables", XTables);
            DBObjects.Add(tblNames);

            File.AppendAllText(db.FilterFile, DBObjects.ToString());

            DialogResult = true;
        }

        private void CheckFromSaved_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
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

                List<string> UsesSchemas = SavedObjects.Select(o => o.GetSchema()).Distinct().ToList();

                foreach (var s in db.Schemas)
                {
                    if (s.State == ObjectState.Не_указан && !UsesSchemas.Contains(s.Name))
                    {
                        s.IsIgnore = true;
                    }
                }
                Filtering();
                DataPanel.UpdateLayout();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ShowTraced_Checked(object sender, RoutedEventArgs e)
        {
            Filtering();
        }

        private void Filtering()
        {
            (gcProcedures.ItemsSource as ListCollectionView).Filter = new Predicate<object>(Filter);
            (gcFunctions.ItemsSource as ListCollectionView).Filter = new Predicate<object>(Filter);
            (gcTables.ItemsSource as ListCollectionView).Filter = new Predicate<object>(Filter);
        }

        private void gcSchemas_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Filtering();
        }

        private void tbFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            timer.Stop();
            timer.Start();
        }

        private void CheckedAllSchemasIgnore(object sender, RoutedEventArgs e)
        {
            db.Schemas.ToList().ForEach(s => s.IsIgnore = true);

            chAllSchemasTrace.Unchecked -= UnCheckedAllSchemasTrace;
            try
            {
                chAllSchemasTrace.IsChecked = false;
            }
            finally
            {
                chAllSchemasTrace.Unchecked += UnCheckedAllSchemasTrace;
            }
        }

        private void UnCheckedAllSchemasIgnore(object sender, RoutedEventArgs e)
        {
            db.Schemas.ToList().ForEach(s => s.IsIgnore = false);
        }

        private void CheckedAllSchemasTrace(object sender, RoutedEventArgs e)
        {
            db.Schemas.ToList().ForEach(s => s.IsTrace = true);

            chAllSchemasIgnore.Unchecked -= UnCheckedAllSchemasIgnore;
            try
            {
                chAllSchemasIgnore.IsChecked = false;
            }
            finally
            {
                chAllSchemasIgnore.Unchecked += UnCheckedAllSchemasIgnore;
            }
        }

        private void UnCheckedAllSchemasTrace(object sender, RoutedEventArgs e)
        {
            db.Schemas.ToList().ForEach(s => s.IsTrace = false);
        }
    }
}
