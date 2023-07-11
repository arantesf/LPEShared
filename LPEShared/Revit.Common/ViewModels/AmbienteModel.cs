using System;
using System.Collections.Generic;
using System.Text;

namespace Revit.Common
{
    class AmbienteModel : ViewModelBase
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public AmbienteModel(string name)
        {
            Name = name;
            IsChecked = true;
        }
    }
}
