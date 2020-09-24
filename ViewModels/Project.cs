﻿using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Data;

namespace DBScriptSaver.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class Project
    {
        [JsonIgnore]
        public DBScriptViewModel vm;
        public string Name { get; set; }
        public string Path { get; set; }
        public ProjectServer Server { get; set; }
        public Project(DBScriptViewModel vm) : base()
        {
            this.vm = vm;
            DataBases.CollectionChanged += DataBases_CollectionChanged;
        }
        private void DataBases_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (ProjectDataBase db in e.NewItems)
                {
                    db.Project = this;
                }
            }
        }
        public ObservableCollection<ProjectDataBase> DataBases { get; } = new ObservableCollection<ProjectDataBase>();

        [JsonIgnore]
        public ListCollectionView EditDataBases
        {
            get
            {
                return new ListCollectionView(DataBases);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
