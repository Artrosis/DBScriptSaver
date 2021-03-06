﻿using DBScriptSaver.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для fmProjectsEditor.xaml
    /// </summary>
    public partial class fmProjectsEditor : Window
    {
        const string Salt = nameof(DBScriptViewModel);

        public static string GetSalt()
        {
            return Salt;
        }

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

        private void btnAddProject_Click(object sender, RoutedEventArgs e)
        {
            Vm.Projects.Add(new Project(Vm));
        }
        ProjectServer CurServer => gcServers.SelectedItem as ProjectServer;
        private void ServerDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Удалить сервер?", "Внимание!", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                Vm.Servers.Remove(CurServer);
            }
        }
        private void pwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            CurServer.DBPassword = Cryptography.Encrypt((sender as PasswordBox).Password, GetSalt());
        }

        private void btnAddServer_Click(object sender, RoutedEventArgs e)
        {
            Vm.Servers.Add(new ProjectServer(Vm));
        }
    }
}
