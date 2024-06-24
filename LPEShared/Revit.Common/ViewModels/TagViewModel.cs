using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Revit.Common
{
    public class TagViewModel : ViewModelBase
    {
        public string Title { get; set; }
        public string Tag { get; set; }
        public TagViewModel(string title, string tag)
        {
            Title = title;
            Tag = tag;
        }
    }

    public class PisoLegendaModel : ViewModelBase
    {
        public string Name { get; set; }
        public string HexColor { get; set; }
        public PisoLegendaModel(string name, string color)
        {
            Name = name;
            HexColor = color;
        }
    }
}
