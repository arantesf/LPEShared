using System;
using System.Collections.Generic;
using System.Text;

namespace Revit.Common
{
    public class FibraViewModel : ViewModelBase
    {
        public string Name { get; set; }
        public string Fibra { get; set; }
        public double Dosagem { get; set; }
        public string FR1 { get; set; }
        public string FR4 { get; set; }
        public FibraViewModel(string fibra, double dosagem, string fr1, string fr4)
        {
            Name = $"{fibra} - {dosagem} kg/m³";
            Fibra = fibra;
            Dosagem = dosagem;
            FR1 = fr1;
            FR4 = fr4;
        }
    }
}
