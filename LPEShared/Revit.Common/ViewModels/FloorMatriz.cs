// Decompiled with JetBrains decompiler
// Type: Revit.Common.FloorMatriz
// Assembly: LPE, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E06EE1B-06C5-4A71-9629-ACF2EB60ECA2
// Assembly location: C:\ProgramData\Autodesk\Revit\Addins\2024\LPE\LPE.dll

using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Revit.Common
{
    public enum MaterialClass
    {
        Todos,
        Concreto,
        FilmePlastico,
        Base,
        SubBase,
        ReforcoSubleito,
        CamadaIsolamento,
        CamadaVentilacao,
        BlocoIntertravado,
        CamadaAssentamento,
        ConcretoAsfaltico,
        PinturaDeLigacao,
        ImprimacaoAsflatica,
    }

    public class FloorMatriz : ViewModelBase, ICloneable
    {
        private ObservableCollection<LayerViewModel> layers = new ObservableCollection<LayerViewModel>();

        public bool IsModelo { get; set; }

        public string FloorName { get; set; }

        public ObservableCollection<LayerViewModel> Layers
        {
            get => this.layers;
            set
            {
                this.layers = value;
                this.OnPropertyChanged(nameof(Layers));
            }
        }

        public object Clone()
        {
            FloorMatriz cloneFloorMatriz = (object)(FloorMatriz)this.MemberwiseClone() as FloorMatriz;
            cloneFloorMatriz.Layers = new ObservableCollection<LayerViewModel>();
            foreach (LayerViewModel layerViewModel in this.Layers)
            {
                cloneFloorMatriz.Layers.Add(new LayerViewModel(layerViewModel.SelectedCamadaTipo, layerViewModel.SelectedMaterial, layerViewModel.Width, layerViewModel.PossibleMaterials, layerViewModel.IsEnabled, layerViewModel.Tag));
            }
            return cloneFloorMatriz;
        } 


        public void GetFloorTypeData(
          FloorType floorType,
          Dictionary<MaterialClass, List<string>> materialsDict, FullAmbienteViewModel fullAmbienteViewModel = null)
        {
            this.FloorName = ((Element)floorType).Name;
            this.IsModelo = this.FloorName.Contains("0.MODELO");
            for (int index1 = 0; index1 < ((HostObjAttributes)floorType).GetCompoundStructure().GetLayers().Count; ++index1)
            {
                CompoundStructureLayer layer1 = ((HostObjAttributes)floorType).GetCompoundStructure().GetLayers()[index1];
                Element element = ((Element)floorType).Document.GetElement(layer1.MaterialId);
                List<string> source = new List<string>();
                string tipo;
                string tag = "";
                if (element.LookupParameter("LPE_MAT_BASE").AsInteger() == 1)
                {
                    bool flag = false;
                    int index2 = index1 + 1;
                    if (index2 < ((HostObjAttributes)floorType).GetCompoundStructure().GetLayers().Count)
                    {
                        CompoundStructureLayer layer2 = ((HostObjAttributes)floorType).GetCompoundStructure().GetLayers()[index2];
                        ((Element)floorType).Document.GetElement(layer2.MaterialId);
                        if (element.LookupParameter("LPE_MAT_BASE").AsInteger() == 1)
                            flag = true;
                    }
                    if (flag)
                    {
                        source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.Base]);
                        tipo = "Base";
                    }
                    else
                    {
                        source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.SubBase]);
                        tipo = "Sub-Base";
                        tag = fullAmbienteViewModel?.TagSubBase;
                    }
                }
                else if (element.LookupParameter("LPE_MAT_SUB-BASE").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.SubBase]);
                    tipo = "Sub-Base";
                    tag = fullAmbienteViewModel?.TagSubBase;
                }
                else if (element.LookupParameter("LPE_MAT_CONCRETO").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.Concreto]);
                    tipo = "Concreto";
                    tag = fullAmbienteViewModel?.TagConcreto;
                }
                else if (element.LookupParameter("LPE_MAT_FILME PLÁSTICO").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.FilmePlastico]);
                    tipo = "Filme Plastico";
                }
                else if (element.LookupParameter("LPE_MAT_REFORÇO DE SUBLEITO").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.ReforcoSubleito]);
                    tipo = "Reforço de Subleito";
                    tag = fullAmbienteViewModel?.TagRefSubleito;
                }
                else if (element.LookupParameter("LPE_MAT_CAMADA DE ISOLAMENTO").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.CamadaIsolamento]);
                    tipo = "Camada de Isolamento";
                }
                else if (element.LookupParameter("LPE_MAT_CAMADA DE VENTILAÇÃO").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.CamadaVentilacao]);
                    tipo = "Camada de Ventilação";
                }
                else if (element.LookupParameter("LPE_MAT_BLOCO INTERTRAVADO").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.BlocoIntertravado]);
                    tipo = "Bloco Intertravado";
                }
                else if (element.LookupParameter("LPE_MAT_CAMADA DE ASSENTAMENTO").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.CamadaAssentamento]);
                    tipo = "Camada de Assentamento";
                }
                else if (element.LookupParameter("LPE_MAT_CONCRETO ASFÁLTICO").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.ConcretoAsfaltico]);
                    tipo = "Concreto Asfáltico";
                    tag = fullAmbienteViewModel?.TagConcreto;
                }
                else if (element.LookupParameter("LPE_MAT_PINTURA DE LIGAÇÃO").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.PinturaDeLigacao]);
                    tipo = "Pintura de Ligação";
                }
                else if (element.LookupParameter("LPE_MAT_IMPRIMAÇÃO ASFÁLTICA").AsInteger() == 1)
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.ImprimacaoAsflatica]);
                    tipo = "Imprimacao Asfáltica";
                }
                else
                {
                    source.AddRange((IEnumerable<string>)materialsDict[MaterialClass.Todos]);
                    tipo = "";
                }
                List<string> list = source.Distinct<string>().ToList<string>();
                list.Sort();
                this.Layers.Add(new LayerViewModel(tipo, element.Name, UnitUtils.ConvertFromInternalUnits(layer1.Width, UnitTypeId.Centimeters), list, true, tag));
                if (((HostObjAttributes)floorType).GetCompoundStructure().GetLastCoreLayerIndex() == index1)
                    this.Layers.Add(new LayerViewModel("", "", 0.0, new List<string>(), false)
                    {
                        CamadaTipos = new List<string>()
                        {
                          "Core Boundary"
                        },
                        SelectedCamadaTipo = "Core Boundary"
                    });
            }
        }
    }
}
