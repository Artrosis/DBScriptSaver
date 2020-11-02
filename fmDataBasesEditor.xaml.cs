﻿using DBScriptSaver.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
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
using Ude;


namespace DBScriptSaver
{
    /// <summary>
    /// Логика взаимодействия для fmDataBasesEditor.xaml
    /// </summary>
    public partial class fmDataBasesEditor : Window
    {
        private Project project => DataContext as Project;
        
        public List<string> List_NamesOfDB { get; set; }
        public List<string> List_ofPath { get; set; }

        public fmDataBasesEditor(Project proj)
        {
            InitializeComponent();

            DataContext = proj;

            cmbDBNames.ItemsSource = proj.Server.GetNamesOfDB();

            List_ofPath = GetPathsForDB(@"" + project.Path);
            cmbDBPath.ItemsSource = List_ofPath;
        }

        public List<string> dirlist = new List<string>();
        public List<string> GetPathsForDB(string path)
        {

            List<string> list = new List<string>();

            list.AddRange(Directory.GetDirectories(path, "changes*", SearchOption.AllDirectories)
             .Concat(Directory.GetDirectories(path, "source*", SearchOption.AllDirectories))
             .Concat(Directory.GetDirectories(path, "tables*", SearchOption.AllDirectories)));


            foreach (var item in list)
            {

                var dir = System.IO.Path.GetDirectoryName(item);
                var dirparent = System.IO.Path.GetDirectoryName(dir);
                dirlist.Add(System.IO.Path.GetFileName(dirparent) +'\\'+ System.IO.Path.GetFileName(dir));
            }

            return dirlist.Union(dirlist).ToList();
        }
        private ProjectDataBase SelectedBase => gcDataBases.SelectedItem as ProjectDataBase;

        private void EditDBObjects_Click(object sender, RoutedEventArgs e)
        {
            var DB = SelectedBase;

            if (DB == null)
            {
                return;
            }

            try
            {
                var fmEditor = new DBObjectsFiltering(DB) { Owner = this };
                fmEditor.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Фильтр объектов");
            }
        }

        private void Compare_Click(object sender, RoutedEventArgs e)
        {
            var DB = SelectedBase;

            if (DB == null)
            {
                return;
            }

            try
            {
                var fmCompare = new CompareScripts();
                fmCompare.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Сравнить");
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            project.DataBases.Add(new ProjectDataBase(project));
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            project.vm.SaveProjects();
            DialogResult = true;
        }

        private void btnDel_Click(object sender, RoutedEventArgs e)
        {
            var DB = SelectedBase;
            if (DB == null)
            {
                MessageBox.Show("Не выбрана база данных");
                return;
            }
            project.DataBases.Remove(SelectedBase);
        }
    }
}
