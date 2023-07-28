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
    public class SplitJointsEC : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            bool OK = true;
            string errors = "";
            if (!Utils.VerifyIfProjectParameterExists(doc, "Ambiente"))
            {
                errors += "\n- Ambiente";
                OK = false;
            }

            if (!OK)
            {
                TaskDialog.Show("ATEN��O!", $"N�o foi poss�vel executar o comando por n�o existir no modelo os seguintes par�metros:\n {errors}");
                return Result.Cancelled;
            }

            ExternalApplication.LPEApp.uiApp = uiapp;
            ExternalApplication.LPEApp.ShowSplitJointsUI();

            return Result.Succeeded;
        }
    }
}
