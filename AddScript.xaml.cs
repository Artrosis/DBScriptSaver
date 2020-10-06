using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml.Linq;
using DBScriptSaver.ViewModels;

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для AddScript.xaml
    /// </summary>
    public partial class AddScript : Window
    {
        private ProjectDataBase dB;

        public AddScript()
        {
            InitializeComponent();
        }

        public AddScript(ProjectDataBase dB):this()
        {
            this.dB = dB;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            dB.CreateChangesXML();

            string NewFileName = tbFileName.Text;
            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                NewFileName = NewFileName.Replace(invalidChar, '_');
            }

            string ScriptBody = new TextRange(tbScriptBody.Document.ContentStart, tbScriptBody.Document.ContentEnd).Text;

            dB.AddMigration(new Migration() { Name = NewFileName, Script = ScriptBody });

            DialogResult = true;
        }
    }
}
