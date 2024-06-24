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
using Revit.Common.Classes;
using Application = Autodesk.Revit.ApplicationServices.Application;

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

            GlobalVariables.MaterialsByClass = AmbienteManagerUtils.GetMaterialsByClass(doc);
            Dictionary<FloorMatrizClass, List<FloorMatriz>> floorMatrizes = AmbienteManagerUtils.GetFloorMatrizes(doc, GlobalVariables.MaterialsByClass);
            List<FibraViewModel> fibras = AmbienteManagerUtils.GetFibras(doc);
            List<TagViewModel> tags = AmbienteManagerUtils.GetTags(doc);
            List<PisoLegendaModel> legendas = AmbienteManagerUtils.GetLegendas(doc);
            List<double> emendas = AmbienteManagerUtils.GetEmendas(doc);
            List<int> telas = AmbienteManagerUtils.GetTelas(doc);
            List<string> tratamentos = AmbienteManagerUtils.GetTratamentoSuperficial(doc);
            GlobalVariables.StaticScheduleData = new StaticScheduleData(fibras, emendas, telas, tratamentos, tags, legendas);
            List<FullAmbienteViewModel> fullAmbienteViewModels = new List<FullAmbienteViewModel>();
            if (!AmbienteManagerUtils.GetAmbientes(doc, out fullAmbienteViewModels))
            {
                Autodesk.Revit.UI.TaskDialog.Show("ATENÇÃO!", "Verificar duplicidade de chaves nos Key Schedules");
                return Result.Failed;
            }
            List<string> allMaterialNames = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Materials)
                .Select(x => x.Name)
                .OrderBy(x => x)
                .ToList();
            ExternalApplication.LPEApp.uiApp = uiapp;
            ExternalApplication.LPEApp.ShowAmbienteManagerUI(floorMatrizes, fullAmbienteViewModels, allMaterialNames, GlobalVariables.MaterialsByClass);

            return Result.Succeeded;
        }
    }
}
