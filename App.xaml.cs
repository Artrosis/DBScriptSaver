using DBScriptSaver.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                App app = new App();

                TaskbarIcon tbi = new TaskbarIcon();
                Stream iconStream = GetResourceStream(new Uri("pack://application:,,,/DBScriptSaver;component/ico/main.ico")).Stream;
                tbi.Icon = new System.Drawing.Icon(iconStream);
                tbi.ToolTipText = "Помошник сохранения скриптов";

                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                tbi.ContextMenu = GetContextMenu();
                app.Run();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Ошибка: {e.Message}");
            }
        }

        private static ContextMenu GetContextMenu()
        {
            var res = new ContextMenu();

            var SettingsItem = new MenuItem();
            SettingsItem.Header = "Настройки";
            SettingsItem.PreviewMouseDown += SettingsItem_PreviewMouseDown;

            SettingsItem.Icon = new System.Windows.Controls.Image() { Source = new BitmapImage(new Uri("pack://application:,,,/DBScriptSaver;component/img/Settings.png")), Width = 16, Height = 16 };

            res.Items.Add(SettingsItem);

            res.Items.Add(new Separator());

            var CloseItem = new MenuItem();
            CloseItem.Header = "Выход";
            CloseItem.PreviewMouseDown += CloseItem_PreviewMouseDown;
            res.Items.Add(CloseItem);

            return res;
        }

        private static void SettingsItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            new fmProjectsEditor(new DBScriptViewModel()).Show();
        }

        private static void CloseItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Current.Shutdown();
        }
    }
}
