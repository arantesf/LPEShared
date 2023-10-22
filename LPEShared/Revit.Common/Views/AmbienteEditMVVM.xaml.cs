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
    /// <summary>
    /// Interaction logic for AmbienteManagerMVVM.xaml
    /// </summary>
    public partial class AmbienteEditMVVM : Window, IDisposable
    {
        public bool Create { get; set; }

        public int MyProperty { get; set; }



        public FullAmbienteViewModel SelectedFullAmbienteViewModel { get; set; }

        public AmbienteEditMVVM(FullAmbienteViewModel fullAmbienteViewModels, bool create)
        {
            SelectedFullAmbienteViewModel = fullAmbienteViewModels;
            InitializeComponent();
            Create = create;
        }

        private void ApplyNewAmbiente_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Create)
            {
                SelectedFullAmbienteViewModel.Action = Action.Create;
                AmbienteManagerMVVM.MainView.ApplyAddAmbiente(SelectedFullAmbienteViewModel);
            }
            else
            {
                SelectedFullAmbienteViewModel.Action = Action.Modify;
                AmbienteManagerMVVM.MainView.ApplyEditAmbiente(SelectedFullAmbienteViewModel);
            }
            Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Dispose()
        {
            Close();
        }

        private void TelaSuperior_Checkbox_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFullAmbienteViewModel.BoolTelaSuperior || SelectedFullAmbienteViewModel.BoolReforcoTelaSuperior)
            {
                EspacadorSuperior_CheckBox.Visibility = Visibility.Visible;
                SelectedFullAmbienteViewModel.BoolEspacadorSuperior = true;
            }
            else
            {
                EspacadorSuperior_CheckBox.Visibility = Visibility.Collapsed;
                SelectedFullAmbienteViewModel.BoolEspacadorSuperior = false;
            }
        }

        private void TelaInferior_Checkbox_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFullAmbienteViewModel.BoolTelaInferior || SelectedFullAmbienteViewModel.BoolReforcoTelaInferior)
            {
                EspacadorInferior_CheckBox.Visibility = Visibility.Visible;
                SelectedFullAmbienteViewModel.BoolEspacadorInferior = true;
            }
            else
            {
                EspacadorInferior_CheckBox.Visibility = Visibility.Collapsed;
                SelectedFullAmbienteViewModel.BoolEspacadorInferior = false;
            }
        }

        private void ReforcoTela_CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!SelectedFullAmbienteViewModel.BoolReforcoDeTela)
            {
                SelectedFullAmbienteViewModel.BoolReforcoTelaSuperior = false;
                SelectedFullAmbienteViewModel.BoolReforcoTelaInferior = false;
            }
        }

        private void StructuralSolution_Combobox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            SelectedFullAmbienteViewModel.ParametersScrollVisible = true;
            SelectedFullAmbienteViewModel.StructuralSolutionBorderThickness = new Thickness(0);
            switch (SelectedFullAmbienteViewModel.TipoDeSolucao)
            {
                case "TELA SIMPLES":
                    SelectedFullAmbienteViewModel.CamadaAsfaltoVisible = false;
                    SelectedFullAmbienteViewModel.CamadaIntertravadoVisible = false;
                    SelectedFullAmbienteViewModel.CamadaConcretoVisible = true;
                    SelectedFullAmbienteViewModel.TipoConcretoTelaVisible = true;
                    SelectedFullAmbienteViewModel.TipoConcretoFibraVisible = false;
                    SelectedFullAmbienteViewModel.EspacadorInferiorVisible = false;
                    SelectedFullAmbienteViewModel.TelaSuperiorIsEnable = false;
                    SelectedFullAmbienteViewModel.BoolTelaSuperior = true;
                    SelectedFullAmbienteViewModel.BoolEspacadorSuperior = true;
                    SelectedFullAmbienteViewModel.BoolTelaInferior = false;
                    SelectedFullAmbienteViewModel.BoolEspacadorInferior = false;
                    SelectedFullAmbienteViewModel.BoolFibra = false;
                    SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn1Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn2Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn3Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn4Visible = true;
                    SelectedFullAmbienteViewModel.DimensoesIsVisible = true;
                    SelectedFullAmbienteViewModel.EspacadorSoldadoIsVisible = true;
                    SelectedFullAmbienteViewModel.TelaSuperiorIsVisible = true;
                    SelectedFullAmbienteViewModel.TelaInferiorIsVisible = true;
                    TelaInferior_GroupBox.Visibility = Visibility.Collapsed;
                    Tratamento_GroupBox.Visibility = Visibility.Visible;
                    Espacadores_GroupBox.Visibility = Visibility.Visible;
                    Reforco_GroupBox.Visibility = Visibility.Visible;
                    Fibra_GroupBox.Visibility = Visibility.Collapsed;
                    break;
                case "TELA DUPLA":
                    CamadaAsfalto_StackPanel.Visibility = Visibility.Collapsed;
                    CamadaIntertravado_StackPanel.Visibility = Visibility.Collapsed;
                    CamadaConcreto_StackPanel.Visibility = Visibility.Visible;
                    ConcretoTelaTipo_Combobox.Visibility = Visibility.Visible;
                    ConcretoFibraTipo_Combobox.Visibility = Visibility.Collapsed;
                    EspacadorInferior_CheckBox.Visibility = Visibility.Visible;
                    TelaSuperior_Checkbox.IsEnabled = false;
                    SelectedFullAmbienteViewModel.BoolTelaSuperior = true;
                    SelectedFullAmbienteViewModel.BoolEspacadorSuperior = true;
                    SelectedFullAmbienteViewModel.BoolTelaInferior = true;
                    SelectedFullAmbienteViewModel.BoolEspacadorInferior = true;
                    SelectedFullAmbienteViewModel.BoolFibra = false;
                    SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn1Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn2Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn3Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn4Visible = true;
                    Dimensoes_GroupBox.Visibility = Visibility.Visible;
                    EspacadorSoldado_GroupBox.Visibility = Visibility.Visible;
                    TelaSuperior_GroupBox.Visibility = Visibility.Visible;
                    TelaInferior_GroupBox.Visibility = Visibility.Visible;
                    Tratamento_GroupBox.Visibility = Visibility.Visible;
                    Espacadores_GroupBox.Visibility = Visibility.Visible;
                    Reforco_GroupBox.Visibility = Visibility.Visible;
                    Fibra_GroupBox.Visibility = Visibility.Collapsed;
                    break;
                case "FIBRA":
                    CamadaAsfalto_StackPanel.Visibility = Visibility.Collapsed;
                    CamadaIntertravado_StackPanel.Visibility = Visibility.Collapsed;
                    CamadaConcreto_StackPanel.Visibility = Visibility.Visible;
                    ConcretoTelaTipo_Combobox.Visibility = Visibility.Collapsed;
                    ConcretoFibraTipo_Combobox.Visibility = Visibility.Visible;
                    EspacadorInferior_CheckBox.Visibility = Visibility.Collapsed;
                    TelaSuperior_Checkbox.IsEnabled = true;
                    SelectedFullAmbienteViewModel.BoolFibra = true;
                    SelectedFullAmbienteViewModel.BoolTelaInferior = false;
                    SelectedFullAmbienteViewModel.BoolEspacadorInferior = false;
                    SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn1Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn2Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn3Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn4Visible = true;
                    Dimensoes_GroupBox.Visibility = Visibility.Visible;
                    EspacadorSoldado_GroupBox.Visibility = Visibility.Visible;
                    TelaInferior_GroupBox.Visibility = Visibility.Collapsed;
                    TelaSuperior_GroupBox.Visibility = Visibility.Visible;
                    Tratamento_GroupBox.Visibility = Visibility.Visible;
                    Espacadores_GroupBox.Visibility = Visibility.Visible;
                    Reforco_GroupBox.Visibility = Visibility.Visible;
                    Fibra_GroupBox.Visibility = Visibility.Visible;
                    break;
                case "PAV. INTERTRAVADO":
                    CamadaAsfalto_StackPanel.Visibility = Visibility.Collapsed;
                    CamadaIntertravado_StackPanel.Visibility = Visibility.Visible;
                    CamadaConcreto_StackPanel.Visibility = Visibility.Collapsed;
                    ConcretoTelaTipo_Combobox.Visibility = Visibility.Collapsed;
                    ConcretoFibraTipo_Combobox.Visibility = Visibility.Collapsed;
                    SelectedFullAmbienteViewModel.BoolTelaSuperior = false;
                    SelectedFullAmbienteViewModel.BoolTelaInferior = false;
                    SelectedFullAmbienteViewModel.BoolFibra = false;
                    SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn1Visible = false;
                    SelectedFullAmbienteViewModel.IsColumn2Visible = false;
                    SelectedFullAmbienteViewModel.IsColumn3Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn4Visible = true;
                    Dimensoes_GroupBox.Visibility = Visibility.Collapsed;
                    EspacadorSoldado_GroupBox.Visibility = Visibility.Collapsed;
                    TelaInferior_GroupBox.Visibility = Visibility.Collapsed;
                    TelaSuperior_GroupBox.Visibility = Visibility.Collapsed;
                    Tratamento_GroupBox.Visibility = Visibility.Collapsed;
                    Espacadores_GroupBox.Visibility = Visibility.Collapsed;
                    Fibra_GroupBox.Visibility = Visibility.Collapsed;
                    Reforco_GroupBox.Visibility = Visibility.Collapsed;
                    break;
                case "PAV. ASFÁLTICO":
                    CamadaAsfalto_StackPanel.Visibility = Visibility.Visible;
                    CamadaIntertravado_StackPanel.Visibility = Visibility.Collapsed;
                    CamadaConcreto_StackPanel.Visibility = Visibility.Collapsed;
                    ConcretoTelaTipo_Combobox.Visibility = Visibility.Collapsed;
                    ConcretoFibraTipo_Combobox.Visibility = Visibility.Collapsed;
                    SelectedFullAmbienteViewModel.BoolTelaSuperior = false;
                    SelectedFullAmbienteViewModel.BoolTelaInferior = false;
                    SelectedFullAmbienteViewModel.BoolFibra = false;
                    SelectedFullAmbienteViewModel.IsColumn0Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn1Visible = false;
                    SelectedFullAmbienteViewModel.IsColumn2Visible = false;
                    SelectedFullAmbienteViewModel.IsColumn3Visible = true;
                    SelectedFullAmbienteViewModel.IsColumn4Visible = true;
                    Dimensoes_GroupBox.Visibility = Visibility.Collapsed;
                    EspacadorSoldado_GroupBox.Visibility = Visibility.Collapsed;
                    TelaInferior_GroupBox.Visibility = Visibility.Collapsed;
                    TelaSuperior_GroupBox.Visibility = Visibility.Collapsed;
                    Tratamento_GroupBox.Visibility = Visibility.Collapsed;
                    Espacadores_GroupBox.Visibility = Visibility.Collapsed;
                    Fibra_GroupBox.Visibility = Visibility.Collapsed;
                    Reforco_GroupBox.Visibility = Visibility.Collapsed;
                    break;
                default:
                    break;
            }
        }
    }
}
