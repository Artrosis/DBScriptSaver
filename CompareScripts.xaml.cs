using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
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

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для CompareScripts.xaml
    /// </summary>
    public partial class CompareScripts : Window
    {
        private ProjectDataBase DB;

        public CompareScripts()
        {
            InitializeComponent();
        }

        public CompareScripts(ProjectDataBase db, List<(string FileName, string FullPath, string ScriptText)> scripts) : this()
        {
            DataContext = scripts;
            gcDBObjects.ItemsSource = new ListCollectionView(scripts.Select(t => new ScriptWrapper(t)).ToList());
            DB = db;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            var Выбранные_скрипты = ((ListCollectionView)gcDBObjects.ItemsSource).Cast<ScriptWrapper>().Where(w => w.Save).Select(w => w.t).ToList();
            DB.UpdateScripts(Выбранные_скрипты);
            DialogResult = true;
        }

        private void ВыбратьВсе_Click(object sender, RoutedEventArgs e)
        {
            ((ListCollectionView)gcDBObjects.ItemsSource).Cast<ScriptWrapper>().ToList().ForEach(w => w.Save = true);
        }

        private void ОтменитьВсе_Click(object sender, RoutedEventArgs e)
        {
            ((ListCollectionView)gcDBObjects.ItemsSource).Cast<ScriptWrapper>().ToList().ForEach(w => w.Save = false);
        }
    }
}
