// Decompiled with JetBrains decompiler
// Type: Revit.Common.AmbienteManagerMVVM
// Assembly: LPE, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E06EE1B-06C5-4A71-9629-ACF2EB60ECA2
// Assembly location: C:\ProgramData\Autodesk\Revit\Addins\2024\LPE\LPE.dll

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Markup;

namespace Revit.Common
{
    public partial class AmbienteManagerMVVM : Window, IComponentConnector
    {
        public static AmbienteManagerMVVM MainView;

        public bool ApplyChanges { get; set; }

        public ExternalEvent AddAmbienteExternalEvent { get; set; } = ExternalEvent.Create((IExternalEventHandler)new AddAmbienteEEH());

        public ObservableCollection<FullAmbienteViewModel> AmbienteViewModels { get; set; } = new ObservableCollection<FullAmbienteViewModel>();

        public List<FullAmbienteViewModel> AmbienteViewModelsToDelete { get; set; } = new List<FullAmbienteViewModel>();

        public FullAmbienteViewModel SelectedFullAmbienteViewModel { get; set; }

        public FullAmbienteViewModel SelectedFullAmbienteViewModelWithoutModify { get; set; }

        public ListCollectionView AmbienteListCollectionView { get; set; }

        public static ExternalEvent ApplyAmbientesExternalEvent { get; set; } = ExternalEvent.Create((IExternalEventHandler)new ApplyAmbientesEEH());

        public Dictionary<FloorMatrizClass, List<FloorMatriz>> FloorMatrizes { get; set; } = new Dictionary<FloorMatrizClass, List<FloorMatriz>>();

        public List<FibraViewModel> Fibras { get; set; } = new List<FibraViewModel>();

        public List<string> AllMaterialNames { get; set; } = new List<string>();

        public Dictionary<MaterialClass, List<string>> MaterialsByClass { get; set; } = new Dictionary<MaterialClass, List<string>>();

        public AmbienteManagerMVVM(
          Dictionary<FloorMatrizClass, List<FloorMatriz>> floorMatrizes,
          List<FullAmbienteViewModel> fullAmbienteViewModels,
          List<string> allMaterialNames,
          Dictionary<MaterialClass, List<string>> materialsByClass)
        {
            this.Closing += new CancelEventHandler(this.OnWindowClosing);
            this.MaterialsByClass = materialsByClass;
            this.FloorMatrizes = floorMatrizes;
            this.AllMaterialNames = allMaterialNames;
            AmbienteManagerMVVM.MainView = this;
            foreach (FullAmbienteViewModel ambienteViewModel in fullAmbienteViewModels)
                this.AmbienteViewModels.Add(ambienteViewModel);
            this.InitializeComponent();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (this.ApplyChanges)
                return;
            switch (System.Windows.Forms.MessageBox.Show("As alterações não serão salvas, tem certeza que deseja sair?", "Atenção!", MessageBoxButtons.YesNo))
            {
                case System.Windows.Forms.DialogResult.No:
                    e.Cancel = true;
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.AmbienteListCollectionView = CollectionViewSource.GetDefaultView((object)this.AmbienteViewModels) as ListCollectionView;
            this.AmbienteListCollectionView.SortDescriptions.Add(new SortDescription("TipoDePiso", ListSortDirection.Ascending));
            this.Ambiente_DataGrid.ItemsSource = (IEnumerable)this.AmbienteListCollectionView;
        }

        private void AddAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            FullAmbienteViewModel newFullAmbienteViewModel = new FullAmbienteViewModel() { EspacamentoBarra = 30, UltimaCamada = "Subleito" };
            AmbienteEditMVVM ambienteEditMvvm = new AmbienteEditMVVM(newFullAmbienteViewModel, true);
            ambienteEditMvvm.Topmost = true;
            ambienteEditMvvm.ShowDialog();
        }

