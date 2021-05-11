using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для CompareScripts.xaml
    /// </summary>
    public partial class CompareScripts : Window
    {
        private readonly ProjectDataBase DB;
        readonly DispatcherTimer timer = new DispatcherTimer();
        private CompareScripts()
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

        TextBox tbFilter => VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(tbFilterWrapper, 0) as Grid, 0) as TextBox;

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

            var filteredTypes = filterTypes.Children.OfType<CheckBox>();

            if (!filteredTypes.Any(cb => (cb == cbSelectAllTypes) && cb.IsChecked == true))
            {
                if (!filteredTypes.Any(cb => cb != cbSelectAllTypes
                                        && cb.IsChecked == true
                                        && (cb.Content as string) == o.ObjectType))
                {
                    return false;
                }
            }

            var filteredStates = filterStates.Children.OfType<CheckBox>();

            if (!filteredStates.Any(cb => (cb == cbSelectAllStates) && cb.IsChecked == true))
            {
                if (!filteredStates.Any(cb => cb != cbSelectAllStates
                                        && (cb.IsChecked == true)
                                        && (cb.Content as ChangeType?) == o.ChangeState))
                {
                    return false;
                }
            }

            return true;
        }

        private readonly ObservableCollection<ScriptWrapper> scriptSource = new ObservableCollection<ScriptWrapper>(); 

        public CompareScripts(ProjectDataBase db) : this()
        {
            DB = db;
            DataContext = scriptSource;
            gcDBObjects.ItemsSource = new ListCollectionView(scriptSource);
        }

        private void SetTotal(string pCaption, int pValue)
        {
            Dispatcher.Invoke(() =>
                {
                    tbLoadProgress.Text = pCaption;
                    pbLoadProgress.Value = pValue;
                });
        }

        public void ClearFilter(object sender, RoutedEventArgs e)
        {
            tbFilter.Text = "";
        }

        IEnumerable<ScriptWrapper> scripts => ((ListCollectionView)gcDBObjects.ItemsSource).Cast<ScriptWrapper>();

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            var Выбранные_скрипты = scripts.Where(w => w.Save).Select(w => w.getScript).ToList();
            DB.UpdateScripts(Выбранные_скрипты, cbUseMigrations.IsChecked ?? false);
            DialogResult = true;
        }

        private void ВыбратьВсе_Click(object sender, RoutedEventArgs e)
        {
            scripts.ToList().ForEach(w => w.Save = true);
        }

        private void ОтменитьВсе_Click(object sender, RoutedEventArgs e)
        {
            scripts.ToList().ForEach(w => w.Save = false);
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

            if (e.Key == Key.Space && gcDBObjects.SelectedItem != null)
            {
                CheckBox cb = gcDBObjects.Columns[3].GetCellContent(gcDBObjects.SelectedItem) as CheckBox;
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
        private void btnTypeFilter_Click(object sender, RoutedEventArgs e)
        {
            popType.IsOpen = true;
        }
        private void popType_Loaded(object sender, RoutedEventArgs e)
        {
            var rows = gcDBObjects.ItemsSource.OfType<ScriptWrapper>();
            var types = rows.Select(t => t.ObjectType).Distinct();

            foreach (var type in types)
            {
                var cb = new CheckBox
                {
                    IsChecked = false,
                    Content = type,
                    Cursor = Cursors.Hand
                };
                cb.Checked += cb_CheckedType;
                cb.Unchecked += cb_CheckedType;
                filterTypes.Children.Add(cb);
            }
        }
        private void cb_CheckedType(object sender, RoutedEventArgs e)
        {
            cbSelectAllTypes.IsChecked = !filterTypes
                                            .Children
                                            .OfType<CheckBox>()
                                            .Any(cb => cb != cbSelectAllTypes && cb.IsChecked == true);
            timer.Stop();
            timer.Start();
        }
        private void btnStateFilter_Click(object sender, RoutedEventArgs e)
        {
            popState.IsOpen = true;
        }
        private void popState_Loaded(object sender, RoutedEventArgs e)
        {
            var rows = (Enum.GetValues(typeof(ChangeType)).Cast<ChangeType>());
            foreach (var state in rows)
            {
                var cb = new CheckBox
                {
                    IsChecked = false,
                    Content = state,
                    Cursor = Cursors.Hand
                };
                cb.Checked += cb_CheckedState;
                cb.Unchecked += cb_CheckedState;
                filterStates.Children.Add(cb);
            }
        }
        private void cb_CheckedState(object sender, RoutedEventArgs e)
        {
            cbSelectAllStates.IsChecked = !filterStates
                                            .Children
                                            .OfType<CheckBox>()
                                            .Any(cb => cb != cbSelectAllStates && cb.IsChecked == true);
            timer.Stop();
            timer.Start();
        }
        private void cbSelectAllTypes_Click(object sender, RoutedEventArgs e)
        {
            if (cbSelectAllTypes.IsChecked != true)
            {
                return;
            }

            foreach (CheckBox cb in filterTypes.Children.OfType<CheckBox>().Where(chb => chb != cbSelectAllTypes))
            {
                cb.Checked -= cb_CheckedType;
                cb.Unchecked -= cb_CheckedType;
                try
                {
                    cb.IsChecked = false;
                }
                finally
                {
                    cb.Checked += cb_CheckedType;
                    cb.Unchecked += cb_CheckedType;
                }
            }
        }
        private void cbSelectAllStates_Click(object sender, RoutedEventArgs e)
        {
            if (cbSelectAllStates.IsChecked != true)
            {
                return;
            }

            foreach (CheckBox cb in filterStates.Children.OfType<CheckBox>().Where(chb => chb != cbSelectAllStates))
            {
                cb.Checked -= cb_CheckedState;
                cb.Unchecked -= cb_CheckedState;
                try
                {
                    cb.IsChecked = false;
                }
                finally
                {
                    cb.Checked += cb_CheckedState;
                    cb.Unchecked += cb_CheckedState;
                }
            }
        }
        private void window1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                var u = e.OriginalSource as UIElement;
                u.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                gcDBObjects.BeginEdit();
            }
        }

        private async void winLoaded(object sender, RoutedEventArgs e)
        {
            await Task.Factory.StartNew(() => DB.ObserveScripts(s => addSource(s), SetTotal));
        }

        private void addSource(Script s)
        {
            Dispatcher.Invoke(() => scriptSource.Add(new ScriptWrapper(s)));
        }
    }
}