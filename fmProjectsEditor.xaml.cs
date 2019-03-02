using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для fmProjectsEditor.xaml
    /// </summary>
    public partial class fmProjectsEditor : Window
    {
        public DBScriptViewModel Vm;
        public fmProjectsEditor(DBScriptViewModel viewModel)
        {
            InitializeComponent();

            Vm = viewModel;

            this.DataContext = Vm;            
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnOK_Copy_Click(object sender, RoutedEventArgs e)
        {
            Vm.AddProject();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Удалить проект?", "Внимание!", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                Vm.Projects.Remove(gcProjects.SelectedItem as Project);
            }
        }
    }
}
