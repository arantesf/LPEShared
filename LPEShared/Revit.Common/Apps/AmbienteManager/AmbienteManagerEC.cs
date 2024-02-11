using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB.Events;
using System.Reflection;
using System.IO;
using System.Runtime;
using Autodesk.Revit.DB.Architecture;
using Microsoft.SqlServer.Server;
using System.Xml.Linq;
using System.Diagnostics.Eventing.Reader;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class AmbienteManagerEC : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            List<FullAmbienteViewModel> fullAmbienteViewModels = AmbienteManagerUtils.GetAmbientes(doc);
            Dictionary<MaterialClass, List<string>> materialsByClass = AmbienteManagerUtils.GetMaterialsByClass(doc);
            Dictionary<FloorMatrizClass, List<FloorMatriz>> floorMatrizes = AmbienteManagerUtils.GetFloorMatrizes(doc, materialsByClass);
            List<string> allMaterialNames = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Materials)
                .Select(x => x.Name)
                .OrderBy(x => x)
                .ToList();
            ExternalApplication.LPEApp.uiApp = uiapp;
            ExternalApplication.LPEApp.ShowAmbienteManagerUI(floorMatrizes, fullAmbienteViewModels, allMaterialNames, materialsByClass);

            return Result.Succeeded;
        }
    }
}
