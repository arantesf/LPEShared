// Decompiled with JetBrains decompiler
// Type: Revit.Common.AmbienteEditMVVM
// Assembly: LPE, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E06EE1B-06C5-4A71-9629-ACF2EB60ECA2
// Assembly location: C:\ProgramData\Autodesk\Revit\Addins\2024\LPE\LPE.dll

using Autodesk.Internal.InfoCenter;
using Autodesk.Revit.UI;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace Revit.Common
{
    public enum FloorMatrizClass
    {
        ConcretoSobreSolo,
        FundacaoDireta,
        PavIntertravado,
        PavAsfaltico,
        Nenhum,
    }

    public partial class AmbienteEditMVVM : Window, IDisposable
    {
        public int MyProperty { get; set; }
        public bool Create { get; set; }

        public FullAmbienteViewModel SelectedFullAmbienteViewModel { get; set; }

        public AmbienteEditMVVM(FullAmbienteViewModel fullAmbienteViewModels, bool create)
        {
            this.SelectedFullAmbienteViewModel = fullAmbienteViewModels;
            this.InitializeComponent();
            Create = create;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Layers_DataGrid.ItemsSource = (IEnumerable)this.SelectedFullAmbienteViewModel.FloorMatriz.Layers;
        }

        private void SetPropertiesBasedOnLayers()
        {
            foreach (var layer in SelectedFullAmbienteViewModel.FloorMatriz.Layers)
            {
                switch (layer.SelectedCamadaTipo)
                {
                    case "Base":
                        if (SelectedFullAmbienteViewModel.TipoDeSolucao == "PAV. INTERTRAVADO" || SelectedFullAmbienteViewModel.TipoDeSolucao == "PAV. ASFÁLTICO")
                        {
                            SelectedFullAmbienteViewModel.HBase = layer.Width;
                            SelectedFullAmbienteViewModel.CBBase = true;
                            SelectedFullAmbienteViewModel.TagBase = layer.Tag;
                        }
                        else
                        {
                            SelectedFullAmbienteViewModel.HBase = 0;
                            SelectedFullAmbienteViewModel.CBBase = false;
                            SelectedFullAmbienteViewModel.TagBase = "";
                        }
                        break;
                    //case "Base Genérica": 
                    //    SelectedFullAmbienteViewModel.CBBaseGenerica = true;
                    //    SelectedFullAmbienteViewModel.TagBaseGenerica = layer.Tag;
                    //    break;
                    case "Bloco Intertravado": 
                        SelectedFullAmbienteViewModel.TagConcreto = layer.Tag;
                        break;
                    case "Camada de Assentamento": break;
                    case "Camada de Isolamento": break;
                    case "Camada de Ventilação": break;
                    case "Concreto":
                        SelectedFullAmbienteViewModel.TagConcreto = layer.Tag;
                        break;
                    case "Concreto Asfáltico": 
                        SelectedFullAmbienteViewModel.TagConcreto = layer.Tag;
                        break;
                    case "Filme Plastico": break;
                    case "Imprimacao Asfáltica": break;
                    case "Pintura de Ligação": break;
                    case "Reforço de Subleito":
                        SelectedFullAmbienteViewModel.CBRefSubleito = true;
                        SelectedFullAmbienteViewModel.TagRefSubleito = layer.Tag;
                        SelectedFullAmbienteViewModel.HRefSubleito = layer.Width;
                        break;
                    case "Sub-Base":
                        SelectedFullAmbienteViewModel.CBSubBase = true;
                        SelectedFullAmbienteViewModel.TagSubBase = layer.Tag;
                        SelectedFullAmbienteViewModel.HSubBase = layer.Width;
                        break;
                    default:
                        break;
                }
            }
            if (SelectedFullAmbienteViewModel.UltimaCamada == "Nenhum")
            {
                SelectedFullAmbienteViewModel.TagUltimaCamada = "";
            }
            SelectedFullAmbienteViewModel.CBSubleito = SelectedFullAmbienteViewModel.UltimaCamada == "Subleito";
            SelectedFullAmbienteViewModel.TagSubleito = SelectedFullAmbienteViewModel.UltimaCamada == "Subleito" ? SelectedFullAmbienteViewModel.TagUltimaCamada : "";
            SelectedFullAmbienteViewModel.CBBaseGenerica = SelectedFullAmbienteViewModel.UltimaCamada == "Base genérica";
            SelectedFullAmbienteViewModel.TagBaseGenerica = SelectedFullAmbienteViewModel.UltimaCamada == "Base genérica" ? SelectedFullAmbienteViewModel.TagUltimaCamada : "";
            if (!SelectedFullAmbienteViewModel.TipoDePiso.Contains("REF"))
            {
                SelectedFullAmbienteViewModel.FloorMatriz.FloorName = SelectedFullAmbienteViewModel.TipoDePiso;
            }
        }

        private void ApplyNewAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFullAmbienteViewModel.Ambiente == "")
            {
                SelectedFullAmbienteViewModel.Errors = "Preencha um nome para o ambiente";
                return;
            }
            if (SelectedFullAmbienteViewModel.SelectedLegenda == null)
            {
                SelectedFullAmbienteViewModel.Errors = "Selecione uma legenda para o piso";
                return;
            }
            int existingViewModelsWithSameNameCount = AmbienteManagerMVVM.MainView.AmbienteViewModels
                .Where(x => x.GUID != SelectedFullAmbienteViewModel.GUID)
                .Where(x => x.TipoDePiso == SelectedFullAmbienteViewModel.TipoDePiso)
                .Count();
            if (existingViewModelsWithSameNameCount > 0)
            {
                SelectedFullAmbienteViewModel.Errors = "Já existe um tipo de piso com esse nome";
                return;
            }
            else
            {
                SelectedFullAmbienteViewModel.Errors = "";
            }
            SelectedFullAmbienteViewModel.StoredFloorMatriz = SelectedFullAmbienteViewModel.SelectedfloorMatriz;   
            SelectedFullAmbienteViewModel.SelectedfloorMatriz = null;
            CleanNonUsedParameters();
            SetPropertiesBasedOnLayers();

            if (Create)
            {
                this.SelectedFullAmbienteViewModel.Action = Action.Create;
                AmbienteManagerMVVM.MainView.ApplyAddAmbiente(this.SelectedFullAmbienteViewModel);
            }
            else
            {
                this.SelectedFullAmbienteViewModel.Action = Action.Modify;
                AmbienteManagerMVVM.MainView.ApplyEditAmbiente(this.SelectedFullAmbienteViewModel);
            }
            AmbienteManagerMVVM.MainView.ApplyParentChangesToChildren(this.SelectedFullAmbienteViewModel);

            
            this.DialogResult = new bool?(true);
            this.Dispose();
        }

        private void CleanNonUsedParameters()
        {
            if (!SelectedFullAmbienteViewModel.BoolTelaInferior)
            {
                SelectedFullAmbienteViewModel.TelaInferior = 0;
                SelectedFullAmbienteViewModel.EmendaTelaInferior = 0;
                SelectedFullAmbienteViewModel.CobrimentoTelaInferior = 0;
            }
            if (!SelectedFullAmbienteViewModel.BoolTelaSuperior)
            {
                SelectedFullAmbienteViewModel.TelaSuperior = 0;
                SelectedFullAmbienteViewModel.FinalidadeTelaSuperior = "";
                SelectedFullAmbienteViewModel.EmendaTelaSuperior = 0;
                SelectedFullAmbienteViewModel.CobrimentoTelaSuperior = 0;
            }
            if (!SelectedFullAmbienteViewModel.BoolFibra)
            {
                SelectedFullAmbienteViewModel.SelectedFibra = null;
                SelectedFullAmbienteViewModel.Fibra = "";
                SelectedFullAmbienteViewModel.DosagemFibra = 0;
                SelectedFullAmbienteViewModel.FR1 = "";
                SelectedFullAmbienteViewModel.FR4 = "";
            }
            if (!SelectedFullAmbienteViewModel.BoolReforcoTelaSuperior)
            {
                SelectedFullAmbienteViewModel.ReforcoTelaSuperior = 0;
                SelectedFullAmbienteViewModel.FinalidadeReforcoTelaSuperior = "";
                SelectedFullAmbienteViewModel.EmendaReforcoTelaSuperior = 0;
            }
            if (!SelectedFullAmbienteViewModel.BoolReforcoTelaInferior)
            {
                SelectedFullAmbienteViewModel.ReforcoTelaInferior = 0;
                SelectedFullAmbienteViewModel.FinalidadeReforcoTelaInferior = "";
                SelectedFullAmbienteViewModel.EmendaReforcoTelaInferior = 0;
            }
        }

        private void ProjBasico_Checkbox_Click(object sender, RoutedEventArgs e)
        {
        }

        public void Dispose() => this.Close();

        private void TelaSuperior_Checkbox_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TelaInferior_Checkbox_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ReforcoTela_CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (this.SelectedFullAmbienteViewModel.BoolReforcoDeTela)
                return;
            this.SelectedFullAmbienteViewModel.BoolReforcoTelaSuperior = false;
            this.SelectedFullAmbienteViewModel.BoolReforcoTelaInferior = false;
        }

        private void LayerUP_Button_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = this.Layers_DataGrid.SelectedIndex;
            if (selectedIndex > 0)
                this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Move(selectedIndex, selectedIndex - 1);
            this.CalculateAndSetH();
        }

        private void LayerDown_Button_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = this.Layers_DataGrid.SelectedIndex;
            if (selectedIndex != -1 && selectedIndex < this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Count - 1)
                this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Move(selectedIndex, selectedIndex + 1);
            this.CalculateAndSetH();
        }

        private void AddLayer_Button_Click(object sender, RoutedEventArgs e)
        {
            this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Add(new LayerViewModel("", "", 0.0, AmbienteManagerMVVM.MainView.AllMaterialNames, true));
            int selectedIndex = this.Layers_DataGrid.SelectedIndex;
            if (selectedIndex != -1)
                this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Move(this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Count - 1, selectedIndex + 1);
            this.CalculateAndSetH();
        }

        private void LayerType_Combobox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (this.Layers_DataGrid.SelectedIndex == -1)
                return;
            List<string> stringList = new List<string>();
            string selectedCamadaTipo = this.SelectedFullAmbienteViewModel.FloorMatriz.Layers[this.Layers_DataGrid.SelectedIndex].SelectedCamadaTipo;
            if (selectedCamadaTipo != null)
            {
                switch (selectedCamadaTipo.Length)
                {
                    case 4:
                        if (selectedCamadaTipo == "Base")
                        {
                            stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.Base]);
                            goto label_33;
                        }
                        else
                            break;
                    case 8:
                        switch (selectedCamadaTipo[0])
                        {
                            case 'C':
                                if (selectedCamadaTipo == "Concreto")
                                {
                                    stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.Concreto]);
                                    goto label_33;
                                }
                                else
                                    break;
                            case 'S':
                                if (selectedCamadaTipo == "Sub-Base")
                                {
                                    stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.SubBase]);
                                    goto label_33;
                                }
                                else
                                    break;
                        }
                        break;
                    case 13:
                        if (selectedCamadaTipo == "Base Genérica")
                            goto label_33;
                        else
                            break;
                    case 14:
                        if (selectedCamadaTipo == "Filme Plastico")
                        {
                            stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.FilmePlastico]);
                            goto label_33;
                        }
                        else
                            break;
                    case 18:
                        switch (selectedCamadaTipo[0])
                        {
                            case 'B':
                                if (selectedCamadaTipo == "Bloco Intertravado")
                                {
                                    stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.BlocoIntertravado]);
                                    goto label_33;
                                }
                                else
                                    break;
                            case 'C':
                                if (selectedCamadaTipo == "Concreto Asfáltico")
                                {
                                    stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.ConcretoAsfaltico]);
                                    goto label_33;
                                }
                                else
                                    break;
                            case 'P':
                                if (selectedCamadaTipo == "Pintura de Ligação")
                                {
                                    stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.PinturaDeLigacao]);
                                    goto label_33;
                                }
                                else
                                    break;
                        }
                        break;
                    case 19:
                        if (selectedCamadaTipo == "Reforço de Subleito")
                        {
                            stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.ReforcoSubleito]);
                            goto label_33;
                        }
                        else
                            break;
                    case 20:
                        switch (selectedCamadaTipo[10])
                        {
                            case ' ':
                                if (selectedCamadaTipo == "Imprimacao Asfáltica")
                                {
                                    stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.ImprimacaoAsflatica]);
                                    goto label_33;
                                }
                                else
                                    break;
                            case 'I':
                                if (selectedCamadaTipo == "Camada de Isolamento")
                                {
                                    stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.CamadaIsolamento]);
                                    goto label_33;
                                }
                                else
                                    break;
                            case 'V':
                                if (selectedCamadaTipo == "Camada de Ventilação")
                                {
                                    stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.CamadaVentilacao]);
                                    goto label_33;
                                }
                                else
                                    break;
                        }
                        break;
                    case 22:
                        if (selectedCamadaTipo == "Camada de Assentamento")
                        {
                            stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.CamadaAssentamento]);
                            goto label_33;
                        }
                        else
                            break;
                }
            }
            stringList.AddRange((IEnumerable<string>)AmbienteManagerMVVM.MainView.MaterialsByClass[MaterialClass.Todos]);
        label_33:
            this.SelectedFullAmbienteViewModel.FloorMatriz.Layers[this.Layers_DataGrid.SelectedIndex].PossibleMaterials = stringList;
        }

        private void CalculateAndSetH()
        {
            double num = 0.0;
            for (int index = 0; index < this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Count && this.SelectedFullAmbienteViewModel.FloorMatriz.Layers[index].IsEnabled; ++index)
                num += this.SelectedFullAmbienteViewModel.FloorMatriz.Layers[index].Width;
            this.SelectedFullAmbienteViewModel.HConcreto = num;
        }

        private void RemoveLayer_Button_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = this.Layers_DataGrid.SelectedIndex;
            if (selectedIndex != -1)
                this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.RemoveAt(selectedIndex);
            this.CalculateAndSetH();
        }

        private void FloorMatriz_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (this.SelectedFullAmbienteViewModel.SelectedfloorMatriz != null)
            {
                this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Clear();
                foreach (LayerViewModel layer in (Collection<LayerViewModel>)this.SelectedFullAmbienteViewModel.SelectedfloorMatriz.Layers)
                    this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Add(layer.Clone() as LayerViewModel);
            }
            this.CalculateAndSetH();
        }

        private void Fibra_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (this.SelectedFullAmbienteViewModel.SelectedFibra != null)
            {
                 SelectedFullAmbienteViewModel.Fibra = SelectedFullAmbienteViewModel.SelectedFibra.Fibra;
                 SelectedFullAmbienteViewModel.DosagemFibra = SelectedFullAmbienteViewModel.SelectedFibra.Dosagem;
                 SelectedFullAmbienteViewModel.FR1 = SelectedFullAmbienteViewModel.SelectedFibra.FR1;
                 SelectedFullAmbienteViewModel.FR4 = SelectedFullAmbienteViewModel.SelectedFibra.FR4;
            }
        }

        private void Layers_DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            this.CalculateAndSetH();
        }

        private void StructuralSolution_Combobox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            this.SelectedFullAmbienteViewModel.ParametersScrollVisible = true;
            this.SelectedFullAmbienteViewModel.StructuralSolutionBorderThickness = new Thickness(0.0);
            if (this.SelectedFullAmbienteViewModel.BoolReforcoDeTela)
            {
                this.SelectedFullAmbienteViewModel.IsColumn2Visible = true;
            }
            else
            {
                this.SelectedFullAmbienteViewModel.IsColumn2Visible = false;
            }
            switch (this.SelectedFullAmbienteViewModel.TipoDeSolucao)
            {
                case "TELA SIMPLES":
                    this.SelectedFullAmbienteViewModel.FloorMatrizes = AmbienteManagerMVVM.MainView.FloorMatrizes[FloorMatrizClass.FundacaoDireta];
                    this.SelectedFullAmbienteViewModel.FibraMaterialsIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaSimplesMaterialsIsVisible = true;
                    this.SelectedFullAmbienteViewModel.TelaDuplaMaterialsIsVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaAsfaltoVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaIntertravadoVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaConcretoVisible = true;
                    this.SelectedFullAmbienteViewModel.EspacadorInferiorVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsEnable = false;
                    this.SelectedFullAmbienteViewModel.BoolTelaSuperior = true;
                    this.SelectedFullAmbienteViewModel.BoolEspacadorSuperior = true;
                    this.SelectedFullAmbienteViewModel.BoolTelaInferior = false;
                    this.SelectedFullAmbienteViewModel.BoolEspacadorInferior = false;
                    this.SelectedFullAmbienteViewModel.BoolFibra = false;
                    this.SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    this.SelectedFullAmbienteViewModel.IsColumn1Visible = true;
                    this.SelectedFullAmbienteViewModel.IsRigid = true;
                    this.SelectedFullAmbienteViewModel.DimensoesIsVisible = true;
                    this.SelectedFullAmbienteViewModel.EspacadorSoldadoIsVisible = true;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsVisible = true;
                    this.SelectedFullAmbienteViewModel.TelaInferiorIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TratamentoIsVisible = true;
                    this.SelectedFullAmbienteViewModel.EspacadoresIsVisible = true;
                    this.SelectedFullAmbienteViewModel.ReforcoIsVisible = true;
                    this.SelectedFullAmbienteViewModel.FibraIsVisible = false;
                    break;
                case "TELA DUPLA":
                    this.SelectedFullAmbienteViewModel.FloorMatrizes = AmbienteManagerMVVM.MainView.FloorMatrizes[FloorMatrizClass.FundacaoDireta];
                    this.SelectedFullAmbienteViewModel.FibraMaterialsIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaSimplesMaterialsIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaDuplaMaterialsIsVisible = true;
                    this.SelectedFullAmbienteViewModel.CamadaAsfaltoVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaIntertravadoVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaConcretoVisible = true;
                    this.SelectedFullAmbienteViewModel.EspacadorInferiorVisible = true;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsEnable = false;
                    this.SelectedFullAmbienteViewModel.BoolTelaSuperior = true;
                    this.SelectedFullAmbienteViewModel.BoolEspacadorSuperior = true;
                    this.SelectedFullAmbienteViewModel.BoolTelaInferior = true;
                    this.SelectedFullAmbienteViewModel.BoolEspacadorInferior = true;
                    this.SelectedFullAmbienteViewModel.BoolFibra = false;
                    this.SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    this.SelectedFullAmbienteViewModel.IsColumn1Visible = true;
                    this.SelectedFullAmbienteViewModel.IsRigid = true;
                    this.SelectedFullAmbienteViewModel.DimensoesIsVisible = true;
                    this.SelectedFullAmbienteViewModel.EspacadorSoldadoIsVisible = true;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsVisible = true;
                    this.SelectedFullAmbienteViewModel.TelaInferiorIsVisible = true;
                    this.SelectedFullAmbienteViewModel.TratamentoIsVisible = true;
                    this.SelectedFullAmbienteViewModel.EspacadoresIsVisible = true;
                    this.SelectedFullAmbienteViewModel.ReforcoIsVisible = true;
                    this.SelectedFullAmbienteViewModel.FibraIsVisible = false;
                    break;
                case "FIBRA":
                    this.SelectedFullAmbienteViewModel.FloorMatrizes = AmbienteManagerMVVM.MainView.FloorMatrizes[FloorMatrizClass.FundacaoDireta];
                    this.SelectedFullAmbienteViewModel.FibraMaterialsIsVisible = true;
                    this.SelectedFullAmbienteViewModel.TelaSimplesMaterialsIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaDuplaMaterialsIsVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaAsfaltoVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaIntertravadoVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaConcretoVisible = true;
                    this.SelectedFullAmbienteViewModel.EspacadorInferiorVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsEnable = true;
                    this.SelectedFullAmbienteViewModel.BoolEspacadorSuperior = false;
                    this.SelectedFullAmbienteViewModel.BoolTelaInferior = false;
                    this.SelectedFullAmbienteViewModel.BoolEspacadorInferior = false;
                    this.SelectedFullAmbienteViewModel.BoolFibra = true;
                    this.SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    this.SelectedFullAmbienteViewModel.IsColumn1Visible = true;
                    this.SelectedFullAmbienteViewModel.IsRigid = true;
                    this.SelectedFullAmbienteViewModel.DimensoesIsVisible = true;
                    this.SelectedFullAmbienteViewModel.EspacadorSoldadoIsVisible = true;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsVisible = true;
                    this.SelectedFullAmbienteViewModel.TelaInferiorIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TratamentoIsVisible = true;
                    this.SelectedFullAmbienteViewModel.EspacadoresIsVisible = true;
                    this.SelectedFullAmbienteViewModel.ReforcoIsVisible = true;
                    this.SelectedFullAmbienteViewModel.FibraIsVisible = true;
                    break;
                case "PAV. INTERTRAVADO":
                    this.SelectedFullAmbienteViewModel.FloorMatrizes = AmbienteManagerMVVM.MainView.FloorMatrizes[FloorMatrizClass.PavIntertravado];
                    this.SelectedFullAmbienteViewModel.CamadaAsfaltoVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaIntertravadoVisible = true;
                    this.SelectedFullAmbienteViewModel.CamadaConcretoVisible = false;
                    this.SelectedFullAmbienteViewModel.EspacadorInferiorVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsEnable = false;
                    this.SelectedFullAmbienteViewModel.BoolTelaSuperior = false;
                    this.SelectedFullAmbienteViewModel.BoolTelaInferior = false;
                    this.SelectedFullAmbienteViewModel.BoolFibra = false;
                    this.SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    this.SelectedFullAmbienteViewModel.IsColumn1Visible = false;
                    this.SelectedFullAmbienteViewModel.IsRigid = false;
                    this.SelectedFullAmbienteViewModel.DimensoesIsVisible = false;
                    this.SelectedFullAmbienteViewModel.EspacadorSoldadoIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaInferiorIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TratamentoIsVisible = false;
                    this.SelectedFullAmbienteViewModel.EspacadoresIsVisible = false;
                    this.SelectedFullAmbienteViewModel.ReforcoIsVisible = false;
                    this.SelectedFullAmbienteViewModel.FibraIsVisible = false;
                    break;
                case "PAV. ASFÁLTICO":
                    this.SelectedFullAmbienteViewModel.FloorMatrizes = AmbienteManagerMVVM.MainView.FloorMatrizes[FloorMatrizClass.PavAsfaltico];
                    this.SelectedFullAmbienteViewModel.CamadaAsfaltoVisible = true;
                    this.SelectedFullAmbienteViewModel.CamadaIntertravadoVisible = false;
                    this.SelectedFullAmbienteViewModel.CamadaConcretoVisible = false;
                    this.SelectedFullAmbienteViewModel.EspacadorInferiorVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsEnable = false;
                    this.SelectedFullAmbienteViewModel.BoolTelaSuperior = false;
                    this.SelectedFullAmbienteViewModel.BoolTelaInferior = false;
                    this.SelectedFullAmbienteViewModel.BoolFibra = false;
                    this.SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    this.SelectedFullAmbienteViewModel.IsColumn1Visible = false;
                    this.SelectedFullAmbienteViewModel.IsRigid = false;
                    this.SelectedFullAmbienteViewModel.DimensoesIsVisible = false;
                    this.SelectedFullAmbienteViewModel.EspacadorSoldadoIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaSuperiorIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TelaInferiorIsVisible = false;
                    this.SelectedFullAmbienteViewModel.TratamentoIsVisible = false;
                    this.SelectedFullAmbienteViewModel.EspacadoresIsVisible = false;
                    this.SelectedFullAmbienteViewModel.ReforcoIsVisible = false;
                    this.SelectedFullAmbienteViewModel.FibraIsVisible = false;
                    break;
            }
        }


    }
}
