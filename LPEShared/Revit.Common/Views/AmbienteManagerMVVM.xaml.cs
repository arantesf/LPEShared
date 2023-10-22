using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Revit.Common
{
    public partial class AmbienteManagerMVVM : Window
    {
        public static AmbienteManagerMVVM MainView;
        public bool ApplyChanges { get; set; } = false;
        public ExternalEvent AddAmbienteExternalEvent { get; set; } = ExternalEvent.Create(new AddAmbienteEEH());
        public ObservableCollection<FullAmbienteViewModel> AmbienteViewModels { get; set; } = new ObservableCollection<FullAmbienteViewModel>();
        public List<FullAmbienteViewModel> AmbienteViewModelsToDelete { get; set; } = new List<FullAmbienteViewModel>();
        public FullAmbienteViewModel SelectedFullAmbienteViewModel { get; set; }
        public ListCollectionView AmbienteListCollectionView { get; set; }
        public static ExternalEvent ApplyAmbientesExternalEvent { get; set; } = ExternalEvent.Create(new ApplyAmbientesEEH());

        public AmbienteManagerMVVM(UIDocument uidoc, List<FullAmbienteViewModel> fullAmbienteViewModels)
        {
            Closing += OnWindowClosing;
            MainView = this;
            foreach (var fullAmbienteViewModel in fullAmbienteViewModels)
            {
                AmbienteViewModels.Add(fullAmbienteViewModel);
            }
            InitializeComponent();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (!ApplyChanges)
            {
                DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("As alterações não serão salvas, tem certeza que deseja sair?", "Atenção!", MessageBoxButtons.YesNo);
                if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                {

                }
                else if (dialogResult == System.Windows.Forms.DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AmbienteListCollectionView = CollectionViewSource.GetDefaultView(AmbienteViewModels) as ListCollectionView;
            AmbienteListCollectionView.SortDescriptions.Add(new SortDescription("TipoDePiso", ListSortDirection.Ascending));
            Ambiente_DataGrid.ItemsSource = AmbienteListCollectionView;
        }

        private void AddAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new AmbienteEditMVVM(new FullAmbienteViewModel(null, null), true);
            createWindow.Topmost = true;
            createWindow.ShowDialog();
        }

        public void SetSameAmbienteItensDeDetalhe(FullAmbienteViewModel fullAmbienteViewModel)
        {
            foreach (var ambienteViewModel in AmbienteViewModels)
            {
                if (ambienteViewModel.Ambiente == fullAmbienteViewModel.Ambiente)
                {
                    ambienteViewModel.CBBaseGenerica = fullAmbienteViewModel.CBBaseGenerica;
                    ambienteViewModel.CBRefSubleito = fullAmbienteViewModel.CBRefSubleito;
                    ambienteViewModel.CBSubBase = fullAmbienteViewModel.CBSubBase;
                    ambienteViewModel.CBSubleito = fullAmbienteViewModel.CBSubleito;
                    ambienteViewModel.HRefSubleito = fullAmbienteViewModel.HRefSubleito;
                    ambienteViewModel.HSubBase = fullAmbienteViewModel.HSubBase;
                    ambienteViewModel.LPECarga = fullAmbienteViewModel.LPECarga;
                    ambienteViewModel.TagBaseGenerica = fullAmbienteViewModel.TagBaseGenerica;
                    ambienteViewModel.TagConcreto = fullAmbienteViewModel.TagConcreto;
                    ambienteViewModel.TagRefSubleito = fullAmbienteViewModel.TagRefSubleito;
                    ambienteViewModel.TagSubBase = fullAmbienteViewModel.TagSubBase;
                    ambienteViewModel.TagSubleito = fullAmbienteViewModel.TagSubleito;
                }
            }
        }

        public void ApplyAddAmbiente(FullAmbienteViewModel fullAmbienteViewModel)
        {
            SetSameAmbienteItensDeDetalhe(fullAmbienteViewModel);
            AmbienteViewModels.Add(fullAmbienteViewModel);
        }

        public void ApplyEditAmbiente(FullAmbienteViewModel fullAmbienteViewModel)
        {
            SetSameAmbienteItensDeDetalhe(fullAmbienteViewModel);
            SelectedFullAmbienteViewModel = fullAmbienteViewModel;
        }

        private void EditAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Ambiente_DataGrid.SelectedIndex > -1)
            {
                var createWindow = new AmbienteEditMVVM(SelectedFullAmbienteViewModel, false);
                createWindow.Topmost = true;
                createWindow.ShowDialog();
            }
        }

        private void ImportAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DeleteAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Ambiente_DataGrid.SelectedIndex > -1)
                {
                    FullAmbienteViewModel fullAmbienteViewModelToDelete = (FullAmbienteViewModel)Ambiente_DataGrid.SelectedItem;
                    if (fullAmbienteViewModelToDelete.Id != new Autodesk.Revit.DB.ElementId(-1))
                    {
                        fullAmbienteViewModelToDelete.Action = Action.Delete;
                        AmbienteViewModelsToDelete.Add(fullAmbienteViewModelToDelete);
                    }
                    AmbienteViewModels.Remove((FullAmbienteViewModel)Ambiente_DataGrid.SelectedItem);
                }
            }
            catch (Exception)
            {
            }
        }

        private void DuplicateAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Ambiente_DataGrid.SelectedIndex > -1)
            {
                FullAmbienteViewModel duplicatedFullAmbienteViewModel = (FullAmbienteViewModel)SelectedFullAmbienteViewModel.Clone();
                duplicatedFullAmbienteViewModel.TipoDePiso += "(2)";
                var createWindow = new AmbienteEditMVVM(duplicatedFullAmbienteViewModel, true)
                {
                    Topmost = true
                };
                createWindow.ShowDialog();
            }
        }

        private void DuplicateReforcoAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Ambiente_DataGrid.SelectedIndex > -1)
            {
                FullAmbienteViewModel duplicatedFullAmbienteViewModel = (FullAmbienteViewModel)SelectedFullAmbienteViewModel.Clone();
                duplicatedFullAmbienteViewModel.TipoDePiso += " - REFORÇO";
                var createWindow = new AmbienteEditMVVM(duplicatedFullAmbienteViewModel, true)
                {
                    Topmost = true
                };
                createWindow.ShowDialog();
            }
        }
        private void Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            ApplyChanges = true;
            ApplyAmbientesExternalEvent.Raise();
            Close();
        }
    }
}
