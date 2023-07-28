using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Revit.Common
{
    public class AmbienteViewModel : ViewModelBase
    {
        private string name;

        public string Name
        {
            get { return name; }
            set 
            { 
                name = value;
                OnPropertyChanged("Name");
            }
        }

        private bool isChecked;

        public bool IsChecked
        {
            get { return isChecked; }
            set 
            { 
                isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }


        public AmbienteViewModel(string name)
        {
            Name = name;
            IsChecked = false;
        }
    }
}
