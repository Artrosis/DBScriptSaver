using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public DBScriptViewModel Vm { get; set; } = new DBScriptViewModel();

        private void EditProjects(object sender, RoutedEventArgs e)
        {
            var fmEditor = new fmProjectsEditor(Vm) { Owner = this };
            fmEditor.ShowDialog();
        }

        internal enum CheckingConResult { HasNewCheckingConnection, Completed, Failed };
        private void teServer_EditValueChanged(object sender)
        {
            //tbServer.Properties.ContextImage = null;

            //Func<object, CheckingConResult> CheckConnectionFunc = ServerName =>
            //{
            //    //Практически уникальное имя потока
            //    Thread.CurrentThread.Name = (string)ServerName + DateTime.Now.Second + DateTime.Now.Millisecond;
            //    SetCheckingServerName(Thread.CurrentThread.Name);

            //    using (SqlConnection con = new SqlConnection())
            //    {
            //        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            //        {
            //            DataSource = (string)ServerName,
            //            InitialCatalog = "master",
            //            UserID = Properties.Settings.Default.DBLogin,
            //            Password = Cryptography.Decrypt(Properties.Settings.Default.DBPass, Salt),
            //            ConnectTimeout = 3
            //        };
            //        con.ConnectionString = builder.ConnectionString;
            //        try
            //        {
            //            con.Open();
            //            return (Thread.CurrentThread.Name != GetCheckingServerName()) ? CheckingConResult.HasNewCheckingConnection : CheckingConResult.Completed;
            //        }
            //        catch (Exception)
            //        {
            //            return (Thread.CurrentThread.Name != GetCheckingServerName()) ? CheckingConResult.HasNewCheckingConnection : CheckingConResult.Failed;
            //        }
            //    }
            //};

            //CheckingConResult CheckConnection = await Task<CheckingConResult>.Factory.StartNew(CheckConnectionFunc, ((Rep)cbServer.SelectedItem).Path);

            //if (CheckConnection == CheckingConResult.HasNewCheckingConnection)
            //{
            //    return;
            //}

            //if (CheckConnection == CheckingConResult.Completed)
            //{
            //    cbServer.Properties.ContextImage = imgs.Images[1];
            //    cbDB.Properties.Items.Clear();

            //    using (SqlConnection con = new SqlConnection())
            //    {
            //        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            //        {
            //            DataSource = ((Rep)cbServer.SelectedItem).Path,
            //            InitialCatalog = "master",
            //            UserID = Properties.Settings.Default.DBLogin,
            //            Password = Cryptography.Decrypt(Properties.Settings.Default.DBPass, Salt),
            //            ConnectTimeout = 3
            //        };
            //        con.ConnectionString = builder.ConnectionString;
            //        try
            //        {
            //            con.Open();

            //            const string strSQL = @"SELECT d.name
            //                                FROM   sys.databases AS d
            //                                WHERE  NAME NOT IN('master', 'model', 'msdb', 'pubs', 'tempdb', 'distribution')";
            //            SqlCommand myCommand = new SqlCommand(strSQL, con);

            //            var Reader = myCommand.ExecuteReader();
            //            while (Reader.Read())
            //            {
            //                cbDB.Properties.Items.Add((string)Reader[0]);
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine(ex.Message);
            //        }
            //        finally
            //        {
            //            con.Close();
            //        }
            //    }
            //}
            //else if (CheckConnection == CheckingConResult.Failed)
            //{
            //    cbServer.Properties.ContextImage = imgs.Images[0];
            //};
        }

        private void EditDBObjectFilter(object sender, RoutedEventArgs e)
        {
            var fmEditor = new fmEditDBObjectFilter(Vm) { Owner = this };
            fmEditor.ShowDialog();
        }

        private void RefreshObjects(object sender, RoutedEventArgs e)
        {

        }
    }
}
