// Decompiled with JetBrains decompiler
// Type: Revit.Common.FullAmbienteViewModel
// Assembly: LPE, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E06EE1B-06C5-4A71-9629-ACF2EB60ECA2
// Assembly location: C:\ProgramData\Autodesk\Revit\Addins\2024\LPE\LPE.dll

using Autodesk.Revit.DB;
using Revit.Common.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Revit.Common
{
    public enum Action
    {
        Continue,
        Delete,
        Modify,
        Create,
    }

    public class FullAmbienteViewModel : ViewModelBase, ICloneable
    {
        private FloorMatriz selectedfloorMatriz;
        private FloorMatriz floorMatriz = new FloorMatriz();
        private ObservableCollection<LayerViewModel> layerViewModels = new ObservableCollection<LayerViewModel>();
        public List<FloorMatriz> floorMatrizes = new List<FloorMatriz>();
        private bool parametersScrollVisible;
        private Thickness structuralSolutionBorderThickness = new Thickness(0.0);
        private bool isRigid;
        private bool fibraMaterialsIsVisible = true;
        private bool telaDuplaMaterialsIsVisible = true;
        private bool telaSimplesMaterialsIsVisible = true;
        private bool camadaAsfaltoVisible = true;
        private bool camadaIntertravadoVisible = true;
        private bool camadaConcretoVisible = true;
        private bool espacadorInferiorVisible = true;
        private bool telaSuperiorIsEnable = true;
        private bool dimensoesIsVisible = true;
        private bool espacadorSoldadoIsVisible = true;
        private bool telaSuperiorIsVisible = true;
        private bool telaInferiorIsVisible = true;
        private bool fibraIsVisible = true;
        private bool tratamentoIsVisible = true;
        private bool espacadoresIsVisible = true;
        private bool reforcoIsVisible = true;
        private bool isColumn0Visible = true;
        private bool isColumn1Visible = true;
        private bool isColumn2Visible = true;
        private bool isColumn3Visible = true;
        private ElementId id = new ElementId(-1);
        private Action action;
        private string tipoDeSolucao;
        private string tipoDePiso = "";
        private string ambiente = "";
        private double hConcreto;
        private double comprimentoDaPlaca;
        private double larguraDaPlaca;
        private bool boolReforcoDeTela;
        private int hEspacadorSoldado;
        private bool boolTelaSuperior;
        private string telaSuperior = "";
        private string finalidadeTelaSuperior = "";
        private double emendaTelaSuperior;
        private bool boolTelaInferior;
        private string telaInferior = "";
        private double emendaTelaInferior;
        private bool boolFibra;
        private string fibra = "";
        private double dosagemFibra;
        private string fR1 = "";
        private string fR4 = "";
        private bool boolTratamentoSuperficial;
        private string tratamentoSuperficial = "";
        private bool boolEspacadorSuperior;
        private int hEspacadorSuperior;
        private bool boolEspacadorInferior;
        private int hEspacadorInferior;
        private bool boolReforcoTelaSuperior;
        private string reforcoTelaSuperior = "";
        private double emendaReforcoTelaSuperior;
        private string finalidadeReforcoTelaSuperior = "";
        private bool boolReforcoTelaInferior;
        private string reforcoTelaInferior = "";
        private double emendaReforcoTelaInferior;
        private string finalidadeReforcoTelaInferior = "";
        private bool boolLPEVeiculosPesados;
        private string tagConcreto = "";
        private bool cBSubBase;
        private double hSubBase;
        private string tagSubBase = "";
        private bool cBBaseGenerica;
        private string tagBaseGenerica = "";
        private bool cBRefSubleito;
        private double hRefSubleito;
        private string tagRefSubleito = "";
        private bool cBSubleito;
        private string tagSubleito = "";
        private string lPECarga = "";
        private double lPECargaParam1;
        private double lPECargaParam2;

        public FloorMatriz SelectedfloorMatriz
        {
            get => this.selectedfloorMatriz;
            set
            {
                this.selectedfloorMatriz = value;
                this.OnPropertyChanged(nameof(SelectedfloorMatriz));
            }
        }

        public FloorMatriz FloorMatriz
        {
            get => this.floorMatriz;
            set
            {
                this.floorMatriz = value;
                this.OnPropertyChanged(nameof(FloorMatriz));
            }
        }

        public ObservableCollection<LayerViewModel> LayerViewModels
        {
            get => this.layerViewModels;
            set
            {
                this.layerViewModels = value;
                this.OnPropertyChanged(nameof(LayerViewModels));
            }
        }

        public List<FloorMatriz> FloorMatrizes
        {
            get => this.floorMatrizes;
            set
            {
                this.floorMatrizes = value;
                this.OnPropertyChanged(nameof(FloorMatrizes));
            }
        }

        public List<string> StructuralSolutions { get; set; } = new List<string>()
    {
      "TELA SIMPLES",
      "TELA DUPLA",
      "FIBRA",
      "PAV. INTERTRAVADO",
      "PAV. ASFÁLTICO"
    };

        private void SetTipoDePiso()
        {
            string str1 = "";
            string str2 = "";
            string str3 = "";
            string str4 = "";
            string str5 = "";
            string str6 = "";
            if (this.boolFibra)
                str1 += string.Format("FIBRA {0}", (object)this.dosagemFibra);
            else if (this.boolTelaSuperior)
            {
                str1 += this.telaSuperior;
                if (this.boolTelaInferior)
                    str1 = str1 + "/" + this.telaInferior;
            }
            else if (this.boolTelaInferior)
                str1 += this.telaInferior;

            //if (this.boolReforcoTelaSuperior)
            //{
            //    str2 = " - REF_" + this.finalidadeReforcoTelaSuperior;
            //    str3 = " (" + this.reforcoTelaSuperior + ")";
            //    if (this.boolReforcoTelaInferior)
            //    {
            //        str4 = " + REF_" + this.finalidadeReforcoTelaInferior;
            //        str5 = " (" + this.reforcoTelaInferior + ")";
            //    }
            //}
            //else if (this.boolReforcoTelaInferior)
            //{
            //    str4 = " - REF_" + this.finalidadeReforcoTelaInferior;
            //    str5 = " (" + this.reforcoTelaInferior + ")";
            //}
            if (this.BoolReforcoDeTela)
            {
                str6 += " - REFORÇO";
            }
            this.TipoDePiso = string.Format("{0} - H={1}cm ({2}){3}{4}{5}{6}{7}", (object)this.ambiente, (object)this.HConcreto, (object)str1, (object)str2, (object)str3, (object)str4, (object)str5, (object)str6);
        }

        public bool ParametersScrollVisible
        {
            get => this.parametersScrollVisible;
            set
            {
                this.parametersScrollVisible = value;
                this.OnPropertyChanged(nameof(ParametersScrollVisible));
            }
        }

        public Thickness StructuralSolutionBorderThickness
        {
            get => this.structuralSolutionBorderThickness;
            set
            {
                this.structuralSolutionBorderThickness = value;
                this.OnPropertyChanged(nameof(StructuralSolutionBorderThickness));
            }
        }

        public bool IsRigid
        {
            get => this.isRigid;
            set
            {
                this.isRigid = value;
                this.OnPropertyChanged(nameof(IsRigid));
            }
        }

        public bool FibraMaterialsIsVisible
        {
            get => this.fibraMaterialsIsVisible;
            set
            {
                this.fibraMaterialsIsVisible = value;
                this.OnPropertyChanged(nameof(FibraMaterialsIsVisible));
            }
        }

        public bool TelaDuplaMaterialsIsVisible
        {
            get => this.telaDuplaMaterialsIsVisible;
            set
            {
                this.telaDuplaMaterialsIsVisible = value;
                this.OnPropertyChanged(nameof(TelaDuplaMaterialsIsVisible));
            }
        }

        public bool TelaSimplesMaterialsIsVisible
        {
            get => this.telaSimplesMaterialsIsVisible;
            set
            {
                this.telaSimplesMaterialsIsVisible = value;
                this.OnPropertyChanged(nameof(TelaSimplesMaterialsIsVisible));
            }
        }

        public bool CamadaAsfaltoVisible
        {
            get => this.camadaAsfaltoVisible;
            set
            {
                this.camadaAsfaltoVisible = value;
                this.OnPropertyChanged(nameof(CamadaAsfaltoVisible));
            }
        }

        public bool CamadaIntertravadoVisible
        {
            get => this.camadaIntertravadoVisible;
            set
            {
                this.camadaIntertravadoVisible = value;
                this.OnPropertyChanged(nameof(CamadaIntertravadoVisible));
            }
        }

        public bool CamadaConcretoVisible
        {
            get => this.camadaConcretoVisible;
            set
            {
                this.camadaConcretoVisible = value;
                this.OnPropertyChanged(nameof(CamadaConcretoVisible));
            }
        }

        public bool EspacadorInferiorVisible
        {
            get => this.espacadorInferiorVisible;
            set
            {
                this.espacadorInferiorVisible = value;
                this.OnPropertyChanged(nameof(EspacadorInferiorVisible));
            }
        }

        public bool TelaSuperiorIsEnable
        {
            get => this.telaSuperiorIsEnable;
            set
            {
                this.telaSuperiorIsEnable = value;
                this.OnPropertyChanged(nameof(TelaSuperiorIsEnable));
            }
        }

        public bool DimensoesIsVisible
        {
            get => this.dimensoesIsVisible;
            set
            {
                this.dimensoesIsVisible = value;
                this.OnPropertyChanged(nameof(DimensoesIsVisible));
            }
        }

        public bool EspacadorSoldadoIsVisible
        {
            get => this.espacadorSoldadoIsVisible;
            set
            {
                this.espacadorSoldadoIsVisible = value;
                this.OnPropertyChanged(nameof(EspacadorSoldadoIsVisible));
            }
        }

        public bool TelaSuperiorIsVisible
        {
            get => this.telaSuperiorIsVisible;
            set
            {
                this.telaSuperiorIsVisible = value;
                this.OnPropertyChanged(nameof(TelaSuperiorIsVisible));
            }
        }

        public bool TelaInferiorIsVisible
        {
            get => this.telaInferiorIsVisible;
            set
            {
                this.telaInferiorIsVisible = value;
                this.OnPropertyChanged(nameof(TelaInferiorIsVisible));
            }
        }

        public bool FibraIsVisible
        {
            get => this.fibraIsVisible;
            set
            {
                this.fibraIsVisible = value;
                this.OnPropertyChanged(nameof(FibraIsVisible));
            }
        }

        public bool TratamentoIsVisible
        {
            get => this.tratamentoIsVisible;
            set
            {
                this.tratamentoIsVisible = value;
                this.OnPropertyChanged(nameof(TratamentoIsVisible));
            }
        }

        public bool EspacadoresIsVisible
        {
            get => this.espacadoresIsVisible;
            set
            {
                this.espacadoresIsVisible = value;
                this.OnPropertyChanged(nameof(EspacadoresIsVisible));
            }
        }

        public bool ReforcoIsVisible
        {
            get => this.reforcoIsVisible;
            set
            {
                this.reforcoIsVisible = value;
                this.OnPropertyChanged(nameof(ReforcoIsVisible));
            }
        }

        public bool IsColumn0Visible
        {
            get => this.isColumn0Visible;
            set
            {
                this.isColumn0Visible = value;
                this.OnPropertyChanged(nameof(IsColumn0Visible));
            }
        }

        public bool IsColumn1Visible
        {
            get => this.isColumn1Visible;
            set
            {
                this.isColumn1Visible = value;
                this.OnPropertyChanged(nameof(IsColumn1Visible));
            }
        }

        public bool IsColumn2Visible
        {
            get => this.isColumn2Visible;
            set
            {
                this.isColumn2Visible = value;
                this.OnPropertyChanged(nameof(IsColumn2Visible));
            }
        }

        public bool IsColumn3Visible
        {
            get => this.isColumn3Visible;
            set
            {
                this.isColumn3Visible = value;
                this.OnPropertyChanged(nameof(IsColumn3Visible));
            }
        }

        public ElementId Id
        {
            get => this.id;
            set => this.id = value;
        }

        public Action Action
        {
            get => this.action;
            set => this.action = value;
        }

        public string TipoDeSolucao
        {
            get => this.tipoDeSolucao;
            set
            {
                this.tipoDeSolucao = value;
                this.OnPropertyChanged(nameof(TipoDeSolucao));
            }
        }

        public string TipoDePiso
        {
            get => this.tipoDePiso;
            set
            {
                this.tipoDePiso = value;
                this.OnPropertyChanged(nameof(TipoDePiso));
            }
        }

        public string Ambiente
        {
            get => this.ambiente;
            set
            {
                this.ambiente = value;
                this.OnPropertyChanged(nameof(Ambiente));
                this.SetTipoDePiso();
            }
        }

        public double HConcreto
        {
            get => this.hConcreto;
            set
            {
                this.hConcreto = value;
                this.OnPropertyChanged(nameof(HConcreto));
                this.SetTipoDePiso();
            }
        }

        public double ComprimentoDaPlaca
        {
            get => this.comprimentoDaPlaca;
            set
            {
                this.comprimentoDaPlaca = value;
                this.OnPropertyChanged(nameof(ComprimentoDaPlaca));
                this.SetTipoDePiso();
            }
        }

        public double LarguraDaPlaca
        {
            get => this.larguraDaPlaca;
            set
            {
                this.larguraDaPlaca = value;
                this.OnPropertyChanged(nameof(LarguraDaPlaca));
                this.SetTipoDePiso();
            }
        }

        public bool BoolReforcoDeTela
        {
            get => this.boolReforcoDeTela;
            set
            {
                this.boolReforcoDeTela = value;
                this.OnPropertyChanged(nameof(BoolReforcoDeTela));
                this.SetTipoDePiso();
            }
        }

        public int HEspacadorSoldado
        {
            get => this.hEspacadorSoldado;
            set
            {
                this.hEspacadorSoldado = value;
                this.OnPropertyChanged(nameof(HEspacadorSoldado));
                this.SetTipoDePiso();
            }
        }

        public bool BoolTelaSuperior
        {
            get => this.boolTelaSuperior;
            set
            {
                this.boolTelaSuperior = value;
                this.OnPropertyChanged(nameof(BoolTelaSuperior));
                this.SetTipoDePiso();
            }
        }

        public string TelaSuperior
        {
            get => this.telaSuperior;
            set
            {
                this.telaSuperior = value;
                this.OnPropertyChanged(nameof(TelaSuperior));
                this.SetTipoDePiso();
            }
        }

        public string FinalidadeTelaSuperior
        {
            get => this.finalidadeTelaSuperior;
            set
            {
                this.finalidadeTelaSuperior = value;
                this.OnPropertyChanged(nameof(FinalidadeTelaSuperior));
                this.SetTipoDePiso();
            }
        }

        public double EmendaTelaSuperior
        {
            get => this.emendaTelaSuperior;
            set
            {
                this.emendaTelaSuperior = value;
                this.OnPropertyChanged(nameof(EmendaTelaSuperior));
                this.SetTipoDePiso();
            }
        }

        public bool BoolTelaInferior
        {
            get => this.boolTelaInferior;
            set
            {
                this.boolTelaInferior = value;
                this.OnPropertyChanged(nameof(BoolTelaInferior));
                this.SetTipoDePiso();
            }
        }

        public string TelaInferior
        {
            get => this.telaInferior;
            set
            {
                this.telaInferior = value;
                this.OnPropertyChanged(nameof(TelaInferior));
                this.SetTipoDePiso();
            }
        }

        public double EmendaTelaInferior
        {
            get => this.emendaTelaInferior;
            set
            {
                this.emendaTelaInferior = value;
                this.OnPropertyChanged(nameof(EmendaTelaInferior));
                this.SetTipoDePiso();
            }
        }

        public bool BoolFibra
        {
            get => this.boolFibra;
            set
            {
                this.boolFibra = value;
                this.OnPropertyChanged(nameof(BoolFibra));
                this.SetTipoDePiso();
            }
        }

        public string Fibra
        {
            get => this.fibra;
            set
            {
                this.fibra = value;
                this.OnPropertyChanged(nameof(Fibra));
                this.SetTipoDePiso();
            }
        }

        public double DosagemFibra
        {
            get => this.dosagemFibra;
            set
            {
                this.dosagemFibra = value;
                this.OnPropertyChanged(nameof(DosagemFibra));
                this.SetTipoDePiso();
            }
        }

        public string FR1
        {
            get => this.fR1;
            set
            {
                this.fR1 = value;
                this.OnPropertyChanged(nameof(FR1));
                this.SetTipoDePiso();
            }
        }

        public string FR4
        {
            get => this.fR4;
            set
            {
                this.fR4 = value;
                this.OnPropertyChanged(nameof(FR4));
                this.SetTipoDePiso();
            }
        }

        public bool BoolTratamentoSuperficial
        {
            get => this.boolTratamentoSuperficial;
            set
            {
                this.boolTratamentoSuperficial = value;
                this.OnPropertyChanged(nameof(BoolTratamentoSuperficial));
                this.SetTipoDePiso();
            }
        }

        public string TratamentoSuperficial
        {
            get => this.tratamentoSuperficial;
            set
            {
                this.tratamentoSuperficial = value;
                this.OnPropertyChanged(nameof(TratamentoSuperficial));
                this.SetTipoDePiso();
            }
        }

        public bool BoolEspacadorSuperior
        {
            get => this.boolEspacadorSuperior;
            set
            {
                this.boolEspacadorSuperior = value;
                this.OnPropertyChanged(nameof(BoolEspacadorSuperior));
                this.SetTipoDePiso();
            }
        }

        public int HEspacadorSuperior
        {
            get => this.hEspacadorSuperior;
            set
            {
                this.hEspacadorSuperior = value;
                this.OnPropertyChanged(nameof(HEspacadorSuperior));
                this.SetTipoDePiso();
            }
        }

        public bool BoolEspacadorInferior
        {
            get => this.boolEspacadorInferior;
            set
            {
                this.boolEspacadorInferior = value;
                this.OnPropertyChanged(nameof(BoolEspacadorInferior));
                this.SetTipoDePiso();
            }
        }

        public int HEspacadorInferior
        {
            get => this.hEspacadorInferior;
            set
            {
                this.hEspacadorInferior = value;
                this.OnPropertyChanged(nameof(HEspacadorInferior));
                this.SetTipoDePiso();
            }
        }

        public bool BoolReforcoTelaSuperior
        {
            get => this.boolReforcoTelaSuperior;
            set
            {
                this.boolReforcoTelaSuperior = value;
                this.OnPropertyChanged(nameof(BoolReforcoTelaSuperior));
                this.SetTipoDePiso();
            }
        }

        public string ReforcoTelaSuperior
        {
            get => this.reforcoTelaSuperior;
            set
            {
                this.reforcoTelaSuperior = value;
                this.OnPropertyChanged(nameof(ReforcoTelaSuperior));
                this.SetTipoDePiso();
            }
        }

        public double EmendaReforcoTelaSuperior
        {
            get => this.emendaReforcoTelaSuperior;
            set
            {
                this.emendaReforcoTelaSuperior = value;
                this.OnPropertyChanged(nameof(EmendaReforcoTelaSuperior));
                this.SetTipoDePiso();
            }
        }

        public string FinalidadeReforcoTelaSuperior
        {
            get => this.finalidadeReforcoTelaSuperior;
            set
            {
                this.finalidadeReforcoTelaSuperior = value;
                this.OnPropertyChanged(nameof(FinalidadeReforcoTelaSuperior));
                this.SetTipoDePiso();
            }
        }

        public bool BoolReforcoTelaInferior
        {
            get => this.boolReforcoTelaInferior;
            set
            {
                this.boolReforcoTelaInferior = value;
                this.OnPropertyChanged(nameof(BoolReforcoTelaInferior));
                this.SetTipoDePiso();
            }
        }

        public string ReforcoTelaInferior
        {
            get => this.reforcoTelaInferior;
            set
            {
                this.reforcoTelaInferior = value;
                this.OnPropertyChanged(nameof(ReforcoTelaInferior));
                this.SetTipoDePiso();
            }
        }

        public double EmendaReforcoTelaInferior
        {
            get => this.emendaReforcoTelaInferior;
            set
            {
                this.emendaReforcoTelaInferior = value;
                this.OnPropertyChanged(nameof(EmendaReforcoTelaInferior));
                this.SetTipoDePiso();
            }
        }

        public string FinalidadeReforcoTelaInferior
        {
            get => this.finalidadeReforcoTelaInferior;
            set
            {
                this.finalidadeReforcoTelaInferior = value;
                this.OnPropertyChanged(nameof(FinalidadeReforcoTelaInferior));
                this.SetTipoDePiso();
            }
        }

        public bool BoolLPEVeiculosPesados
        {
            get => this.boolLPEVeiculosPesados;
            set
            {
                this.boolLPEVeiculosPesados = value;
                this.OnPropertyChanged(nameof(BoolLPEVeiculosPesados));
                this.SetTipoDePiso();
            }
        }

        public string TagConcreto
        {
            get => this.tagConcreto;
            set
            {
                this.tagConcreto = value;
                this.OnPropertyChanged(nameof(TagConcreto));
                this.SetTipoDePiso();
            }
        }

        public bool CBSubBase
        {
            get => this.cBSubBase;
            set
            {
                this.cBSubBase = value;
                this.OnPropertyChanged(nameof(CBSubBase));
                this.SetTipoDePiso();
            }
        }

        public double HSubBase
        {
            get => this.hSubBase;
            set
            {
                this.hSubBase = value;
                this.OnPropertyChanged(nameof(HSubBase));
                this.SetTipoDePiso();
            }
        }

        public string TagSubBase
        {
            get => this.tagSubBase;
            set
            {
                this.tagSubBase = value;
                this.OnPropertyChanged(nameof(TagSubBase));
                this.SetTipoDePiso();
            }
        }

        public bool CBBaseGenerica
        {
            get => this.cBBaseGenerica;
            set
            {
                this.cBBaseGenerica = value;
                this.OnPropertyChanged(nameof(CBBaseGenerica));
                this.SetTipoDePiso();
            }
        }

        public string TagBaseGenerica
        {
            get => this.tagBaseGenerica;
            set
            {
                this.tagBaseGenerica = value;
                this.OnPropertyChanged(nameof(TagBaseGenerica));
                this.SetTipoDePiso();
            }
        }

        public bool CBRefSubleito
        {
            get => this.cBRefSubleito;
            set
            {
                this.cBRefSubleito = value;
                this.OnPropertyChanged(nameof(CBRefSubleito));
                this.SetTipoDePiso();
            }
        }

        public double HRefSubleito
        {
            get => this.hRefSubleito;
            set
            {
                this.hRefSubleito = value;
                this.OnPropertyChanged(nameof(HRefSubleito));
                this.SetTipoDePiso();
            }
        }

        public string TagRefSubleito
        {
            get => this.tagRefSubleito;
            set
            {
                this.tagRefSubleito = value;
                this.OnPropertyChanged(nameof(TagRefSubleito));
                this.SetTipoDePiso();
            }
        }

        public bool CBSubleito
        {
            get => this.cBSubleito;
            set
            {
                this.cBSubleito = value;
                this.OnPropertyChanged(nameof(CBSubleito));
                this.SetTipoDePiso();
            }
        }

        public string TagSubleito
        {
            get => this.tagSubleito;
            set
            {
                this.tagSubleito = value;
                this.OnPropertyChanged(nameof(TagSubleito));
                this.SetTipoDePiso();
            }
        }

        public string LPECarga
        {
            get => this.lPECarga;
            set
            {
                this.lPECarga = value;
                this.OnPropertyChanged(nameof(LPECarga));
                this.SetTipoDePiso();
            }
        }

        public double LPECargaParam1
        {
            get => this.lPECargaParam1;
            set
            {
                this.lPECargaParam1 = value;
                this.OnPropertyChanged(nameof(LPECargaParam1));
                this.SetTipoDePiso();
            }
        }

        public double LPECargaParam2
        {
            get => this.lPECargaParam2;
            set
            {
                this.lPECargaParam2 = value;
                this.OnPropertyChanged(nameof(LPECargaParam2));
                this.SetTipoDePiso();
            }
        }
        public FullAmbienteViewModel()
        {

        }
        public FullAmbienteViewModel(Element tipoDePisoElement, Element itensDeDetalheElement, Document doc)
        {
            if (tipoDePisoElement.LookupParameter("(s/n) Fibra").AsInteger() == 1)
                this.TipoDeSolucao = "FIBRA";
            else if (tipoDePisoElement.LookupParameter("(s/n) Tela Superior").AsInteger() == 1)
                this.TipoDeSolucao = tipoDePisoElement.LookupParameter("(s/n) Tela Inferior").AsInteger() != 1 ? "TELA SIMPLES" : "TELA DUPLA";
            if (tipoDePisoElement == null)
                return;
            this.Id = tipoDePisoElement.Id;
            this.Action = Action.Continue;
            this.Ambiente = tipoDePisoElement.LookupParameter(nameof(Ambiente)).AsString() ?? "";
            this.BoolEspacadorInferior = tipoDePisoElement.LookupParameter("(s/n) Espaçador Inf.").AsInteger() == 1;
            this.BoolEspacadorSuperior = tipoDePisoElement.LookupParameter("(s/n) Espaçador Sup.").AsInteger() == 1;
            this.BoolFibra = tipoDePisoElement.LookupParameter("(s/n) Fibra").AsInteger() == 1;
            this.BoolLPEVeiculosPesados = tipoDePisoElement.LookupParameter("LPE_VEÍCULOS PESADOS").AsInteger() == 1;
            this.BoolReforcoTelaInferior = tipoDePisoElement.LookupParameter("(s/n) Ref. Tela Inferior").AsInteger() == 1;
            this.BoolReforcoTelaSuperior = tipoDePisoElement.LookupParameter("(s/n) Ref. Tela Superior").AsInteger() == 1;
            this.BoolTelaInferior = tipoDePisoElement.LookupParameter("(s/n) Tela Inferior").AsInteger() == 1;
            this.BoolTelaSuperior = tipoDePisoElement.LookupParameter("(s/n) Tela Superior").AsInteger() == 1;
            this.BoolTratamentoSuperficial = tipoDePisoElement.LookupParameter("(s/n) Tratamento Superficial").AsInteger() == 1;
            this.ComprimentoDaPlaca = tipoDePisoElement.LookupParameter("Comprimento Placa").AsDouble();
            this.DosagemFibra = tipoDePisoElement.LookupParameter("Dosagem da Fibra (kg/m\u00B3)").AsDouble();
            this.EmendaReforcoTelaInferior = tipoDePisoElement.LookupParameter("Emenda - Ref. Tela Inf").AsDouble();
            this.EmendaReforcoTelaSuperior = tipoDePisoElement.LookupParameter("Emenda - Ref. Tela Sup").AsDouble();
            this.EmendaTelaInferior = tipoDePisoElement.LookupParameter("Emenda - Tela Inferior (\"0,xx\")").AsDouble();
            this.EmendaTelaSuperior = tipoDePisoElement.LookupParameter("Emenda - Tela Superior (\"0,xx\")").AsDouble();
            this.Fibra = tipoDePisoElement.LookupParameter(nameof(Fibra)).AsString() ?? "";
            try
            {
                this.BoolReforcoDeTela = tipoDePisoElement.LookupParameter("Reforço de Tela").AsInteger() == 1;
                this.FinalidadeReforcoTelaInferior = tipoDePisoElement.LookupParameter("Finalidade Ref. Tela Inf").AsString() ?? "";
                this.FinalidadeReforcoTelaSuperior = tipoDePisoElement.LookupParameter("Finalidade Ref. Tela Sup").AsString() ?? "";
                this.FinalidadeTelaSuperior = tipoDePisoElement.LookupParameter("Finalidade - Tela Superior").AsString() ?? "";
            }
            catch (Exception ex)
            {
            }
            this.FR1 = tipoDePisoElement.LookupParameter(nameof(FR1)).AsString() ?? "";
            this.FR4 = tipoDePisoElement.LookupParameter(nameof(FR4)).AsString() ?? "";
            this.HConcreto = UnitUtils.ConvertFromInternalUnits(tipoDePisoElement.LookupParameter("H Concreto").AsDouble(), UnitTypeId.Centimeters);
            this.HEspacadorInferior = tipoDePisoElement.LookupParameter("H-Espaçador Inferior (cm)").AsInteger();
            this.HEspacadorSoldado = tipoDePisoElement.LookupParameter("H-Espaçador Soldado (cm)").AsInteger();
            this.HEspacadorSuperior = tipoDePisoElement.LookupParameter("H-Espaçador Superior (cm)").AsInteger();
            this.LarguraDaPlaca = tipoDePisoElement.LookupParameter("Largura da Placa").AsDouble();
            this.ReforcoTelaInferior = tipoDePisoElement.LookupParameter("Ref. Tela Inferior").AsString() ?? "";
            this.ReforcoTelaSuperior = tipoDePisoElement.LookupParameter("Ref. Tela Superior").AsString() ?? "";
            this.TelaInferior = tipoDePisoElement.LookupParameter("Tela Inferior").AsString() ?? "";
            this.TelaSuperior = tipoDePisoElement.LookupParameter("Tela Superior").AsString() ?? "";
            this.TratamentoSuperficial = tipoDePisoElement.LookupParameter("Tratamento Superficial").AsString() ?? "";
            this.TipoDePiso = tipoDePisoElement.Name;
            if (itensDeDetalheElement == null)
                return;
            this.CBBaseGenerica = itensDeDetalheElement.LookupParameter("CB_BASE GENÉRICA").AsInteger() == 1;
            this.CBRefSubleito = itensDeDetalheElement.LookupParameter("CB_REF. SUBLEITO").AsInteger() == 1;
            this.CBSubBase = itensDeDetalheElement.LookupParameter("CB_SUB_BASE").AsInteger() == 1;
            this.CBSubleito = itensDeDetalheElement.LookupParameter("CB_SUBLEITO").AsInteger() == 1;
            try
            {
                this.HRefSubleito = UnitUtils.ConvertFromInternalUnits(itensDeDetalheElement.LookupParameter("H Ref. Subleito").AsDouble(), UnitTypeId.Centimeters);
            }
            catch (Exception ex)
            {
            }
            this.HSubBase = UnitUtils.ConvertFromInternalUnits(itensDeDetalheElement.LookupParameter("H SUB_BASE").AsDouble(), UnitTypeId.Centimeters);
            this.LPECarga = itensDeDetalheElement.LookupParameter("LPE_CARGA").AsString();
            this.LPECargaParam1 = 0.0;
            this.LPECargaParam2 = 0.0;
            this.TagBaseGenerica = itensDeDetalheElement.LookupParameter("TAG_BASE GENÉRICA").AsString();
            this.TagConcreto = itensDeDetalheElement.LookupParameter("TAG_CONCRETO").AsString();
            this.TagRefSubleito = itensDeDetalheElement.LookupParameter("TAG_REF. SUBLEITO").AsString();
            this.TagSubBase = itensDeDetalheElement.LookupParameter("TAG_SUB-BASE").AsString();
            this.TagSubleito = itensDeDetalheElement.LookupParameter("TAG_SUBLEITO").AsString();
            try
            {
                FloorType floorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .FirstOrDefault(x => this.TipoDePiso == x.Name);

                if (floorType != null)
                {
                    FloorMatriz floorMatriz = new FloorMatriz();
                    floorMatriz.GetFloorTypeData(floorType, GlobalVariables.materialsByClass);
                    FloorMatriz = floorMatriz;
                }
            }
            catch (Exception)
            {
            }
        }

        public object Clone() => (object)(FullAmbienteViewModel)this.MemberwiseClone();
    }
}
