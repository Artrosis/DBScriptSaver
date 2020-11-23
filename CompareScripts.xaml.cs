using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для CompareScripts.xaml
    /// </summary>
    public partial class CompareScripts : Window
    {
        private ProjectDataBase DB;
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        public CompareScripts()
        {
            InitializeComponent();
            timer.Tick += ApplyTextFilter;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 400);
        }
        private void ApplyTextFilter(object sender, EventArgs e)
        {
            timer.Stop();
            Filtering();
        }
        private void Filtering()
        {
            (gcDBObjects.ItemsSource as ListCollectionView).Filter = new Predicate<object>(Filter);
        }
        private bool Filter(object obj)
        {
            var o = obj as ScriptWrapper;

            if (tbFilter.Text.Length > 0)
            {
                if (!o.FileName.ToUpper().Contains(tbFilter.Text.ToUpper()))
                {
                    return false;
                }
            }
            return true;
        }

        public CompareScripts(ProjectDataBase db) : this()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var scripts = db.GetUpdateScripts();
                DataContext = scripts;
                gcDBObjects.ItemsSource = new ListCollectionView(scripts.Select(t => new ScriptWrapper(t)).ToList());
                DB = db;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            var Выбранные_скрипты = ((ListCollectionView)gcDBObjects.ItemsSource).Cast<ScriptWrapper>().Where(w => w.Save).Select(w => w.getScript).ToList();
            DB.UpdateScripts(Выбранные_скрипты, cbUseMigrations.IsChecked ?? false);
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

        private void GcDBObjects_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Cmp_Files();
        }
        private void Cmp_Files()
        {
            if (!File.Exists(FileComparer.GetPath()))
            {
                //не указана сравнивалка
                return;
            }
            if (gcDBObjects.SelectedItem is ScriptWrapper s)
            {
                string tempFile = Path.GetTempFileName();

                File.WriteAllText(tempFile, s.ScriptText, new UTF8Encoding(true));

                s.EditedFilePath = tempFile;

                if (!File.Exists(s.FullPath))
                {
                    Process.Start(FileComparer.GetPath(), $@"""{tempFile}"" ""{tempFile}""");
                }
                else
                {
                    Process.Start(FileComparer.GetPath(), $@"""{s.FullPath}"" ""{tempFile}""");
                }
            }
        }

        private ScriptWrapper SelectedObject => gcDBObjects.SelectedItem as ScriptWrapper;

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Отменить изменения в базе данных?", "Отмена изменений", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }
            DB.RevertObject(SelectedObject);
            ((ListCollectionView)gcDBObjects.ItemsSource).Remove(SelectedObject);
        }

        private void tbFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            timer.Stop();
            timer.Start();
        }
        private void gcDBObjects_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var u = e.OriginalSource as UIElement;
            if (e.Key == Key.Enter)
            {
                Cmp_Files();
                e.Handled = true;
                u.MoveFocus(new TraversalRequest(FocusNavigationDirection.Last));
            }

            CheckBox cb = new CheckBox();
            if (e.Key == Key.Space && gcDBObjects.SelectedItem != null)
            {
                cb = gcDBObjects.Columns[3].GetCellContent(gcDBObjects.SelectedItem) as CheckBox;
                cb.IsChecked = !cb.IsChecked;
            }
        }
        private void scv_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
        private void gcDBObjects_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            dataGrid.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
        private void popType_MouseLeave(object sender, MouseEventArgs e)
        {
            Point leavePoint = e.GetPosition(btnTypeFilter);
            if (leavePoint.X > btnTypeFilter.ActualWidth)
                popType.IsOpen = false;
            var checkedTypes = new List<string>();
            foreach (CheckBox checkBox in filterTypes.Children)
            {
                if (checkBox.Name != "cbSelectAllTypes" && checkBox.IsChecked == true)
                {
                    checkedTypes.Add((string)checkBox.Content);
                }
            }
            FilterCollectionView = CollectionViewSource.GetDefaultView(gcDBObjects.ItemsSource);
            FilterCollectionView.Filter = t => checkedTypes.Contains((t as ScriptWrapper).ObjectType);
        }
        private void popState_MouseLeave(object sender, MouseEventArgs e)
        {
            Point leavePoint = e.GetPosition(btnStateFilter);
            if (leavePoint.X > btnStateFilter.ActualWidth)
                popState.IsOpen = false;
            var checkedState = new List<ChangeType>();
            foreach (CheckBox checkBox in filterStates.Children)
            {
                if (checkBox.Name != "cbSelectAllStates" && checkBox.IsChecked == true)
                {
                    checkedState.Add((ChangeType)checkBox.Content);
                }
            }
            FilterCollectionView = CollectionViewSource.GetDefaultView(gcDBObjects.ItemsSource);
            FilterCollectionView.Filter = t => checkedState.Contains((t as ScriptWrapper).ChangeState);
        }
        public ICollectionView FilterCollectionView { get; private set; }
        private void btnTypeFilter_Click(object sender, RoutedEventArgs e)
        {
            popType.PlacementTarget = sender as Button;
            popType.IsOpen = true;
            popState.IsOpen = false;
        }
        private void popType_Loaded(object sender, RoutedEventArgs e)
        {
            var rows = gcDBObjects.ItemsSource.OfType<ScriptWrapper>();
            var cols = rows.Select(t => t.ObjectType).Distinct();

            foreach (var type in cols)
            {
                var cb = new CheckBox
                {
                    IsChecked = true,
                    Content = type,
                    Cursor = Cursors.Hand,
                    
                };
                cb.Checked += new RoutedEventHandler(cb_Checked);
                cb.Unchecked += new RoutedEventHandler(cb_Checked);
                filterTypes.Children.Add(cb);
            }
        }
        private void cb_Checked(object sender, RoutedEventArgs e)
        {
            cbSelectAllTypes.IsChecked = filterTypes.Children.OfType<CheckBox>().
                Where(cb => cb.Name != "cbSelectAllTypes" && cb.IsChecked == false).Count() > 0? false : true;

            cbSelectAllStates.IsChecked = filterStates.Children.OfType<CheckBox>().
                Where(cb => cb.Name != "cbSelectAllStates" && cb.IsChecked == false).Count() > 0 ? false : true;
        }
        private void btnStateFilter_Click(object sender, RoutedEventArgs e)
        {
            popState.PlacementTarget = sender as Button;
            popType.IsOpen = false;
            popState.IsOpen = true;
        }
        private void popState_Loaded(object sender, RoutedEventArgs e)
        {
            var rows = (Enum.GetValues(typeof(ChangeType)).Cast<ChangeType>());
            foreach (var text in rows)
            {
                var cb = new CheckBox
                {
                    IsChecked = true,
                    Content = text,
                    Cursor = Cursors.Hand
                };
                cb.Checked += new RoutedEventHandler(cb_Checked);
                cb.Unchecked += new RoutedEventHandler(cb_Checked);
                filterStates.Children.Add(cb);
            }
        }
        private void cbSelectAllTypes_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in filterTypes.Children)
                cb.IsChecked = cbSelectAllTypes.IsChecked == true ? false : true;
                
            cbSelectAllTypes.IsChecked = !cbSelectAllTypes.IsChecked;
        }
        private void cbSelectAllStates_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in filterStates.Children)
                cb.IsChecked = cbSelectAllStates.IsChecked == true ? false : true;

            cbSelectAllStates.IsChecked = !cbSelectAllStates.IsChecked;

        }

    }
}