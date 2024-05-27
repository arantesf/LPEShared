// Decompiled with JetBrains decompiler
// Type: Revit.Common.LayerViewModel
// Assembly: LPE, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E06EE1B-06C5-4A71-9629-ACF2EB60ECA2
// Assembly location: C:\ProgramData\Autodesk\Revit\Addins\2024\LPE\LPE.dll

using System;
using System.Collections.Generic;

namespace Revit.Common
{
    public class LayerViewModel : ViewModelBase, ICloneable
    {
        private string tag;
        private double width;
        private List<string> possibleMaterials;
        private string selectedMaterialName;

        public string Tag
        {
            get => this.tag;
            set => this.tag = value;
        }

        public double Width
        {
            get => this.width;
            set => this.width = value;
        }

        public string SelectedCamadaTipo { get; set; }

        public List<string> CamadaTipos { get; set; } = new List<string>()
    {
      "",
      "Base",
      //"Base Genérica",
      "Bloco Intertravado",
      "Camada de Assentamento",
      "Camada de Isolamento",
      "Camada de Ventilação",
      "Concreto",
      "Concreto Asfáltico",
      "Filme Plastico",
      "Imprimacao Asfáltica",
      "Pintura de Ligação",
      "Reforço de Subleito",
      "Sub-Base"
    };

        public List<string> PossibleMaterials
        {
            get => this.possibleMaterials;
            set
            {
                this.possibleMaterials = value;
                this.OnPropertyChanged(nameof(PossibleMaterials));
            }
        }

        public string SelectedMaterial
        {
            get => this.selectedMaterialName;
            set
            {
                this.selectedMaterialName = value;
                this.OnPropertyChanged(nameof(SelectedMaterial));
            }
        }

        public bool IsEnabled { get; set; }

        public LayerViewModel(
          string tipo,
          string materialName,
          double width,
          List<string> possibleMaterials,
          bool isEnabled)
        {
            if (this.CamadaTipos.Contains(tipo))
                this.SelectedCamadaTipo = tipo;
            this.PossibleMaterials = possibleMaterials;
            this.SelectedMaterial = materialName;
            this.Width = width;
            this.IsEnabled = isEnabled;
        }

        public LayerViewModel(
          string tipo,
          string materialName,
          double width,
          List<string> possibleMaterials,
          bool isEnabled,
          string tag)
        {
            if (this.CamadaTipos.Contains(tipo))
                this.SelectedCamadaTipo = tipo;
            this.PossibleMaterials = possibleMaterials;
            this.SelectedMaterial = materialName;
            this.Width = width;
            this.IsEnabled = isEnabled;
            this.Tag = tag;
        }

        public object Clone() => (object)(this.MemberwiseClone() as LayerViewModel);
    }
}
