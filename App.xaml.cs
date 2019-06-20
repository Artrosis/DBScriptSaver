using DBScriptSaver.ViewModels;
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
                Application app = new App();
                app.Run(new fmProjectsEditor(new DBScriptViewModel()));

                
            }
            catch (Exception e)
            {
                MessageBox.Show($"Ошибка: {e.Message}");
            }
        }
    }
}
