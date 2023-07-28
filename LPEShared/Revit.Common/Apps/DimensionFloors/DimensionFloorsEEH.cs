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
using System.Windows.Controls;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class DimensionFloorsEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            try
            {
                SelectAmbienteMVVM.MainView.Ambientes_ListBox.IsEnabled = false;
                SelectAmbienteMVVM.MainView.SelectAll_CheckBox.IsEnabled = false;
                SelectAmbienteMVVM.MainView.SelectPisos_Button.IsEnabled = false;
                SelectAmbienteMVVM.MainView.Execute_Button.IsEnabled = false;

                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                View initialView = uidoc.ActiveView;
                List<ElementId> selectedFloorIds = SelectAmbienteMVVM.MainView.SelectedFloorsIds;

                List<string> selectedAmbientes = SelectAmbienteMVVM.MainView.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name).ToList();

                List<string> floors = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                   .WhereElementIsNotElementType()
                   .OfCategory(BuiltInCategory.OST_Floors)
                   .Where(a => a.LookupParameter("Ambiente").HasValue)
                   .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                   .Select(a => a.First().LookupParameter("Ambiente").AsString())
                   .ToList();

                List<Element> allFloors = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .ToList();

                DimensionType cotaLPEType = new FilteredElementCollector(doc)
                    .OfClass(typeof(DimensionType))
                    .Where(a => a.Name == "Cota_LPE")
                    .Cast<DimensionType>()
                    .FirstOrDefault();

                Options options = new Options
                {
                    View = uidoc.ActiveView,
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = true
                };
                TransactionGroup tg = new TransactionGroup(doc, "Criar Cotas");
                tg.Start();
                using (Transaction tx = new Transaction(doc, "Create Dimensions"))
                {
                    tx.Start();
                    doc.SetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType, cotaLPEType.Id);
                    tx.Commit();

                    ElementCategoryFilter dimFilter = new ElementCategoryFilter(BuiltInCategory.OST_Dimensions);
                    ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);

                    if (!selectedFloorIds.Any())
                    {
                        foreach (string item in SelectAmbienteMVVM.MainView.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name))
                        {
                            selectedAmbientes.Add(item);
                            selectedFloorIds.AddRange(new FilteredElementCollector(doc)
                                .WhereElementIsNotElementType()
                                .OfCategory(BuiltInCategory.OST_Floors)
                                .Where(a => a.LookupParameter("Ambiente").AsString() == item)
                                .Select(a => a.Id)
                                .ToList());
                        }
                    }

                    SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                    SelectAmbienteMVVM.MainView.ProgressBar.Maximum = selectedFloorIds.Count;
                    int count = 0;

                    foreach (var floor in selectedFloorIds.Select(a => doc.GetElement(a)))
                    {
                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;
                        count++;
                        ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Cotando pisos ({count}/{selectedFloorIds.Count})";
                        if (floor.LookupParameter("Reforço de Tela").AsInteger() == 1 || !selectedAmbientes.Contains(floor.LookupParameter("Ambiente").AsString()))
                        {
                            continue;
                        }
                        tx.Start();
                        foreach (Dimension dimension in floor.GetDependentElements(dimFilter).Select(a => doc.GetElement(a) as Dimension))
                        {
                            if (dimension.View.Id == initialView.Id && dimension.DimensionType.Id == cotaLPEType.Id)
                            {
                                doc.Delete(dimension.Id);
                            }
                        }
                        tx.Commit();

                        GeometryElement wholeGridGeometry = floor.get_Geometry(options);
                        List<Edge> allFloorEdges = new List<Edge>();
                        foreach (GeometryObject geomObj in wholeGridGeometry)
                        {
                            if (geomObj is Solid)
                            {
                                Solid solid = geomObj as Solid;
                                foreach (var face in solid.Faces.Cast<Face>().Where(face => face.ComputeNormal(new UV(0, 0)).Z > 0.1))
                                {
                                    foreach (EdgeArray edgearray in face.EdgeLoops)
                                    {
                                        foreach (Edge edge in edgearray)
                                        {
                                            if (edge.AsCurve() is Line)
                                            {
                                                if ((edge.AsCurve() as Line).Direction.Z < 0.9)
                                                {
                                                    allFloorEdges.Add(edge);
                                                }
                                            }
                                        }
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
                        foreach (var edge in allFloorEdges)
                        {
                            foreach (var modelCurve in outsideCurves)
                            {
                                try
                                {
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
                        points = points.OrderBy(a => a.TranformedXYZ.X).ToList();
                        //points = points.GroupBy(a => new { a.XYZ.X, a.XYZ.Y, a.XYZ.Z }).Select(a => a.First()).ToList();

                        ReferenceArray rArray = new ReferenceArray();
                        List<TransformedPoint> point0List = points.Where(a => Math.Abs(a.TranformedXYZ.X - points.First().TranformedXYZ.X) < 0.1).OrderBy(a => a.TranformedXYZ.Y).ToList();
                        rArray.Append(point0List.First().Reference);
                        List<TransformedPoint> point1List = points.Where(a => Math.Abs(a.TranformedXYZ.X - points.Last().TranformedXYZ.X) < 0.1).OrderBy(a => a.TranformedXYZ.Y).ToList();
                        rArray.Append(point1List.First().Reference);
                        Line line1 = Line.CreateBound(point1List.First().XYZ - rightDirection, point1List.First().XYZ);
                        tx.Start();
                        DetailCurve mline1 = doc.Create.NewDetailCurve(initialView, line1);
                        tx.Commit();
                        rArray.Append(mline1.GeometryCurve.Reference);

                        points = points.OrderBy(a => a.TranformedXYZ.Y).ToList();

                        ReferenceArray rArray2 = new ReferenceArray();
                        List<TransformedPoint> point0List2 = points.Where(a => Math.Abs(a.TranformedXYZ.Y - points.First().TranformedXYZ.Y) < 0.1).OrderBy(a => a.TranformedXYZ.X).ToList();
                        rArray2.Append(point0List2.Last().Reference);
                        List<TransformedPoint> point1List2 = points.Where(a => Math.Abs(a.TranformedXYZ.Y - points.Last().TranformedXYZ.Y) < 0.1).OrderBy(a => a.TranformedXYZ.X).ToList();
                        rArray2.Append(point1List2.Last().Reference);
                        Line line2 = Line.CreateBound(point1List2.Last().XYZ - upDirection, point1List2.Last().XYZ);
                        tx.Start();
                        DetailCurve mline2 = doc.Create.NewDetailCurve(initialView, line2);
                        tx.Commit();
                        rArray2.Append(mline2.GeometryCurve.Reference);

                        tx.Start();
                        Dimension dimRight = doc.Create.NewDimension(initialView, Utils.GetCurveProjection(line1) as Line, rArray);
                        Dimension dimUp = doc.Create.NewDimension(initialView, Utils.GetCurveProjection(line2) as Line, rArray2);
                        tx.Commit();

                        tx.Start();
                        doc.Delete(new List<ElementId>() { mline1.Id, mline2.Id });
                        tx.Commit();
                    }
                    //tx.Commit();
                }

                tg.Assimilate();
                SelectAmbienteMVVM.MainView.Dispose();
            }
            catch (Exception ex)
            {
                SelectAmbienteMVVM.MainView.Dispose(); 
                TaskDialog.Show("ATENÇÃO!", "Erro não mapeado, contate os desenvolvedores.\n\n" + ex.StackTrace);
                throw;
            }
        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }
}
