using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Revit.Common
{
    /// <summary>
    /// Interaction logic for SelectAmbienteReinforcementMVVM.xaml
    /// </summary>
    public partial class SelectAmbienteReinforcementMVVM : Window
    {
        public static SelectAmbienteReinforcementMVVM MainView { get; set; }
        public static ProgressBarViewModel ProgressBarViewModel { get; set; }

        private string fatorDeFormaGlobal;

        public string FatorDeFormaGlobal
        {
            get { return fatorDeFormaGlobal; }
            set { fatorDeFormaGlobal = value; }
        }

        private ObservableCollection<AmbienteAndReinforcementViewModel> ambienteAndReinforcementViewModels = new ObservableCollection<AmbienteAndReinforcementViewModel>();

        public ObservableCollection<AmbienteAndReinforcementViewModel> AmbienteAndReinforcementViewModels
        {
            get { return ambienteAndReinforcementViewModels; }
            set { ambienteAndReinforcementViewModels = value; }
        }

        public ExternalEvent ExternalEvent { get; set; } = ExternalEvent.Create(new MeshReinforcementEEH());

        ListCollectionView AmbienteCollectionView;
        string ExecuteButtonText = "Dividir Pisos";
        public bool Execute { get; set; } = false;
        public bool Select { get; set; } = false;

        public SelectAmbienteReinforcementMVVM(UIDocument uidoc)
        {
            Document doc = uidoc.Document;
            Dictionary<string, List<Element>> ambientesWithReinforcements = new Dictionary<string, List<Element>>();

            var keyScheduleView = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules)
                .Where(view => view.Name == "LPE_TIPO DE PISO")
                .FirstOrDefault();

            List<Element> tiposDePiso = new FilteredElementCollector(doc, keyScheduleView.Id).ToList();

            List<string> ambienteFloorStrings = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                  .WhereElementIsNotElementType()
                  .OfCategory(BuiltInCategory.OST_Floors)
                  .Where(a => a.LookupParameter("Ambiente").HasValue)
                  .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                  .Select(a => a.First().LookupParameter("Ambiente").AsString())
                  .ToList();

            foreach (var tipoDePiso in tiposDePiso)
            {
                string ambiente = tipoDePiso.LookupParameter("Ambiente").AsString();

                if (ambienteFloorStrings.Contains(ambiente))
                {
                    if (!ambientesWithReinforcements.ContainsKey(ambiente))
                    {
                        ambientesWithReinforcements.Add(ambiente, new List<Element>());
                    }
                    bool telaSuperior = tipoDePiso.LookupParameter("(s/n) Tela Superior").AsInteger() == 1;
                    bool fibra = tipoDePiso.LookupParameter("(s/n) Fibra").AsInteger() == 1;
                    if (telaSuperior && fibra)
                    {
                        ambientesWithReinforcements[ambiente].Add(tipoDePiso);
                    }
                }
            }
            double fatorDeFormaLimite = 2;
            try
            {
                ElementId globalParameterId = GlobalParametersManager.FindByName(doc, "LPE_FATOR DE FORMA GLOBAL");
                GlobalParameter gParam = doc.GetElement(globalParameterId) as GlobalParameter;
                DoubleParameterValue doubleParameterValue = gParam.GetValue() as DoubleParameterValue;
                fatorDeFormaLimite = doubleParameterValue.Value;
            }
            catch (Exception)
            {

            }


            foreach (var ambiente in ambientesWithReinforcements)
            {
                AmbienteAndReinforcementViewModels.Add(new AmbienteAndReinforcementViewModel(ambiente.Key, ambiente.Value));
            }
            FatorDeFormaGlobal = string.Format("{0:0.0}", fatorDeFormaLimite);
            ExecuteButtonText = "REFORÇAR PISOS";
            ProgressBarViewModel = new ProgressBarViewModel();
            MainView = this; 
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AmbienteCollectionView = CollectionViewSource.GetDefaultView(AmbienteAndReinforcementViewModels) as ListCollectionView;
            AmbienteCollectionView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
            Ambientes_ListBox.ItemsSource = AmbienteCollectionView;
            Execute_Button.Content = ExecuteButtonText;
            this.Title = ExecuteButtonText;
        }

        private void Execute_Button_Click(object sender, RoutedEventArgs e)
        {
            ExternalEvent.Raise();
        }

        public void Dispose()
        {
            this.Close();
        }
    }
}
