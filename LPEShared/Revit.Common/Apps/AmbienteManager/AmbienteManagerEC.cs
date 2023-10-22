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

            ExternalApplication.LPEApp.uiApp = uiapp;
            ExternalApplication.LPEApp.ShowAmbienteManagerUI(fullAmbienteViewModels);

            return Result.Succeeded;
        }
    }
}
