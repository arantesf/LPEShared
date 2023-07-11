using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;

namespace Revit.Common
{
    /// <summary>
    /// Interaction logic for SelecAmbienteMVVM.xaml
    /// </summary>
    public partial class SelectAmbienteMVVM : Window, IDisposable
    {
        ObservableCollection<AmbienteModel> AmbienteViewModels = new ObservableCollection<AmbienteModel>();
        ListCollectionView AmbienteCollectionView;
        string ExecuteButtonText = "Dividir Pisos";
        Visibility SelectIsVisible = Visibility.Collapsed;
        public bool Execute { get; set; } = false;
        public bool Select { get; set; } = false;
        public List<string> SelectedAmbientes { get; set; } = new List<string>();


        public SelectAmbienteMVVM(List<string> ambientes, string executeButtonText, Visibility selectIsVisible)
        {
            foreach (var ambiente in ambientes)
            {
                AmbienteViewModels.Add(new AmbienteModel(ambiente));
            }
            SelectIsVisible = selectIsVisible;
            ExecuteButtonText = executeButtonText;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AmbienteCollectionView = CollectionViewSource.GetDefaultView(AmbienteViewModels) as ListCollectionView;
            AmbienteCollectionView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
            Ambientes_ListBox.ItemsSource = AmbienteCollectionView;
            SelectPisos_Button.Visibility = SelectIsVisible;
            Execute_Button.Content = ExecuteButtonText;
            this.Title = ExecuteButtonText;
        }

        private void Execute_Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in AmbienteViewModels.Where(x => x.IsChecked))
            {
                SelectedAmbientes.Add(item.Name);
            }
            Execute = true;
            Close();
        }

        private void Select_Button_Click(object sender, RoutedEventArgs e)
        {
            Select = true;
            Close();
        }

        public void Dispose()
        {
        }
    }
}
