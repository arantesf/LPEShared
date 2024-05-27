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
using System.Threading.Tasks;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class MeshReinforcement : IExternalCommand
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
                Autodesk.Revit.UI.TaskDialog.Show("ATENÇÃO!", errorFinal);
                return Result.Cancelled;
            }
            DimensionType cotaLPEType = cotaLPETypeList.First();
            Element tagOK = tagOKList.First();
            Element tagNao = tagNaoList.First();

            Dictionary<string, List<Element>> ambientesWithReinforcements = new Dictionary<string, List<Element>>();

            var keyScheduleView = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules)
                .Where(view => view.Name == "LPE_TIPO DE PISO")
                .FirstOrDefault();

            List<Element> tiposDePiso = new FilteredElementCollector(doc, keyScheduleView.Id).ToList();

            List<string> ambienteFloorStrings = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                  .WhereElementIsNotElementType()
                  .OfCategory(BuiltInCategory.OST_Floors)
                  .Where(a => a.LookupParameter("Ambiente").HasValue)
                  .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                  .Select(a => a.First().LookupParameter("Ambiente").AsString())
                  .ToList();

            foreach (var tipoDePiso in tiposDePiso)
            {
                string ambiente = tipoDePiso.LookupParameter("Ambiente").AsString();

                if (ambienteFloorStrings.Contains(ambiente))
                {
                    if (!ambientesWithReinforcements.ContainsKey(ambiente))
                    {
                        ambientesWithReinforcements.Add(ambiente, new List<Element>());
                    }
                    bool telaSuperior = tipoDePiso.LookupParameter("(s/n) Tela Superior").AsInteger() == 1;
                    bool fibra = tipoDePiso.LookupParameter("(s/n) Fibra").AsInteger() == 1;
                    if (telaSuperior && fibra)
                    {
                        ambientesWithReinforcements[ambiente].Add(tipoDePiso);
                    }
                }
            }
            double fatorDeFormaLimite = 2;
            try
            {
                ElementId globalParameterId = GlobalParametersManager.FindByName(doc, "LPE_FATOR DE FORMA GLOBAL");
                GlobalParameter gParam = doc.GetElement(globalParameterId) as GlobalParameter;
                DoubleParameterValue doubleParameterValue = gParam.GetValue() as DoubleParameterValue;
                fatorDeFormaLimite = doubleParameterValue.Value;
            }
            catch (Exception)
            {

            }

            var window = new SelectAmbienteReinforcementMVVM(uidoc);
            window.ShowDialog();

            if (!window.Execute)
            {
                return Result.Cancelled;
            }

            TransactionGroup tg = new TransactionGroup(doc, "Reforçar com Tela");
            tg.Start();
            Autodesk.Revit.DB.View initialView = uidoc.ActiveView;
            Options options = new Options
            {
                View = initialView,
                ComputeReferences = true,
                IncludeNonVisibleObjects = true
            };

            double proportion = double.Parse(window.FatorDeFormaGlobal.Replace(".",","));


            foreach (var checkedAmbiente in window.AmbienteAndReinforcementViewModels.Where(x => x.SelectedReinforcement.Name != ""))
            {
                string ambiente = checkedAmbiente.Name;

                List<Element> floors = new FilteredElementCollector(doc, initialView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .Where(a => a.LookupParameter("Ambiente").AsString() == ambiente)
                    .ToList();

                ElementId reinforcementFloorTypeId = checkedAmbiente.SelectedReinforcement.Id;

                using (Transaction tx = new Transaction(doc, "Create Dimensions"))
                {
                    tx.Start();
                    doc.SetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType, cotaLPEType.Id);
                    tx.Commit();

                    List<Element> existingView = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfClass(typeof(Autodesk.Revit.DB.View))
                        .Where(a => a.Name == ambiente + " - FATOR DE FORMA")
                        .ToList();

                    if (existingView.Any())
                    {
                        tx.Start();
                        doc.Delete(existingView.First().Id);
                        tx.Commit();
                    }

                    tx.Start();
                    Autodesk.Revit.DB.View view = doc.GetElement(initialView.Duplicate(ViewDuplicateOption.Duplicate)) as Autodesk.Revit.DB.View;
                    view.Name = ambiente + " - FATOR DE FORMA";
                    tx.Commit();
                    uidoc.ActiveView = view;

                    ElementCategoryFilter dimFilter = new ElementCategoryFilter(BuiltInCategory.OST_Dimensions);
                    ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);
                    int count = 0;

                    Dictionary<ElementId, ElementId> floorsToTag = new Dictionary<ElementId, ElementId>();

                    foreach (var floor in floors)
                    {
                        count++;
                        if (floor.LookupParameter("(s/n) Fibra").AsInteger() == 0)
                        {
                            continue;
                        }

                        GeometryElement wholeGridGeometry = floor.get_Geometry(options);
                        List<Edge> allFloorEdges = new List<Edge>();
                        foreach (GeometryObject geomObj in wholeGridGeometry)
                        {
                            if (geomObj is Solid)
                            {
                                Solid solid = geomObj as Solid;
                                foreach (Edge edge in solid.Edges)
                                {
                                    if (edge.AsCurve() is Line)
                                    {
                                        allFloorEdges.Add(edge);
                                    }
                                }
                            }
                        }

                        if (allFloorEdges.Count == 0)
                        {
                            continue;
                        }

                        // GET ONLY EDGES THAT REFERENCES BOUNDARIES

                        List<Edge> floorEdges = new List<Edge>();
                        List<Curve> outsideCurves = floor.GetDependentElements(linesFilter).Select(a => (doc.GetElement(a) as ModelCurve).GeometryCurve).ToList();
                        List<ModelCurve> floorModelCurves = floor.GetDependentElements(linesFilter).Select(a => (doc.GetElement(a) as ModelCurve)).ToList();
                        List<Curve> boundaryCurves = new List<Curve>();
                        foreach (var curve in floorModelCurves)
                        {
                            foreach (Parameter param in curve.Parameters)
                            {
                                if ((param.Definition as InternalDefinition).BuiltInParameter == BuiltInParameter.ANALYTICAL_MODEL_SKETCH_ALIGNMENT_METHOD)
                                {
                                    boundaryCurves.Add(curve.GeometryCurve);
                                    break;
                                }
                            }
                        }

                        boundaryCurves.AddRange(Utils.GetFloorOpeningsCurves(floor as Floor));

                        foreach (var edge in allFloorEdges)
                        {
                            foreach (var modelCurve in outsideCurves)
                            {
                                try
                                {
                                    if (edge.AsCurve() is Line)
                                    {
                                        if ((edge.AsCurve() as Line).Direction.IsAlmostEqualTo(XYZ.BasisZ) || (edge.AsCurve() as Line).Direction.IsAlmostEqualTo(-XYZ.BasisZ))
                                        {
                                            continue;
                                        }
                                    }
                                    if (Utils.IsCurvesEqual(Utils.GetCurveProjection(edge.AsCurve()), Utils.GetCurveProjection(modelCurve)))
                                    {
                                        floorEdges.Add(edge);
                                    }
                                }
                                catch (Exception)
                                {

                                }

                            }
                        }

                        floorEdges = floorEdges.OrderBy(a => a.AsCurve().Length).ToList();
                        XYZ rightDirection = (floorEdges.Last().AsCurve() as Line).Direction;

                        rightDirection = (rightDirection - rightDirection.Z * XYZ.BasisZ).Normalize();
                        Transform rotate90 = Transform.CreateRotation(XYZ.BasisZ, -Math.PI / 2);
                        if (Math.Abs(rightDirection.X) > 0.01 && rightDirection.X < 0)
                        {
                            rightDirection = -rightDirection;
                        }
                        if (Math.Abs(rightDirection.Y) > 0.01 && rightDirection.Y > 0)
                        {
                            rightDirection = rotate90.OfPoint(rightDirection);
                        }
                        if (rightDirection.Y < -0.99 || rightDirection.Y > 0.99)
                        {
                            rightDirection = XYZ.BasisX;
                        }

                        XYZ upDirection = XYZ.BasisZ.CrossProduct(rightDirection);
                        Transform transform = null;
                        if (XYZ.BasisY.CrossProduct(upDirection).AngleTo(XYZ.BasisZ) > Math.PI / 2)
                        {
                            transform = Transform.CreateRotation(XYZ.BasisZ, XYZ.BasisY.AngleTo(upDirection));
                        }
                        else
                        {
                            if (upDirection.IsAlmostEqualTo(XYZ.BasisY))
                            {
                                transform = Transform.CreateTranslation(new XYZ());
                            }
                            else
                            {
                                transform = Transform.CreateRotation(XYZ.BasisZ, XYZ.BasisY.AngleTo(upDirection) + Math.PI);
                            }
                        }

                        List<TransformedPoint> points = new List<TransformedPoint>();
                        foreach (Edge edge in floorEdges)
                        {
                            points.Add(new TransformedPoint(edge, 0, transform));
                            points.Add(new TransformedPoint(edge, 1, transform));
                        }
                        
                        double dimUpValue = 0;
                        double dimRightValue = 0;

                        points = points.OrderBy(a => a.TranformedXYZ.X).ToList();

                        List<TransformedPoint> point0List = points.Where(a => Math.Abs(a.TranformedXYZ.X - points.First().TranformedXYZ.X) < 0.1).OrderBy(a => a.TranformedXYZ.Y).ToList();
                        List<TransformedPoint> point1List = points.Where(a => Math.Abs(a.TranformedXYZ.X - points.Last().TranformedXYZ.X) < 0.1).OrderBy(a => a.TranformedXYZ.Y).ToList();
                        Line unboundX = Line.CreateUnbound(new XYZ(), rightDirection);
                        dimRightValue = Line.CreateBound(unboundX.Project(point0List.First().XYZ).XYZPoint, unboundX.Project(point1List.First().XYZ).XYZPoint).Length;

                        points = points.OrderBy(a => a.TranformedXYZ.Y).ToList();

                        List<TransformedPoint> point0List2 = points.Where(a => Math.Abs(a.TranformedXYZ.Y - points.First().TranformedXYZ.Y) < 0.1).OrderBy(a => a.TranformedXYZ.X).ToList();
                        List<TransformedPoint> point1List2 = points.Where(a => Math.Abs(a.TranformedXYZ.Y - points.Last().TranformedXYZ.Y) < 0.1).OrderBy(a => a.TranformedXYZ.X).ToList();
                        Line unboundY = Line.CreateUnbound(new XYZ(), upDirection);
                        dimUpValue = Line.CreateBound(unboundY.Project(point0List2.Last().XYZ).XYZPoint, unboundY.Project(point1List2.Last().XYZ).XYZPoint).Length;

                        tx.Start();
                        double factor = (dimUpValue / dimRightValue) < 1 ? (1 / (dimUpValue / dimRightValue)) : (dimUpValue / dimRightValue);
                        floor.LookupParameter("Fator de Forma").Set(Math.Round(factor, 2));
                        if (factor > proportion)
                        {
                            floor.LookupParameter("LPE_TIPO DE PISO").Set(reinforcementFloorTypeId);
                            double floorThickness = (floor as Floor).FloorType.GetCompoundStructure().GetLayers().Where(a => a.MaterialId == (floor as Floor).FloorType.StructuralMaterialId).FirstOrDefault().Width;
                        }
                        Reference refer = new Reference(floor);
                        if (factor > proportion)
                        {
                            floorsToTag.Add(floor.Id, tagNao.Id);
                        }
                        else
                        {
                            floorsToTag.Add(floor.Id, tagOK.Id);
                        }
                        tx.Commit();
                    }

                    tx.Start();
                    view.IsolateElementsTemporary(floors.Where(a => a.LookupParameter("(s/n) Fibra").AsInteger() == 1).Select(a => a.Id).ToList());
                    view.ConvertTemporaryHideIsolateToPermanent();
                    tx.Commit();

                    tx.Start();
                    for (int i = 0; i < floorsToTag.Count; i++)
                    {
                        XYZ centroid = (doc.GetElement(floorsToTag.ElementAt(i).Key).get_Geometry(new Options()).FirstOrDefault() as Solid).ComputeCentroid();
                        Reference reference = new Reference(doc.GetElement(floorsToTag.ElementAt(i).Key));
                        IndependentTag tag = IndependentTag.Create(doc, floorsToTag.ElementAt(i).Value, view.Id, reference, false, TagOrientation.Horizontal, centroid);
                    }
                    tx.Commit();
                    uidoc.ActiveView = initialView;
                }
            }
            tg.Assimilate();

            return Result.Succeeded;
        }
    }
}
