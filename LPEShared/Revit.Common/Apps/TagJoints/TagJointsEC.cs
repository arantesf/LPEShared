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
    public class TagJointsEC : IExternalCommand
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
            string error = "";
            if (!Utils.VerifyIfProjectParameterExists(doc, "Ambiente"))
            {
                error += "\n- Ambiente";
                OK = false;
            }
            if (!OK)
            {
                TaskDialog.Show("ATENÇÃO!", $"Não foi possível executar o comando por não existir no modelo os seguintes parâmetros:\n {error}");
                return Result.Cancelled;
            }

            if (uidoc.ActiveView is View3D)
            {
                TaskDialog.Show("Atenção!", "Abra uma vista de planta para rodar o plug-in!");
                return Result.Cancelled;
            }

            ExternalApplication.LPEApp.uiApp = uiapp;
            ExternalApplication.LPEApp.ShowTagJointsUI();

            return Result.Succeeded;
        }
    }
}
