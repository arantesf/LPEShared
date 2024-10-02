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

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class SplitFloorsEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            try
            {
                SelectAmbienteMVVM.MainView.Ambientes_ListBox.IsEnabled = false;
                SelectAmbienteMVVM.MainView.SelectAll_CheckBox.IsEnabled = false;
                SelectAmbienteMVVM.MainView.SelectPisos_Button.IsEnabled = false;
                SelectAmbienteMVVM.MainView.Execute_Button.IsEnabled = false;
                SelectAmbienteMVVM.MainView.DivideReinforcement_CheckBox.IsEnabled = false;


                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                List<string> checkedAmbientesNames = SelectAmbienteMVVM.MainView.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name).ToList();
                SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                SelectAmbienteMVVM.MainView.ProgressBar.Maximum = 1;
                TransactionGroup transactionGroupMaster = new TransactionGroup(doc);
                transactionGroupMaster.Start("Dividir Pisos");
                foreach (var ambiente in checkedAmbientesNames)
                {
                    List<Element> pisofloors = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_Floors)
                        .Where(floor => floor.LookupParameter("Ambiente").HasValue && floor.LookupParameter("Piso em placas").AsInteger() == 0 && floor.LookupParameter("Reforço de Tela").AsInteger() == 0 && floor.LookupParameter("Ambiente").AsString() == ambiente)
                        .ToList();

                    List<Element> openings = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                       .WhereElementIsNotElementType()
                       .OfCategory(BuiltInCategory.OST_FloorOpening)
                       .Where(opening => (opening as Opening).Host.LookupParameter("Ambiente").HasValue && (opening as Opening).Host.LookupParameter("Piso em placas").AsInteger() == 0 && (opening as Opening).Host.LookupParameter("Ambiente").AsString() == ambiente)
                       .ToList();

                    List<Element> joints = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .Where(a => a.LookupParameter("Ambiente").HasValue && a.LookupParameter("Ambiente").AsString() == ambiente)
                        .ToList();

                    List<Element> reforco = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_Floors)
                        .Where(floor => floor.LookupParameter("Ambiente").HasValue && floor.LookupParameter("Ambiente").AsString() == ambiente && (floor.LookupParameter("Reforço de Tela").AsInteger() == 1))
                        .ToList();

                    CurveArray openingsCurveArray = new CurveArray();
                    CurveArray reforcoOpeningsCurveArray = new CurveArray();
                    List<Solid> openingsSolids = new List<Solid>();
                    List<Solid> reforcoOpeningsSolids = new List<Solid>();
                    if (openings.Any())
                    {
                        foreach (Opening opening in openings.Cast<Opening>())
                        {
                            CurveArray boundaryCurves = opening.BoundaryCurves;
                            List<CurveLoop> loopsByCurveArray = Utils.GetCurveLoopsByCurveArray(boundaryCurves);

                            if (opening.Host.LookupParameter("Reforço de Tela").AsInteger() == 1)
                            {
                                foreach (Curve curve in boundaryCurves)
                                {
                                    reforcoOpeningsCurveArray.Append(curve);
                                }
                                if (loopsByCurveArray.Any())
                                {
                                    Solid openingSolidDown = GeometryCreationUtilities.CreateExtrusionGeometry(loopsByCurveArray, -XYZ.BasisZ, 1000);
                                    if (!openingsSolids.Any())
                                    {
                                        reforcoOpeningsSolids.Add(openingSolidDown);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            reforcoOpeningsSolids[0] = BooleanOperationsUtils.ExecuteBooleanOperation(reforcoOpeningsSolids[0], openingSolidDown, BooleanOperationsType.Union);
                                        }
                                        catch (Exception)
                                        {
                                            reforcoOpeningsSolids.Add(openingSolidDown);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (Curve curve in boundaryCurves)
                                {
                                    openingsCurveArray.Append(curve);
                                }
                                if (loopsByCurveArray.Any())
                                {
                                    Solid openingSolidDown = GeometryCreationUtilities.CreateExtrusionGeometry(loopsByCurveArray, -XYZ.BasisZ, 1000);
                                    if (!openingsSolids.Any())
                                    {
                                        openingsSolids.Add(openingSolidDown);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            openingsSolids[0] = BooleanOperationsUtils.ExecuteBooleanOperation(openingsSolids[0], openingSolidDown, BooleanOperationsType.Union);
                                        }
                                        catch (Exception)
                                        {
                                            openingsSolids.Add(openingSolidDown);
                                        }
                                    }
                                }
                            }

                        }
                    }

                    List<List<Element>> pisosAndReforços = new List<List<Element>>() { pisofloors, reforco };
                    List<(Element Element, Solid Solid)> principalFloorsToJoin = new List<(Element Element, Solid Solid)>();
                    List<ElementId> reinforcementFloorsToJoin = new List<ElementId>();

                    SelectAmbienteMVVM.MainView.ProgressBar.Maximum = pisosAndReforços.Count;
                    for (int i = 0; i < pisosAndReforços.Count; i++)
                    {
                        if (!(bool)SelectAmbienteMVVM.MainView.DivideReinforcement_CheckBox.IsChecked)
                        {
                            for (int k = 1; k < pisosAndReforços.Count; k++)
                                reinforcementFloorsToJoin.AddRange(pisosAndReforços[k].Select(element => element.Id));
                            if (i != 0)
                                break;
                        }

                        List<Element> floors = pisosAndReforços[i];

                        List<ElementId> selectedFloors = SelectAmbienteMVVM.MainView.SelectedFloorsIds;
                        if (selectedFloors.Any())
                        {
                            floors = floors.Where(floor => selectedFloors.Contains(floor.Id)).ToList();
                            reforco = reforco.Where(floor => selectedFloors.Contains(floor.Id)).ToList();
                        }

                        if (!floors.Any())
                        {
                            continue;
                        }


                        List<Solid> floorSolids = new List<Solid>();
                        foreach (Floor floor in floors.Cast<Floor>())
                        {
                            try
                            {
#if Revit2021
                            CurveArrArray floorArrays = new CurveArrArray() {  };
                            CurveArray curveArray = new CurveArray();
                            foreach (var curve in floor.GetDependentElements(new ElementClassFilter(typeof(CurveElement))).Select(id => (doc.GetElement(id) as CurveElement).GeometryCurve))
                            {
                                curveArray.Append(curve);
                            }
                            floorArrays.Append(curveArray);
#else
                                CurveArrArray floorArrays = (doc.GetElement(floor.SketchId) as Sketch).Profile;
#endif

                                List<CurveLoop> curveLoops = new List<CurveLoop>();
                                foreach (CurveArray floorArray in floorArrays)
                                {
                                    curveLoops.AddRange(Utils.GetCurveLoopsByCurveArray(floorArray));
                                }
                                Solid floorSolid = GeometryCreationUtilities.CreateExtrusionGeometry(curveLoops, -XYZ.BasisZ, 1000);

                                if (!floorSolids.Any())
                                {
                                    floorSolids.Add(floorSolid);
                                }
                                else
                                {
                                    try
                                    {
                                        floorSolids[0] = BooleanOperationsUtils.ExecuteBooleanOperation(floorSolids[0], floorSolid, BooleanOperationsType.Union);
                                    }
                                    catch (Exception)
                                    {
                                        floorSolids.Add(floorSolid);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }

                        //List<Solid> finalSolids = new List<Solid>();
                        //if (openingsSolid != null)
                        //{
                        //    foreach (var solid in floorSolids)
                        //    {
                        //        try
                        //        {
                        //            finalSolids.Add(BooleanOperationsUtils.ExecuteBooleanOperation(solid, openingsSolid, BooleanOperationsType.Difference));
                        //        }
                        //        catch (Exception)
                        //        {
                        //        }
                        //    }
                        //}
                        //else
                        //{
                        //    finalSolids = floorSolids;
                        //}

                        if (floors.Any() && joints.Any())
                        {
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                            SelectAmbienteMVVM.MainView.ProgressBar.Maximum = 3;
                            ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Preparando para dividir os pisos do ambiente \"{ambiente}\"";

                            CurveArray curveArrayToCreateRoomBoundaries = new CurveArray();
                            TransactionGroup transactionGroup2 = new TransactionGroup(doc);
                            transactionGroup2.Start("transactionToRollBack");

                            List<Element> roomsToDelete = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                                .WhereElementIsNotElementType()
                                .WherePasses(new ElementMulticategoryFilter(new List<BuiltInCategory>() { BuiltInCategory.OST_Rooms, BuiltInCategory.OST_RoomSeparationLines }))
                                .ToList();

                            Utils.DeleteElements(doc, roomsToDelete);

                            foreach (Floor floor in floors.Cast<Floor>())
                            {
                                try
                                {

#if Revit2021
                                CurveArrArray floorArrays = new CurveArrArray() { };
                                CurveArray curveArrayy = new CurveArray();
                                foreach (var curve in floor.GetDependentElements(new ElementClassFilter(typeof(CurveElement))).Select(id => (doc.GetElement(id) as CurveElement).GeometryCurve))
                                {
                                    curveArrayy.Append(curve);
                                }
                                floorArrays.Append(curveArrayy);
#else
                                    CurveArrArray floorArrays = (doc.GetElement(floor.SketchId) as Sketch).Profile;
#endif
                                    foreach (CurveArray curveArray in floorArrays)
                                    {
                                        foreach (Curve curve in curveArray)
                                        {
                                            curveArrayToCreateRoomBoundaries.Append(curve);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                            if (i == 0)
                            {
                                foreach (Curve curve in openingsCurveArray)
                                {
                                    curveArrayToCreateRoomBoundaries.Append(curve);
                                }
                            }
                            else
                            {
                                foreach (Curve curve in reforcoOpeningsCurveArray)
                                {
                                    curveArrayToCreateRoomBoundaries.Append(curve);
                                }
                            }


                            Utils.AddJointCurvesToCurveArray(doc, curveArrayToCreateRoomBoundaries, joints);

                            Level level = Utils.CreateLevel(doc, 10000);
                            Autodesk.Revit.DB.View deletarView = new FilteredElementCollector(doc)
                                .WhereElementIsNotElementType()
                                .OfClass(typeof(ViewPlan))
                                .Where(view => view.Name == "Deletar")
                                .Cast<Autodesk.Revit.DB.View>()
                                .FirstOrDefault();
                            Utils.DeleteElements(doc, new List<Element>() { deletarView });

                            deletarView = Utils.CreateTemporaryViewInLevel(doc, level);

                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 1;

                            CurveArray curveArrayToCreateRoomBoundariesScaled = new CurveArray();
                            Transform transform10 = Transform.CreateTranslation(new XYZ(0, 0, 0)).ScaleBasis(10);
                            Transform transform1 = Transform.CreateTranslation(new XYZ(0, 0, 0)).ScaleBasis(0.1);
                            foreach (Curve curve in curveArrayToCreateRoomBoundaries)
                            {
                                curveArrayToCreateRoomBoundariesScaled.Append(curve.CreateTransformed(transform10));
                            }
                            ModelCurveArray modelCurveArray = Utils.CreateRoomBoundaryLines(doc, level.Id, curveArrayToCreateRoomBoundariesScaled, deletarView);
                            List<Room> createdRooms = Utils.CreateRooms(doc, level);

                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 2;


                            List<CurveArray> dividedFloorsCurveArrays = new List<CurveArray>();
                            List<(List<CurveLoop> CurveLoops, Curve CurveToIntersect)> dividedFloorsCurveLoops = new List<(List<CurveLoop>, Curve)>();
                            SolidCurveIntersectionOptions solidCurveIntersectionOptions = new SolidCurveIntersectionOptions();
                            List<Solid> recreatedSolids = new List<Solid>();
                            foreach (Room createdRoom in createdRooms)
                            {
                                try
                                {
                                    XYZ roomPoint = (createdRoom.Location as LocationPoint).Point;
                                    XYZ roomPointTrasnformed = transform1.OfPoint(roomPoint);
                                    Curve curveToIntersectSolid = Line.CreateBound(roomPointTrasnformed - XYZ.BasisZ * 10000, roomPointTrasnformed + XYZ.BasisZ * 10000);
                                    bool intersectOpening = false;
                                    if (i == 0)
                                    {
                                        foreach (var openingSolid in openingsSolids)
                                        {
                                            if (openingSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions).Any())
                                            {
                                                intersectOpening = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (var openingSolid in reforcoOpeningsSolids)
                                        {
                                            if (openingSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions).Any())
                                            {
                                                intersectOpening = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (intersectOpening)
                                    {
                                        continue;
                                    }
                                    bool intersect = false;
                                    foreach (var finalSolid in floorSolids)
                                    {
                                        if (finalSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions).Any())
                                        {
                                            intersect = true;
                                            break;
                                        }
                                    }

                                    if (intersect)
                                    {
                                        List<CurveLoop> curveLoops = new List<CurveLoop>();
                                        CurveArray curveArray = new CurveArray();
                                        SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
                                        IList<IList<BoundarySegment>> boundarySegmentArray = createdRoom.GetBoundarySegments(opt);
                                        foreach (IList<BoundarySegment> list in boundarySegmentArray)
                                        {
                                            CurveLoop cLoop = new CurveLoop();
                                            foreach (BoundarySegment boundarySegment in list)
                                            {
                                                Curve curve = boundarySegment.GetCurve().CreateTransformed(transform1);
                                                curveArray.Append(curve);
                                                cLoop.Append(curve);
                                            }
                                            if (cLoop.IsOpen())
                                            {
                                                try
                                                {
                                                    cLoop.Append(Line.CreateBound(cLoop.Last().GetEndPoint(1), cLoop.First().GetEndPoint(0)));
                                                }
                                                catch (Exception)
                                                {
                                                    Line correctionLine = Line.CreateBound(cLoop.Last().GetEndPoint(0), cLoop.First().GetEndPoint(0));
                                                    CurveLoop newCLoop = new CurveLoop();
                                                    foreach (var item in cLoop.Take(cLoop.Count() - 1))
                                                    {
                                                        newCLoop.Append(item);
                                                    }
                                                    newCLoop.Append(correctionLine);
                                                    cLoop = newCLoop;
                                                }
                                            }
                                            curveLoops.Add(cLoop);
                                        }
                                        dividedFloorsCurveArrays.Add(curveArray);
                                        dividedFloorsCurveLoops.Add((curveLoops, curveToIntersectSolid));
                                    }
                                }
                                catch (Exception)
                                {
                                }
                            }
                            //Utils.DeleteElements(doc, new List<Element>() { deletarView });
                            transactionGroup2.RollBack();
                            transactionGroup2.Start("Dividir Pisos");

                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 3;
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                            SelectAmbienteMVVM.MainView.ProgressBar.Maximum = dividedFloorsCurveLoops.Count;
                            int count = 0;
                            for (int j = 0; j < dividedFloorsCurveLoops.Count; j++)
                            {
                                try
                                {
                                    List<CurveLoop> curveLoops = dividedFloorsCurveLoops[j].CurveLoops;
                                    if (curveLoops.Any())
                                    {
                                        Curve CurveToIntersect = dividedFloorsCurveLoops[j].CurveToIntersect;
                                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;
                                        count++;
                                        ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Criando pisos do ambiente \"{ambiente}\" ({count}/{dividedFloorsCurveLoops.Count})";
                                        List<CurveLoop> orderedCurveLoops = curveLoops.OrderBy(curveLoop => curveLoop.GetExactLength()).ToList();
                                        XYZ secondVectorInMultiplication = -XYZ.BasisZ;
                                        if (curveLoops.First().IsCounterclockwise(XYZ.BasisZ))
                                        {
                                            secondVectorInMultiplication = XYZ.BasisZ;
                                        }

                                        //CurveLoop offsetCurveLoop = CurveLoop.CreateViaOffset(orderedCurveLoops.First(), -0.01, XYZ.BasisZ);
                                        //XYZ pointInsideCurveLoop = offsetCurveLoop.First().GetEndPoint(0);
                                        Line firstLine = (Line)curveLoops.First().FirstOrDefault(curve => curve is Line);
                                        if (firstLine == null)
                                        {
                                            continue;
                                        }
                                        XYZ middlePoint = firstLine.Evaluate(0.5, true);
                                        XYZ pointInsideCurveLoop = middlePoint + firstLine.Direction.CrossProduct(secondVectorInMultiplication) * 0.01;


                                        List<CurveLoop> fixedSmallLinesCurveLoops = Utils.FixCurveLoopsSmallLines(orderedCurveLoops, 0.01);

                                        //Curve curveToIntersectSolid = Line.CreateBound(pointInsideCurveLoop - XYZ.BasisZ * 10000, pointInsideCurveLoop + XYZ.BasisZ * 10000);
                                        Curve curveToIntersectSolid = CurveToIntersect;
                                        ElementId typeId = floors.First().GetTypeId();
                                        ElementId levelId = floors.First().LevelId;
                                        Floor createdFloor = Utils.CreateFloor(doc, fixedSmallLinesCurveLoops, typeId, levelId);
                                        if (createdFloor == null)
                                        {
                                            continue;
                                        }

                                        foreach (Floor existingFloor in floors.Cast<Floor>())
                                        {
                                            Solid existingFloorSolid = Utils.GetElementSolid(existingFloor);
                                            if (existingFloorSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions).Any())
                                            {
                                                Utils.ChangeTypeId(createdFloor, existingFloor.GetTypeId());
                                                Utils.CopyAllParameters(existingFloor, createdFloor, new List<BuiltInParameter>() { BuiltInParameter.ALL_MODEL_MARK });
#if Revit2021 || Revit2022 || Revit2023
                                            SlabShapeEditor slabShapeEditor = existingFloor.SlabShapeEditor;
#else
                                                SlabShapeEditor slabShapeEditor = existingFloor.GetSlabShapeEditor();
#endif
                                                Utils.SetPointsElevationsOfNewFloor(existingFloor as Floor, createdFloor, 0);
                                                break;
                                            }
                                        }
                                        if (pisosAndReforços.Count() > 1)
                                        {
                                            if (i == 0)
                                            {
                                                principalFloorsToJoin.Add((createdFloor, Utils.GetSolid(createdFloor)));
                                            }
                                            else
                                            {
                                                reinforcementFloorsToJoin.Add(createdFloor.Id);
                                            }
                                        }
                                        Utils.SetParameter(createdFloor, "Piso em placas", 1);

                                    }
                                }
                                catch (Exception)
                                {
                                }

                            }

                            Utils.DeleteElements(doc, floors);

                            transactionGroup2.Assimilate();

                        }
                    }

                    using (var trans = new Transaction(doc))
                    {
                        trans.Start("Internal Transaction");
                        Utils.HideRevitWarnings(trans);

                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = reinforcementFloorsToJoin.Count;
                        int count = 0;

                        foreach (var reinforcementFloorId in reinforcementFloorsToJoin)
                        {
                            try
                            {


                                SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;
                                count++;
                                ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Unindo pisos do ambiente \"{ambiente}\" ({count}/{reinforcementFloorsToJoin.Count})";
                                Element reinforcementFloor = doc.GetElement(reinforcementFloorId);
                                Solid createdFloorSolid = Utils.GetSolid(reinforcementFloor);
                                List<Element> intersectElements = new List<Element>();
                                foreach (var floor in principalFloorsToJoin)
                                {
                                    try
                                    {
                                        if (BooleanOperationsUtils.ExecuteBooleanOperation(createdFloorSolid, floor.Solid, BooleanOperationsType.Intersect).Volume > 0)
                                        {
                                            intersectElements.Add(floor.Element);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }

                                //ElementIntersectsElementFilter filter = new ElementIntersectsElementFilter(reinforcementFloor);
                                //List<Element> intersectElements = new FilteredElementCollector(doc, principalFloorsToJoin).WherePasses(filter).ToList();
                                foreach (Element intersectElement in intersectElements)
                                {
                                    try
                                    {
                                        JoinGeometryUtils.JoinGeometry(doc, reinforcementFloor, intersectElement);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                        trans.Commit();
                    }

                }
                transactionGroupMaster.Assimilate();
                SelectAmbienteMVVM.MainView.Dispose();

            }
            catch (Exception ex)
            {
                SelectAmbienteMVVM.MainView.Dispose();
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
