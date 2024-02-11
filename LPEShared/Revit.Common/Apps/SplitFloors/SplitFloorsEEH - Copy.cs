// Decompiled with JetBrains decompiler
// Type: Revit.Common.SplitFloorsEEH
// Assembly: LPE, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E06EE1B-06C5-4A71-9629-ACF2EB60ECA2
// Assembly location: C:\ProgramData\Autodesk\Revit\Addins\2024\LPE\LPE.dll

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Revit.Common
{
    //[Transaction(TransactionMode.Manual)]
    //public class SplitFloorsEEH2 : IExternalEventHandler
    //{
    //    public void Execute(UIApplication uiapp)
    //    {
    //        SelectAmbienteMVVM.MainView.Ambientes_ListBox.IsEnabled = false;
    //        SelectAmbienteMVVM.MainView.SelectAll_CheckBox.IsEnabled = false;
    //        SelectAmbienteMVVM.MainView.SelectPisos_Button.IsEnabled = false;
    //        SelectAmbienteMVVM.MainView.Execute_Button.IsEnabled = false;
    //        SelectAmbienteMVVM.MainView.DivideReinforcement_CheckBox.IsEnabled = false;
    //        UIDocument activeUiDocument = uiapp.ActiveUIDocument;
    //        Document document = activeUiDocument.Document;
    //        List<string> list1 = SelectAmbienteMVVM.MainView.AmbienteViewModels.Where<AmbienteViewModel>((Func<AmbienteViewModel, bool>)(x => x.IsChecked)).Select<AmbienteViewModel, string>((Func<AmbienteViewModel, string>)(x => x.Name)).ToList<string>();
    //        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
    //        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = 1.0;
    //        TransactionGroup transactionGroup1 = new TransactionGroup(document);
    //        transactionGroup1.Start("Dividir Pisos");
    //        foreach (string str in list1)
    //        {
    //            string ambiente = str;
    //            List<Element> pisofloors = ((IEnumerable<Element>)new FilteredElementCollector(document, ((Element)activeUiDocument.ActiveView).Id).WhereElementIsNotElementType().OfCategory((BuiltInCategory) - 2000032L)).Where<Element>((Func<Element, bool>)(floor => floor.LookupParameter("Ambiente").HasValue && floor.LookupParameter("Piso em placas").AsInteger() == 0 && floor.LookupParameter("Reforço de Tela").AsInteger() == 0 && floor.LookupParameter("Ambiente").AsString() == ambiente)).ToList<Element>();
    //            List<Element> openings = ((IEnumerable<Element>)new FilteredElementCollector(document, ((Element)activeUiDocument.ActiveView).Id).WhereElementIsNotElementType().OfCategory((BuiltInCategory) - 2000898L)).Where<Element>((Func<Element, bool>)(opening => (opening as Opening).Host.LookupParameter("Ambiente").HasValue && (opening as Opening).Host.LookupParameter("Piso em placas").AsInteger() == 0 && (opening as Opening).Host.LookupParameter("Ambiente").AsString() == ambiente)).ToList<Element>();
    //            List<Element> joints = ((IEnumerable<Element>)new FilteredElementCollector(document, ((Element)activeUiDocument.ActiveView).Id).WhereElementIsNotElementType().OfCategory((BuiltInCategory) - 2001320L)).Where<Element>((Func<Element, bool>)(a => a.LookupParameter("Ambiente").HasValue && a.LookupParameter("Ambiente").AsString() == ambiente)).ToList<Element>();
    //            List<Element> reforco = ((IEnumerable<Element>)new FilteredElementCollector(document, ((Element)activeUiDocument.ActiveView).Id).WhereElementIsNotElementType().OfCategory((BuiltInCategory) - 2000032L)).Where<Element>((Func<Element, bool>)(floor => floor.LookupParameter("Ambiente").HasValue && floor.LookupParameter("Ambiente").AsString() == ambiente && floor.LookupParameter("Reforço de Tela").AsInteger() == 1)).ToList<Element>();
    //            CurveArray reforcoOpeningsCurveArray = new CurveArray();
    //            CurveArray openingsCurveArray = new CurveArray();
    //            Solid openingsSolid = (Solid)null;
    //            if (openings.Any<Element>())
    //            {
    //                foreach (Opening opening in openings.Cast<Opening>())
    //                {
    //                    CurveArray boundaryCurves = opening.BoundaryCurves;
    //                    foreach (Curve curve in boundaryCurves)
    //                    {
    //                        if (opening.Host.LookupParameter("Reforço de Tela").AsInteger() == 1)
    //                            reforcoOpeningsCurveArray.Append(curve);
    //                        else
    //                            openingsCurveArray.Append(curve);
    //                    }
    //                    List<CurveLoop> loopsByCurveArray = Utils.GetCurveLoopsByCurveArray(boundaryCurves);
    //                    if (loopsByCurveArray.Any<CurveLoop>())
    //                    {
    //                        Solid extrusionGeometry = GeometryCreationUtilities.CreateExtrusionGeometry((IList<CurveLoop>)loopsByCurveArray, XYZ.op_UnaryNegation(XYZ.BasisZ), 1000.0);
    //                        openingsSolid = !GeometryObject.op_Equality((GeometryObject)openingsSolid, (GeometryObject)null) ? BooleanOperationsUtils.ExecuteBooleanOperation(openingsSolid, extrusionGeometry, (BooleanOperationsType)0) : extrusionGeometry;
    //                    }
    //                }
    //            }
    //            List<List<Element>> pisosAndReforços = new List<List<Element>>()
    //            {
    //                pisofloors,
    //                reforco
    //            };
    //            List<(Element, Solid)> principalFloorsToJoin = new List<(Element, Solid)>();
    //            List<ElementId> reinforcementFloorsToJoin = new List<ElementId>();

    //            SelectAmbienteMVVM.MainView.ProgressBar.Maximum = (double)pisosAndReforços.Count;
    //            for (int i = 0; i < pisosAndReforços.Count; ++i)
    //            {
    //                if (!SelectAmbienteMVVM.MainView.DivideReinforcement_CheckBox.IsChecked.Value)
    //                {
    //                    for (int k = 1; k < pisosAndReforços.Count; ++k)
    //                        reinforcementFloorsToJoin.AddRange(pisosAndReforços[k].Select<Element, ElementId>((Func<Element, ElementId>)(element => element.Id)));
    //                    if (i != 0)
    //                        break;
    //                }
    //                List<Element> floors = pisosAndReforços[i];
    //                List<ElementId> selectedFloors = SelectAmbienteMVVM.MainView.SelectedFloorsIds;
    //                if (selectedFloors.Any<ElementId>())
    //                {
    //                    floors = floors.Where<Element>((Func<Element, bool>)(floor => selectedFloors.Contains(floor.Id))).ToList<Element>();
    //                    reforco = reforco.Where<Element>((Func<Element, bool>)(floor => selectedFloors.Contains(floor.Id))).ToList<Element>();
    //                }
    //                if (floors.Any<Element>())
    //                {
    //                    List<Solid> floorSolids = new List<Solid>();
    //                    foreach (Floor floor in floors.Cast<Floor>())
    //                    {
    //                        CurveArrArray floorArrays = (document.GetElement(floor.SketchId) as Sketch).Profile;
    //                        Solid floorSolid = Utils.GetElementSolid((Element)floor);
    //                        if (!floorSolids.Any<Solid>())
    //                        {
    //                            floorSolids.Add(floorSolid);
    //                        }
    //                        else
    //                        {
    //                            try
    //                            {
    //                                floorSolids[0] = BooleanOperationsUtils.ExecuteBooleanOperation(floorSolids[0], floorSolid, (BooleanOperationsType)0);
    //                            }
    //                            catch (Exception ex)
    //                            {
    //                                floorSolids.Add(floorSolid);
    //                            }
    //                        }
    //                    }
    //                    if (floors.Any<Element>() && joints.Any<Element>())
    //                    {
    //                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
    //                        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = 3.0;
    //                        ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = "Preparando para dividir os pisos do ambiente \"" + ambiente + "\"";
    //                        CurveArray curveArrayToCreateRoomBoundaries = new CurveArray();
    //                        TransactionGroup transactionGroup2 = new TransactionGroup(document);
    //                        transactionGroup2.Start("transactionToRollBack");
    //                        List<Element> roomsToDelete = ((IEnumerable<Element>)new FilteredElementCollector(document, ((Element)activeUiDocument.ActiveView).Id).WhereElementIsNotElementType().WherePasses((ElementFilter)new ElementMulticategoryFilter((ICollection<BuiltInCategory>)new List<BuiltInCategory>()
    //                          {
    //                            BuiltInCategory.OST_Rooms,
    //                            BuiltInCategory.OST_RoomSeparationLines
    //                          }))).ToList<Element>();
    //                        Utils.DeleteElements(document, (IEnumerable<Element>)roomsToDelete);
    //                        foreach (Floor floor in floors.Cast<Floor>())
    //                        {
    //                            foreach (CurveArray curveArray4 in (document.GetElement(floor.SketchId) as Sketch).Profile)
    //                            {
    //                                foreach (Curve curve in curveArray4)
    //                                    curveArrayToCreateRoomBoundaries.Append(curve);
    //                            }
    //                        }
    //                        if (i == 0)
    //                        {
    //                            foreach (Curve curve in openingsCurveArray)
    //                                curveArrayToCreateRoomBoundaries.Append(curve);
    //                        }
    //                        else
    //                        {
    //                            foreach (Curve curve in reforcoOpeningsCurveArray)
    //                                curveArrayToCreateRoomBoundaries.Append(curve);
    //                        }
    //                        Utils.AddJointCurvesToCurveArray(document, curveArrayToCreateRoomBoundaries, (IEnumerable<Element>)joints);
    //                        Level level = Utils.CreateLevel(document, 10000.0);
    //                        View deletarView = ((IEnumerable<Element>)new FilteredElementCollector(document).WhereElementIsNotElementType().OfClass(typeof(ViewPlan))).Where<Element>((Func<Element, bool>)(view => view.Name == "Deletar")).Cast<View>().FirstOrDefault<View>();
    //                        Utils.DeleteElements(document, (IEnumerable<Element>)new List<Element>()
    //                          {
    //                            (Element) deletarView
    //                          });
    //                        deletarView = Utils.CreateTemporaryViewInLevel(document, level);
    //                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 1;
    //                        CurveArray curveArrayToCreateRoomBoundariesScaled = new CurveArray();
    //                        Transform transform10 = Transform.CreateTranslation(new XYZ(0.0, 0.0, 0.0)).ScaleBasis(10.0);
    //                        Transform transform1 = Transform.CreateTranslation(new XYZ(0.0, 0.0, 0.0)).ScaleBasis(0.1);
    //                        foreach (Curve curve in curveArrayToCreateRoomBoundaries)
    //                            curveArrayToCreateRoomBoundariesScaled.Append(curve.CreateTransformed(transform10));
    //                        Utils.CreateRoomBoundaryLines(document, ((Element)level).Id, curveArrayToCreateRoomBoundariesScaled, temporaryViewInLevel);
    //                        List<Room> createdRooms = Utils.CreateRooms(document, level);
    //                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 2;
    //                        List<CurveArray> dividedFloorsCurveArrays = new List<CurveArray>();
    //                        List<(List<CurveLoop>, Curve)> dividedFloorsCurveLoops = new List<(List<CurveLoop>, Curve)>();
    //                        SolidCurveIntersectionOptions solidCurveIntersectionOptions = new SolidCurveIntersectionOptions();
    //                        List<Solid> recreatedSolids = new List<Solid>();
    //                        foreach (Room createdRoom in createdRooms)
    //                        {
    //                            XYZ roomPoint = (((Element)createdRoom).Location as LocationPoint).Point;
    //                            XYZ roomPointTrasnformed = transform1.OfPoint(roomPoint);
    //                            Curve curveToIntersectSolid = (Curve)Line.CreateBound(XYZ.op_Subtraction(roomPointTrasnformed, XYZ.op_Multiply(XYZ.BasisZ, 10000.0)), XYZ.op_Addition(roomPointTrasnformed, XYZ.op_Multiply(XYZ.BasisZ, 10000.0)));
    //                            bool intersect = false;
    //                            foreach (Solid finalSolid in floorSolids)
    //                            {
    //                                if (((IEnumerable<Curve>)finalSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions)).Any<Curve>())
    //                                {
    //                                    intersect = true;
    //                                    break;
    //                                }
    //                            }
    //                            if (intersect)
    //                            {
    //                                List<CurveLoop> curveLoops = new List<CurveLoop>();
    //                                CurveArray curveArray = new CurveArray();
    //                                SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
    //                                foreach (IList<BoundarySegment> boundarySegment1 in (IEnumerable<IList<BoundarySegment>>)((SpatialElement)createdRoom).GetBoundarySegments(opt))
    //                                {
    //                                    CurveLoop cLoop = new CurveLoop();
    //                                    foreach (BoundarySegment list in (IEnumerable<BoundarySegment>)boundarySegment1)
    //                                    {
    //                                        Curve transformed = list.GetCurve().CreateTransformed(transform1);
    //                                        curveArray.Append(transformed);
    //                                        cLoop.Append(transformed);
    //                                    }
    //                                    if (cLoop.IsOpen())
    //                                    {
    //                                        try
    //                                        {
    //                                            cLoop.Append((Curve)Line.CreateBound(((IEnumerable<Curve>)cLoop).Last<Curve>().GetEndPoint(1), ((IEnumerable<Curve>)cLoop).First<Curve>().GetEndPoint(0)));
    //                                        }
    //                                        catch (Exception ex)
    //                                        {
    //                                            Line bound2 = Line.CreateBound(((IEnumerable<Curve>)cLoop).Last<Curve>().GetEndPoint(0), ((IEnumerable<Curve>)cLoop).First<Curve>().GetEndPoint(0));
    //                                            CurveLoop curveLoop = new CurveLoop();
    //                                            foreach (Curve curve in ((IEnumerable<Curve>)cLoop).Take<Curve>(((IEnumerable<Curve>)cLoop).Count<Curve>() - 1))
    //                                                curveLoop.Append(curve);
    //                                            curveLoop.Append((Curve)bound2);
    //                                            cLoop = curveLoop;
    //                                        }
    //                                    }
    //                                    curveLoops.Add(cLoop);
    //                                }
    //                                dividedFloorsCurveArrays.Add(curveArray);
    //                                dividedFloorsCurveLoops.Add((curveLoops, curveToIntersectSolid));
    //                            }
    //                        }
    //                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 3;
    //                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
    //                        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = (double)dividedFloorsCurveLoops.Count;
    //                        int count = 0;
    //                        for (int j = 0; j < dividedFloorsCurveLoops.Count; ++j)
    //                        {
    //                            List<CurveLoop> curveLoops = dividedFloorsCurveLoops[j].Item1;
    //                            if (curveLoops.Any<CurveLoop>())
    //                            {
    //                                Curve CurveToIntersect = dividedFloorsCurveLoops[j].Item2;
    //                                ++SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue;
    //                                ++count;
    //                                ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = string.Format("Criando pisos do ambiente \"{0}\" ({1}/{2})", (object)ambiente, (object)count, (object)dividedFloorsCurveLoops.Count);
    //                                List<CurveLoop> orderedCurveLoops = curveLoops.OrderBy<CurveLoop, double>((Func<CurveLoop, double>)(curveLoop => curveLoop.GetExactLength())).ToList<CurveLoop>();
    //                                XYZ secondVectorInMultiplication = XYZ.op_UnaryNegation(XYZ.BasisZ);
    //                                if (curveLoops.First<CurveLoop>().IsCounterclockwise(XYZ.BasisZ))
    //                                    secondVectorInMultiplication = XYZ.BasisZ;
    //                                Line firstLine = (Line)((IEnumerable<Curve>)curveLoops.First<CurveLoop>()).FirstOrDefault<Curve>((Func<Curve, bool>)(curve => curve is Line));
    //                                if (!GeometryObject.op_Equality((GeometryObject)firstLine, (GeometryObject)null))
    //                                {
    //                                    XYZ.op_Addition(((Curve)firstLine).Evaluate(0.5, true), XYZ.op_Multiply(firstLine.Direction.CrossProduct(secondVectorInMultiplication), 0.01));
    //                                    List<CurveLoop> fixedSmallLinesCurveLoops = Utils.FixCurveLoopsSmallLines(orderedCurveLoops, 0.01);
    //                                    Curve curveToIntersectSolid = CurveToIntersect;
    //                                    ElementId typeId = floors.First<Element>().GetTypeId();
    //                                    ElementId levelId = floors.First<Element>().LevelId;
    //                                    Floor createdFloor = Utils.CreateFloor(document, fixedSmallLinesCurveLoops, typeId, levelId);
    //                                    Utils.GetElementSolid((Element)createdFloor);
    //                                    if (createdFloor != null)
    //                                    {
    //                                        List<Floor> list9 = floors.Cast<Floor>().Where<Floor>((Func<Floor, bool>)(f => ((Element)f).LookupParameter("Reforço de Tela").AsInteger() == 0)).ToList<Floor>();
    //                                        list9.AddRange((IEnumerable<Floor>)floors.Cast<Floor>().Where<Floor>((Func<Floor, bool>)(f => ((Element)f).LookupParameter("Reforço de Tela").AsInteger() == 1)).ToList<Floor>());
    //                                        foreach (Floor floor2 in list9)
    //                                        {
    //                                            if (((IEnumerable<Curve>)Utils.GetElementSolid((Element)floor2).IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions)).Any<Curve>())
    //                                            {
    //                                                Utils.ChangeTypeId((Element)createdFloor, ((Element)floor2).GetTypeId());
    //                                                Utils.CopyAllParameters((Element)floor2, (Element)createdFloor, new List<BuiltInParameter>()
    //                                                  {
    //                                                    (BuiltInParameter) -1001203L
    //                                                  });
    //                                                floor2.GetSlabShapeEditor();
    //                                                Utils.SetPointsElevationsOfNewFloor(floor2, createdFloor, 0.0);
    //                                                break;
    //                                            }
    //                                        }
    //                                        if (pisosAndReforços.Count<List<Element>>() > 1)
    //                                        {
    //                                            if (i == 0)
    //                                                principalFloorsToJoin.Add(((Element)createdFloor, Utils.GetSolid((Element)createdFloor)));
    //                                            else
    //                                                reinforcementFloorsToJoin.Add(((Element)createdFloor).Id);
    //                                        }
    //                                        Utils.SetParameter((Element)createdFloor, "Piso em placas", (object)1);
    //                                    }
    //                                }
    //                            }
    //                        }
    //                        Utils.DeleteElements(document, (IEnumerable<Element>)floors);
    //                        transactionGroup2.Assimilate();
    //                    }
    //                }
    //            }
    //            using (Transaction trans = new Transaction(document))
    //            {
    //                trans.Start("Internal Transaction");
    //                Utils.HideRevitWarnings(trans);
    //                SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
    //                SelectAmbienteMVVM.MainView.ProgressBar.Maximum = (double)reinforcementFloorsToJoin.Count;
    //                int num = 0;
    //                foreach (ElementId elementId in reinforcementFloorsToJoin)
    //                {
    //                    ++SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue;
    //                    ++num;
    //                    ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = string.Format("Unindo pisos do ambiente \"{0}\" ({1}/{2})", (object)ambiente, (object)num, (object)reinforcementFloorsToJoin.Count);
    //                    Element element1 = document.GetElement(elementId);
    //                    Solid solid3 = Utils.GetSolid(element1);
    //                    List<Element> elementList = new List<Element>();
    //                    foreach ((Element, Solid) valueTuple in principalFloorsToJoin)
    //                    {
    //                        try
    //                        {
    //                            if (BooleanOperationsUtils.ExecuteBooleanOperation(solid3, valueTuple.Item2, (BooleanOperationsType)2).Volume > 0.0)
    //                                elementList.Add(valueTuple.Item1);
    //                        }
    //                        catch (Exception ex)
    //                        {
    //                        }
    //                    }
    //                    foreach (Element element2 in elementList)
    //                    {
    //                        try
    //                        {
    //                            JoinGeometryUtils.JoinGeometry(document, element1, element2);
    //                        }
    //                        catch (Exception ex)
    //                        {
    //                        }
    //                    }
    //                }
    //                trans.Commit();
    //            }
    //        }
    //        transactionGroup1.Assimilate();
    //        SelectAmbienteMVVM.MainView.Dispose();
    //    }

    //    public string GetName() => this.GetType().Name;
    //}
}
