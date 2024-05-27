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
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class DimensionFloorsEC : IExternalCommand
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
            Autodesk.Revit.DB.View initialView = uidoc.ActiveView;

            bool OK = true;
            string errors = "";
            if (!Utils.VerifyIfProjectParameterExists(doc, "Ambiente"))
            {
                errors += "\n- Ambiente";
                OK = false;
            }
            if (!Utils.VerifyIfProjectParameterExists(doc, "Refor�o de Tela"))
            {
                errors += "\n- Refor�o de Tela";
                OK = false;
            }
            if (!OK)
            {
                Autodesk.Revit.UI.TaskDialog.Show("ATEN��O!", $"N�o foi poss�vel executar o comando por n�o existir no modelo os seguintes par�metros:\n {errors}");
                return Result.Cancelled;
            }

            if (uidoc.ActiveView is View3D)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Aten��o!", "Abra uma vista de planta para rodar o plug-in!");
                return Result.Cancelled;
            }

            DimensionType cotaLPEType = new FilteredElementCollector(doc)
                   .OfClass(typeof(DimensionType))
                   .Where(a => a.Name == "Cota_LPE")
                   .Cast<DimensionType>()
                   .FirstOrDefault();

            if (cotaLPEType == null)
            {
                Autodesk.Revit.UI.TaskDialog.Show("ATEN��O!", $"N�o foi poss�vel executar o comando por n�o existir as seguintes fam�lias no modelo:\n Cota_LPE");
                return Result.Cancelled;
            }

            ExternalApplication.LPEApp.uiApp = uiapp;
            ExternalApplication.LPEApp.ShowDimensionFloorsUI();

            return Result.Succeeded;
        }
    }
}
