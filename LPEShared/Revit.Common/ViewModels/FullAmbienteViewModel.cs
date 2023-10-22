using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;

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
        #region ComboBoxes

        public List<string> ConcretoMaterials { get; set; } = new List<string>();
        public List<string> ConcretoTypes { get; set; } = new List<string>();

        private string selectedConcretoMaterial;

        public string SelectedConcretoMaterial
        {
            get { return selectedConcretoMaterial; }
            set { selectedConcretoMaterial = value; OnPropertyChanged(); }
        }

        private string selectedConcretoType;

        public string SelectedConcretoType
        {
            get { return selectedConcretoType; }
            set { selectedConcretoType = value; OnPropertyChanged(); }
        }

        public List<string> IntertravadoMaterials { get; set; } = new List<string>();
        public List<string> IntertravadoTypes { get; set; } = new List<string>();

        private string selectedIntertravadoMaterial;

        public string SelectedIntertravadoMaterial
        {
            get { return selectedIntertravadoMaterial; }
            set { selectedIntertravadoMaterial = value; }
        }

        private string selectedIntertravadoType;

        public string SelectedIntertravadoType
        {
            get { return selectedIntertravadoType; OnPropertyChanged(); }
            set { selectedIntertravadoType = value; OnPropertyChanged(); }
        }
        public List<string> AsfaltoMaterials { get; set; } = new List<string>();
        public List<string> AsfaltoTypes { get; set; } = new List<string>();

        private string selectedAsfaltoMaterial;
        public string SelectedAsfaltoMaterial
        {
            get { return selectedAsfaltoMaterial; }
            set { selectedAsfaltoMaterial = value; }
        }

        private string selectedAsfaltoType;

        public string SelectedAsfaltoType
        {
            get { return selectedAsfaltoType; }
            set { selectedAsfaltoType = value; }
        }
        #endregion
        public List<string> StructuralSolutions { get; set; } = new List<string>
            {
                "TELA SIMPLES",
                "TELA DUPLA",
                "FIBRA",
                "PAV. INTERTRAVADO",
                "PAV. ASFÁLTICO",
            };
        void SetTipoDePiso()
        {
            string solucao = "";
            string reforcoSuperior = "";
            string solucaoReforcoSuperior = "";
            string reforcoInferior = "";
            string solucaoReforcoInferior = "";
            if (boolFibra)
            {
                solucao += $"FIBRA {dosagemFibra}";
            }
            else if (boolTelaSuperior)
            {
                solucao += telaSuperior;
                if (boolTelaInferior)
                {
                    solucao += $"/{telaInferior}";
                }
            }
            else if (boolTelaInferior)
            {
                solucao += telaInferior;
            }
            else
            {

            }

            if (boolReforcoTelaSuperior)
            {
                reforcoSuperior = $" - REF_{finalidadeReforcoTelaSuperior}";
                solucaoReforcoSuperior = $" ({reforcoTelaSuperior})";
                if (boolReforcoTelaInferior)
                {
                    reforcoInferior = $" + REF_{finalidadeReforcoTelaInferior}";
                    solucaoReforcoInferior = $" ({reforcoTelaInferior})";
                }
            }
            else if (boolReforcoTelaInferior)
            {
                reforcoInferior = $" - REF_{finalidadeReforcoTelaInferior}";
                solucaoReforcoInferior = $" ({reforcoTelaInferior})";
            }
            TipoDePiso = $"{ambiente} - H={HConcreto}cm ({solucao}){reforcoSuperior}{solucaoReforcoSuperior}{reforcoInferior}{solucaoReforcoInferior}";
        }

        #region Visibilidades
        private bool parametersScrollVisible = true;

        public bool ParametersScrollVisible
        {
            get { return parametersScrollVisible; }
            set
            {
                parametersScrollVisible = value;
                OnPropertyChanged();
            }
        }

        private Thickness structuralSolutionBorderThickness = new Thickness(0);

        public Thickness StructuralSolutionBorderThickness
        {
            get { return structuralSolutionBorderThickness; }
            set
            {
                structuralSolutionBorderThickness = value;
                OnPropertyChanged();
            }
        }

        private bool camadaAsfaltoVisible = true;

        public bool CamadaAsfaltoVisible
        {
            get { return camadaAsfaltoVisible; }
            set
            {
                camadaAsfaltoVisible = value;
                OnPropertyChanged();
            }
        }

        private bool camadaIntertravadoVisible = true;

        public bool CamadaIntertravadoVisible
        {
            get { return camadaIntertravadoVisible; }
            set
            {
                camadaIntertravadoVisible = value;
                OnPropertyChanged();
            }
        }

        private bool camadaConcretoVisible = true;

        public bool CamadaConcretoVisible
        {
            get { return camadaConcretoVisible; }
            set
            {
                camadaConcretoVisible = value;
                OnPropertyChanged();
            }
        }

        private bool tipoConcretoTelaVisible = true;

        public bool TipoConcretoTelaVisible
        {
            get { return tipoConcretoTelaVisible; }
            set
            {
                tipoConcretoTelaVisible = value;
                OnPropertyChanged();
            }
        }

        private bool tipoConcretoFibraVisible = true;

        public bool TipoConcretoFibraVisible
        {
            get { return tipoConcretoFibraVisible; }
            set
            {
                tipoConcretoFibraVisible = value;
                OnPropertyChanged();
            }
        }

        private bool espacadorInferiorVisible = true;

        public bool EspacadorInferiorVisible
        {
            get { return espacadorInferiorVisible; }
            set
            {
                espacadorInferiorVisible = value;
                OnPropertyChanged();
            }
        }

        private bool telaSuperiorIsEnable = true;

        public bool TelaSuperiorIsEnable
        {
            get { return telaSuperiorIsEnable; }
            set
            {
                telaSuperiorIsEnable = value;
                OnPropertyChanged();
            }
        }

        private bool dimensoesIsVisible = true;

        public bool DimensoesIsVisible
        {
            get { return dimensoesIsVisible; }
            set
            {
                dimensoesIsVisible = value;
                OnPropertyChanged();
            }
        }
        private bool espacadorSoldadoIsVisible = true;

        public bool EspacadorSoldadoIsVisible
        {
            get { return espacadorSoldadoIsVisible; }
            set
            {
                espacadorSoldadoIsVisible = value;
                OnPropertyChanged();
            }
        }
        private bool telaSuperiorIsVisible = true;

        public bool TelaSuperiorIsVisible
        {
            get { return telaSuperiorIsVisible; }
            set
            {
                telaSuperiorIsVisible = value;
                OnPropertyChanged();
            }
        }
        private bool telaInferiorIsVisible = true;

        public bool TelaInferiorIsVisible
        {
            get { return telaInferiorIsVisible; }
            set
            {
                telaInferiorIsVisible = value;
                OnPropertyChanged();
            }
        }
        private bool fibraIsVisible = true;

        public bool FibraIsVisible
        {
            get { return fibraIsVisible; }
            set
            {
                fibraIsVisible = value;
                OnPropertyChanged();
            }
        }

        private bool tratamentoIsVisible = true;

        public bool TratamentoIsVisible
        {
            get { return tratamentoIsVisible; }
            set
            {
                tratamentoIsVisible = value;
                OnPropertyChanged();
            }
        }

        private bool espacadoresIsVisible = true;

        public bool EspacadoresIsVisible
        {
            get { return espacadoresIsVisible; }
            set
            {
                espacadoresIsVisible = value;
                OnPropertyChanged();
            }
        }

        private bool reforcoIsVisible = true;

        public bool ReforcoIsVisible
        {
            get { return reforcoIsVisible; }
            set
            {
                reforcoIsVisible = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Visibilidade de colunas

        private bool isColumn0Visible = true;

        public bool IsColumn0Visible
        {
            get { return isColumn0Visible; }
            set
            {
                isColumn0Visible = value;
                OnPropertyChanged();
            }
        }
        private bool isColumn1Visible = true;

        public bool IsColumn1Visible
        {
            get { return isColumn1Visible; }
            set
            {
                isColumn1Visible = value;
                OnPropertyChanged();
            }
        }
        private bool isColumn2Visible = true;

        public bool IsColumn2Visible
        {
            get { return isColumn2Visible; }
            set
            {
                isColumn2Visible = value;
                OnPropertyChanged();
            }
        }
        private bool isColumn3Visible = true;

        public bool IsColumn3Visible
        {
            get { return isColumn3Visible; }
            set
            {
                isColumn3Visible = value;
                OnPropertyChanged();
            }
        }
        private bool isColumn4Visible = true;

        public bool IsColumn4Visible
        {
            get { return isColumn4Visible; }
            set
            {
                isColumn4Visible = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Parametros de ação

        private ElementId id = new ElementId(-1);

        public ElementId Id
        {
            get { return id; }
            set { id = value; }
        }

        private Action action;

        public Action Action
        {
            get { return action; }
            set { action = value; }
        }


        #endregion

        #region Parametros KeySchedule Piso

        #region Identidade do Piso

        private string tipoDeSolucao = "";

        public string TipoDeSolucao
        {
            get { return tipoDeSolucao; }
            set
            {
                tipoDeSolucao = value;
                OnPropertyChanged();
            }
        }

        private string tipoDePiso;

        public string TipoDePiso
        {
            get { return tipoDePiso; }
            set
            {
                tipoDePiso = value;
                OnPropertyChanged();
            }
        }

        private string ambiente;

        public string Ambiente
        {
            get { return ambiente; }
            set
            {
                ambiente = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double hConcreto;

        public double HConcreto
        {
            get { return hConcreto; }
            set
            {
                hConcreto = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double comprimentoDaPlaca;

        public double ComprimentoDaPlaca
        {
            get { return comprimentoDaPlaca; }
            set
            {
                comprimentoDaPlaca = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double larguraDaPlaca;

        public double LarguraDaPlaca
        {
            get { return larguraDaPlaca; }
            set
            {
                larguraDaPlaca = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private bool boolReforcoDeTela;

        public bool BoolReforcoDeTela
        {
            get { return boolReforcoDeTela; }
            set
            {
                boolReforcoDeTela = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region H Espaçador Soldado
        private int hEspacadorSoldado;

        public int HEspacadorSoldado
        {
            get { return hEspacadorSoldado; }
            set
            {
                hEspacadorSoldado = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }
        #endregion

        #region Tela Superior

        private bool boolTelaSuperior;

        public bool BoolTelaSuperior
        {
            get { return boolTelaSuperior; }
            set
            {
                boolTelaSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string telaSuperior;

        public string TelaSuperior
        {
            get { return telaSuperior; }
            set
            {
                telaSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string finalidadeTelaSuperior;

        public string FinalidadeTelaSuperior
        {
            get { return finalidadeTelaSuperior; }
            set
            {
                finalidadeTelaSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double emendaTelaSuperior;

        public double EmendaTelaSuperior
        {
            get { return emendaTelaSuperior; }
            set
            {
                emendaTelaSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region Tela Inferior

        private bool boolTelaInferior;
        public bool BoolTelaInferior
        {
            get { return boolTelaInferior; }
            set
            {
                boolTelaInferior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string telaInferior;

        public string TelaInferior
        {
            get { return telaInferior; }
            set
            {
                telaInferior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double emendaTelaInferior;

        public double EmendaTelaInferior
        {
            get { return emendaTelaInferior; }
            set
            {
                emendaTelaInferior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region Fibra

        private bool boolFibra;
        public bool BoolFibra
        {
            get { return boolFibra; }
            set
            {
                boolFibra = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string fibra;

        public string Fibra
        {
            get { return fibra; }
            set
            {
                fibra = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double dosagemFibra;

        public double DosagemFibra
        {
            get { return dosagemFibra; }
            set
            {
                dosagemFibra = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string fR1;

        public string FR1
        {
            get { return fR1; }
            set
            {
                fR1 = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string fR4;

        public string FR4
        {
            get { return fR4; }
            set
            {
                fR4 = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region Tratamento

        private bool boolTratamentoSuperficial;
        public bool BoolTratamentoSuperficial
        {
            get { return boolTratamentoSuperficial; }
            set
            {
                boolTratamentoSuperficial = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string tratamentoSuperficial;

        public string TratamentoSuperficial
        {
            get { return tratamentoSuperficial; }
            set
            {
                tratamentoSuperficial = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region Espaçadores para Telas

        private bool boolEspacadorSuperior;
        public bool BoolEspacadorSuperior
        {
            get { return boolEspacadorSuperior; }
            set
            {
                boolEspacadorSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private int hEspacadorSuperior;
        public int HEspacadorSuperior
        {
            get { return hEspacadorSuperior; }
            set
            {
                hEspacadorSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private bool boolEspacadorInferior;
        public bool BoolEspacadorInferior
        {
            get { return boolEspacadorInferior; }
            set
            {
                boolEspacadorInferior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private int hEspacadorInferior;
        public int HEspacadorInferior
        {
            get { return hEspacadorInferior; }
            set
            {
                hEspacadorInferior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region Reforço de Tela

        private bool boolReforcoTelaSuperior;
        public bool BoolReforcoTelaSuperior
        {
            get { return boolReforcoTelaSuperior; }
            set
            {
                boolReforcoTelaSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string reforcoTelaSuperior;

        public string ReforcoTelaSuperior
        {
            get { return reforcoTelaSuperior; }
            set
            {
                reforcoTelaSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double emendaReforcoTelaSuperior;

        public double EmendaReforcoTelaSuperior
        {
            get { return emendaReforcoTelaSuperior; }
            set
            {
                emendaReforcoTelaSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string finalidadeReforcoTelaSuperior;

        public string FinalidadeReforcoTelaSuperior
        {
            get { return finalidadeReforcoTelaSuperior; }
            set
            {
                finalidadeReforcoTelaSuperior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private bool boolReforcoTelaInferior;
        public bool BoolReforcoTelaInferior
        {
            get { return boolReforcoTelaInferior; }
            set
            {
                boolReforcoTelaInferior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string reforcoTelaInferior;

        public string ReforcoTelaInferior
        {
            get { return reforcoTelaInferior; }
            set
            {
                reforcoTelaInferior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double emendaReforcoTelaInferior;

        public double EmendaReforcoTelaInferior
        {
            get { return emendaReforcoTelaInferior; }
            set
            {
                emendaReforcoTelaInferior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string finalidadeReforcoTelaInferior;

        public string FinalidadeReforcoTelaInferior
        {
            get { return finalidadeReforcoTelaInferior; }
            set
            {
                finalidadeReforcoTelaInferior = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region LPE_VEÍCULOS PESADOS

        private bool boolLPEVeiculosPesados;
        public bool BoolLPEVeiculosPesados
        {
            get { return boolLPEVeiculosPesados; }
            set
            {
                boolLPEVeiculosPesados = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #endregion

        #region Parametros KeySchedule Itens De Detalhe

        #region Camada1
        //private bool cBConcreto;

        //public bool CBConcreto
        //{
        //    get { return cBConcreto; }
        //    set
        //    {
        //        cBConcreto = value;
        //        OnPropertyChanged(); SetTipoDePiso();
        //    }
        //}

        //private bool cBFibra;

        //public bool CBFibra
        //{
        //    get { return cBFibra; }
        //    set
        //    {
        //        cBFibra = value;
        //        OnPropertyChanged(); SetTipoDePiso();
        //    }
        //}

        private string tagConcreto;

        public string TagConcreto
        {
            get { return tagConcreto; }
            set
            {
                tagConcreto = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region Camada2

        private bool cBSubBase;

        public bool CBSubBase
        {
            get { return cBSubBase; }
            set
            {
                cBSubBase = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double hSubBase;

        public double HSubBase
        {
            get { return hSubBase; }
            set
            {
                hSubBase = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string tagSubBase;

        public string TagSubBase
        {
            get { return tagSubBase; }
            set
            {
                tagSubBase = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private bool cBBaseGenerica;

        public bool CBBaseGenerica
        {
            get { return cBBaseGenerica; }
            set
            {
                cBBaseGenerica = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string tagBaseGenerica;

        public string TagBaseGenerica
        {
            get { return tagBaseGenerica; }
            set
            {
                tagBaseGenerica = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region Camada3


        private bool cBRefSubleito;

        public bool CBRefSubleito
        {
            get { return cBRefSubleito; }
            set
            {
                cBRefSubleito = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double hRefSubleito;

        public double HRefSubleito
        {
            get { return hRefSubleito; }
            set
            {
                hRefSubleito = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string tagRefSubleito;

        public string TagRefSubleito
        {
            get { return tagRefSubleito; }
            set
            {
                tagRefSubleito = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region Camada4


        private bool cBSubleito;

        public bool CBSubleito
        {
            get { return cBSubleito; }
            set
            {
                cBSubleito = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private string tagSubleito;

        public string TagSubleito
        {
            get { return tagSubleito; }
            set
            {
                tagSubleito = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #region Cargas


        private string lPECarga;

        public string LPECarga
        {
            get { return lPECarga; }
            set
            {
                lPECarga = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double lPECargaParam1;

        public double LPECargaParam1
        {
            get { return lPECargaParam1; }
            set
            {
                lPECargaParam1 = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        private double lPECargaParam2;

        public double LPECargaParam2
        {
            get { return lPECargaParam2; }
            set
            {
                lPECargaParam2 = value;
                OnPropertyChanged(); SetTipoDePiso();
            }
        }

        #endregion

        #endregion

        public FullAmbienteViewModel(Element tipoDePisoElement, Element itensDeDetalheElement)
        {
            if (tipoDePisoElement != null)
            {

                Id = tipoDePisoElement.Id;
                Action = Action.Continue;
                Ambiente = tipoDePisoElement.LookupParameter("Ambiente").AsString();
                BoolEspacadorInferior = tipoDePisoElement.LookupParameter("(s/n) Espaçador Inf.").AsInteger() == 1;
                BoolEspacadorSuperior = tipoDePisoElement.LookupParameter("(s/n) Espaçador Sup.").AsInteger() == 1;
                BoolFibra = tipoDePisoElement.LookupParameter("(s/n) Fibra").AsInteger() == 1;
                BoolLPEVeiculosPesados = tipoDePisoElement.LookupParameter("LPE_VEÍCULOS PESADOS").AsInteger() == 1;
                BoolReforcoTelaInferior = tipoDePisoElement.LookupParameter("(s/n) Ref. Tela Inferior").AsInteger() == 1;
                BoolReforcoTelaSuperior = tipoDePisoElement.LookupParameter("(s/n) Ref. Tela Superior").AsInteger() == 1;
                BoolTelaInferior = tipoDePisoElement.LookupParameter("(s/n) Tela Inferior").AsInteger() == 1;
                BoolTelaSuperior = tipoDePisoElement.LookupParameter("(s/n) Tela Superior").AsInteger() == 1;
                BoolTratamentoSuperficial = tipoDePisoElement.LookupParameter("(s/n) Tratamento Superficial").AsInteger() == 1;
                ComprimentoDaPlaca = tipoDePisoElement.LookupParameter("Comprimento Placa").AsDouble();
                DosagemFibra = tipoDePisoElement.LookupParameter("Dosagem da Fibra (kg/m³)").AsDouble();
                EmendaReforcoTelaInferior = tipoDePisoElement.LookupParameter("Emenda - Ref. Tela Inf").AsDouble();
                EmendaReforcoTelaSuperior = tipoDePisoElement.LookupParameter("Emenda - Ref. Tela Sup").AsDouble();
                EmendaTelaInferior = tipoDePisoElement.LookupParameter("Emenda - Tela Inferior (\"0,xx\")").AsDouble();
                EmendaTelaSuperior = tipoDePisoElement.LookupParameter("Emenda - Tela Superior (\"0,xx\")").AsDouble();
                Fibra = tipoDePisoElement.LookupParameter("Fibra").AsString();
                try
                {
                    BoolReforcoDeTela = tipoDePisoElement.LookupParameter("Reforço de Tela").AsInteger() == 1;
                    FinalidadeReforcoTelaInferior = tipoDePisoElement.LookupParameter("Finalidade Ref. Tela Inf").AsString();
                    FinalidadeReforcoTelaSuperior = tipoDePisoElement.LookupParameter("Finalidade Ref. Tela Sup").AsString();
                    FinalidadeTelaSuperior = tipoDePisoElement.LookupParameter("Finalidade - Tela Superior").AsString();
                }
                catch (Exception)
                {
                }
                FR1 = tipoDePisoElement.LookupParameter("FR1").AsString();
                FR4 = tipoDePisoElement.LookupParameter("FR4").AsString();
                HConcreto = UnitUtils.ConvertFromInternalUnits(tipoDePisoElement.LookupParameter("H Concreto").AsDouble(), UnitTypeId.Centimeters);
                HEspacadorInferior = tipoDePisoElement.LookupParameter("H-Espaçador Inferior (cm)").AsInteger();
                HEspacadorSoldado = tipoDePisoElement.LookupParameter("H-Espaçador Soldado (cm)").AsInteger();
                HEspacadorSuperior = tipoDePisoElement.LookupParameter("H-Espaçador Superior (cm)").AsInteger();
                LarguraDaPlaca = tipoDePisoElement.LookupParameter("Largura da Placa").AsDouble();
                ReforcoTelaInferior = tipoDePisoElement.LookupParameter("Ref. Tela Inferior").AsString();
                ReforcoTelaSuperior = tipoDePisoElement.LookupParameter("Ref. Tela Superior").AsString();
                TelaInferior = tipoDePisoElement.LookupParameter("Tela Inferior").AsString();
                TelaSuperior = tipoDePisoElement.LookupParameter("Tela Superior").AsString();
                TratamentoSuperficial = tipoDePisoElement.LookupParameter("Tratamento Superficial").AsString();
                TipoDePiso = tipoDePisoElement.Name;

                if (itensDeDetalheElement != null)
                {
                    CBBaseGenerica = itensDeDetalheElement.LookupParameter("CB_BASE GENÉRICA").AsInteger() == 1;
                    //CBConcreto = itensDeDetalheElement.LookupParameter("CB_CONCRETO").AsInteger() == 1;
                    //CBFibra = itensDeDetalheElement.LookupParameter("CB_FIBRA").AsInteger() == 1;
                    CBRefSubleito = itensDeDetalheElement.LookupParameter("CB_REF. SUBLEITO").AsInteger() == 1;
                    CBSubBase = itensDeDetalheElement.LookupParameter("CB_SUB_BASE").AsInteger() == 1;
                    CBSubleito = itensDeDetalheElement.LookupParameter("CB_SUBLEITO").AsInteger() == 1;
                    try
                    {
                        HRefSubleito = UnitUtils.ConvertFromInternalUnits(itensDeDetalheElement.LookupParameter("H Ref. Subleito").AsDouble(), UnitTypeId.Centimeters);
                    }
                    catch (Exception)
                    {
                    }
                    HSubBase = UnitUtils.ConvertFromInternalUnits(itensDeDetalheElement.LookupParameter("H SUB_BASE").AsDouble(), UnitTypeId.Centimeters);
                    LPECarga = itensDeDetalheElement.LookupParameter("LPE_CARGA").AsString();
                    LPECargaParam1 = 0;
                    LPECargaParam2 = 0;
                    TagBaseGenerica = itensDeDetalheElement.LookupParameter("TAG_BASE GENÉRICA").AsString();
                    TagConcreto = itensDeDetalheElement.LookupParameter("TAG_CONCRETO").AsString();
                    TagRefSubleito = itensDeDetalheElement.LookupParameter("TAG_REF. SUBLEITO").AsString();
                    TagSubBase = itensDeDetalheElement.LookupParameter("TAG_SUB-BASE").AsString();
                    TagSubleito = itensDeDetalheElement.LookupParameter("TAG_SUBLEITO").AsString();
                }
            }

        }

        public object Clone()
        {
            return (FullAmbienteViewModel)this.MemberwiseClone();
            //return new FullAmbienteViewModel
            //{
            //    Ambiente = this.Ambiente,
            //    BoolEspacadorInferior = this.BoolEspacadorInferior,
            //    BoolEspacadorSuperior = this.BoolEspacadorSuperior,
            //    BoolFibra = this.BoolFibra ,
            //    BoolLPEVeiculosPesados = this.BoolLPEVeiculosPesados,
            //    BoolReforcoTelaInferior = this.BoolReforcoTelaInferior,
            //    BoolReforcoTelaSuperior = this.BoolEspacadorSuperior,
            //    BoolTelaInferrior = this.BoolTelaInferrior,
            //    BoolTelaSuperior = this.BoolTelaSuperior,
            //    BoolTratamentoSuperficial = this.,
            //    ComprimentoDaPlaca = ,
            //    DosagemFibra = ,
            //    EmendaReforcoTelaInferior = ,
            //    EmendaReforcoTelaSuperior = ,
            //    EmendaTelaInferior = ,
            //    EmendaTelaSuperior = ,
            //    Fibra = ,
            //    FinalidadeReforcoTelaInferior = ,
            //    FinalidadeReforcoTelaSuperior = ,
            //    FinalidadeTelaSuperior = tipoDePisoElement.LookupParameter("Finalidade - Tela Superior").AsString(),
            //    FR1 = tipoDePisoElement.LookupParameter("FR1").AsString(),
            //    FR4 = tipoDePisoElement.LookupParameter("FR4").AsString(),
            //    HConcreto = UnitUtils.ConvertFromInternalUnits(tipoDePisoElement.LookupParameter("H Concreto").AsDouble(), UnitTypeId.Centimeters),
            //    HEspacadorInferior = tipoDePisoElement.LookupParameter("H-Espaçador Inferior (cm)").AsInteger(),
            //    HEspacadorSoldado = tipoDePisoElement.LookupParameter("H-Espaçador Soldado (cm)").AsInteger(),
            //    HEspacadorSuperior = tipoDePisoElement.LookupParameter("H-Espaçador Superior (cm)").AsInteger(),
            //    LarguraDaPlaca = tipoDePisoElement.LookupParameter("Largura da Placa").AsDouble(),
            //    ReforcoTelaInferior = tipoDePisoElement.LookupParameter("Ref. Tela Inferior").AsString(),
            //    ReforcoTelaSuperior = tipoDePisoElement.LookupParameter("Ref. Tela Superior").AsString(),
            //    TelaInferior = tipoDePisoElement.LookupParameter("Tela Inferior").AsString(),
            //    TelaSuperior = tipoDePisoElement.LookupParameter("Tela Superior").AsString(),
            //    TratamentoSuperficial = tipoDePisoElement.LookupParameter("Tratamento Superficial").AsString(),
            //    TipoDePiso = tipoDePisoElement.Name,
            //                CBBaseGenerica = itensDeDetalheElement.LookupParameter("CB_BASE GENÉRICA").AsInteger() == 1,
            //    CBRefSubleito = itensDeDetalheElement.LookupParameter("CB_REF. SUBLEITO").AsInteger() == 1,
            //    CBSubBase = itensDeDetalheElement.LookupParameter("CB_SUB_BASE").AsInteger() == 1,
            //    CBSubleito = itensDeDetalheElement.LookupParameter("CB_SUBLEITO").AsInteger() == 1,
            //    HRefSubleito = itensDeDetalheElement.LookupParameter("H SUB_BASE").AsDouble(),
            //    HSubBase = itensDeDetalheElement.LookupParameter("H SUB_BASE").AsDouble(),
            //    LPECarga = itensDeDetalheElement.LookupParameter("LPE_CARGA").AsString(),
            //    LPECargaParam1 = 0,
            //    LPECargaParam2 = 0,
            //    TagBaseGenerica = itensDeDetalheElement.LookupParameter("TAG_BASE GENÉRICA").AsString(),
            //    TagConcreto = itensDeDetalheElement.LookupParameter("TAG_CONCRETO").AsString(),
            //    TagRefSubleito = itensDeDetalheElement.LookupParameter("TAG_REF. SUBLEITO").AsString(),
            //    TagSubBase = itensDeDetalheElement.LookupParameter("TAG_SUB-BASE").AsString(),
            //    TagSubleito = itensDeDetalheElement.LookupParameter("TAG_SUBLEITO").AsString(),
            //}
        }
    }
}
