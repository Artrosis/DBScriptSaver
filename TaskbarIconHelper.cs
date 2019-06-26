﻿using DBScriptSaver.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DBScriptSaver
{
    public class TaskbarIconHelper
    {
        static TaskbarIcon tbi = new TaskbarIcon();
        internal static void CreateTaskbarIcon()
        {
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/DBScriptSaver;component/ico/main.ico")).Stream;
            tbi.Icon = new System.Drawing.Icon(iconStream);
            tbi.ToolTipText = "Помошник сохранения скриптов";
            UpdateContextMenu();
        }

        public static void UpdateContextMenu()
        {
            var menu = new ContextMenu();

            var Settings = new DBScriptViewModel();

            foreach (var proj in Settings.Projects)
            {
                var ProjectItem = new MenuItem();
                ProjectItem.Header = proj.Name;
                ProjectItem.Tag = proj;
                menu.Items.Add(ProjectItem);

                foreach (var db in proj.DataBases)
                {
                    var DBItem = new MenuItem();
                    DBItem.Header = db.Name;
                    DBItem.Tag = db;
                    ProjectItem.Items.Add(DBItem);

                    var UpdateDBItem = new MenuItem();
                    UpdateDBItem.Header = "Обновить";
                    UpdateDBItem.Tag = db;
                    UpdateDBItem.PreviewMouseDown += UpdateDBItem_PreviewMouseDown;
                    UpdateDBItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/Refresh.png")), Width = 16, Height = 16 };
                    DBItem.Items.Add(UpdateDBItem);

                    var DBSettingsItem = new MenuItem();
                    DBSettingsItem.Header = "Настройки объектов БД";
                    DBSettingsItem.Tag = db;
                    DBSettingsItem.PreviewMouseDown += DBSettingsItem_PreviewMouseDown; ;
                    DBSettingsItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/Settings.png")), Width = 16, Height = 16 };
                    DBItem.Items.Add(DBSettingsItem);
                }

                var ProjectSettingsItem = new MenuItem();
                ProjectSettingsItem.Header = "Настройки проекта";
                ProjectSettingsItem.Tag = proj;
                ProjectSettingsItem.PreviewMouseDown += ProjectSettingsItem_PreviewMouseDown; ;
                ProjectSettingsItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/Settings.png")), Width = 16, Height = 16 };
                ProjectItem.Items.Add(ProjectSettingsItem);
            }

            menu.Items.Add(new Separator());

            var SettingsItem = new MenuItem();
            SettingsItem.Header = "Настройки";
            SettingsItem.PreviewMouseDown += SettingsItem_PreviewMouseDown;
            SettingsItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/Settings.png")), Width = 16, Height = 16 };
            menu.Items.Add(SettingsItem);

            menu.Items.Add(new Separator());

            var CloseItem = new MenuItem();
            CloseItem.Header = "Выход";
            CloseItem.PreviewMouseDown += CloseItem_PreviewMouseDown;
            menu.Items.Add(CloseItem);

            tbi.ContextMenu = menu;
        }

        private static void DBSettingsItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var DB = ((MenuItem)sender).Tag as ProjectDataBase;

            if (DB == null)
            {
                return;
            }

            new DBObjectsFiltering(DB).ShowDialog();
        }

        private static void UpdateDBItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var DB = ((MenuItem)sender).Tag as ProjectDataBase;

            if (DB == null)
            {
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DB.UpdateScripts();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }            
        }

        private static void ProjectSettingsItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var proj = ((MenuItem)sender).Tag as Project;

            if (proj == null)
            {
                return;
            }

            new fmDataBasesEditor(proj).ShowDialog();
        }

        private static void SettingsItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            new fmProjectsEditor(new DBScriptViewModel()).Show();
        }

        private static void CloseItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}