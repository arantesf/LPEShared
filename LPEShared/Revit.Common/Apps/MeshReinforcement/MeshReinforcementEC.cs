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
    public class MeshReinforcementEC : IExternalCommand
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

            if (uidoc.ActiveView is View3D)
            {
                TaskDialog.Show("Atenção!", "Abra uma vista de planta para rodar o plug-in!");
                return Result.Cancelled;
            }

            List<DimensionType> cotaLPETypeList = new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Where(a => a.Name == "Cota_LPE")
                .Cast<DimensionType>()
                .ToList();

            List<Element> tagOKList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MultiCategoryTags)
                .Where(a => a.Name == "Fator de Forma - Ok!")
                .ToList();

            List<Element> tagNaoList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MultiCategoryTags)
                .Where(a => a.Name == "Fator de Forma - Não!")
                .ToList();

            bool OK = true;
            string error = "";
            if (!Utils.VerifyIfProjectParameterExists(doc, "Ambiente"))
            {
                error += "\n- Ambiente";
                OK = false;
            }
            if (!Utils.VerifyIfProjectParameterExists(doc, "Piso em placas"))
            {
                error += "\n- Piso em placas";
                OK = false;
            }
            if (!OK)
            {
                error = $"Não foi possível executar o comando por não existir no modelo os seguintes parâmetros:\n {error}";
            }

            bool OKfamily = true;
            string errorFamily = "";
            if (!cotaLPETypeList.Any())
            {
                errorFamily += "\n- (Dimension) Cota_LPE";
                OKfamily = false;
            }
            if (!tagOKList.Any())
            {
                errorFamily += "\n- (Tag) Fator de Forma - OK!";
                OKfamily = false;
            }
            if (!tagNaoList.Any())
            {
                errorFamily += "\n- (Tag) Fator de Forma - Não!";
                OKfamily = false;
            }
            if (!OKfamily)
            {
                errorFamily = $"Não foi possível executar o comando por não existir no modelo as seguites famílias:\n {errorFamily}";
            }

            string errorFinal = "";
            if (!OK)
            {
                errorFinal = error;
            }
            if (!OKfamily)
            {
                errorFinal += $"\n\n {errorFamily}";
            }
            if (!OK || !OKfamily)
            {
                TaskDialog.Show("ATENÇÃO!", errorFinal);
                return Result.Cancelled;
            }

            ExternalApplication.LPEApp.uiApp = uiapp;
            ExternalApplication.LPEApp.ShowMeshReinforcementUI();

            return Result.Succeeded;
        }
    }
}
