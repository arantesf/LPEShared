using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Revit.Common
{
    public class KeyScheduleElementViewModel : ViewModelBase
    {
        private Element element;

        public Element Element
        {
            get { return element; }
            set { element = value; OnPropertyChanged(); }
        }

        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged(); }
        }

        private ElementId id;

        public ElementId Id
        {
            get { return id; }
            set { id = value; OnPropertyChanged(); }
        }

        public KeyScheduleElementViewModel(Element element)
        {
            if (element == null)
            {
                Element = null;
                Name = "";
                Id = null;
            }
            else
            {
                Element = element;
                Name = element.Name;
                Id = element.Id;
            }
        }
    }
}
