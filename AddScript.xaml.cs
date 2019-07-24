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
            if (!File.Exists(dB.ChangesXML))
            {
                XDocument EmptyChanges = new XDocument();
                XElement project = new XElement("project");
                project.Add(new XAttribute("name", dB.Project.Name));

                XElement VersionElem = new XElement("ver");
                VersionElem.Add(new XAttribute("id", "1.0.0.0"));
                VersionElem.Add(new XAttribute("date", DateTime.Now.ToShortDateString()));
                project.Add(VersionElem);

                EmptyChanges.Save(dB.ChangesXML);
            }

            XDocument xdoc = XDocument.Load(dB.ChangesXML);

            var LastVer = xdoc.Element("project").Elements("ver").Last();

            string NewFileName = tbFileName.Text + ".xml";

            var NewElement = new XElement("file", NewFileName);
            NewElement.Add(new XAttribute("autor", Environment.MachineName));
            NewElement.Add(new XAttribute("date", DateTime.Now.ToShortDateString()));

            LastVer.Add(NewElement);

            xdoc.Save(dB.ChangesXML);

            string ScriptBody = new TextRange(tbScriptBody.Document.ContentStart, tbScriptBody.Document.ContentEnd).Text;

            File.WriteAllText(dB.ChangesFolder + NewFileName, ScriptBody, new UTF8Encoding(true));

            DialogResult = true;
        }
    }
}
