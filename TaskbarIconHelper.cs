using DBScriptSaver.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DBScriptSaver
{
    public class TaskbarIconHelper
    {
        static readonly TaskbarIcon tbi = new TaskbarIcon();
        internal static void CreateTaskbarIcon()
        {
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/DBScriptSaver;component/ico/main.ico")).Stream;
            tbi.Icon = new System.Drawing.Icon(iconStream);
            tbi.ToolTipText = "Помощник сохранения скриптов";
            UpdateContextMenu();
        }

        public static void UpdateContextMenu()
        {
            var menu = new ContextMenu();

            var Settings = new DBScriptViewModel();

            foreach (var proj in Settings.Projects)
            {
                var ProjectItem = new MenuItem
                {
                    Header = proj.Name,
                    Tag = proj
                };
                menu.Items.Add(ProjectItem);

                foreach (var db in proj.DataBases)
                {
                    bool HasConnect = false;

                    try
                    {
                        HasConnect = db.Project.Server.GetDBQueryHelper().CheckConnection();
                    }
                    catch (Exception) { }

                    var DBItem = new MenuItem
                    {
                        IsEnabled = false,
                        Header = db.Name + (!HasConnect ? @"(Нет подключения)" : ""),
                        Tag = db
                    };
                    ProjectItem.Items.Add(DBItem);

                    var AddScriptItem = new MenuItem
                    {
                        Header = @"Добавить миграцию",
                        Tag = db
                    };
                    AddScriptItem.PreviewMouseDown += AddScriptItem_PreviewMouseDown; ;
                    AddScriptItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/add_script.png")), Width = 16, Height = 16 };
                    ProjectItem.Items.Add(AddScriptItem);

                    var DBDependenciesItem = new MenuItem
                    {
                        Header = @"Зависимости развёртывания",
                        Tag = db
                    };
                    DBDependenciesItem.PreviewMouseDown += DBDependenciesItem_PreviewMouseDown;
                    DBDependenciesItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/chain.png")), Width = 16, Height = 16 };
                    ProjectItem.Items.Add(DBDependenciesItem);

                    if (!HasConnect)
                    {
                        continue;
                    }

                    var UpdateDBItem = new MenuItem
                    {
                        Header = @"Обновить",
                        Tag = db
                    };
                    UpdateDBItem.PreviewMouseDown += UpdateDBItem_PreviewMouseDown;
                    UpdateDBItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/Refresh.png")), Width = 16, Height = 16 };
                    ProjectItem.Items.Add(UpdateDBItem);

                    var DBSettingsItem = new MenuItem
                    {
                        Header = @"Настройки объектов БД",
                        Tag = db
                    };
                    DBSettingsItem.PreviewMouseDown += DBSettingsItem_PreviewMouseDown; ;
                    DBSettingsItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/Settings.png")), Width = 16, Height = 16 };
                    ProjectItem.Items.Add(DBSettingsItem);

                    ProjectItem.Items.Add(new Separator());
                }

                var ProjectSettingsItem = new MenuItem
                {
                    Header = @"Настройки проекта",
                    Tag = proj
                };
                ProjectSettingsItem.PreviewMouseDown += ProjectSettingsItem_PreviewMouseDown; ;
                ProjectSettingsItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/Settings.png")), Width = 16, Height = 16 };
                ProjectItem.Items.Add(ProjectSettingsItem);
            }

            menu.Items.Add(new Separator());

            var SettingsItem = new MenuItem
            {
                Header = "Настройки"
            };
            SettingsItem.PreviewMouseDown += SettingsItem_PreviewMouseDown;
            SettingsItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/Settings.png")), Width = 16, Height = 16 };
            menu.Items.Add(SettingsItem);

            menu.Items.Add(new Separator());

            var CloseItem = new MenuItem
            {
                Header = "Выход"
            };
            CloseItem.PreviewMouseDown += CloseItem_PreviewMouseDown;
            menu.Items.Add(CloseItem);

            tbi.ContextMenu = menu;
        }

        private static void DBDependenciesItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(((MenuItem)sender).Tag is ProjectDataBase DB))
            {
                return;
            }

            new DependenciesSettings(DB).ShowDialog();
        }

        private static void AddScriptItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(((MenuItem)sender).Tag is ProjectDataBase DB))
            {
                return;
            }

            new AddScript(DB).ShowDialog();
        }

        private static void DBSettingsItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(((MenuItem)sender).Tag is ProjectDataBase DB))
            {
                return;
            }

            try
            {
                new DBObjectsFiltering(DB).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Фильтр объектов");
            }
        }

        private static void UpdateDBItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(((MenuItem)sender).Tag is ProjectDataBase DB))
            {
                return;
            }

            try
            {
                new CompareScripts(DB).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"Ошибка обновления скриптов: {ex.Message}", @"Изменения по скриптам");
            }
        }

        private static void ProjectSettingsItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(((MenuItem)sender).Tag is Project proj))
            {
                return;
            }

            new fmDataBasesEditor(proj).ShowDialog();
        }

        private static void SettingsItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            new fmProjectsEditor(new DBScriptViewModel()).ShowDialog();
        }

        private static void CloseItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
