using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
        public static SelectAmbienteMVVM MainView { get; set; }
        public ObservableCollection<AmbienteViewModel> AmbienteViewModels { get; set; } = new ObservableCollection<AmbienteViewModel>();
        private ListCollectionView AmbienteCollectionView { get; set; }
        private string ExecuteButtonText { get; set; } = "Dividir Pisos";
        private System.Windows.Visibility SelectIsVisible { get; set; } = System.Windows.Visibility.Collapsed;
        public bool Execute { get; set; } = false;
        public bool Select { get; set; } = false;
        public static ProgressBarViewModel ProgressBarViewModel { get; set; }
        public static ExternalEvent SelectExternalEvent { get; set; }
        public static ExternalEvent ExecuteExternalEvent { get; set; }
        public List<ElementId> SelectedFloorsIds { get; set; } = new List<ElementId>();
        public Document ActiveDocument { get; set; }
        public UIDocument ActiveUIDocument { get; set; }

        public SelectAmbienteMVVM(UIDocument uidoc, SelectAmbientMVVMExecuteCommand ambienteCommand)
        {
            ActiveUIDocument = uidoc;
            ActiveDocument = uidoc.Document;
            List<string> ambientes = new List<string>();
            SelectExternalEvent = ExternalEvent.Create(new SelectFloorsEEH());

            switch (ambienteCommand)
            {
                case SelectAmbientMVVMExecuteCommand.SplitFloors:
                    ambientes = new FilteredElementCollector(ActiveDocument, uidoc.ActiveView.Id)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_Floors)
                        .Where(a => a.LookupParameter("Ambiente").HasValue && a.LookupParameter("Piso em placas").AsInteger() == 0 && a.LookupParameter("Reforço de Tela").AsInteger() == 0)
                        .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                        .Select(a => a.First().LookupParameter("Ambiente").AsString())
                        .ToList();
                    SelectIsVisible = System.Windows.Visibility.Visible;
                    ExecuteExternalEvent = ExternalEvent.Create(new SplitFloorsEEH());
                    ExecuteButtonText = "DIVIDIR PISOS";
                    break;

                case SelectAmbientMVVMExecuteCommand.SplitJoints:
                    List<string> dividedAmbienteJoints = new FilteredElementCollector(ActiveDocument, uidoc.ActiveView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .Where(a => a.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).HasValue && a.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString() != "")
                    .Where(a => a.LookupParameter("Ambiente").HasValue)
                    .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                    .Select(a => a.First().LookupParameter("Ambiente").AsString())
                    .ToList();
                    ambientes = new FilteredElementCollector(ActiveDocument, uidoc.ActiveView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .Where(a => a.LookupParameter("Ambiente").HasValue)
                    .Where(a => !dividedAmbienteJoints.Contains(a.LookupParameter("Ambiente").AsString()))
                    .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                    .Select(a => a.First().LookupParameter("Ambiente").AsString())
                    .ToList();
                    ExecuteExternalEvent = ExternalEvent.Create(new SplitJointsEEH());
                    ExecuteButtonText = "DIVIDIR JUNTAS";
                    break;

                case SelectAmbientMVVMExecuteCommand.RestoreFloors:
                    ambientes = new FilteredElementCollector(ActiveDocument, uidoc.ActiveView.Id)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_Floors)
                        .Where(a => a.LookupParameter("Ambiente").HasValue && a.LookupParameter("Piso em placas").AsInteger() == 1)
                        .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                        .Select(a => a.First().LookupParameter("Ambiente").AsString())
                        .ToList();
                    List<string> jointAmbientes = new FilteredElementCollector(ActiveDocument, uidoc.ActiveView.Id)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                        .Select(a => a.First().LookupParameter("Ambiente").AsString())
                        .ToList();
                    foreach (string jointAmbiente in jointAmbientes)
                    {
                        if (!ambientes.Contains(jointAmbiente))
                        {
                            ambientes.Add(jointAmbiente);
                        }
                    }
                    ExecuteExternalEvent = ExternalEvent.Create(new RestoreFloorsEEH());
                    ExecuteButtonText = "RESTAURAR PISOS";
                    break;

                case SelectAmbientMVVMExecuteCommand.TagJoints:
                    ambientes = new FilteredElementCollector(ActiveDocument, uidoc.ActiveView.Id)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .Where(a => a.LookupParameter("Ambiente").HasValue)
                        .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                        .Select(a => a.First().LookupParameter("Ambiente").AsString())
                        .ToList();
                    ExecuteExternalEvent = ExternalEvent.Create(new TagJointsEEH());
                    ExecuteButtonText = "TAGEAR JUNTAS";
                    break;

                case SelectAmbientMVVMExecuteCommand.DimensionFloors:
                    ambientes = new FilteredElementCollector(ActiveDocument, uidoc.ActiveView.Id)
                       .WhereElementIsNotElementType()
                       .OfCategory(BuiltInCategory.OST_Floors)
                       .Where(a => a.LookupParameter("Ambiente").HasValue)
                       .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                       .Select(a => a.First().LookupParameter("Ambiente").AsString())
                       .ToList();
                    ExecuteExternalEvent = ExternalEvent.Create(new DimensionFloorsEEH());
                    ExecuteButtonText = "COTAR PISOS";
                    break;

                default:
                    break;
            }
            foreach (var ambiente in ambientes)
            {
                AmbienteViewModels.Add(new AmbienteViewModel(ambiente));
            }
            ProgressBarViewModel = new ProgressBarViewModel();
            MainView = this;
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
            try
            {
                ExecuteExternalEvent.Raise();
                //Command();
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private void Select_Button_Click(object sender, RoutedEventArgs e)
        {
            SelectExternalEvent.Raise();
        }

        private void SelectAll_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var ambienteViewModel in AmbienteViewModels)
            {
                ambienteViewModel.IsChecked = true;
            }
        }

        private void SelectAll_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var ambienteViewModel in AmbienteViewModels)
            {
                ambienteViewModel.IsChecked = false;
            }
        }

        public void Dispose()
        {
            this.Close();
        }
    }
    public enum SelectAmbientMVVMExecuteCommand

    {
        SplitFloors,
        SplitJoints,
        RestoreFloors,
        TagJoints,
        DimensionFloors
    }
}
