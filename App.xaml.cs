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
                App app = new App
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };

                TaskbarIconHelper.CreateTaskbarIcon();
                app.Run();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Ошибка: {e.Message}");
            }
        }
    }
}
