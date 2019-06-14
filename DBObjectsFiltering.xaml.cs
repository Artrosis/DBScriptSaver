using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
    /// Логика взаимодействия для DBObjectsFiltering.xaml
    /// </summary>
    public partial class DBObjectsFiltering : Window
    {
        public DBObjectsFiltering(ViewModels.ProjectDataBase dB)
        {
            InitializeComponent();

            DataContext = dB;

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            {
                DataSource = dB.Project.Server,
                InitialCatalog = dB.Name ?? @"master",
                UserID = "Kobra_main",
                Password = "Ggv123",
                ConnectTimeout = 3
            };
            SqlConnection conn = new SqlConnection(builder.ConnectionString);

            Server server = new Server(new ServerConnection(conn));
            Database dataBase = server.Databases.Cast<Database>().ToList().Single(d => d.Name == dB.Name);

            dataBase.StoredProcedures.Cast<StoredProcedure>().ToList().Where(sp => sp.Schema != "sys").ToList().ForEach(sp => listProcedures.Children.Add(new Label() { Content = $@"{sp.Schema}.{sp.Name}"}));
        }
    }
}
