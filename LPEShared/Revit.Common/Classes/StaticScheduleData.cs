using System;
using System.Collections.Generic;
using System.Text;

namespace Revit.Common
{
    public class StaticScheduleData
    {
        public List<FibraViewModel> Fibras {  get; set; }
        public List<double> Emendas { get; set; }
        public List<int> Telas { get; set; }
        public List<string> Tratamentos { get; set; }
        public List<TagViewModel> Tags { get; set; }
        public List<PisoLegendaModel> Legendas { get; set; }
        public StaticScheduleData(List<FibraViewModel> fibras, List<double> emendas, List<int> telas, List<string> tratamentos, List<TagViewModel> tags, List<PisoLegendaModel> legendas)
        {
            Fibras = fibras;
            Emendas = emendas;
            Telas = telas;
            Tratamentos = tratamentos;
            Tags = tags;
            Legendas = legendas;
        }
    }
}
