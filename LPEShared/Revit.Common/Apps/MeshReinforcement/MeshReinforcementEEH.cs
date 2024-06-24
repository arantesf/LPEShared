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
using System.Windows;
using System.Threading.Tasks;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class MeshReinforcementEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            try
            {
                SelectAmbienteReinforcementMVVM.MainView.Ambientes_ListBox.IsEnabled = false;
                SelectAmbienteReinforcementMVVM.MainView.FatorForma_TextBox.IsEnabled = false;
                SelectAmbienteReinforcementMVVM.MainView.Execute_Button.IsEnabled = false;

                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                Autodesk.Revit.DB.View initialView = uidoc.ActiveView;

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

                DimensionType cotaLPEType = cotaLPETypeList.First();
                Element tagOK = tagOKList.First();
                Element tagNao = tagNaoList.First();

                TransactionGroup tg = new TransactionGroup(doc, "Reforçar com Tela");
                tg.Start();
                Options options = new Options
                {
                    View = initialView,
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = true
                };
                double proportion = double.Parse(SelectAmbienteReinforcementMVVM.MainView.FatorDeFormaGlobal.Replace(",", "."));


                foreach (var checkedAmbiente in SelectAmbienteReinforcementMVVM.MainView.AmbienteAndReinforcementViewModels.Where(x => x.SelectedReinforcement.Name != ""))
                {
                    string ambiente = checkedAmbiente.Name;

                    List<Element> floors = new FilteredElementCollector(doc, initialView.Id)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_Floors)
                        .Where(a => a.LookupParameter("Ambiente").AsString() == ambiente)
                        .ToList();

                    ElementId reinforcementFloorTypeId = checkedAmbiente.SelectedReinforcement.Id;
                    ElementId floorId = new FilteredElementCollector(doc)
                                        .OfClass(typeof(FloorType))
                                        .Select(a => a.Id)
                                        .FirstOrDefault(x => doc.GetElement(x).Name == doc.GetElement(reinforcementFloorTypeId).Name);

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

                        Dictionary<ElementId, ElementId> floorsToTag = new Dictionary<ElementId, ElementId>();

                        SelectAmbienteReinforcementMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                        SelectAmbienteReinforcementMVVM.MainView.ProgressBar.Maximum = floors.Count;
                        int count = 0;

                        foreach (var floor in floors)
                        {
                            
                            if (floor.LookupParameter("(s/n) Fibra").AsInteger() == 0 || floor.LookupParameter("Reforço de Tela").AsInteger() == 1)
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
                            List<ModelCurve> floorModelCurves = floor.GetDependentElements(linesFilter).Select(a => (doc.GetElement(a) as ModelCurve)).ToList();
                            List<Curve> outsideCurves = floorModelCurves.Select(a => a.GeometryCurve).ToList();
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
                                        if (Utils.GetCurveProjection(modelCurve).Project(Utils.GetCurveProjection(edge.AsCurve()).Evaluate(0.5, true)).Distance < 0.1);
                                        {
                                            floorEdges.Add(edge);
                                            break;
                                        }
                                        //if (Utils.IsCurvesEqual(Utils.GetCurveProjection(edge.AsCurve()), Utils.GetCurveProjection(modelCurve)))
                                        //{
                                        //    floorEdges.Add(edge);
                                        //}
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
                                try
                                {
                                    floor.ChangeTypeId(floorId);
                                }
                                catch (Exception)
                                {
                                }
                                double floorThickness = (floor as Floor).FloorType.GetCompoundStructure().GetLayers().Where(a => a.MaterialId == (floor as Floor).FloorType.StructuralMaterialId).FirstOrDefault().Width;
                            }
                            //floor.LookupParameter("Comprimento Placa")?.Set(Math.Round(UnitUtils.ConvertFromInternalUnits((dimUpValue / dimRightValue) < 1 ? dimRightValue : dimUpValue, UnitTypeId.Meters),2));
                            //floor.LookupParameter("Largura da Placa")?.Set(Math.Round(UnitUtils.ConvertFromInternalUnits((dimUpValue / dimRightValue) < 1 ? dimUpValue : dimRightValue, UnitTypeId.Meters),2));
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
                            SelectAmbienteReinforcementMVVM.ProgressBarViewModel.ProgressBarValue += 1;
                            count++;
                            ExternalApplication.LPEApp.SelectAmbienteReinforcementMVVM.ProgressBar_TextBlock.Text = $"Analisando fator de forma dos pisos ({count}/{floors.Count})";
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
                        ExternalApplication.LPEApp.SelectAmbienteReinforcementMVVM.ProgressBar_TextBlock.Text = $"Finalizando transação...";
                        SelectAmbienteReinforcementMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                        SelectAmbienteReinforcementMVVM.MainView.ProgressBar.Maximum = 2;
                        SelectAmbienteReinforcementMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                        tx.Commit();
                        SelectAmbienteReinforcementMVVM.ProgressBarViewModel.ProgressBarValue += 1;
                        uidoc.ActiveView = initialView;
                    }
                }
                tg.Assimilate();
                SelectAmbienteReinforcementMVVM.MainView.Dispose();

            }
            catch (Exception ex)
            {
                SelectAmbienteReinforcementMVVM.MainView.Dispose(); 
                Autodesk.Revit.UI.TaskDialog.Show("ATENÇÃO!", "Erro não mapeado, contate os desenvolvedores.\n\n" + ex.StackTrace);
                throw;
            }
        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }
}
