using DBScriptSaver.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.IO;
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

            DataContext = Vm;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Vm.SaveProjects();
            DialogResult = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Удалить проект?", "Внимание!", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                Vm.Projects.Remove(CurProject);
            }
        }

        Project CurProject => gcProjects.SelectedItem as Project;

        private void EditDataBase_Click(object sender, RoutedEventArgs e)
        {
            if (CurProject == null)
            {
                return;
            }

            var fmEditor = new fmDataBasesEditor(CurProject) { Owner = this };
            fmEditor.ShowDialog();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            Vm.Projects.Add(new Project(Vm));
        }

        private void pwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            CurProject.DBPassword = (sender as PasswordBox).SecurePassword;
        }
    }
}
