using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Revit.Common
{ 
    public class SelectFloorsEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            List<string> selectedAmbientes = new List<string>();
            List<ElementId> selectedFloors = new List<ElementId>();
            foreach (var reference in uidoc.Selection.PickObjects(ObjectType.Element, new FloorSelectionFilter(), "Selecione os pisos que deseja dividir"))
            {
                selectedFloors.Add(doc.GetElement(reference).Id);
                selectedAmbientes.Add(doc.GetElement(reference).LookupParameter("Ambiente").AsString());
            }
            selectedAmbientes = selectedAmbientes.Distinct().ToList();
            foreach (var vm in SelectAmbienteMVVM.MainView.AmbienteViewModels)
            {
                if (selectedAmbientes.Contains(vm.Name))
                {
                    vm.IsChecked = true;
                }
                else
                {
                    vm.IsChecked = false;
                }
            }
            SelectAmbienteMVVM.MainView.SelectedFloorsIds = selectedFloors;
            SelectAmbienteMVVM.MainView.Ambientes_ListBox.IsEnabled = false;
            SelectAmbienteMVVM.MainView.SelectAll_CheckBox.IsEnabled = false;
            SelectAmbienteMVVM.MainView.WindowState = WindowState.Normal;

            //return Result.Succeeded;

        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }

}