        public void SetSameAmbienteItensDeDetalhe(FullAmbienteViewModel fullAmbienteViewModel)
        {
            foreach (FullAmbienteViewModel ambienteViewModel in (Collection<FullAmbienteViewModel>)this.AmbienteViewModels)
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
            //this.SetSameAmbienteItensDeDetalhe(fullAmbienteViewModel);
            this.AmbienteViewModels.Add(fullAmbienteViewModel);
        }

        public void ApplyEditAmbiente(FullAmbienteViewModel fullAmbienteViewModel)
        {
            //this.SetSameAmbienteItensDeDetalhe(fullAmbienteViewModel);
            this.SelectedFullAmbienteViewModel = fullAmbienteViewModel;
        }

        public void ApplyParentChangesToChildren(FullAmbienteViewModel fullAmbienteViewModel)
        {
            List<FullAmbienteViewModel> ambientesToAdd = new List<FullAmbienteViewModel>();
            for (int i = 0; i < AmbienteViewModels.Count; i++)
            {
                var viewModel = AmbienteViewModels[i];
                if (viewModel.ParentAmbienteViewModelGUID == fullAmbienteViewModel.GUID)
                {
                    bool boolRefSuperior = viewModel.BoolReforcoTelaSuperior;
                    int telaRefSuperior = viewModel.ReforcoTelaSuperior;
                    double emendaRefSuperior = viewModel.EmendaReforcoTelaSuperior;
                    string finalidadeRefSuperior = viewModel.FinalidadeReforcoTelaSuperior;
                    bool boolRefInferior = viewModel.BoolReforcoTelaInferior;
                    int telaRefInferior = viewModel.ReforcoTelaInferior;
                    double emendaRefInferior = viewModel.EmendaReforcoTelaInferior;
                    string finalidadeRefInferior = viewModel.FinalidadeReforcoTelaInferior;
                    ElementId pisoId = viewModel.PisoId;
                    ElementId ksPisoId = viewModel.KSPisoId;
                    ElementId ksDetalheId = viewModel.KSDetalheId;
                    ElementId ksJuntaId = viewModel.KSJuntaId;
                    string guid = viewModel.GUID;
                    string parentGuid = viewModel.ParentAmbienteViewModelGUID;
                    var newViewModel = fullAmbienteViewModel.Clone() as FullAmbienteViewModel;
                    newViewModel.GUID = guid;
                    newViewModel.ParentAmbienteViewModelGUID = parentGuid;
                    newViewModel.PisoId = pisoId;
                    newViewModel.KSPisoId = ksPisoId;
                    newViewModel.KSJuntaId= ksJuntaId;
                    newViewModel.KSDetalheId= ksDetalheId;
                    newViewModel.BoolReforcoDeTela = true;
                    newViewModel.BoolReforcoTelaSuperior = boolRefSuperior;
                    newViewModel.BoolReforcoTelaSuperior = boolRefSuperior;
                    newViewModel.ReforcoTelaSuperior = telaRefSuperior;
                    newViewModel.EmendaReforcoTelaSuperior = emendaRefSuperior;
                    newViewModel.FinalidadeReforcoTelaSuperior = finalidadeRefSuperior;
                    newViewModel.BoolReforcoTelaInferior = boolRefInferior;
                    newViewModel.ReforcoTelaInferior = telaRefInferior;
                    newViewModel.EmendaReforcoTelaInferior = emendaRefInferior;
                    newViewModel.FinalidadeReforcoTelaInferior = finalidadeRefInferior;
                    newViewModel.SetTipoDePiso();
                    AmbienteViewModels.Remove(viewModel);
                    i--;
                    ambientesToAdd.Add(newViewModel);
                }
            }
            foreach (var ambiente in ambientesToAdd)
            {
                AmbienteViewModels.Add(ambiente);
            }
        }

