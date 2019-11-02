using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Dependence: INotifyPropertyChanged
    {
        public DependenceObject ЗависимыйОбъект;

        public Dependence()
        {
            Зависимости.CollectionChanged += Зависимости_CollectionChanged;
        }

        private void Зависимости_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (PropertyChanged == null)
            {
                return;
            }
            PropertyChanged(this, new PropertyChangedEventArgs(nameof(ListDependence)));
        }

        [JsonIgnore]
        public string ObjName => ЗависимыйОбъект.ToString();

        public ObservableCollection<DependenceObject> Зависимости = new ObservableCollection<DependenceObject>();

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public string ListDependence 
        {
            get
            {
                string result = "";
                foreach (var d in Зависимости)
                {
                    result += d.ObjectName + Environment.NewLine;
                }

                return result;
            }
        }
    }
}