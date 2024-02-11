// Decompiled with JetBrains decompiler
// Type: Revit.Common.AmbienteEditMVVM
// Assembly: LPE, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E06EE1B-06C5-4A71-9629-ACF2EB60ECA2
// Assembly location: C:\ProgramData\Autodesk\Revit\Addins\2024\LPE\LPE.dll

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        public bool Create { get; set; }

        public int MyProperty { get; set; }

        public FullAmbienteViewModel SelectedFullAmbienteViewModel { get; set; }

        public AmbienteEditMVVM(FullAmbienteViewModel fullAmbienteViewModels, bool create)
        {
            this.SelectedFullAmbienteViewModel = fullAmbienteViewModels;
            this.InitializeComponent();
            this.Create = create;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Layers_DataGrid.ItemsSource = (IEnumerable)this.SelectedFullAmbienteViewModel.FloorMatriz.Layers;
        }

        private void ApplyNewAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = new bool?(true);
            if (this.Create)
            {
                this.SelectedFullAmbienteViewModel.Action = Action.Create;
                AmbienteManagerMVVM.MainView.ApplyAddAmbiente(this.SelectedFullAmbienteViewModel);
            }
            else
            {
                this.SelectedFullAmbienteViewModel.Action = Action.Modify;
                AmbienteManagerMVVM.MainView.ApplyEditAmbiente(this.SelectedFullAmbienteViewModel);
            }
            this.Dispose();
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
            this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Clear();
            if (this.SelectedFullAmbienteViewModel.SelectedfloorMatriz != null)
            {
                foreach (LayerViewModel layer in (Collection<LayerViewModel>)this.SelectedFullAmbienteViewModel.SelectedfloorMatriz.Layers)
                    this.SelectedFullAmbienteViewModel.FloorMatriz.Layers.Add(layer.Clone() as LayerViewModel);
            }
            this.CalculateAndSetH();
        }

        private void Layers_DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            this.CalculateAndSetH();
        }

        private void StructuralSolution_Combobox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            this.SelectedFullAmbienteViewModel.ParametersScrollVisible = true;
            this.SelectedFullAmbienteViewModel.StructuralSolutionBorderThickness = new Thickness(0.0);
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
                    this.SelectedFullAmbienteViewModel.IsColumn2Visible = false;
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
                    this.SelectedFullAmbienteViewModel.IsColumn2Visible = false;
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
                    this.SelectedFullAmbienteViewModel.BoolTelaSuperior = false;
                    this.SelectedFullAmbienteViewModel.BoolEspacadorSuperior = false;
                    this.SelectedFullAmbienteViewModel.BoolTelaInferior = false;
                    this.SelectedFullAmbienteViewModel.BoolEspacadorInferior = false;
                    this.SelectedFullAmbienteViewModel.BoolFibra = true;
                    this.SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    this.SelectedFullAmbienteViewModel.IsColumn1Visible = true;
                    this.SelectedFullAmbienteViewModel.IsColumn2Visible = false;
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
                    this.SelectedFullAmbienteViewModel.IsColumn2Visible = false;
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
                    this.SelectedFullAmbienteViewModel.IsColumn2Visible = false;
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
