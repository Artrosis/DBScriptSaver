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
    /// Логика взаимодействия для fmDataBasesEditor.xaml
    /// </summary>
    public partial class fmDataBasesEditor : Window
    {
        public fmDataBasesEditor(Project proj)
        {
            InitializeComponent();

            DataContext = proj;
        }
    }
}