        private void EditAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.Ambiente_DataGrid.SelectedIndex <= -1)
            {
                WarningMVVM warningMVVM = new WarningMVVM("Selecione um item para continuar.");
                warningMVVM.ShowDialog();
                return;
            }
            this.SelectedFullAmbienteViewModelWithoutModify = this.SelectedFullAmbienteViewModel.Clone() as FullAmbienteViewModel;
            AmbienteEditMVVM ambienteEditMvvm = new AmbienteEditMVVM(this.SelectedFullAmbienteViewModel, false);
            ambienteEditMvvm.Topmost = true;
            bool? nullable = ambienteEditMvvm.ShowDialog();
            if (nullable.HasValue && nullable.Value)
                return;
            this.AmbienteViewModels[this.AmbienteViewModels.IndexOf(this.SelectedFullAmbienteViewModel)] = this.SelectedFullAmbienteViewModelWithoutModify;
        }

        private void ImportAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DeleteAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.Ambiente_DataGrid.SelectedIndex <= -1)
                {
                    WarningMVVM warningMVVM = new WarningMVVM("Selecione um item para continuar.");
                    warningMVVM.ShowDialog();
                    return;
                }
                FullAmbienteViewModel selectedItem = (FullAmbienteViewModel)this.Ambiente_DataGrid.SelectedItem;
                if (selectedItem.KSPisoId != new ElementId(-1))
                {
                    selectedItem.Action = Action.Delete;
                    this.AmbienteViewModelsToDelete.Add(selectedItem);
                }
                this.AmbienteViewModels.Remove((FullAmbienteViewModel)this.Ambiente_DataGrid.SelectedItem);
            }
            catch (Exception ex)
            {
            }
        }

        private void DuplicateAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.Ambiente_DataGrid.SelectedIndex <= -1)
            {
                WarningMVVM warningMVVM = new WarningMVVM("Selecione um item para continuar.");
                warningMVVM.ShowDialog();
                return;
            }
            FullAmbienteViewModel fullAmbienteViewModel = (FullAmbienteViewModel)this.SelectedFullAmbienteViewModel.Clone();
            fullAmbienteViewModel.FloorMatriz = SelectedFullAmbienteViewModel.FloorMatriz.Clone() as FloorMatriz;
            //fullAmbienteViewModels.TipoDePiso += "(2)";
            fullAmbienteViewModel.KSPisoId = new ElementId(-1);
            fullAmbienteViewModel.KSDetalheId = new ElementId(-1);
            fullAmbienteViewModel.KSJuntaId = new ElementId(-1);
            fullAmbienteViewModel.PisoId = new ElementId(-1);
            fullAmbienteViewModel.GUID = Guid.NewGuid().ToString();
            AmbienteEditMVVM ambienteEditMvvm = new AmbienteEditMVVM(fullAmbienteViewModel, true);
            ambienteEditMvvm.Topmost = true;
            ambienteEditMvvm.ShowDialog();
        }

        private void DuplicateReforcoAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.Ambiente_DataGrid.SelectedIndex <= -1)
            {
                WarningMVVM warningMVVM = new WarningMVVM("Selecione um item para continuar.");
                warningMVVM.ShowDialog();
                return;
            }
            FullAmbienteViewModel fullAmbienteViewModels = (FullAmbienteViewModel)this.SelectedFullAmbienteViewModel.Clone();
            fullAmbienteViewModels.BoolReforcoDeTela = true;
            fullAmbienteViewModels.ParentAmbienteViewModelGUID = SelectedFullAmbienteViewModel.GUID;
            //fullAmbienteViewModels.TipoDePiso += " - REFORÇO";
            fullAmbienteViewModels.KSPisoId = new ElementId(-1);
            fullAmbienteViewModels.KSDetalheId = new ElementId(-1);
            fullAmbienteViewModels.KSJuntaId = new ElementId(-1);
            fullAmbienteViewModels.PisoId = new ElementId(-1);
            fullAmbienteViewModels.GUID = Guid.NewGuid().ToString();
            AmbienteEditMVVM ambienteEditMvvm = new AmbienteEditMVVM(fullAmbienteViewModels, true);
            ambienteEditMvvm.Topmost = true;
            ambienteEditMvvm.ShowDialog();
        }

        private void Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            this.ApplyChanges = true;
            AmbienteManagerMVVM.ApplyAmbientesExternalEvent.Raise();
            this.Close();
        }

    }
}
