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
    /// Логика взаимодействия для DependenciesSettings.xaml
    /// </summary>
    public partial class DependenciesSettings : Window
    {
        private ProjectDataBase dB => (ProjectDataBase)DataContext;
        public DependenciesSettings()
        {
            InitializeComponent();
        }
        
        public DependenciesSettings(ProjectDataBase dB) : this()
        {
            DataContext = dB;
            dB.UpdateDBObjects();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            dB.SaveDependencies();
        }

        private void AddDependence(object sender, RoutedEventArgs e)
        {
            var depObj = cbDepObj.SelectedItem as DependenceObject;
            var dependency = cbDependency.SelectedItem as DependenceObject;

            if (depObj == null || dependency == null)
            {
                return;
            }

            var curDepObj = dB.Dependencies
                                .SingleOrDefault(d => d.ЗависимыйОбъект.ObjectType == depObj.ObjectType 
                                                    && d.ЗависимыйОбъект.ObjectName == depObj.ObjectName);

            if (curDepObj != null)
            {
                var curDependency = curDepObj.Зависимости
                                                .SingleOrDefault(d => d.ObjectType == dependency.ObjectType
                                                                    && d.ObjectName == dependency.ObjectName);

                if (curDependency == null)
                {
                    curDepObj.Зависимости.Add(dependency);
                }
            }
            else
            {
                var NewDependence = new Dependence();
                NewDependence.ЗависимыйОбъект = depObj;
                NewDependence.Зависимости.Add(dependency);
                dB.Dependencies.Add(NewDependence);
            }
        }

        private void CbDepObj_TextChanged(object sender, TextChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            cb.IsDropDownOpen = true;
            var tb = (TextBox)e.OriginalSource;
            CollectionView cv = (CollectionView)CollectionViewSource.GetDefaultView(cb.ItemsSource);
            cv.Filter = s => ((DependenceObject)s).ObjectName.IndexOf(cb.Text.Substring(0, tb.SelectionStart), StringComparison.CurrentCultureIgnoreCase) >= 0;                
        }

        private void DeleteDependence(object sender, RoutedEventArgs e)
        {
            var depObj = cbDepObj.SelectedItem as DependenceObject;
            var dependency = cbDependency.SelectedItem as DependenceObject;

            if (depObj == null || dependency == null)
            {
                return;
            }

            var curDepObj = dB.Dependencies
                                .SingleOrDefault(d => d.ЗависимыйОбъект.ObjectType == depObj.ObjectType
                                                    && d.ЗависимыйОбъект.ObjectName == depObj.ObjectName);

            if (curDepObj != null)
            {
                var curDependency = curDepObj.Зависимости
                                                .SingleOrDefault(d => d.ObjectType == dependency.ObjectType
                                                                    && d.ObjectName == dependency.ObjectName);

                if (curDependency != null)
                {
                    curDepObj.Зависимости.Remove(curDependency);
                }
            }
        }
    }
}
