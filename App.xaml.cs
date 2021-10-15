using System;
using System.Threading;
using System.Windows;

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string AppMutexName = "ScriptSaver";
        [STAThread]
        public static void Main()
        {
            try
            {
                using (Mutex mutex = new Mutex(false, AppMutexName))
                {
                    bool Running = !mutex.WaitOne(0, false);
                    if (!Running)
                    {
                        App app = new App
                        {
                            ShutdownMode = ShutdownMode.OnExplicitShutdown
                        };

                        TaskbarIconHelper.CreateTaskbarIcon();
                        app.Run();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Ошибка: {e.Message}");
            }
        }
    }
}
