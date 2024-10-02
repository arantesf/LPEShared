using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;

namespace Revit.Common
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool)
            {
                flag = (bool)value;
            }
            else if (value is bool?)
            {
                bool? flag2 = (bool?)value;
                flag = flag2.HasValue && flag2.Value;
            }

            return (!flag) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Visibility)
            {
                return (System.Windows.Visibility)value == System.Windows.Visibility.Visible;
            }

            return false;
        }

    }

    public class XYZComparer : IEqualityComparer<XYZ>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(XYZ zero, XYZ one)
        {

            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(zero, one)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(zero, null) || Object.ReferenceEquals(one, null))
                return false;

            //Check whether the products' properties are equal.
            return zero.X == one.X && zero.Y == one.Y;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(XYZ xyz)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(xyz, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashProductName = xyz.X.GetHashCode();

            //Get hash code for the Code field.
            int hashProductCode = xyz.Y.GetHashCode();

            //Calculate the hash code for the product.
            return hashProductName ^ hashProductCode;
        }
    }

    public class FloorSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            try
            {
                Category category = element.Category;
                if (category == null) return false;
#if Revit2021 || Revit2022 || Revit2023
                int categoryId = element.Category.Id.IntegerValue;
#else
                int categoryId = (int)element.Category.Id.Value;
#endif
                if (element.Category != null && categoryId == (int)BuiltInCategory.OST_Floors)
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        public bool AllowReference(Reference refer, XYZ point)
        {
            return false;
        }
    }

    public class MyPreProcessor : IFailuresPreprocessor
    {
        FailureProcessingResult IFailuresPreprocessor.PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            String transactionName = failuresAccessor.GetTransactionName();

            IList<FailureMessageAccessor> fmas = failuresAccessor.GetFailureMessages();

            if (fmas.Count == 0)
                return FailureProcessingResult.Continue;

            if (transactionName.Equals("Internal Transaction"))
            {
                foreach (FailureMessageAccessor fma in fmas)
                {
                    if (fma.GetSeverity() == FailureSeverity.Error)
                    {
                        failuresAccessor.ResolveFailure(fma);
                        return FailureProcessingResult.ProceedWithCommit;
                    }
                    else
                    {
                        failuresAccessor.DeleteWarning(fma);
                    }
                }
            }
            else
            {
                foreach (FailureMessageAccessor fma in fmas)
                {
                    failuresAccessor.DeleteAllWarnings();
                }
            }
            return FailureProcessingResult.Continue;
        }
    }


    public static class Utils
    {
        public enum OffsetVerifyResult
        {
            Error0 = 0,
            Error1 = 1,
            BothError = 2,
            OK = 42

        }
        public static OffsetVerifyResult VerifyOffset(Autodesk.Revit.ApplicationServices.Application app, Element joint, Curve offset, List<Element> joints, List<Curve> floorCurves)
        {

            bool offset1ep0OK = false;
            bool offset1ep1OK = false;
            foreach (var otherJoint in joints)
            {
                if (otherJoint.Id != joint.Id)
                {
                    Curve otherJointCurve = (joint.Location as LocationCurve).Curve;
                    if (otherJointCurve.Distance(offset.GetEndPoint(0)) < app.ShortCurveTolerance / 2)
                    {
                        offset1ep0OK = true;
                    }
                    if (otherJointCurve.Distance(offset.GetEndPoint(1)) < app.ShortCurveTolerance / 2)
                    {
                        offset1ep1OK = true;
                    }
                    if (offset1ep0OK && offset1ep1OK)
                    {
                        break;
                    }
                }

            }
            if (!offset1ep0OK || !offset1ep1OK)
            {
                foreach (var floorCurve in floorCurves)
                {
                    if (floorCurve.Distance(offset.GetEndPoint(0)) < app.ShortCurveTolerance / 2)
                    {
                        offset1ep0OK = true;
                    }
                    if (floorCurve.Distance(offset.GetEndPoint(1)) < app.ShortCurveTolerance / 2)
                    {
                        offset1ep1OK = true;
                    }
                    if (offset1ep0OK && offset1ep1OK)
                    {
                        break;
                    }
                }
            }
            if (!offset1ep0OK && !offset1ep1OK)
            {
                return OffsetVerifyResult.BothError;
            }
            else if (!offset1ep0OK && offset1ep1OK)
            {
                return OffsetVerifyResult.Error0;
            }
            else if (offset1ep0OK && !offset1ep1OK)
            {
                return OffsetVerifyResult.Error1;
            }
            else
            {
                return OffsetVerifyResult.OK;
            }

        }

        public static void HideRevitWarnings(Transaction trans)
        {
            FailureHandlingOptions options = trans.GetFailureHandlingOptions();
            MyPreProcessor preproccessor = new MyPreProcessor();
            options.SetClearAfterRollback(true);
            options.SetFailuresPreprocessor(preproccessor);
            trans.SetFailureHandlingOptions(options);
        }


        public static Opening CreateOpening(Document doc, Element hostElement, CurveArray curveArray, bool structural)
        {
            using (var trans = new Transaction(doc))
            {
                try
                {
                    trans.Start("Internal Transaction");
                    HideRevitWarnings(trans);
                    Opening op = doc.Create.NewOpening(hostElement, curveArray, false);
                    trans.Commit();
                    return op;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static Floor CreateFloor(Document doc, CurveArray curveArray, ElementId typeId, ElementId levelId)
        {
            using (var trans = new Transaction(doc))
            {
                try
                {
                    trans.Start("Internal Transaction");
                    HideRevitWarnings(trans);
#if Revit2021
                    Floor createdFloor = doc.Create.NewFloor(curveArray, false);
#else
                    List<CurveLoop> curveLoops = GetCurveLoopsByCurveArray(curveArray);
                    Floor createdFloor = Floor.Create(doc, curveLoops, typeId, levelId);
#endif
                    trans.Commit();
                    return createdFloor;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static Floor CreateFloor(Document doc, List<CurveLoop> curveLoops, ElementId typeId, ElementId levelId)
        {
            using (var trans = new Transaction(doc))
            {
                //try
                //{
                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);
#if Revit2021
                    CurveArray curveArray = new CurveArray();
                    foreach (var curveloop in curveLoops)
                    {
                        foreach (var curve in curveloop)
                        {
                            curveArray.Append(curve);
                        }
                    }
                    Floor createdFloor = doc.Create.NewFloor(curveArray,doc.GetElement(typeId) as FloorType, doc.GetElement(levelId) as Level, false);
#else
                Floor createdFloor = Floor.Create(doc, curveLoops, typeId, levelId);
#endif
                trans.Commit();
                return createdFloor;
                //}
                //catch (Exception)
                //{
                //    return null;
                //}
            }
        }
        public static void ChangeTypeId(Element element, ElementId newTypeId)
        {
            using (var trans = new Transaction(element.Document))
            {
                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);
                element.ChangeTypeId(newTypeId);
                trans.Commit();
            }
        }


        public static void JoinElements(Document doc, Element firstElement, Element secondElement)
        {
            using (var trans = new Transaction(doc))
            {
                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);
                JoinGeometryUtils.JoinGeometry(doc, firstElement, secondElement);
                trans.Commit();
            }
        }

        public static void DeleteElements(Document doc, IEnumerable<Element> elements)
        {
            using (var trans = new Transaction(doc))
            {
                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);
                foreach (var element in elements)
                {
                    if (element != null && element.IsValidObject)
                    {
                        doc.Delete(element.Id);
                    }
                };
                trans.Commit();
            }
        }

        public static ModelCurveArray CreateRoomBoundaryLines(Document doc, ElementId levelId, CurveArray curveArray, Autodesk.Revit.DB.View view)
        {
            using (var trans = new Transaction(doc))
            {
                double levelZValue = (doc.GetElement(levelId) as Level).Elevation;
                CurveArray curveArrayInLevelElevation = new CurveArray();
                foreach (Curve curve in curveArray)
                {
                    if (curve is Arc)
                    {
                        if (curve.IsBound)
                        {
                            XYZ ep0 = new XYZ(curve.GetEndPoint(0).X, curve.GetEndPoint(0).Y, levelZValue);
                            XYZ midPoint = new XYZ(curve.Evaluate(0.5, true).X, curve.Evaluate(0.5, true).Y, levelZValue);
                            XYZ ep1 = new XYZ(curve.GetEndPoint(1).X, curve.GetEndPoint(1).Y, levelZValue);
                            curveArrayInLevelElevation.Append(Arc.Create(ep0, ep1, midPoint));
                        }
                        else
                        {
                            XYZ center = (curve as Arc).Center;
                            double radius = (curve as Arc).Radius;
                            XYZ ep0 = new XYZ((center + radius * XYZ.BasisY).X, (center + radius * XYZ.BasisY).Y, levelZValue);
                            XYZ ep025 = new XYZ((center + radius * XYZ.BasisX).X, (center + radius * XYZ.BasisX).Y, levelZValue);
                            XYZ ep050 = new XYZ((center - radius * XYZ.BasisY).X, (center - radius * XYZ.BasisY).Y, levelZValue);
                            XYZ ep075 = new XYZ((center - radius * XYZ.BasisX).X, (center - radius * XYZ.BasisX).Y, levelZValue);
                            curveArrayInLevelElevation.Append(Arc.Create(ep0, ep025, ep050));
                            curveArrayInLevelElevation.Append(Arc.Create(ep050, ep075, ep0));
                        }
                    }
                    else if (curve is Ellipse)
                    {
                        curveArrayInLevelElevation.Append((Ellipse)curve);
                    }
                    else if (curve is HermiteSpline)
                    {
                        HermiteSpline spline = (HermiteSpline)curve;
                        List<XYZ> newControlPoints = new List<XYZ>();
                        foreach (var controlPoint in spline.ControlPoints)
                        {
                            newControlPoints.Add(new XYZ(controlPoint.X, controlPoint.Y, levelZValue));
                        }
                        curveArrayInLevelElevation.Append(HermiteSpline.Create(newControlPoints, spline.IsPeriodic));

                    }
                    else
                    {
                        XYZ ep0 = new XYZ(curve.GetEndPoint(0).X, curve.GetEndPoint(0).Y, levelZValue);
                        XYZ ep1 = new XYZ(curve.GetEndPoint(1).X, curve.GetEndPoint(1).Y, levelZValue);
                        try
                        {
                            curveArrayInLevelElevation.Append(Line.CreateBound(ep0, ep1));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);
                SketchPlane plane = SketchPlane.Create(doc, levelId);
                ModelCurveArray modelCurveArray = doc.Create.NewRoomBoundaryLines(plane, curveArrayInLevelElevation, view);
                trans.Commit();
                return modelCurveArray;
            }
        }

        public static Autodesk.Revit.DB.View CreateTemporaryViewInLevel(Document doc, Level level)
        {
            using (var trans = new Transaction(doc))
            {
                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);

                ElementId firstViewPlanType = new FilteredElementCollector(doc)
                    .WhereElementIsElementType()
                    .OfClass(typeof(ViewFamilyType))
                    .Where(vft => (vft as ViewFamilyType).ViewFamily == ViewFamily.FloorPlan)
                    .Select(vft => vft.Id)
                    .FirstOrDefault();
                Autodesk.Revit.DB.View initialView = ViewPlan.Create(doc, firstViewPlanType, level.Id);
                initialView.Name = "Deletar";

                trans.Commit();
                return initialView;
            }
        }
        public static Level CreateLevel(Document doc, double elevation)
        {
            using (var trans = new Transaction(doc))
            {
                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);
                Level level = Level.Create(doc, elevation);
                trans.Commit();
                return level;
            }
        }


        public static List<Room> CreateRooms(Document doc, Level level)
        {
            List<Room> newListRoom = new List<Room>();
            using (var trans = new Transaction(doc))
            {
                //trans.Start("Internal Transaction");
                //HideRevitWarnings(trans);
                //PlanTopology pt = doc.get_PlanTopology(level);
                //trans.Commit();
                //PlanCircuitSet pcs = pt.Circuits;
                //foreach (PlanCircuit pc in pcs)
                //{
                //if ((pc != null) && (pc.IsRoomLocated == false))
                //{
                try
                {

                    trans.Start("Internal Transaction");
                    HideRevitWarnings(trans);
                    ICollection<ElementId> newRoomsIds = doc.Create.NewRooms2(level);
                    newListRoom.AddRange(newRoomsIds.Select(id => doc.GetElement(id) as Room));
                    trans.Commit();
                }
                catch { }
                //}
                //}

            }
            return newListRoom;
        }

        public static List<CurveLoop> GetCurveLoopsByCurveArray(CurveArray curves)
        {
            List<int> usedCurvesIndexes = new List<int>();

            List<CurveLoop> curveLoops = new List<CurveLoop>();
        RecursivePoint:
            CurveLoop curveLoop = new CurveLoop();
            int count = 0;
            while (curveLoop.IsOpen() && count < 1000)
            {
                count++;
                for (int i = 0; i < curves.Size; i++)
                {
                    if (!usedCurvesIndexes.Contains(i))
                    {
                        if (curveLoop.Count() == 0)
                        {
                            if (!curves.get_Item(i).IsBound)
                            {
                                Arc arc = curves.get_Item(i) as Arc;
                                XYZ center = arc.Center;
                                double radius = arc.Radius;
                                double angle0 = 0;
                                double angle180 = Math.PI;
                                double angle360 = 2 * Math.PI;
                                Arc semiArc1 = Arc.Create(center, radius, angle0, angle180, XYZ.BasisX, XYZ.BasisY);
                                Arc semiArc2 = Arc.Create(center, radius, angle180, angle360, XYZ.BasisX, XYZ.BasisY);
                                curveLoop.Append(semiArc1);
                                curveLoop.Append(semiArc2);
                                usedCurvesIndexes.Add(i);
                                break;
                            }
                            else
                            {
                                curveLoop.Append(curves.get_Item(i));
                                usedCurvesIndexes.Add(i);
                                if (!curveLoop.IsOpen())
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            XYZ lastPoint = curveLoop.Last().GetEndPoint(1);
                            if (curves.get_Item(i).IsBound && curveLoop.Last().GetEndPoint(1).DistanceTo(curves.get_Item(i).GetEndPoint(0)) < 0.01)
                            {
                                curveLoop.Append(curves.get_Item(i));
                                usedCurvesIndexes.Add(i);
                                if (!curveLoop.IsOpen())
                                {
                                    break;
                                }
                            }
                            else if (curves.get_Item(i).IsBound && curveLoop.Last().GetEndPoint(1).DistanceTo(curves.get_Item(i).GetEndPoint(1)) < 0.01)
                            {
                                curveLoop.Append(curves.get_Item(i).CreateReversed());
                                usedCurvesIndexes.Add(i);
                                if (!curveLoop.IsOpen())
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (curveLoop.Any() && !curveLoop.IsOpen() && count < 1000)
            {
                curveLoops.Add(curveLoop);
            }
            if (usedCurvesIndexes.Count != curves.Size)
            {
                goto RecursivePoint;
            }

            return curveLoops;
        }

        public static Solid GetSolid(Element element)
        {
            Solid solid = null;
            Options opt = new Options();
            try
            {
                GeometryElement geoEle = element.get_Geometry(opt);
                foreach (GeometryObject geoObj in geoEle)
                {
                    if (geoObj is Solid)
                    {
                        solid = geoObj as Solid;
                    }
                }
            }
            catch { }

            return solid;
        }

        public static List<CurveLoop> FixCurveLoopsSmallLines(List<CurveLoop> curveLoops, double tolerance)
        {
            List<CurveLoop> fixedCurveLoops = new List<CurveLoop>();
            CurveArray curveArray = new CurveArray();
            foreach (var curveLoop in curveLoops)
            {
                if (curveLoop.Where(curve => curve.Length < tolerance).Any())
                {
                    CurveLoop fixedCurveLoop = new CurveLoop();
                    for (int i = 0; i < curveLoop.Count(); i++)
                    {
                        Curve curve = curveLoop.ElementAt(i);
                        if (curve.Length > tolerance)
                        {
                            try
                            {
                                fixedCurveLoop.Append(curve);
                            }
                            catch (Exception)
                            {
                                Curve correctionCurve = Line.CreateBound(fixedCurveLoop.Last().GetEndPoint(1), curve.GetEndPoint(1));
                                fixedCurveLoop.Append(correctionCurve);
                            }
                        }
                        else
                        {
                            int realInt = -1;
                        RedoCurveLoop:;
                            if (i == curveLoop.Count() - 1)
                            {
                                //if (realInt != -1)
                                //{
                                //    i = realInt;
                                //}
                                for (int j = 0; j < 10; j++)
                                {
                                    List<Curve> otherCurves = new List<Curve>();
                                    otherCurves.AddRange(fixedCurveLoop);
                                    int count = fixedCurveLoop.Count();
                                    Curve nextCurve = fixedCurveLoop.ElementAt(0 + j);
                                    fixedCurveLoop = new CurveLoop();
                                    XYZ initialPoint = curve.GetEndPoint(0);
                                    if (initialPoint.DistanceTo(nextCurve.GetEndPoint(1)) < 0.0028)
                                    {
                                        continue;
                                    }
                                    Curve fixedCurve = null;
                                    if (nextCurve is Arc)
                                    {
                                        fixedCurve = Arc.Create(initialPoint, nextCurve.GetEndPoint(1), nextCurve.Evaluate(0.5, true));
                                    }
                                    else if (nextCurve is HermiteSpline)
                                    {
                                        HermiteSpline spline = (HermiteSpline)nextCurve;
                                        List<XYZ> newPoints = new List<XYZ>() { initialPoint };
                                        newPoints.AddRange(spline.Tessellate().Skip(1));
                                        fixedCurve = HermiteSpline.Create(newPoints, spline.IsPeriodic);
                                    }
                                    else
                                    {
                                        fixedCurve = Line.CreateBound(initialPoint, nextCurve.GetEndPoint(1));
                                    }
                                    fixedCurveLoop.Append(fixedCurve);
                                    for (int k = 1 + j; k < otherCurves.Count; k++)
                                    {
                                        fixedCurveLoop.Append(otherCurves[k]);
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                for (int j = 0; j < 10; j++)
                                {
                                    if (i + 1 + j > curveLoop.Count() - 1)
                                    {
                                        realInt = i;
                                        i = curveLoop.Count() - 1;
                                        goto RedoCurveLoop;
                                    }
                                    Curve nextCurve = curveLoop.ElementAt(i + 1 + j);
                                    XYZ initialPoint = curve.GetEndPoint(0);
                                    if (initialPoint.DistanceTo(nextCurve.GetEndPoint(1)) < 0.0028)
                                    {
                                        continue;
                                    }
                                    Curve fixedCurve = null;
                                    if (nextCurve is Arc)
                                    {
                                        fixedCurve = Arc.Create(initialPoint, nextCurve.GetEndPoint(1), nextCurve.Evaluate(0.5, true));
                                    }
                                    else if (nextCurve is HermiteSpline)
                                    {
                                        HermiteSpline spline = (HermiteSpline)nextCurve;
                                        List<XYZ> newPoints = new List<XYZ>() { initialPoint };
                                        newPoints.AddRange(spline.Tessellate().Skip(1));
                                        fixedCurve = HermiteSpline.Create(newPoints, spline.IsPeriodic);
                                    }
                                    else
                                    {
                                        fixedCurve = Line.CreateBound(initialPoint, nextCurve.GetEndPoint(1));
                                    }
                                    fixedCurveLoop.Append(fixedCurve);
                                    i++;
                                    i += j;
                                    break;
                                }
                            }
                        }
                    }
                    fixedCurveLoops.Add(fixedCurveLoop);
                }
                else
                {
                    fixedCurveLoops.Add(curveLoop);
                }
            }
            return fixedCurveLoops;
        }

        public static CurveArray GetCurveArrayByCurveLoop(CurveLoop curveLoop)
        {
            CurveArray curveArray = new CurveArray();
            foreach (var curve in curveLoop)
            {
                curveArray.Append(curve);
            }
            return curveArray;
        }

        public static Solid GetElementSolid(Element element)
        {
            Options opt = new Options();
            opt.IncludeNonVisibleObjects = true;
            opt.ComputeReferences = true;

            Solid solid1 = null;

            GeometryElement geoEle1 = element.get_Geometry(opt);

            foreach (GeometryObject geoObj in geoEle1)
            {
                if (geoObj is Solid)
                {
                    if (solid1 == null)
                    {
                        solid1 = geoObj as Solid;
                    }
                    else
                    {
                        BooleanOperationsUtils.ExecuteBooleanOperation(solid1, geoObj as Solid, BooleanOperationsType.Union);
                    }
                }
                else if (geoObj is GeometryInstance)
                {
                    foreach (GeometryObject geoInstanceObj in (geoObj as GeometryInstance).GetInstanceGeometry())
                    {
                        if (solid1 == null)
                        {
                            solid1 = geoInstanceObj as Solid;
                        }
                        else
                        {
                            BooleanOperationsUtils.ExecuteBooleanOperation(solid1, geoInstanceObj as Solid, BooleanOperationsType.Union);
                        }
                    }
                }
            }
            return solid1;
        }

        public static bool ElementIntersectElement(Element element1, Element element2)
        {
            Options opt = new Options();
            //opt.View = element1.Document.ActiveView;
            opt.IncludeNonVisibleObjects = true;
            opt.ComputeReferences = true;

            try
            {
                Solid solid1 = GetElementSolid(element1);

                if (solid1 == null)
                {
                    return false;
                }

                Solid solid2 = GetElementSolid(element2);

                if (solid2 == null)
                {
                    return false;
                }

                Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Intersect);
                if (intersection != null && intersection.Volume != 0)
                {
                    return true;
                }
                else
                {
                    return false;

                }

            }
            catch { return false; }
        }

        public static bool ElementIntersectSolid(Element element1, Solid solid2)
        {
            Options opt = new Options();
            opt.IncludeNonVisibleObjects = true;
            opt.ComputeReferences = true;

            try
            {
                Solid solid1 = GetElementSolid(element1);

                if (solid1 == null)
                {
                    return false;
                }

                if (solid2 == null)
                {
                    return false;
                }

                Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Intersect);
                if (intersection != null && intersection.Volume != 0)
                {
                    return true;
                }
                else
                {
                    return false;

                }

            }
            catch { return false; }
        }

        public static bool SolidIntersectSolid(Solid solid1, Solid solid2)
        {
            Options opt = new Options();
            opt.IncludeNonVisibleObjects = true;
            opt.ComputeReferences = true;

            try
            {
                if (solid1 == null)
                {
                    return false;
                }

                if (solid2 == null)
                {
                    return false;
                }

                Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Intersect);
                if (intersection != null && intersection.Volume > 10)
                {
                    return true;
                }
                else
                {
                    return false;

                }

            }
            catch { return false; }
        }


        public static CurveArray AddFloorTopFacesCurvesToCurveArray(Document doc, CurveArray curveArray, IEnumerable<Element> floors)
        {
            foreach (var floor in floors)
            {
                Options opt = new Options();
                opt.View = doc.ActiveView;
                opt.IncludeNonVisibleObjects = true;

                GeometryElement geoEle = floor.get_Geometry(opt);
                foreach (GeometryObject geoObj in geoEle)
                {
                    if (geoObj is Solid)
                    {
                        Solid solid = geoObj as Solid;
                        FaceArray solidFaces = solid.Faces;
                        foreach (Face face in solidFaces)
                        {
                            if (face is PlanarFace)
                            {
                                if ((face as PlanarFace).FaceNormal.Z > 0.5)
                                {
                                    EdgeArrayArray edgeArrays = face.EdgeLoops;
                                    foreach (EdgeArray edgeArray in edgeArrays)
                                    {
                                        foreach (Edge edge in edgeArray)
                                        {
                                            curveArray.Append(edge.AsCurve());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return curveArray;
        }

        public static CurveArray AddJointCurvesToCurveArray(Document doc, CurveArray curveArray, IEnumerable<Element> joints)
        {
            foreach (var joint in joints)
            {
                Curve curve = (joint.Location as LocationCurve).Curve;
                curveArray.Append(curve);
            }
            return curveArray;
        }

        public static List<Curve> SplitCurveByOtherCurves(Curve curveToSplit, List<Curve> curvesToIntersect)
        {
            List<Curve> splitFloorCurves = new List<Curve>();
            SortedDictionary<double, XYZ> intersectionPoints = new SortedDictionary<double, XYZ>();
            foreach (var otherCurve in curvesToIntersect)
            {
                if (
                    !curveToSplit.GetEndPoint(0).IsAlmostEqualTo(otherCurve.GetEndPoint(0)) &&
                    !curveToSplit.GetEndPoint(0).IsAlmostEqualTo(otherCurve.GetEndPoint(1)) &&
                    !curveToSplit.GetEndPoint(1).IsAlmostEqualTo(otherCurve.GetEndPoint(0)) &&
                    !curveToSplit.GetEndPoint(1).IsAlmostEqualTo(otherCurve.GetEndPoint(1))
                    )
                {
                    curveToSplit.Intersect(otherCurve, out IntersectionResultArray array);
                    if (array != null)
                    {
                        XYZ intersectionPoint = array.get_Item(0).XYZPoint;

                        double parameter = curveToSplit.Project(intersectionPoint).Parameter;
                        try
                        {
                            intersectionPoints.Add(parameter, intersectionPoint);
                        }
                        catch (Exception)
                        {

                        }

                    }
                    else
                    {
                        if (curveToSplit.Distance(otherCurve.GetEndPoint(0)) < 0.001)
                        {
                            IntersectionResult iResult = curveToSplit.Project(otherCurve.GetEndPoint(0));
                            if (!intersectionPoints.ContainsKey(iResult.Parameter))
                            {
                                intersectionPoints.Add(iResult.Parameter, iResult.XYZPoint);
                            }
                        }
                        if (curveToSplit.Distance(otherCurve.GetEndPoint(1)) < 0.001)
                        {
                            IntersectionResult iResult = curveToSplit.Project(otherCurve.GetEndPoint(1));
                            if (!intersectionPoints.ContainsKey(iResult.Parameter))
                            {
                                intersectionPoints.Add(iResult.Parameter, iResult.XYZPoint);
                            }
                        }
                    }

                }

            }

            if (intersectionPoints.Count() != 0)
            {

                if (curveToSplit is Line)
                {
                    try
                    {
                        splitFloorCurves.Add(Line.CreateBound(curveToSplit.GetEndPoint(0), curveToSplit.Evaluate(intersectionPoints.First().Key, false)));
                    }
                    catch (Exception) { }
                    try
                    {
                        splitFloorCurves.Add(Line.CreateBound(curveToSplit.Evaluate(intersectionPoints.Last().Key, false), curveToSplit.GetEndPoint(1)));
                    }
                    catch (Exception) { }
                    if (intersectionPoints.Count() > 1)
                    {
                        for (int i = 0; i < intersectionPoints.Count - 1; i++)
                        {
                            try
                            {
                                splitFloorCurves.Add(Line.CreateBound(curveToSplit.Evaluate(intersectionPoints.ElementAt(i).Key, false), curveToSplit.Evaluate(intersectionPoints.ElementAt(i + 1).Key, false)));

                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
                else
                {
                    if (intersectionPoints.Count() == 1)
                    {
                        double midParameter0 = (curveToSplit.GetEndParameter(0) + intersectionPoints.First().Key) / 2;
                        double normalizedMidParameter0 = curveToSplit.ComputeNormalizedParameter(midParameter0);
                        double midParameter1 = (curveToSplit.GetEndParameter(1) + intersectionPoints.Last().Key) / 2;
                        double normalizedMidParameter1 = curveToSplit.ComputeNormalizedParameter(midParameter1);
                        splitFloorCurves.Add(Arc.Create(curveToSplit.GetEndPoint(0), curveToSplit.Evaluate(intersectionPoints.First().Key, false), curveToSplit.Evaluate(normalizedMidParameter0, true)));
                        splitFloorCurves.Add(Arc.Create(curveToSplit.Evaluate(intersectionPoints.Last().Key, false), curveToSplit.GetEndPoint(1), curveToSplit.Evaluate(normalizedMidParameter1, true)));
                    }
                    if (intersectionPoints.Count() > 1)
                    {
                        for (int i = 0; i < intersectionPoints.Count - 1; i++)
                        {
                            try
                            {
                                splitFloorCurves.Add(Arc.Create(curveToSplit.Evaluate(intersectionPoints.ElementAt(i).Key, false), curveToSplit.Evaluate(intersectionPoints.ElementAt(i + 1).Key, false), curveToSplit.Evaluate((intersectionPoints.ElementAt(i + 1).Key + intersectionPoints.ElementAt(i).Key) / 2, false)));
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }
            else
            {
                splitFloorCurves.Add(curveToSplit);
            }
            return splitFloorCurves;
        }

        public static double GetTelaSize(string tela)
        {
            Dictionary<string, double> dict = new Dictionary<string, double>();
            dict.Add("Q113", 1);
            dict.Add("Q138", 1);
            dict.Add("Q159", 1);
            dict.Add("Q196", 1);
            dict.Add("Q246", 1);
            dict.Add("Q283", 1);
            dict.Add("Q335", 2);
            dict.Add("Q396", 2);
            dict.Add("Q503", 2);
            dict.Add("Q636", 2);
            dict.Add("Q785", 2);
            return dict[tela];
        }

        public static bool VerifyIfProjectParameterExists(Document doc, string name)
        {
            BindingMap map = doc.ParameterBindings;
            DefinitionBindingMapIterator it
              = map.ForwardIterator();
            it.Reset();
            while (it.MoveNext())
            {
                string paramName = it.Key.Name;
                if (paramName == name)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<Curve> SplitCurvesByOtherCurves(List<Curve> curvesToSplit, List<Curve> curvesToIntersect)
        {
            List<Curve> splitFloorCurves = new List<Curve>();
            foreach (var curveToSplit in curvesToSplit)
            {
                splitFloorCurves.AddRange(SplitCurveByOtherCurves(curveToSplit, curvesToIntersect));
            }
            return splitFloorCurves;
        }

        public static List<Curve> GetFloorOpeningsCurves(Floor floor)
        {
            List<Curve> floorOpeningCurves = new List<Curve>();
            SolidCurveIntersectionOptions solidOptions = new SolidCurveIntersectionOptions();
            List<Opening> openings = new FilteredElementCollector(floor.Document)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_FloorOpening)
                        .Cast<Opening>()
                        .Where(a => a.Host.Id == floor.Id)
                        .ToList();
            List<CurveLoop> openingsCurveLoops = new List<CurveLoop>();

            List<Curve> openingCurves = new List<Curve>();
            foreach (var opening in openings)
            {
                foreach (Curve curve in opening.BoundaryCurves)
                {
                    openingCurves.Add(curve);
                }
            }

            return openingCurves;
        }

        public static List<CurveLoop> GetFloorOpeningsCurveLoops(Floor floor)
        {
            List<Curve> floorOpeningCurves = new List<Curve>();
            SolidCurveIntersectionOptions solidOptions = new SolidCurveIntersectionOptions();
            List<Opening> openings = new FilteredElementCollector(floor.Document)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_FloorOpening)
                        .Cast<Opening>()
                        .Where(a => a.Host.Id == floor.Id)
                        .ToList();
            List<CurveLoop> openingsCurveLoops = new List<CurveLoop>();

            List<Curve> openingCurves = new List<Curve>();
            foreach (var opening in openings)
            {
                foreach (Curve curve in opening.BoundaryCurves)
                {
                    openingCurves.Add(GetCurveProjection(curve));
                }

            }

            return OrderCurvesToCurveLoops(openingCurves);
        }

        public static void GetSideFloorOpeningsCurveLoops(Floor floor, Solid solid, out List<CurveLoop> insideCurveLoops, out List<CurveLoop> sideCurveLoops)
        {
            List<CurveLoop> openingsCurveLoops = GetFloorOpeningsCurveLoops(floor);
            sideCurveLoops = new List<CurveLoop>();
            insideCurveLoops = new List<CurveLoop>();

            foreach (var loop in openingsCurveLoops)
            {
                Solid openingSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { loop }, XYZ.BasisZ, 10000);
                if (BooleanOperationsUtils.ExecuteBooleanOperation(solid, openingSolid, BooleanOperationsType.Intersect).Volume != openingSolid.Volume)
                {
                    sideCurveLoops.Add(loop);
                }
                else
                {
                    insideCurveLoops.Add(loop);
                }
            }
        }

        public static void GetSideFloorOpeningsCurveLoops2(List<Curve> floorExternalCurves, List<CurveLoop> openingsCurveLoops, out List<CurveLoop> insideCurveLoops, out List<CurveLoop> sideCurveLoops)
        {
            sideCurveLoops = new List<CurveLoop>();
            insideCurveLoops = new List<CurveLoop>();

            foreach (var loop in openingsCurveLoops)
            {
                foreach (var openingCurve in loop)
                {
                    foreach (var floorExternalCurve in floorExternalCurves)
                    {
                        if ((openingCurve.Intersect(floorExternalCurve) != SetComparisonResult.Disjoint && !IsCurvesEqual(openingCurve, floorExternalCurve)) ||
                            openingCurve.Distance(floorExternalCurve.GetEndPoint(0)) < 0.01 ||
                            openingCurve.Distance(floorExternalCurve.GetEndPoint(1)) < 0.01 ||
                            floorExternalCurve.Distance(openingCurve.GetEndPoint(0)) < 0.01 ||
                            floorExternalCurve.Distance(openingCurve.GetEndPoint(0)) < 0.01)
                        {
                            sideCurveLoops.Add(loop);
                            goto NextLoop;
                        }
                    }
                }
                insideCurveLoops.Add(loop);
            NextLoop:;
            }
        }

        //public static void GetSideFloorOpeningsCurveLoops3(List<CurveLoop> floorCurveLoops, List<CurveLoop> openingsCurveLoops, out List<CurveLoop> insideCurveLoops, out List<CurveLoop> sideCurveLoops)
        //{
        //    Solid floorSolid = GeometryCreationUtilities.CreateExtrusionGeometry(floorCurveLoops,XYZ.)

        //    foreach (var loop in openingsCurveLoops)
        //    {
        //        foreach (var openingCurve in loop)
        //        {
        //            foreach (var floorExternalCurve in floorExternalCurves)
        //            {
        //                if (openingCurve.Intersect(floorExternalCurve) != SetComparisonResult.Disjoint && !IsCurvesEqual(openingCurve, floorExternalCurve))
        //                {
        //                    sideCurveLoops.Add(loop);
        //                    goto NextLoop;
        //                }
        //            }
        //        }
        //        insideCurveLoops.Add(loop);
        //    NextLoop:;
        //    }
        //}

        public static List<CurveLoop> GetSideOpeningCurveLoops(Document doc, Floor floor)
        {
            List<Curve> originalFloorCurves = new List<Curve>();
            ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);

            // GET FLOOR CURVES

            List<CurveLoop> openingsCurveLoops = GetFloorOpeningsCurveLoops(floor);


            foreach (var item in floor.GetDependentElements(linesFilter))
            {
#if Revit2021 || Revit2022 || Revit2023
                int categoryId = doc.GetElement(item).Category.Id.IntegerValue;
#else
                int categoryId = (int)doc.GetElement(item).Category.Id.Value;
#endif
                if (categoryId == (int)BuiltInCategory.OST_SketchLines)
                {
                    bool boundaryLine = false;
                    foreach (Parameter parameter in (doc.GetElement(item) as CurveElement).Parameters)
                    {
                        if ((parameter.Definition as InternalDefinition).BuiltInParameter == BuiltInParameter.CURVE_IS_SLOPE_DEFINING)
                        {
                            boundaryLine = true;
                        }
                    }

                    if (!boundaryLine)
                    {
                        continue;
                    }
                    Curve curve = null;
                    if (doc.GetElement(item) is ModelArc)
                    {
                        curve = (doc.GetElement(item) as ModelArc).GeometryCurve;
                    }
                    else
                    {
                        curve = (doc.GetElement(item) as ModelLine).GeometryCurve;
                    }

                    if (curve.IsBound)
                    {
                        foreach (var curveloop in openingsCurveLoops)
                        {
                            foreach (var openingCurve in curveloop)
                            {
                                if (openingCurve.IsBound)
                                {
                                    if (curve.GetEndPoint(0).IsAlmostEqualTo(openingCurve.GetEndPoint(0)) && curve.GetEndPoint(1).IsAlmostEqualTo(openingCurve.GetEndPoint(1)))
                                    {
                                        goto continueToNextCurve;
                                    }
                                }
                            }
                        }
                        originalFloorCurves.Add(Utils.GetCurveProjection(curve));
                    }
                    else
                    {
                        foreach (var curveloop in openingsCurveLoops)
                        {
                            foreach (var openingCurve in curveloop)
                            {
                                if (openingCurve is Arc)
                                {
                                    if ((curve as Arc).Center.DistanceTo((openingCurve as Arc).Center) > 0.001 && Math.Abs((curve as Arc).Radius - (openingCurve as Arc).Radius) > 0.01)
                                    {
                                        goto continueToNextCurve;
                                    }
                                }
                            }
                        }
                        List<Curve> halfArcs = Utils.GetUnBoundArcHalfProjections(curve);
                        originalFloorCurves.Add(halfArcs[0]);
                        originalFloorCurves.Add(halfArcs[1]);

                    }

                //originalFloorCurves.Add(Utils.GetCurveProjection(curve));
                continueToNextCurve:;
                }
            }
            List<CurveLoop> curveLoops = Utils.OrderCurvesToCurveLoops(originalFloorCurves);
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoops.Last() }, XYZ.BasisZ, 10000);
            List<CurveLoop> sideOpeningCurveLoops = new List<CurveLoop>();
            List<CurveLoop> insideOpeningCurveLoops = new List<CurveLoop>();
            //GetSideFloorOpeningsCurveLoops(floor, solid, out insideOpeningCurveLoops, out sideOpeningCurveLoops);
            GetSideFloorOpeningsCurveLoops2(originalFloorCurves, openingsCurveLoops, out insideOpeningCurveLoops, out sideOpeningCurveLoops);
            return sideOpeningCurveLoops;
        }

        public static List<Curve> GetFloorLinesWithoutSideOpenings(Document doc, Floor floor, out List<CurveLoop> insideOpeningCurveLoops)
        {
            List<Curve> resultLines = new List<Curve>();
            List<Curve> originalFloorCurves = new List<Curve>();
            ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);

            // GET FLOOR CURVES

            List<CurveLoop> openingsCurveLoops = GetFloorOpeningsCurveLoops(floor);


            foreach (var item in floor.GetDependentElements(linesFilter))
            {
#if Revit2021 || Revit2022 || Revit2023
                int categoryId = doc.GetElement(item).Category.Id.IntegerValue;
#else
                int categoryId = (int)doc.GetElement(item).Category.Id.Value;
#endif
                if (categoryId == (int)BuiltInCategory.OST_SketchLines)
                {
                    bool boundaryLine = false;
                    foreach (Parameter parameter in (doc.GetElement(item) as CurveElement).Parameters)
                    {
                        if ((parameter.Definition as InternalDefinition).BuiltInParameter == BuiltInParameter.CURVE_IS_SLOPE_DEFINING)
                        {
                            boundaryLine = true;
                        }
                    }

                    if (!boundaryLine)
                    {
                        continue;
                    }
                    Curve curve = null;
                    if (doc.GetElement(item) is ModelArc)
                    {
                        curve = (doc.GetElement(item) as ModelArc).GeometryCurve;
                    }
                    else
                    {
                        curve = (doc.GetElement(item) as ModelLine).GeometryCurve;
                    }

                    if (curve.IsBound)
                    {
                        foreach (var curveloop in openingsCurveLoops)
                        {
                            foreach (var openingCurve in curveloop)
                            {
                                if (openingCurve.IsBound)
                                {
                                    if (curve.GetEndPoint(0).IsAlmostEqualTo(openingCurve.GetEndPoint(0)) && curve.GetEndPoint(1).IsAlmostEqualTo(openingCurve.GetEndPoint(1)))
                                    {
                                        goto continueToNextCurve;
                                    }
                                }
                            }
                        }
                        originalFloorCurves.Add(Utils.GetCurveProjection(curve));
                    }
                    else
                    {
                        foreach (var curveloop in openingsCurveLoops)
                        {
                            foreach (var openingCurve in curveloop)
                            {
                                if (openingCurve is Arc)
                                {
                                    if ((curve as Arc).Center.DistanceTo((openingCurve as Arc).Center) > 0.001 && Math.Abs((curve as Arc).Radius - (openingCurve as Arc).Radius) > 0.01)
                                    {
                                        goto continueToNextCurve;
                                    }
                                }
                            }
                        }
                        List<Curve> halfArcs = Utils.GetUnBoundArcHalfProjections(curve);
                        originalFloorCurves.Add(halfArcs[0]);
                        originalFloorCurves.Add(halfArcs[1]);

                    }

                //originalFloorCurves.Add(Utils.GetCurveProjection(curve));
                continueToNextCurve:;
                }
            }
            List<CurveLoop> curveLoops = Utils.OrderCurvesToCurveLoops(originalFloorCurves);
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoops.Last() }, XYZ.BasisZ, 10000);
            List<CurveLoop> sideOpeningCurveLoops = new List<CurveLoop>();
            //GetSideFloorOpeningsCurveLoops(floor, solid, out insideOpeningCurveLoops, out sideOpeningCurveLoops);
            GetSideFloorOpeningsCurveLoops2(originalFloorCurves, openingsCurveLoops, out insideOpeningCurveLoops, out sideOpeningCurveLoops);
            if (insideOpeningCurveLoops.Count == 0 && sideOpeningCurveLoops.Count == 0)
            {
                return originalFloorCurves;
            }
            foreach (var curveLoop in insideOpeningCurveLoops)
            {
                foreach (var curve in curveLoop)
                {
                    resultLines.Add(curve);
                }
            }
            List<Curve> openingSideCurves = new List<Curve>();
            foreach (var curveLoop in sideOpeningCurveLoops)
            {
                foreach (var curve in curveLoop)
                {
                    openingSideCurves.Add(GetCurveProjection(curve));
                }
            }

            SolidCurveIntersectionOptions solidOptions = new SolidCurveIntersectionOptions();
            List<Curve> externalCurves = new List<Curve>();
            foreach (var curve in curveLoops.Last())
            {
                externalCurves.Add(curve);
            }

            // SPLIT OPENINGS CURVES IN INTERSECTIONS

            List<Curve> splitOpeningCurves = SplitCurvesByOtherCurves(openingSideCurves, originalFloorCurves);
            List<Curve> openingCurvesToAddToExternalLoop = new List<Curve>();
            foreach (var curve in splitOpeningCurves)
            {
                Curve curve0 = Line.CreateBound(curve.GetEndPoint(0), curve.GetEndPoint(0) + XYZ.BasisZ * 10000);
                Curve curve1 = Line.CreateBound(curve.GetEndPoint(1), curve.GetEndPoint(1) + XYZ.BasisZ * 10000);
                Curve midCurve = Line.CreateBound(curve.Evaluate(0.5, true), curve.Evaluate(0.5, true) + XYZ.BasisZ * 10000);
                if (!(solid.IntersectWithCurve(curve0, solidOptions).SegmentCount == 0 || solid.IntersectWithCurve(curve1, solidOptions).SegmentCount == 0 || solid.IntersectWithCurve(midCurve, solidOptions).SegmentCount == 0))
                {
                    bool point0OnBoundary = false;
                    bool point1OnBoundary = false;
                    bool pointMidOnBoundary = false;
                    foreach (var floorCurve in externalCurves)
                    {
                        if (floorCurve.Distance(curve.GetEndPoint(0)) < 0.01)
                        {
                            point0OnBoundary = true;
                        }
                        if (floorCurve.Distance(curve.Evaluate(0.5, true)) < 0.01)
                        {
                            pointMidOnBoundary = true;
                        }
                        if (floorCurve.Distance(curve.GetEndPoint(1)) < 0.01)
                        {
                            point1OnBoundary = true;
                        }
                        if (point0OnBoundary && point1OnBoundary && pointMidOnBoundary)
                        {
                            break;
                        }
                    }
                    if (!(point0OnBoundary && point1OnBoundary && pointMidOnBoundary))
                    {
                        openingCurvesToAddToExternalLoop.Add(curve);
                    }
                }
            }

            // GET BOUNDARY CURVES THAT ARENT INSIDE THE OPENING SOLIDS

            List<Curve> externalSplitCurves = SplitCurvesByOtherCurves(externalCurves, openingCurvesToAddToExternalLoop);



            Solid openingsSolid = null;
            foreach (var curveloop in openingsCurveLoops)
            {
                try
                {
                    Solid solid1 = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveloop }, XYZ.BasisZ, 10000);
                    if (openingsSolid == null)
                    {
                        openingsSolid = solid1;
                    }
                    else
                    {
                        openingsSolid = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, openingsSolid, BooleanOperationsType.Union);
                    }
                }
                catch (Exception)
                {
                    //Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                    //Transaction tx1 = new Transaction(doc, "aaa");
                    //tx1.Start("aaa");
                    //SketchPlane sPlane = SketchPlane.Create(doc, plane);
                    //foreach (var curve in curveloop)
                    //{
                    //    doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
                    //}
                    //tx1.Commit();

                }

            }



            //Solid openingsSolid = GeometryCreationUtilities.CreateExtrusionGeometry(openingsCurveLoops, XYZ.BasisZ, 10000);

            List<Curve> cleanExternalCurves = new List<Curve>();
            foreach (var curve in externalSplitCurves)
            {
                Curve midCurve = Line.CreateBound(curve.Evaluate(0.5, true), curve.Evaluate(0.5, true) + XYZ.BasisZ * 10000);
                if (openingsSolid.IntersectWithCurve(midCurve, solidOptions).SegmentCount == 0)
                {
                    openingCurvesToAddToExternalLoop.Add(curve);
                }
            }

            foreach (var curve in openingCurvesToAddToExternalLoop)
            {
                resultLines.Add(curve);
            }

            //Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
            //Transaction tx1 = new Transaction(doc, "aaa");
            //tx1.Start("aaa");
            //SketchPlane sPlane = SketchPlane.Create(doc, plane);
            //foreach (var curve in externalSplitCurves)
            //{
            //    doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
            //}
            //tx1.Commit();

            return resultLines;
        }

        public static List<Curve> GetFloorLinesWithoutSideOpeningsForJoint(Document doc, Floor floor, out List<CurveLoop> insideOpeningCurveLoops)
        {
            List<Curve> resultLines = new List<Curve>();
            List<Curve> originalFloorCurves = new List<Curve>();
            ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);

            // GET FLOOR CURVES

            List<CurveLoop> openingsCurveLoops = GetFloorOpeningsCurveLoops(floor);


            foreach (var item in floor.GetDependentElements(linesFilter))
            {
#if Revit2021 || Revit2022 || Revit2023
                int categoryId = doc.GetElement(item).Category.Id.IntegerValue;
#else
                int categoryId = (int)doc.GetElement(item).Category.Id.Value;
#endif
                if (categoryId == (int)BuiltInCategory.OST_SketchLines)
                {
                    bool boundaryLine = false;
                    foreach (Parameter parameter in (doc.GetElement(item) as CurveElement).Parameters)
                    {
                        if ((parameter.Definition as InternalDefinition).BuiltInParameter == BuiltInParameter.CURVE_IS_SLOPE_DEFINING)
                        {
                            boundaryLine = true;
                        }
                    }

                    if (!boundaryLine)
                    {
                        continue;
                    }
                    Curve curve = null;
                    if (doc.GetElement(item) is ModelArc)
                    {
                        curve = (doc.GetElement(item) as ModelArc).GeometryCurve;
                    }
                    else
                    {
                        curve = (doc.GetElement(item) as ModelLine).GeometryCurve;
                    }

                    if (curve.IsBound)
                    {
                        foreach (var curveloop in openingsCurveLoops)
                        {
                            foreach (var openingCurve in curveloop)
                            {
                                if (openingCurve.IsBound)
                                {
                                    if (curve.GetEndPoint(0).IsAlmostEqualTo(openingCurve.GetEndPoint(0)) && curve.GetEndPoint(1).IsAlmostEqualTo(openingCurve.GetEndPoint(1)))
                                    {
                                        goto continueToNextCurve;
                                    }
                                }
                            }
                        }
                        originalFloorCurves.Add(Utils.GetCurveProjection(curve));
                    }
                    else
                    {
                        foreach (var curveloop in openingsCurveLoops)
                        {
                            foreach (var openingCurve in curveloop)
                            {
                                if (openingCurve is Arc)
                                {
                                    if ((curve as Arc).Center.DistanceTo((openingCurve as Arc).Center) > 0.001 && Math.Abs((curve as Arc).Radius - (openingCurve as Arc).Radius) > 0.01)
                                    {
                                        goto continueToNextCurve;
                                    }
                                }
                            }
                        }
                        List<Curve> halfArcs = Utils.GetUnBoundArcHalfProjections(curve);
                        originalFloorCurves.Add(halfArcs[0]);
                        originalFloorCurves.Add(halfArcs[1]);

                    }

                //originalFloorCurves.Add(Utils.GetCurveProjection(curve));
                continueToNextCurve:;
                }
            }
            List<CurveLoop> curveLoops = Utils.OrderCurvesToCurveLoops(originalFloorCurves);
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(curveLoops, XYZ.BasisZ, 10000);
            List<CurveLoop> sideOpeningCurveLoops = new List<CurveLoop>();
            //GetSideFloorOpeningsCurveLoops(floor, solid, out insideOpeningCurveLoops, out sideOpeningCurveLoops);
            GetSideFloorOpeningsCurveLoops2(originalFloorCurves, openingsCurveLoops, out insideOpeningCurveLoops, out sideOpeningCurveLoops);
            if (insideOpeningCurveLoops.Count == 0 && sideOpeningCurveLoops.Count == 0)
            {
                return originalFloorCurves;
            }
            foreach (var curveLoop in insideOpeningCurveLoops)
            {
                foreach (var curve in curveLoop)
                {
                    resultLines.Add(curve);
                }
            }
            List<Curve> openingSideCurves = new List<Curve>();
            foreach (var curveLoop in sideOpeningCurveLoops)
            {
                foreach (var curve in curveLoop)
                {
                    openingSideCurves.Add(GetCurveProjection(curve));
                }
            }

            SolidCurveIntersectionOptions solidOptions = new SolidCurveIntersectionOptions();
            List<Curve> externalCurves = new List<Curve>();
            foreach (var curveLoop in curveLoops)
            {
                foreach (var curve in curveLoop)
                {
                    externalCurves.Add(curve);
                }
            }


            // SPLIT OPENINGS CURVES IN INTERSECTIONS

            List<Curve> splitOpeningCurves = SplitCurvesByOtherCurves(openingSideCurves, originalFloorCurves);
            List<Curve> openingCurvesToAddToExternalLoop = new List<Curve>();
            foreach (var curve in splitOpeningCurves)
            {
                Curve curve0 = Line.CreateBound(curve.GetEndPoint(0), curve.GetEndPoint(0) + XYZ.BasisZ * 10000);
                Curve curve1 = Line.CreateBound(curve.GetEndPoint(1), curve.GetEndPoint(1) + XYZ.BasisZ * 10000);
                Curve midCurve = Line.CreateBound(curve.Evaluate(0.5, true), curve.Evaluate(0.5, true) + XYZ.BasisZ * 10000);
                if (!(solid.IntersectWithCurve(curve0, solidOptions).SegmentCount == 0 || solid.IntersectWithCurve(curve1, solidOptions).SegmentCount == 0 || solid.IntersectWithCurve(midCurve, solidOptions).SegmentCount == 0))
                {
                    bool point0OnBoundary = false;
                    bool point1OnBoundary = false;
                    bool pointMidOnBoundary = false;
                    foreach (var floorCurve in externalCurves)
                    {
                        if (floorCurve.Distance(curve.GetEndPoint(0)) < 0.01)
                        {
                            point0OnBoundary = true;
                        }
                        if (floorCurve.Distance(curve.Evaluate(0.5, true)) < 0.01)
                        {
                            pointMidOnBoundary = true;
                        }
                        if (floorCurve.Distance(curve.GetEndPoint(1)) < 0.01)
                        {
                            point1OnBoundary = true;
                        }
                        if (point0OnBoundary && point1OnBoundary && pointMidOnBoundary)
                        {
                            break;
                        }
                    }
                    if (!(point0OnBoundary && point1OnBoundary && pointMidOnBoundary))
                    {
                        openingCurvesToAddToExternalLoop.Add(curve);
                    }
                }
            }

            // GET BOUNDARY CURVES THAT ARENT INSIDE THE OPENING SOLIDS

            List<Curve> externalSplitCurves = SplitCurvesByOtherCurves(externalCurves, openingCurvesToAddToExternalLoop);
            Solid openingsSolid = GeometryCreationUtilities.CreateExtrusionGeometry(openingsCurveLoops, XYZ.BasisZ, 10000);
            List<Curve> cleanExternalCurves = new List<Curve>();
            foreach (var curve in externalSplitCurves)
            {
                Curve midCurve = Line.CreateBound(curve.Evaluate(0.5, true), curve.Evaluate(0.5, true) + XYZ.BasisZ * 10000);
                if (openingsSolid.IntersectWithCurve(midCurve, solidOptions).SegmentCount == 0)
                {
                    openingCurvesToAddToExternalLoop.Add(curve);
                }
            }

            foreach (var curve in openingCurvesToAddToExternalLoop)
            {
                resultLines.Add(curve);
            }

            //Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
            //Transaction tx1 = new Transaction(doc, "aaa");
            //tx1.Start("aaa");
            //SketchPlane sPlane = SketchPlane.Create(doc, plane);
            //foreach (var curve in openingCurvesToAddToExternalLoop)
            //{
            //    doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
            //}
            ////    foreach (var curve in splitjointCurves)
            ////    {
            ////        doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
            ////    }
            ////    //foreach (var curve in notBorderJoints.Select(a => (a.Location as LocationCurve).Curve))
            ////    //{
            ////    //    doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
            ////    //}
            ////    //foreach (var curveArray in originalCurveArrays)
            ////    //{
            ////    //    foreach (Curve curve1 in curveArray)
            ////    //    {
            ////    //        doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve1, plane), sPlane);
            ////    //    }
            ////    //}
            ////    //foreach (var curve in splitjointCurves)
            ////    //{
            ////    //    doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
            ////    //}
            //tx1.Commit();

            return resultLines;
        }

        public static bool IsCurvesEqual(Curve curve1, Curve curve2)
        {
            if (curve1 is Line && curve2 is Line)
            {
                if (
                    (curve1.GetEndPoint(0).DistanceTo(curve2.GetEndPoint(0)) < 0.1 && curve1.GetEndPoint(1).DistanceTo(curve2.GetEndPoint(1)) < 0.1) ||
                    (curve1.GetEndPoint(1).DistanceTo(curve2.GetEndPoint(0)) < 0.1 && curve1.GetEndPoint(0).DistanceTo(curve2.GetEndPoint(1)) < 0.1)
                    )
                {
                    return true;
                }
            }
            if (curve1 is Arc && curve2 is Arc)
            {
                if (curve1.IsBound && curve2.IsBound)
                {
                    if (
                    (curve1.GetEndPoint(0).IsAlmostEqualTo(curve2.GetEndPoint(0)) && curve1.GetEndPoint(1).IsAlmostEqualTo(curve2.GetEndPoint(1)) && curve1.Evaluate(0.5, true).IsAlmostEqualTo(curve2.Evaluate(0.5, true))) ||
                    (curve1.GetEndPoint(1).IsAlmostEqualTo(curve2.GetEndPoint(0)) && curve1.GetEndPoint(0).IsAlmostEqualTo(curve2.GetEndPoint(1)) && curve1.Evaluate(0.5, true).IsAlmostEqualTo(curve2.Evaluate(0.5, true)))
                    )
                    {
                        return true;
                    }
                }
                if (!curve1.IsBound && !curve2.IsBound)
                {
                    if ((curve1 as Arc).Radius - (curve2 as Arc).Radius < 0.001 && (curve1 as Arc).Center.IsAlmostEqualTo((curve2 as Arc).Center))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsLineInsideOther(Curve smallCurve, Curve bigCurve)
        {
            if (smallCurve is Line && bigCurve is Line)
            {
                if (bigCurve.Distance(smallCurve.GetEndPoint(0)) < 0.01 && bigCurve.Distance(smallCurve.GetEndPoint(1)) < 0.01) 
                {
                    return true;
                }
            }

            return false;
        }


        public static bool DoesCurvesHaveSameEndPoints(Curve curve1, Curve curve2)
        {
            if (
                (curve1.GetEndPoint(0).DistanceTo(curve2.GetEndPoint(0)) < 0.1 && curve1.GetEndPoint(1).DistanceTo(curve2.GetEndPoint(1)) < 0.1) ||
                (curve1.GetEndPoint(1).DistanceTo(curve2.GetEndPoint(0)) < 0.1 && curve1.GetEndPoint(0).DistanceTo(curve2.GetEndPoint(1)) < 0.1)
                )
            {
                return true;
            }

            return false;
        }

        public static List<Floor> DivideFloorsWithMultipleLoops(List<Floor> floors)
        {
            List<Floor> dividedFloors = new List<Floor>();
            Transaction txDivide = new Transaction(floors[0].Document, "DivideFloorWithLoops");
            FailureHandlingOptions failOpt = txDivide.GetFailureHandlingOptions();

            ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);
            foreach (var floor in floors)
            {
                List<Curve> floorBoundaryCurves = new List<Curve>();
                foreach (var item in floor.GetDependentElements(linesFilter))
                {
                    CurveElement cElement = floor.Document.GetElement(item) as CurveElement;
                    foreach (Parameter parameter in cElement.Parameters)
                    {
                        if ((parameter.Definition as InternalDefinition).BuiltInParameter == BuiltInParameter.CURVE_IS_SLOPE_DEFINING)
                        {
                            floorBoundaryCurves.Add(cElement.GeometryCurve);
                            break;
                        }
                    }
                }

                List<CurveLoop> boundaryLoops = OrderCurvesToCurveLoops(floorBoundaryCurves);
                if (boundaryLoops.Count > 1)
                {

                    foreach (CurveLoop curveLoop in boundaryLoops)
                    {
                        CurveArray cArray = new CurveArray();
                        foreach (var curve in curveLoop)
                        {
                            cArray.Append(curve);
                        }
                        failOpt = txDivide.GetFailureHandlingOptions();
                        failOpt.SetFailuresPreprocessor(new FloorWarningsSwallower());
                        txDivide.SetFailureHandlingOptions(failOpt);
                        txDivide.Start();

#if Revit2021
                        Floor newFloor = floor.Document.Create.NewFloor(cArray, floor.FloorType, floor.Document.GetElement(floor.LevelId) as Level, true);

#else
                        CurveLoop loop = Utils.ArrayToLoop(cArray);
                        Floor newFloor = Floor.Create(floor.Document, new List<CurveLoop>() { loop }, floor.FloorType.Id, floor.LevelId);
#endif



                        txDivide.Commit();
                        dividedFloors.Add(newFloor);
                        txDivide.Start();

                        CopyAllParameters(floor, newFloor, new List<BuiltInParameter>() { BuiltInParameter.ALL_MODEL_MARK });
                        txDivide.Commit();

                        if (floor.get_Parameter(BuiltInParameter.ROOF_SLOPE).AsDouble() != 0)
                        {
                            Utils.SetPointsElevationsOfNewFloor(floor as Floor, newFloor, 0);
                            continue;
                            //Utils.SetPreviousPointsElevationsToNewFloor(floor as Floor, newFloor);

                        }
#if Revit2021 || Revit2022 || Revit2023
                        SlabShapeEditor slabShapeEditor = floor.SlabShapeEditor;
#else
                        SlabShapeEditor slabShapeEditor = floor.GetSlabShapeEditor();
#endif
                        if (slabShapeEditor == null)
                        {
                            SetPointsElevationsOfNewFloor(floor as Floor, newFloor, 0);
                            continue;
                        }
                        else if (slabShapeEditor.IsEnabled)
                        {
                            double e = (floor.Document.GetElement(floor.LevelId) as Level).Elevation + floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble();
                            bool change = false;
                            foreach (SlabShapeVertex XYZ in slabShapeEditor.SlabShapeVertices)
                            {
                                if (e != XYZ.Position.Z)
                                {
                                    change = true;
                                    break;
                                }
                            }
                            if (change)
                            {
                                Utils.SetPointsElevationsOfNewFloor(floor as Floor, newFloor, 0);
                                continue;
                            }
                        }

                        //SetPointsElevationsOfNewFloor(floor, newFloor,0);

                        Solid floorSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop }, XYZ.BasisZ, 1000);
                        foreach (var openingCurveLoop in GetFloorOpeningsCurveLoops(floor))
                        {
                            Solid openingSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { openingCurveLoop }, XYZ.BasisZ, 1000);
                            if (BooleanOperationsUtils.ExecuteBooleanOperation(floorSolid, openingSolid, BooleanOperationsType.Intersect).Volume != 0)
                            {
                                CurveArray cArray2 = new CurveArray();
                                foreach (var curve in openingCurveLoop)
                                {
                                    cArray2.Append(curve);
                                }
                                txDivide.Start();
                                floor.Document.Create.NewOpening(newFloor, cArray2, false);
                                txDivide.Commit();
                            }
                        }
                    }
                    txDivide.Start();
                    floor.Document.Delete(floor.Id);
                    txDivide.Commit();
                }
                else
                {
                    dividedFloors.Add(floor);
                }
            }
            return dividedFloors;
        }

        //public static List<XYZ> VerifyJoints(string ambiente, List<Curve> floorCurves, List<CurveLoop> insideOpeningCurveLoops, List<Element> joints, out List<Element> correctJoints)
        //{
        //    correctJoints = new List<Element>();
        //    Document doc = joints[0].Document;
        //    ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);
        //    List<XYZ> errorPoints = new List<XYZ>();

        //    // GET ALL JOINTS AND FLOORS

        //    int numberOfErrors = 0;

        //    using (TransactionGroup tg = new TransactionGroup(doc))
        //    {
        //        tg.Start("Verificar Juntas");
        //        foreach (var joint in joints)
        //        {
        //            int jointId = joint.Id.IntegerValue;
        //            CurveAndProjection jointCurve = new CurveAndProjection((joint.Location as LocationCurve).Curve);
        //            XYZ direction = (jointCurve.Projection as Line).Direction;
        //            if ((Math.Abs(direction.X) < 0.002 && Math.Abs(direction.X) > 0.000001) || (Math.Abs(direction.Y) < 0.002 && Math.Abs(direction.Y) > 0.000001))
        //            {
        //                errorPoints.Add(jointCurve.Curve.Evaluate(0.5, true));
        //                numberOfErrors++;
        //                continue;
        //            }
        //            //Curve jointMiddlePointCurve = Line.CreateBound(EvaluateCurve(jointCurve.Curve, 0.5) + XYZ.BasisZ * 10000, Utils.EvaluateCurve(jointCurve.Curve, 0.5) - XYZ.BasisZ * 10000);

        //            //List<Curve> floorProjectionCurves = new List<Curve>();
        //            List<Curve> floorProjectionCurves = floorCurves.Select(a => GetCurveProjection(a)).ToList();


        //            //int numberOfIntersections = 0;
        //            //foreach (var curve in floorCurves.Select(a => GetCurveProjection(a)))
        //            //{
        //            //    floorProjectionCurves.Add(curve);
        //            //    if (jointCurve.Projection.Intersect(curve, out IntersectionResultArray array) != SetComparisonResult.Disjoint)
        //            //    {
        //            //        numberOfIntersections++;
        //            //    }
        //            //    //if (jointCurve.Projection.Intersect(curve, out IntersectionResultArray array) != SetComparisonResult.Disjoint)
        //            //    //{
        //            //    //    numberOfIntersections++;
        //            //    //}
        //            //}
        //            //CurveLoop floorProjectionCurveLoop = Utils.OrderCurvesToCurveLoops(floorProjectionCurves).Last();
        //            //Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { floorProjectionCurveLoop }, XYZ.BasisZ, 10000);
        //            //if (numberOfIntersections == 0)
        //            //{
        //            //    Curve curve0 = Line.CreateBound(jointCurve.Curve.GetEndPoint(0) + XYZ.BasisZ * 5000, jointCurve.Curve.GetEndPoint(0) - XYZ.BasisZ * 10000);
        //            //    Curve curve1 = Line.CreateBound(jointCurve.Curve.GetEndPoint(1) + XYZ.BasisZ * 5000, jointCurve.Curve.GetEndPoint(1) - XYZ.BasisZ * 10000);
        //            //    if (!(solid.IntersectWithCurve(curve0, new SolidCurveIntersectionOptions()).SegmentCount > 0 || solid.IntersectWithCurve(curve1, new SolidCurveIntersectionOptions()).SegmentCount > 0))
        //            //    {
        //            //        errorPoints.Add(jointCurve.Curve.GetEndPoint(0));
        //            //        errorPoints.Add(jointCurve.Curve.GetEndPoint(1));
        //            //        numberOfErrors++;
        //            //        numberOfErrors++;
        //            //    }
        //            //    else
        //            //    {
        //            //        correctJoints.Add(joint);
        //            //    }
        //            //}


        //            List<CurveLoop> floorProjectionCurveLoops = Utils.OrderCurvesToCurveLoops(floorProjectionCurves);
        //            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(floorProjectionCurveLoops, XYZ.BasisZ, 10000);
        //            Curve curveMid = Line.CreateBound(jointCurve.Curve.Evaluate(0.5, true) + XYZ.BasisZ * 5000, jointCurve.Curve.Evaluate(0.5, true) - XYZ.BasisZ * 10000);
        //            if (!(solid.IntersectWithCurve(curveMid, new SolidCurveIntersectionOptions()).SegmentCount > 0))
        //            {
        //                errorPoints.Add(jointCurve.Curve.Evaluate(0.5, true));
        //                numberOfErrors++;
        //            }
        //            else
        //            {

        //                //if (numberOfIntersections > 0)
        //                //{
        //                int numberOfIntersections0 = 0;
        //                int numberOfIntersections1 = 0;
        //                foreach (var curve in floorProjectionCurves)
        //                {
        //                    if (curve.Distance(jointCurve.Projection.GetEndPoint(0)) < 0.01)
        //                    {
        //                        numberOfIntersections0++;
        //                    }
        //                    if (curve.Distance(jointCurve.Projection.GetEndPoint(1)) < 0.01)
        //                    {
        //                        numberOfIntersections1++;
        //                    }
        //                }
        //                foreach (var joint2 in joints)
        //                {
        //                    if (joint2.Id.IntegerValue != joint.Id.IntegerValue)
        //                    {
        //                        CurveAndProjection jointCurve2 = new CurveAndProjection((joint2.Location as LocationCurve).Curve);
        //                        if (jointCurve2.Projection.Distance(jointCurve.Projection.GetEndPoint(0)) < 0.01)
        //                        {
        //                            numberOfIntersections0++;
        //                        }
        //                        if (jointCurve2.Projection.Distance(jointCurve.Projection.GetEndPoint(1)) < 0.01)
        //                        {
        //                            numberOfIntersections1++;
        //                        }
        //                    }
        //                }
        //                if (numberOfIntersections0 == 0)
        //                {
        //                    errorPoints.Add(jointCurve.Curve.GetEndPoint(0));
        //                    numberOfErrors++;
        //                }
        //                if (numberOfIntersections1 == 0)
        //                {
        //                    errorPoints.Add(jointCurve.Curve.GetEndPoint(1));
        //                    numberOfErrors++;
        //                }
        //                if (numberOfIntersections0 != 0 && numberOfIntersections1 != 0)
        //                {
        //                    correctJoints.Add(joint);
        //                }
        //            }
        //        }
        //        return errorPoints;
        //    }
        //}

        //public static Tuple<List<XYZ>, List<XYZ>> VerifyJoints2(string ambiente, List<Curve> floorCurves, List<CurveLoop> insideOpeningCurveLoops, List<Element> joints, out List<Element> correctJoints)
        //{
        //    correctJoints = new List<Element>();
        //    Document doc = joints[0].Document;
        //    ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);
        //    List<XYZ> errorPoints1 = new List<XYZ>();
        //    List<XYZ> errorPoints2 = new List<XYZ>();


        //    // GET ALL JOINTS AND FLOORS

        //    int numberOfErrors = 0;

        //    using (TransactionGroup tg = new TransactionGroup(doc))
        //    {
        //        tg.Start("Verificar Juntas");
        //        foreach (var joint in joints)
        //        {
        //            //int jointId = joint.Id.IntegerValue;
        //            CurveAndProjection jointCurve = new CurveAndProjection((joint.Location as LocationCurve).Curve);

        //            if (jointCurve.Curve is Arc)
        //            {

        //            }
        //            else
        //            {
        //                XYZ direction = (jointCurve.Projection as Line).Direction;
        //                if ((Math.Abs(direction.X) < 0.002 && Math.Abs(direction.X) > 0.000001) || (Math.Abs(direction.Y) < 0.002 && Math.Abs(direction.Y) > 0.000001))
        //                {
        //                    errorPoints2.Add(jointCurve.Curve.Evaluate(0.5, true));
        //                    numberOfErrors++;
        //                    continue;
        //                }
        //            }


        //            //foreach (var curveLoop in insideOpeningCurveLoops)
        //            //{
        //            //    foreach (var curve in curveLoop)
        //            //    {
        //            //        floorCurves.Add(curve);
        //            //    }
        //            //}

        //            //Curve jointMiddlePointCurve = Line.CreateBound(EvaluateCurve(jointCurve.Curve, 0.5) + XYZ.BasisZ * 10000, Utils.EvaluateCurve(jointCurve.Curve, 0.5) - XYZ.BasisZ * 10000);

        //            //List<Curve> floorProjectionCurves = new List<Curve>();
        //            List<Curve> floorProjectionCurves = floorCurves.Select(a => GetCurveProjection(a)).ToList();


        //            //int numberOfIntersections = 0;
        //            //foreach (var curve in floorCurves.Select(a => GetCurveProjection(a)))
        //            //{
        //            //    floorProjectionCurves.Add(curve);
        //            //    if (jointCurve.Projection.Intersect(curve, out IntersectionResultArray array) != SetComparisonResult.Disjoint)
        //            //    {
        //            //        numberOfIntersections++;
        //            //    }
        //            //    //if (jointCurve.Projection.Intersect(curve, out IntersectionResultArray array) != SetComparisonResult.Disjoint)
        //            //    //{
        //            //    //    numberOfIntersections++;
        //            //    //}
        //            //}
        //            //CurveLoop floorProjectionCurveLoop = Utils.OrderCurvesToCurveLoops(floorProjectionCurves).Last();
        //            //Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { floorProjectionCurveLoop }, XYZ.BasisZ, 10000);
        //            //if (numberOfIntersections == 0)
        //            //{
        //            //    Curve curve0 = Line.CreateBound(jointCurve.Curve.GetEndPoint(0) + XYZ.BasisZ * 5000, jointCurve.Curve.GetEndPoint(0) - XYZ.BasisZ * 10000);
        //            //    Curve curve1 = Line.CreateBound(jointCurve.Curve.GetEndPoint(1) + XYZ.BasisZ * 5000, jointCurve.Curve.GetEndPoint(1) - XYZ.BasisZ * 10000);
        //            //    if (!(solid.IntersectWithCurve(curve0, new SolidCurveIntersectionOptions()).SegmentCount > 0 || solid.IntersectWithCurve(curve1, new SolidCurveIntersectionOptions()).SegmentCount > 0))
        //            //    {
        //            //        errorPoints.Add(jointCurve.Curve.GetEndPoint(0));
        //            //        errorPoints.Add(jointCurve.Curve.GetEndPoint(1));
        //            //        numberOfErrors++;
        //            //        numberOfErrors++;
        //            //    }
        //            //    else
        //            //    {
        //            //        correctJoints.Add(joint);
        //            //    }
        //            //}

        //            //REMOVE DUPLICATES

        //            List<int> duplicateCurvesIndex = new List<int>();
        //            for (int i = 0; i < floorProjectionCurves.Count; i++)
        //            {
        //                Curve ci = floorProjectionCurves[i];
        //                XYZ epi0 = ci.GetEndPoint(0);
        //                XYZ epi1 = ci.GetEndPoint(1);
        //                XYZ mpi = ci.Evaluate(0.5, true);


        //                for (int j = 0; j < floorProjectionCurves.Count; j++)
        //                {
        //                    if (i != j)
        //                    {
        //                        Curve cj = floorProjectionCurves[j];
        //                        XYZ epj0 = cj.GetEndPoint(0);
        //                        XYZ epj1 = cj.GetEndPoint(1);
        //                        XYZ mpj = cj.Evaluate(0.5, true);

        //                        if ((mpi.DistanceTo(mpj) < 0.0001) && ((epi0.DistanceTo(epj0) < 0.0001 && epi1.DistanceTo(epj1) < 0.0001) || epi0.DistanceTo(epj1) < 0.0001 && epi1.DistanceTo(epj0) < 0.0001))
        //                        {
        //                            duplicateCurvesIndex.Add(i);
        //                            duplicateCurvesIndex.Add(j);
        //                        }
        //                    }
        //                }
        //            }

        //            List<Curve> curvesWithoutDuplicates = new List<Curve>();
        //            for (int i = 0; i < floorProjectionCurves.Count; i++)
        //            {
        //                if (!duplicateCurvesIndex.Contains(i))
        //                {
        //                    curvesWithoutDuplicates.Add(floorProjectionCurves[i]);
        //                }
        //            }


        //            //List<CurveLoop> floorProjectionCurveLoops = OrderCurvesToCurveLoops(curvesWithoutDuplicates);
        //            //Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { floorProjectionCurveLoops.Last() }, XYZ.BasisZ, 10000);
        //            //Curve curveMid = Line.CreateBound(jointCurve.Curve.Evaluate(0.5, true) + XYZ.BasisZ * 5000, jointCurve.Curve.Evaluate(0.5, true) - XYZ.BasisZ * 10000);
        //            //if ((solid.IntersectWithCurve(curveMid, new SolidCurveIntersectionOptions()).SegmentCount > 0))
        //            //{

        //            //if (numberOfIntersections > 0)
        //            //{
        //            int numberOfIntersections0 = 0;
        //            int numberOfIntersections1 = 0;
        //            foreach (var curve in floorProjectionCurves)
        //            {
        //                if (curve.Distance(jointCurve.Projection.GetEndPoint(0)) < 0.01)
        //                {
        //                    numberOfIntersections0++;
        //                }
        //                if (curve.Distance(jointCurve.Projection.GetEndPoint(1)) < 0.01)
        //                {
        //                    numberOfIntersections1++;
        //                }
        //            }
        //            foreach (var joint2 in joints)
        //            {
        //                if (joint2.Id.IntegerValue != joint.Id.IntegerValue)
        //                {
        //                    CurveAndProjection jointCurve2 = new CurveAndProjection((joint2.Location as LocationCurve).Curve);
        //                    if (jointCurve2.Projection.Distance(jointCurve.Projection.GetEndPoint(0)) < 0.01)
        //                    {
        //                        numberOfIntersections0++;
        //                    }
        //                    if (jointCurve2.Projection.Distance(jointCurve.Projection.GetEndPoint(1)) < 0.01)
        //                    {
        //                        numberOfIntersections1++;
        //                    }
        //                }
        //            }
        //            if (numberOfIntersections0 == 0)
        //            {
        //                errorPoints1.Add(jointCurve.Curve.GetEndPoint(0));
        //                numberOfErrors++;
        //            }
        //            if (numberOfIntersections1 == 0)
        //            {
        //                errorPoints1.Add(jointCurve.Curve.GetEndPoint(1));
        //                numberOfErrors++;
        //            }
        //            if (numberOfIntersections0 != 0 && numberOfIntersections1 != 0)
        //            {
        //                correctJoints.Add(joint);
        //            }
        //            //}
        //        }
        //        return new Tuple<List<XYZ>, List<XYZ>>(errorPoints1, errorPoints2);
        //    }
        //}

        public static Tuple<List<XYZ>, List<XYZ>> VerifyJoints3(string ambiente, List<Curve> floorCurves, List<CurveLoop> insideOpeningCurveLoops, List<Element> joints, out List<Element> correctJoints)
        {
            correctJoints = new List<Element>();
            Document doc = joints[0].Document;
            ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);
            List<XYZ> errorPoints1 = new List<XYZ>();
            List<XYZ> errorPoints2 = new List<XYZ>();

            //TESTE
            //foreach (var curveLoop in insideOpeningCurveLoops)
            //{
            //    foreach (var curve in curveLoop)
            //    {
            //        floorCurves.Add(curve);
            //    }
            //}

            List<Curve> floorProjectionCurves = floorCurves.Select(a => GetCurveProjection(a)).ToList();

            //REMOVE DUPLICATES
            List<int> duplicateCurvesIndex = new List<int>();
            for (int i = 0; i < floorProjectionCurves.Count; i++)
            {
                Curve ci = floorProjectionCurves[i];
                XYZ epi0 = ci.GetEndPoint(0);
                XYZ epi1 = ci.GetEndPoint(1);
                XYZ mpi = ci.Evaluate(0.5, true);


                for (int j = 0; j < floorProjectionCurves.Count; j++)
                {
                    if (i != j)
                    {
                        Curve cj = floorProjectionCurves[j];
                        XYZ epj0 = cj.GetEndPoint(0);
                        XYZ epj1 = cj.GetEndPoint(1);
                        XYZ mpj = cj.Evaluate(0.5, true);

                        if ((mpi.DistanceTo(mpj) < 0.0001) && ((epi0.DistanceTo(epj0) < 0.0001 && epi1.DistanceTo(epj1) < 0.0001) || epi0.DistanceTo(epj1) < 0.0001 && epi1.DistanceTo(epj0) < 0.0001))
                        {
                            duplicateCurvesIndex.Add(i);
                            duplicateCurvesIndex.Add(j);
                        }
                    }
                }
            }

            List<Curve> curvesWithoutDuplicates = new List<Curve>();
            for (int i = 0; i < floorProjectionCurves.Count; i++)
            {
                if (!duplicateCurvesIndex.Contains(i))
                {
                    curvesWithoutDuplicates.Add(floorProjectionCurves[i]);
                }
            }

            //Transaction tx2 = new Transaction(doc, "create");
            //Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
            //tx2.Start("aaa");
            //SketchPlane sPlane = SketchPlane.Create(doc, plane);
            //foreach (var curve in curvesWithoutDuplicates)
            //{
            //    doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
            //}
            //tx2.Commit();
            //return null;

            List<CurveLoop> floorProjectionCurveLoops = OrderCurvesToCurveLoops(curvesWithoutDuplicates);
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { floorProjectionCurveLoops.Last() }, XYZ.BasisZ, 10000);
            SolidCurveIntersectionOptions solidOptions = new SolidCurveIntersectionOptions();
            List<int> joinedSolids = new List<int>();
            joinedSolids.Add(floorProjectionCurveLoops.Count - 1);
            for (int i = floorProjectionCurveLoops.Count - 2; i > -1; i--)
            {
                if (!joinedSolids.Contains(i))
                {
                    Solid nextSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { floorProjectionCurveLoops[i] }, XYZ.BasisZ, 10000);
                    if (BooleanOperationsUtils.ExecuteBooleanOperation(solid, nextSolid, BooleanOperationsType.Intersect).Volume == 0)
                    {
                        joinedSolids.Add(i);
                        solid = BooleanOperationsUtils.ExecuteBooleanOperation(solid, nextSolid, BooleanOperationsType.Union);
                        i = floorProjectionCurveLoops.Count - 1;
                    }
                }
            }



            // GET ALL JOINTS AND FLOORS

            int numberOfErrors = 0;

            Solid openingSolid = null;
            if (insideOpeningCurveLoops.Count != 0)
            {
                openingSolid = GeometryCreationUtilities.CreateExtrusionGeometry(insideOpeningCurveLoops, XYZ.BasisZ, 5000);
            }


            foreach (var joint in joints)
            {
                //int jointId = joint.Id.IntegerValue;
                if ((joint.Location as LocationCurve).Curve.Length == 0)
                {
                    continue;
                }
                CurveAndProjection jointCurve = new CurveAndProjection((joint.Location as LocationCurve).Curve);
                Curve curve1 = Line.CreateBound(jointCurve.Projection.Evaluate(0.5, true), jointCurve.Projection.Evaluate(0.5, true) + 5000 * XYZ.BasisZ);

                if (insideOpeningCurveLoops.Count != 0)
                {
                    if (openingSolid.IntersectWithCurve(curve1, new SolidCurveIntersectionOptions()).SegmentCount > 0)
                    {
                        errorPoints1.Add(jointCurve.Curve.Evaluate(0.5, true));
                    }
                }


                if (jointCurve.Curve is Arc)
                {

                }
                else
                {
                    XYZ direction = (jointCurve.Projection as Line).Direction;
                    if ((Math.Abs(direction.X) < 0.01 && Math.Abs(direction.X) > 0.000001) || (Math.Abs(direction.Y) < 0.01 && Math.Abs(direction.Y) > 0.000001))
                    {
                        errorPoints2.Add(jointCurve.Curve.Evaluate(0.5, true));
                        numberOfErrors++;
                        continue;
                    }
                }


                //foreach (var curveLoop in insideOpeningCurveLoops)
                //{
                //    foreach (var curve in curveLoop)
                //    {
                //        floorCurves.Add(curve);
                //    }
                //}

                //Curve jointMiddlePointCurve = Line.CreateBound(EvaluateCurve(jointCurve.Curve, 0.5) + XYZ.BasisZ * 10000, Utils.EvaluateCurve(jointCurve.Curve, 0.5) - XYZ.BasisZ * 10000);

                //List<Curve> floorProjectionCurves = new List<Curve>();



                //int numberOfIntersections = 0;
                //foreach (var curve in floorCurves.Select(a => GetCurveProjection(a)))
                //{
                //    floorProjectionCurves.Add(curve);
                //    if (jointCurve.Projection.Intersect(curve, out IntersectionResultArray array) != SetComparisonResult.Disjoint)
                //    {
                //        numberOfIntersections++;
                //    }
                //    //if (jointCurve.Projection.Intersect(curve, out IntersectionResultArray array) != SetComparisonResult.Disjoint)
                //    //{
                //    //    numberOfIntersections++;
                //    //}
                //}
                //CurveLoop floorProjectionCurveLoop = Utils.OrderCurvesToCurveLoops(floorProjectionCurves).Last();
                //Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { floorProjectionCurveLoop }, XYZ.BasisZ, 10000);
                //if (numberOfIntersections == 0)
                //{
                //    Curve curve0 = Line.CreateBound(jointCurve.Curve.GetEndPoint(0) + XYZ.BasisZ * 5000, jointCurve.Curve.GetEndPoint(0) - XYZ.BasisZ * 10000);
                //    Curve curve1 = Line.CreateBound(jointCurve.Curve.GetEndPoint(1) + XYZ.BasisZ * 5000, jointCurve.Curve.GetEndPoint(1) - XYZ.BasisZ * 10000);
                //    if (!(solid.IntersectWithCurve(curve0, new SolidCurveIntersectionOptions()).SegmentCount > 0 || solid.IntersectWithCurve(curve1, new SolidCurveIntersectionOptions()).SegmentCount > 0))
                //    {
                //        errorPoints.Add(jointCurve.Curve.GetEndPoint(0));
                //        errorPoints.Add(jointCurve.Curve.GetEndPoint(1));
                //        numberOfErrors++;
                //        numberOfErrors++;
                //    }
                //    else
                //    {
                //        correctJoints.Add(joint);
                //    }
                //}




                Curve curveMid = Line.CreateBound(jointCurve.Curve.Evaluate(0.5, true) + XYZ.BasisZ * 5000, jointCurve.Curve.Evaluate(0.5, true) - XYZ.BasisZ * 10000);
                if ((solid.IntersectWithCurve(curveMid, new SolidCurveIntersectionOptions()).SegmentCount > 0))
                {

                    //if (numberOfIntersections > 0)
                    //{
                    int numberOfIntersections0 = 0;
                    int numberOfIntersections1 = 0;
                    foreach (var curve in floorProjectionCurves)
                    {
                        if (curve.Distance(jointCurve.Projection.GetEndPoint(0)) < 0.00001)
                        {
                            numberOfIntersections0++;
                        }
                        if (curve.Distance(jointCurve.Projection.GetEndPoint(1)) < 0.00001)
                        {
                            numberOfIntersections1++;
                        }
                    }
                    foreach (var joint2 in joints)
                    {
                        if (joint2.Id != joint.Id)
                        {
                            if ((joint2.Location as LocationCurve).Curve.Length == 0)
                            {
                                continue;
                            }
                            CurveAndProjection jointCurve2 = new CurveAndProjection((joint2.Location as LocationCurve).Curve);
                            if (jointCurve2.Projection.Distance(jointCurve.Projection.GetEndPoint(0)) < 0.000001)
                            {
                                numberOfIntersections0++;
                            }
                            if (jointCurve2.Projection.Distance(jointCurve.Projection.GetEndPoint(1)) < 0.000001)
                            {
                                numberOfIntersections1++;
                            }
                        }
                    }
                    if (numberOfIntersections0 == 0)
                    {
                        errorPoints1.Add(jointCurve.Curve.GetEndPoint(0) + XYZ.BasisX * 0.001 + XYZ.BasisY * 0.001);
                        numberOfErrors++;
                    }
                    if (numberOfIntersections1 == 0)
                    {
                        errorPoints1.Add(jointCurve.Curve.GetEndPoint(1) + XYZ.BasisX * 0.001 + XYZ.BasisY * 0.001);
                        numberOfErrors++;
                    }
                    if (numberOfIntersections0 != 0 && numberOfIntersections1 != 0)
                    {
                        correctJoints.Add(joint);
                    }
                }
            }

            //Transaction tx2 = new Transaction(doc, "create");
            //Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
            //tx2.Start("aaa");
            //SketchPlane sPlane = SketchPlane.Create(doc, plane);
            //foreach (var curve in floorProjectionCurves)
            //{
            //    doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
            //}
            //tx2.Commit();

            return new Tuple<List<XYZ>, List<XYZ>>(errorPoints1, errorPoints2);
        }


        public class FloorWarningsSwallower : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                IList<FailureMessageAccessor> failList = new List<FailureMessageAccessor>();
                // Inside event handler, get all warnings
                failList = failuresAccessor.GetFailureMessages();


                foreach (FailureMessageAccessor failure in failList)
                {
                    FailureDefinitionId failID = failure.GetFailureDefinitionId();
                    // prevent Revit from showing Unenclosed room warnings
                    //if (failID == BuiltInFailures.FloorFailures.FloorSlopeExceedsThreshold || 
                    //    failID == BuiltInFailures.OverlapFailures.FloorsOverlap || 
                    //    failID == BuiltInFailures.AnalyticalModelFailures.AdjustEdgeTooShortErr ||
                    //    failID == BuiltInFailures.AnalyticalModelFailures.AdjustEdgeTooShort ||
                    //    failID == BuiltInFailures.AnalyticalModelFailures.AdjustEdgeTooShortWarn ||
                    //    failID == BuiltInFailures.AnalyticalModelFailures.AnalyticalModelIsIncorrect ||
                    //    failID == BuiltInFailures.AnalyticalModelFailures.CannotCreateAnalyticalGeometry ||
                    //    failID == BuiltInFailures.AnalyticalModelFailures.CannotKeepAnalyticalAdjustment ||
                    //    failID == BuiltInFailures.AnalyticalModelFailures.CannotMakeAnalyticalGeometry )
                    if (failID.Guid.ToString() != "a")
                    {
                        failuresAccessor.DeleteWarning(failure);
                    }
                }

                return FailureProcessingResult.Continue;
            }
        }

        public class BeamWarningsSwallower : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                IList<FailureMessageAccessor> failList = new List<FailureMessageAccessor>();
                // Inside event handler, get all warnings
                failList = failuresAccessor.GetFailureMessages();
                foreach (FailureMessageAccessor failure in failList)
                {
                    FailureDefinitionId failID = failure.GetFailureDefinitionId();
                    // prevent Revit from showing Unenclosed room warnings
                    if (failID == BuiltInFailures.InaccurateFailures.InaccurateBeamOrBrace)
                    {
                        failuresAccessor.DeleteWarning(failure);
                    }
                }

                return FailureProcessingResult.Continue;
            }
        }

        public static List<XYZ> GetIntersectPoints(List<Curve> curves)
        {
            List<XYZ> points = new List<XYZ>();
            for (int i = 0; i < curves.Count(); i++)
            {
                for (int j = 0; j < curves.Count(); j++)
                {
                    if (i != j)
                    {
                        SetComparisonResult result = curves[i].Intersect(curves[j], out IntersectionResultArray resultArray);
                        if (resultArray != null)
                        {
                            foreach (IntersectionResult intersection in resultArray)
                            {
                                points.Add(intersection.XYZPoint - XYZ.BasisZ * intersection.XYZPoint.Z);
                            }

                        }
                    }
                }
            }
            points = points.Distinct(new XYZComparer()).ToList();
            return points;
        }

        public static Curve GetCurveInPlane(Curve curve, Plane plane)
        {
            if (curve is Arc)
            {
                double elevation = plane.Origin.Z;
                XYZ ep0 = curve.GetEndPoint(0) - XYZ.BasisZ * (curve.GetEndPoint(0).Z - elevation);
                XYZ ep1 = curve.GetEndPoint(1) - XYZ.BasisZ * (curve.GetEndPoint(1).Z - elevation);
                XYZ midPoint = curve.Evaluate(0.5, true) - XYZ.BasisZ * (curve.Evaluate(0.5, true).Z - elevation);
                return Arc.Create(ep0, ep1, midPoint);
            }
            else
            {
                double elevation = plane.Origin.Z;
                XYZ ep0 = curve.GetEndPoint(0) - XYZ.BasisZ * (curve.GetEndPoint(0).Z - elevation);
                XYZ ep1 = curve.GetEndPoint(1) - XYZ.BasisZ * (curve.GetEndPoint(1).Z - elevation);
                return Line.CreateBound(ep0, ep1);
            }
        }

        public static List<Curve> GetUnBoundArcHalfProjections(Curve curve)
        {
            List<Curve> halfArcs = new List<Curve>();
            Arc arc = curve as Arc;
            XYZ pt1 = arc.Center + XYZ.BasisX * arc.Radius - XYZ.BasisZ * arc.Center.Z;
            XYZ pt2 = arc.Center - XYZ.BasisY * arc.Radius - XYZ.BasisZ * arc.Center.Z;
            XYZ pt3 = arc.Center - XYZ.BasisX * arc.Radius - XYZ.BasisZ * arc.Center.Z;
            XYZ pt4 = arc.Center + XYZ.BasisY * arc.Radius - XYZ.BasisZ * arc.Center.Z;
            Curve halfArc1 = Arc.Create(pt1, pt3, pt2);
            Curve halfArc2 = Arc.Create(pt3, pt1, pt4);
            halfArcs.Add(halfArc1);
            halfArcs.Add(halfArc2);
            return halfArcs;
        }

        public static Curve GetCurveProjection(Curve curve)
        {
            Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ());
            if (curve is Arc)
            {
                if (!curve.IsBound)
                {
                    return Arc.Create((curve as Arc).Center, (curve as Arc).Radius, 0, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY);
                }
                else
                {
                    double elevation = plane.Origin.Z;
                    XYZ ep0 = curve.GetEndPoint(0) - XYZ.BasisZ * (curve.GetEndPoint(0).Z - elevation);
                    XYZ ep1 = curve.GetEndPoint(1) - XYZ.BasisZ * (curve.GetEndPoint(1).Z - elevation);
                    XYZ midPoint = curve.Evaluate(0.5, true) - XYZ.BasisZ * (curve.Evaluate(0.5, true).Z - elevation);
                    return Arc.Create(ep0, ep1, midPoint);
                }

            }
            else
            {
                double elevation = plane.Origin.Z;
                XYZ ep0 = curve.GetEndPoint(0) - XYZ.BasisZ * (curve.GetEndPoint(0).Z - elevation);
                XYZ ep1 = curve.GetEndPoint(1) - XYZ.BasisZ * (curve.GetEndPoint(1).Z - elevation);
                return Line.CreateBound(ep0, ep1);
            }
        }
        public enum LineDirection
        {
            Up,
            Down,
            Left,
            Right
        }
        public static LineDirection GetLineDirection(Line line, XYZ rightDirection)
        {
            XYZ direction = line.Direction;
            if (direction.AngleTo(rightDirection) <= Math.PI / 4)
            {
                return LineDirection.Right;
            }
            else if (direction.AngleTo(rightDirection) >= Math.PI * 3 / 4)
            {
                return LineDirection.Left;
            }
            else if (rightDirection.CrossProduct(direction).IsAlmostEqualTo(XYZ.BasisZ))
            {
                return LineDirection.Up;
            }
            else
            {
                return LineDirection.Down;
            }
        }
        public enum ShareResult
        {
            Point0 = 0,
            Point1 = 1,
            DoesNot = 2,
            Does = 3
        }

        public static ShareResult DoesCurveShareEp1(Curve curve1, Curve curve2)
        {
            if (curve1.GetEndPoint(1).IsAlmostEqualTo(curve2.GetEndPoint(0)))
            {
                return ShareResult.Point0;
            }
            if (curve1.GetEndPoint(1).IsAlmostEqualTo(curve2.GetEndPoint(1)))
            {
                return ShareResult.Point1;
            }

            return ShareResult.DoesNot;
        }

        public static ShareResult Does2ndCurveShare1stEp(int ep, Curve curve1, Curve curve2)
        {
            if (curve1.GetEndPoint(ep).IsAlmostEqualTo(curve2.GetEndPoint(0)))
            {
                return ShareResult.Does;
            }
            if (curve1.GetEndPoint(ep).IsAlmostEqualTo(curve2.GetEndPoint(1)))
            {
                return ShareResult.Does;
            }

            return ShareResult.DoesNot;
        }


        public static XYZ EvaluateCurve(Curve curve, double param)
        {
            double param1 = curve.GetEndParameter(0);
            double param2 = curve.GetEndParameter(1);

            double paramCalc = param1 + ((param2 - param1)
              * param);

            XYZ evaluatedPoint = null;

            if (curve.IsInside(paramCalc))
            {
                double normParam = curve
                  .ComputeNormalizedParameter(paramCalc);

                evaluatedPoint = curve.Evaluate(
                  normParam, true);
            }
            return evaluatedPoint;
        }
        public static CurveLoop OrderCurvesToCurveLoop(List<Curve> curves)
        {
            CurveLoop curveLoop = new CurveLoop();
            curveLoop.Append(curves[0]);
            bool closedLoop = false;
            List<int> usedCurves = new List<int>();
            int nuuumber = 0;
            while (!closedLoop)
            {
                for (int i = 1; i < curves.Count; i++)
                {
                    if (!usedCurves.Contains(i))
                    {
                        XYZ c1p1 = curveLoop.ElementAt(curveLoop.Count() - 1).GetEndPoint(1);
                        XYZ c2p0 = curves[i].GetEndPoint(0);
                        XYZ c2p1 = curves[i].GetEndPoint(1);
                        if (c1p1.DistanceTo(c2p0) < 0.01)
                        {
                            curveLoop.Append(curves[i]);
                            usedCurves.Add(i);
                        }
                        else if (c1p1.DistanceTo(c2p1) < 0.01)
                        {
                            curveLoop.Append(curves[i].CreateReversed());
                            usedCurves.Add(i);
                        }
                    }
                }
                if (curveLoop.Last().GetEndPoint(0).DistanceTo(curveLoop.First().GetEndPoint(0)) < 0.01 || curveLoop.Last().GetEndPoint(1).DistanceTo(curveLoop.First().GetEndPoint(0)) < 0.01)
                {
                    closedLoop = true;
                }
                else
                {
                    nuuumber++;
                    if (nuuumber > 1000)
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("ERRO", "Muitas tentativas de criar um Loop Com as curvas fornecidas");
                        return null;
                    }
                }

            }
            return curveLoop;

        }

        public static CurveLoop OrderCurvesToCurveLoopBKP(List<Curve> curves)
        {
            CurveLoop curveLoop = new CurveLoop();
            curveLoop.Append(curves[0]);
            bool closedLoop = false;
            List<int> usedCurves = new List<int>();
            int nuuumber = 0;
            while (!closedLoop)
            {
                for (int i = 1; i < curves.Count; i++)
                {
                    if (!usedCurves.Contains(i))
                    {
                        XYZ c1p1 = curveLoop.ElementAt(curveLoop.Count() - 1).GetEndPoint(1);
                        XYZ c2p0 = curves[i].GetEndPoint(0);
                        XYZ c2p1 = curves[i].GetEndPoint(1);
                        if (c1p1.IsAlmostEqualTo(c2p0))
                        {
                            curveLoop.Append(curves[i]);
                            usedCurves.Add(i);
                        }
                        else if (c1p1.IsAlmostEqualTo(c2p1))
                        {
                            curveLoop.Append(curves[i].CreateReversed());
                            usedCurves.Add(i);
                        }
                    }
                }
                if (curveLoop.Last().GetEndPoint(0).IsAlmostEqualTo(curveLoop.First().GetEndPoint(0)) || curveLoop.Last().GetEndPoint(1).IsAlmostEqualTo(curveLoop.First().GetEndPoint(0)))
                {
                    closedLoop = true;
                }
                else
                {
                    nuuumber++;
                    if (nuuumber > 1000)
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("ERRO", "Muitas tentativas de criar um Loop Com as curvas fornecidas");
                        return null;
                    }
                }

            }
            return curveLoop;

        }

        public static void SetPointsElevationsOfNewFloor(Floor oldFloor, Floor newFloor)
        {
            Transaction tx = new Transaction(oldFloor.Document, "transatcion");

            tx.Start("Enable Editor");
#if Revit2021 || Revit2022 || Revit2023
            SlabShapeEditor sse = newFloor.SlabShapeEditor;
#else
            SlabShapeEditor sse = newFloor.GetSlabShapeEditor();
#endif
            sse.Enable();
            tx.Commit();

            tx.Start("Change Points Elevations");
            FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
            failOpt = tx.GetFailureHandlingOptions();
            failOpt.SetFailuresPreprocessor(new Utils.FloorWarningsSwallower());
            tx.SetFailureHandlingOptions(failOpt);


            foreach (SlabShapeVertex newVertex in sse.SlabShapeVertices)
            {
                XYZ newPoint = newVertex.Position;

                try
                {
                    XYZ projectedPoint = oldFloor.GetVerticalProjectionPoint(newPoint, FloorFace.Top);
                    sse.ModifySubElement(newVertex, projectedPoint.Z - newVertex.Position.Z);
                }
                catch (Exception)
                {

                }

            }

            tx.Commit();

        }
        public static CurveLoop SortCurveLoop(CurveLoop curveLoop)
        {
            CurveLoop newCurveLoop = new CurveLoop();

            // Coleta o ponto mais extremo superior à esquerda para iniciar o curveLoop
            List<XYZ> points = new List<XYZ>();
            foreach (var curve in curveLoop)
            {
                points.Add(curve.GetEndPoint(0));
                points.Add(curve.GetEndPoint(1));
            }

            var extremePointX = points.OrderBy(x => x.X).ThenByDescending(x => x.Y).First();

            var extremePointsX = points.Where(x => Math.Round(x.X, 3) == Math.Round(extremePointX.X, 3));

            var extremePoint = extremePointsX.Aggregate((x, y) => x.Y > y.Y ? x : y);

            // Obtém a primeira curva com base no ponto mais extremo superior à esquerda coletado
            Curve firstCurve = curveLoop.First(x => x.GetEndPoint(0).IsAlmostEqualTo(extremePoint));

            List<Curve> curveLoopList = new List<Curve>();

            List<Curve> newCurveLoopList = new List<Curve>();

            foreach (var curve in curveLoop)
            {
                curveLoopList.Add(curve);
            }

            Curve firstCurveInCurveLoop = curveLoopList.First(x => x.GetEndPoint(0).IsAlmostEqualTo(firstCurve.GetEndPoint(0)) &&
            x.GetEndPoint(1).IsAlmostEqualTo(firstCurve.GetEndPoint(1)));

            int index = curveLoopList.IndexOf(firstCurveInCurveLoop);

            for (int i = index; i < curveLoopList.Count; i++)
            {
                newCurveLoopList.Add(curveLoopList[i]);
            }

            for (int i = 0; i < index; i++)
            {
                newCurveLoopList.Add(curveLoopList[i]);
            }

            foreach (var curve in newCurveLoopList)
            {
                try
                {
                    newCurveLoop.Append(curve);
                }
                catch { }
            }

            return newCurveLoop;
        }


        public static CurveLoop JoinColinearCurves(Document doc, CurveLoop curveLoop, out bool repeat)
        {
            CurveLoop finalCurveLoop = new CurveLoop();
            repeat = false;

            CurveLoop sortedCLoop = SortCurveLoop(curveLoop);

            foreach (Curve curve1 in sortedCLoop)
            {
                try
                {
                    int trigger = 0;

                    XYZ sP1 = curve1.GetEndPoint(0);
                    XYZ eP1 = curve1.GetEndPoint(1);
                    Line line1 = null;
                    Arc arc1 = null;
                    XYZ p1 = null;
                    XYZ xDir1 = null;
                    XYZ yDir1 = null;

                    if (curve1 is Line)
                    {
                        line1 = curve1 as Line;
                        p1 = line1.Direction;
                    }
                    else if (curve1 is Arc)
                    {
                        arc1 = curve1 as Arc;
                        xDir1 = arc1.XDirection;
                        yDir1 = arc1.YDirection;
                    }

                    int index = 0;

                    foreach (Curve curve2 in sortedCLoop)
                    {
                        try
                        {
                            XYZ sP2 = curve2.GetEndPoint(0);
                            XYZ eP2 = curve2.GetEndPoint(1);
                            Line line2 = null;
                            Arc arc2 = null;
                            XYZ p2 = null;
                            XYZ xDir2 = null;
                            XYZ yDir2 = null;

                            if (curve2 is Line)
                            {
                                line2 = curve2 as Line;
                                p2 = line2.Direction;
                            }
                            else if (curve2 is Arc)
                            {
                                arc2 = curve2 as Arc;
                                xDir2 = arc2.XDirection;
                                yDir2 = arc2.YDirection;
                            }

                            if (arc1 == null && arc2 == null)
                            {
                                double angleTo = p1.AngleTo(p2);
                                bool startPointIsAlmostEqualTo = sP1.IsAlmostEqualTo(sP2);
                                bool endPointIsAlmostEqualTo = eP1.IsAlmostEqualTo(eP2);
                                bool isContinuous = eP1.IsAlmostEqualTo(sP2);

                                if (p1.AngleTo(p2) < 0.0001 && !sP1.IsAlmostEqualTo(sP2) && !eP1.IsAlmostEqualTo(eP2) && eP1.IsAlmostEqualTo(sP2))
                                {
                                    trigger = 1;

                                    repeat = true;

                                    try
                                    {
                                        Curve newCurve = Line.CreateBound(sP1, eP2) as Curve;
                                        finalCurveLoop.Append(newCurve);
                                    }
                                    catch { }

                                    break;
                                }
                            }
                            else if (arc1 != null && arc2 != null)
                            {
                                if (arc1.GetEndPoint(1).IsAlmostEqualTo(arc2.GetEndPoint(0)))
                                {
                                    trigger = 1;

                                    repeat = true;

                                    try
                                    {
                                        Arc newCurve = Arc.Create(arc1.GetEndPoint(0), arc2.GetEndPoint(1), arc1.GetEndPoint(1));
                                        finalCurveLoop.Append(newCurve);
                                    }
                                    catch { }

                                    break;
                                }
                            }
                        }
                        catch { }

                        index++;
                    }

                    if (trigger == 0)
                    {
                        try
                        {
                            finalCurveLoop.Append(curve1);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            while (repeat == true)
            {
                finalCurveLoop = JoinColinearCurves(doc, finalCurveLoop, out repeat);
            }

            return finalCurveLoop;
        }


        public static void SetPointsElevationsOfNewFloor(Floor oldFloor, Floor newFloor, double offset)
        {
            Transaction tx = new Transaction(oldFloor.Document, "transatcion");
#if Revit2021 || Revit2022 || Revit2023
            SlabShapeEditor sse = newFloor.SlabShapeEditor;
#else
            SlabShapeEditor sse = newFloor.GetSlabShapeEditor();
#endif
            if (!sse.IsEnabled)
            {
                tx.Start("Enable Editor");
                sse.Enable();
                tx.Commit();
            }



            tx.Start("Change Points Elevations");
            FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
            failOpt = tx.GetFailureHandlingOptions();
            failOpt.SetFailuresPreprocessor(new Utils.FloorWarningsSwallower());
            tx.SetFailureHandlingOptions(failOpt);


            foreach (SlabShapeVertex newVertex in sse.SlabShapeVertices)
            {
                XYZ newPoint = newVertex.Position;

                //try
                //{
                XYZ projectedPoint = oldFloor.GetVerticalProjectionPoint(newPoint, FloorFace.Top);
                if (projectedPoint != null)
                {
                    sse.ModifySubElement(newVertex, projectedPoint.Z - newVertex.Position.Z + offset);
                }
                else
                {
                    Curve intersectCurve = Line.CreateBound(newPoint + XYZ.BasisZ * +10000, newPoint + XYZ.BasisZ * -10000);
                    SolidCurveIntersection intersection = GetSolid(oldFloor).IntersectWithCurve(intersectCurve, new SolidCurveIntersectionOptions());
                    if (intersection.SegmentCount > 0)
                    {
                        projectedPoint = intersection.First().GetEndPoint(0);
                        sse.ModifySubElement(newVertex, projectedPoint.Z - newVertex.Position.Z + offset);
                    }
                }
                //}
                //catch (Exception)
                //{

                //}

            }

            tx.Commit();

        }

        public static void SetPointsElevationsOfNewFloor(List<Floor> oldFloors, Floor newFloor, double offset)
        {
            Transaction tx = new Transaction(newFloor.Document, "transatcion");
#if Revit2021 || Revit2022 || Revit2023
            SlabShapeEditor sse = newFloor.SlabShapeEditor;
#else
            SlabShapeEditor sse = newFloor.GetSlabShapeEditor();
#endif
            if (!sse.IsEnabled)
            {
                tx.Start("Enable Editor");
                sse.Enable();
                tx.Commit();
            }



            tx.Start("Change Points Elevations");
            FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
            failOpt = tx.GetFailureHandlingOptions();
            failOpt.SetFailuresPreprocessor(new Utils.FloorWarningsSwallower());
            tx.SetFailureHandlingOptions(failOpt);


            foreach (SlabShapeVertex newVertex in sse.SlabShapeVertices)
            {
                XYZ newPoint = newVertex.Position;

                foreach (var oldFloor in oldFloors)
                {
                    XYZ projectedPoint = oldFloor.GetVerticalProjectionPoint(newPoint, FloorFace.Top);
                    if (projectedPoint != null)
                    {
                        sse.ModifySubElement(newVertex, projectedPoint.Z - newVertex.Position.Z + offset);
                        break;
                    }
                    else
                    {
                        Curve intersectCurve = Line.CreateBound(newPoint + XYZ.BasisZ * +10000, newPoint + XYZ.BasisZ * -10000);
                        SolidCurveIntersection intersection = GetSolid(oldFloor).IntersectWithCurve(intersectCurve, new SolidCurveIntersectionOptions());
                        if (intersection.SegmentCount > 0)
                        {
                            projectedPoint = intersection.First().GetEndPoint(0);
                            sse.ModifySubElement(newVertex, projectedPoint.Z - newVertex.Position.Z + offset);
                            break;
                        }
                    }
                }
            }

            tx.Commit();

        }


        public static void SetPreviousPointsElevationsToNewFloor(Floor oldFloor, Floor newFloor)
        {
            Transaction tx = new Transaction(newFloor.Document, "transatcion");

            tx.Start("Enable Editor");
#if Revit2021 || Revit2022 || Revit2023
            SlabShapeEditor newSSE = newFloor.SlabShapeEditor;
            SlabShapeEditor oldSSE = oldFloor.SlabShapeEditor;
#else
            SlabShapeEditor newSSE = newFloor.GetSlabShapeEditor();
            SlabShapeEditor oldSSE = oldFloor.GetSlabShapeEditor();
#endif
            newSSE.Enable();
            tx.Commit();


            FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
            failOpt = tx.GetFailureHandlingOptions();
            failOpt.SetFailuresPreprocessor(new Utils.FloorWarningsSwallower());
            tx.SetFailureHandlingOptions(failOpt);
            tx.Start("Change Points Elevations");


            foreach (SlabShapeVertex oldVertex in oldSSE.SlabShapeVertices)
            {
                XYZ oldPoint = oldVertex.Position;
                bool foundSimilarPoint = false;
                foreach (SlabShapeVertex newVertex in newSSE.SlabShapeVertices)
                {
                    XYZ newPoint = newVertex.Position;

                    if (Math.Abs(oldPoint.X - newPoint.X) < 0.1 && Math.Abs(oldPoint.Y - newPoint.Y) < 0.1)
                    {
                        if (oldPoint.Z != newPoint.Z)
                        {
                            try
                            {
                                XYZ projectedPoint = oldFloor.GetVerticalProjectionPoint(newPoint, FloorFace.Top);
                                newSSE.ModifySubElement(newVertex, projectedPoint.Z - newVertex.Position.Z);
                            }
                            catch (Exception)
                            {

                            }
                        }
                        foundSimilarPoint = true;
                        break;
                    }
                }
                if (!foundSimilarPoint)
                {
                    //sse.DrawPoint(oldPoint);
                }
            }

            tx.Commit();
        }


        public static void CopyAllParameters(Element elementFrom, Element elementTo, List<BuiltInParameter> parametersToIgnore)
        {
            using (var trans = new Transaction(elementFrom.Document))
            {
                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);

                foreach (Parameter parameter in (elementFrom).Parameters)
                {
                    if (!parameter.IsReadOnly && parameter.HasValue)
                    {
                        foreach (Parameter newFloorParameter in elementTo.Parameters)
                        {
                            if (!newFloorParameter.IsReadOnly &&
                                !parametersToIgnore.Contains((newFloorParameter.Definition as InternalDefinition).BuiltInParameter))
                            {
                                if (parameter.Definition.Name == newFloorParameter.Definition.Name)
                                {
                                    switch (parameter.StorageType)
                                    {
                                        case StorageType.None:
                                            break;
                                        case StorageType.Integer:
                                            newFloorParameter.Set(parameter.AsInteger());
                                            break;
                                        case StorageType.Double:
                                            newFloorParameter.Set(parameter.AsDouble());
                                            break;
                                        case StorageType.String:
                                            newFloorParameter.Set(parameter.AsString());
                                            break;
                                        case StorageType.ElementId:
                                            newFloorParameter.Set(parameter.AsElementId());
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                trans.Commit();
            }

        }
        public static void CopyAllParametersWithoutTransaction(Element elementFrom, Element elementTo, List<BuiltInParameter> parametersToIgnore)
        {

            foreach (Parameter parameter in (elementFrom).Parameters)
            {
                if (!parameter.IsReadOnly && parameter.HasValue)
                {
                    foreach (Parameter newFloorParameter in elementTo.Parameters)
                    {
                        if (!newFloorParameter.IsReadOnly &&
                            !parametersToIgnore.Contains((newFloorParameter.Definition as InternalDefinition).BuiltInParameter))
                        {
                            if (parameter.Definition.Name == newFloorParameter.Definition.Name)
                            {
                                switch (parameter.StorageType)
                                {
                                    case StorageType.None:
                                        break;
                                    case StorageType.Integer:
                                        newFloorParameter.Set(parameter.AsInteger());
                                        break;
                                    case StorageType.Double:
                                        newFloorParameter.Set(parameter.AsDouble());
                                        break;
                                    case StorageType.String:
                                        newFloorParameter.Set(parameter.AsString());
                                        break;
                                    case StorageType.ElementId:
                                        newFloorParameter.Set(parameter.AsElementId());
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }

        }

        public static void SetParameter(Element element, BuiltInParameter builtInParameter, object value)
        {
            using (var trans = new Transaction(element.Document))
            {
                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);
                Parameter parameter = element.get_Parameter(builtInParameter);
                switch (parameter.StorageType)
                {
                    case StorageType.None:
                        break;
                    case StorageType.Integer:
                        parameter.Set((int)value);
                        break;
                    case StorageType.Double:
                        parameter.Set((double)value);
                        break;
                    case StorageType.String:
                        parameter.Set((string)value);
                        break;
                    case StorageType.ElementId:
                        parameter.Set((ElementId)value);
                        break;
                    default:
                        break;
                }
                trans.Commit();
            }
        }

        public static void SetParameter(Element element, string parameterName, object value)
        {
            using (var trans = new Transaction(element.Document))
            {
                trans.Start("Internal Transaction");
                HideRevitWarnings(trans);
                Parameter parameter = element.LookupParameter(parameterName);
                switch (parameter.StorageType)
                {
                    case StorageType.None:
                        break;
                    case StorageType.Integer:
                        parameter.Set((int)value);
                        break;
                    case StorageType.Double:
                        parameter.Set((double)value);
                        break;
                    case StorageType.String:
                        parameter.Set((string)value);
                        break;
                    case StorageType.ElementId:
                        parameter.Set((ElementId)value);
                        break;
                    default:
                        break;
                }
                trans.Commit();
            }
        }

        public static void CopyAllParameters(Element element1, List<Floor> elements, List<BuiltInParameter> parametersToIgnore)
        {

            foreach (Parameter parameter in (element1).Parameters)
            {
                if (!parameter.IsReadOnly && parameter.HasValue)
                {
                    foreach (var element2 in elements)
                    {
                        foreach (Parameter newFloorParameter in element2.Parameters)
                        {
                            if (parameter.Definition.Name == newFloorParameter.Definition.Name)
                            {
                                if (!newFloorParameter.IsReadOnly &&
                                !parametersToIgnore.Contains((newFloorParameter.Definition as InternalDefinition).BuiltInParameter))
                                {
                                    if (parameter.Definition.Name == newFloorParameter.Definition.Name)
                                    {
                                        switch (parameter.StorageType)
                                        {
                                            case StorageType.None:
                                                break;
                                            case StorageType.Integer:
                                                newFloorParameter.Set(parameter.AsInteger());
                                                break;
                                            case StorageType.Double:
                                                newFloorParameter.Set(parameter.AsDouble());
                                                break;
                                            case StorageType.String:
                                                newFloorParameter.Set(parameter.AsString());
                                                break;
                                            case StorageType.ElementId:
                                                newFloorParameter.Set(parameter.AsElementId());
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    //try
                                    //{
                                    //    if (parameter.GUID == newFloorParameter.GUID)
                                    //    {

                                    //    }
                                    //}
                                    //catch (Exception)
                                    //{

                                    //}

                                    //try
                                    //{
                                    //    if (parameter.Id == newFloorParameter.Id)
                                    //    {
                                    //        switch (parameter.StorageType)
                                    //        {
                                    //            case StorageType.None:
                                    //                break;
                                    //            case StorageType.Integer:
                                    //                newFloorParameter.Set(parameter.AsInteger());
                                    //                break;
                                    //            case StorageType.Double:
                                    //                newFloorParameter.Set(parameter.AsDouble());
                                    //                break;
                                    //            case StorageType.String:
                                    //                newFloorParameter.Set(parameter.AsString());
                                    //                break;
                                    //            case StorageType.ElementId:
                                    //                newFloorParameter.Set(parameter.AsElementId());
                                    //                break;
                                    //            default:
                                    //                break;
                                    //        }
                                    //    }
                                    //}
                                    //catch (Exception)
                                    //{

                                    //}
                                }
                            }
                        }
                    }
                }

                //tx.Commit();
                //}

            }
        }

        public static List<CurveLoop> OrderCurvesToCurveLoopsBKP(List<Curve> curves)
        {

            List<CurveLoop> curveLoops = new List<CurveLoop>();
            int tryNumber = 0;
            List<int> usedCurves = new List<int>();

            List<Curve> unboundCurves = curves.Where(a => !a.IsBound).ToList();
            for (int i = 0; i < unboundCurves.Count; i++)
            {
                Arc arc = unboundCurves[i] as Arc;
                usedCurves.Add(i);
                CurveLoop unboundCurveLoop = new CurveLoop();
                XYZ pt1 = arc.Center + XYZ.BasisX * arc.Radius;
                XYZ pt2 = arc.Center - XYZ.BasisY * arc.Radius;
                XYZ pt3 = arc.Center - XYZ.BasisX * arc.Radius;
                XYZ pt4 = arc.Center + XYZ.BasisY * arc.Radius;

                Curve halfArc1 = Arc.Create(pt1, pt3, pt2);
                Curve halfArc2 = Arc.Create(pt3, pt1, pt4);
                unboundCurveLoop.Append(halfArc1);
                unboundCurveLoop.Append(halfArc2);
                curveLoops.Add(unboundCurveLoop);
            }

            curves = curves.Where(a => a.IsBound).ToList();

            List<Curve> nonUsedCurves = new List<Curve>();
            nonUsedCurves.AddRange(curves);

            while (nonUsedCurves.Count != 0)
            {
                CurveLoop curveLoop = new CurveLoop();

                curveLoop.Append(nonUsedCurves[0]);
                XYZ pt0 = nonUsedCurves[0].GetEndPoint(0);
                XYZ pt1 = nonUsedCurves[0].GetEndPoint(1);
                nonUsedCurves.RemoveAt(0);

                tryNumber++;
                bool closedLoop = false;
                int number = 0;



                while (!closedLoop)
                {

                    for (int i = 0; i < nonUsedCurves.Count; i++)
                    {
                        XYZ c2p0 = nonUsedCurves[i].GetEndPoint(0);
                        XYZ c2p1 = nonUsedCurves[i].GetEndPoint(1);
                        if (pt1.IsAlmostEqualTo(c2p0))
                        {
                            curveLoop.Append(nonUsedCurves[i]);
                            nonUsedCurves.RemoveAt(i);
                            pt1 = c2p1;
                            break;
                        }
                        else if (pt1.IsAlmostEqualTo(c2p1))
                        {
                            curveLoop.Append(nonUsedCurves[i].CreateReversed());
                            nonUsedCurves.RemoveAt(i);
                            pt1 = c2p0;
                            break;
                        }
                    }
                    if (pt0.DistanceTo(pt1) < 0.001)
                    {
                        closedLoop = true;
                        curveLoops.Add(curveLoop);
                    }
                    else
                    {
                        number++;
                        if (number > 1000)
                        {
                            Autodesk.Revit.UI.TaskDialog.Show("erro", "erro");
                            goto finish;
                        }
                    }
                }

            }
        finish:;
            curveLoops = curveLoops.OrderBy(a => a.GetExactLength()).ToList();
            return curveLoops;
        }

        public static List<CurveLoop> OrderCurvesToCurveLoops(List<Curve> curves)
        {
            //List<XYZ> allPoints = new List<XYZ>();
            //foreach (var curve in curves)
            //{
            //    allPoints.Add(curve.GetEndPoint(0));
            //    allPoints.Add(curve.GetEndPoint(1));
            //}
            //List<XYZ> adjacentPoints = new List<XYZ>();
            //List<int> usedPoints = new List<int>();
            //for (int i = 0; i < allPoints.Count; i++)
            //{
            //    if (!usedPoints.Contains(i))
            //    {
            //        usedPoints.Add(i);
            //        int count = 0;
            //        for (int j = 0; j < allPoints.Count; j++)
            //        {
            //            if (!usedPoints.Contains(j))
            //            {
            //                usedPoints.Add(j);
            //                if (allPoints[i].IsAlmostEqualTo(allPoints[j]))
            //                {
            //                    count++;
            //                }
            //            }
            //        }
            //        if (count > 3)
            //        {
            //            adjacentPoints.Add(allPoints[i]);
            //        }
            //    }
            //}

            //for (int i = 0; i < curves.Count; i++)
            //{
            //    foreach (var point in adjacentPoints)
            //    {
            //        if (curves[i].GetEndPoint(0).IsAlmostEqualTo(point) || curves[i].GetEndPoint(1).IsAlmostEqualTo(point))
            //        {
            //            break;
            //        }
            //    }

            //}

            List<CurveLoop> curveLoops = new List<CurveLoop>();
            int tryNumber = 0;
            List<int> usedCurves = new List<int>();

            List<Curve> unboundCurves = curves.Where(a => !a.IsBound).ToList();
            for (int i = 0; i < unboundCurves.Count; i++)
            {
                Arc arc = unboundCurves[i] as Arc;
                usedCurves.Add(i);
                CurveLoop unboundCurveLoop = new CurveLoop();
                XYZ pt1 = arc.Center + XYZ.BasisX * arc.Radius;
                XYZ pt2 = arc.Center - XYZ.BasisY * arc.Radius;
                XYZ pt3 = arc.Center - XYZ.BasisX * arc.Radius;
                XYZ pt4 = arc.Center + XYZ.BasisY * arc.Radius;

                Curve halfArc1 = Arc.Create(pt1, pt3, pt2);
                Curve halfArc2 = Arc.Create(pt3, pt1, pt4);
                unboundCurveLoop.Append(halfArc1);
                unboundCurveLoop.Append(halfArc2);
                curveLoops.Add(unboundCurveLoop);
            }

            curves = curves.Where(a => a.IsBound).ToList();
            //for (int i = 0; i < nonUsedCurves.Count; i++)
            //{
            //    if (!nonUsedCurves[i].IsBound)
            //    {
            //        Arc arc = nonUsedCurves[i] as Arc;
            //        usedCurves.Add(i);
            //        CurveLoop unboundCurveLoop = new CurveLoop();
            //        XYZ pt1 = arc.Center + XYZ.BasisX * arc.Radius;
            //        XYZ pt2 = arc.Center - XYZ.BasisY * arc.Radius;
            //        XYZ pt3 = arc.Center - XYZ.BasisX * arc.Radius;
            //        XYZ pt4 = arc.Center + XYZ.BasisY * arc.Radius;

            //        Curve halfArc1 = Arc.Create(pt1, pt3, pt2);
            //        Curve halfArc2 = Arc.Create(pt3, pt1, pt4);
            //        unboundCurveLoop.Append(halfArc1);
            //        unboundCurveLoop.Append(halfArc2);
            //        curveLoops.Add(unboundCurveLoop);
            //        nonUsedCurves.RemoveAt(i);
            //    }
            //}
            List<Curve> nonUsedCurves = new List<Curve>();
            nonUsedCurves.AddRange(curves);

            while (nonUsedCurves.Count != 0)
            {
                CurveLoop curveLoop = new CurveLoop();

                curveLoop.Append(nonUsedCurves[0]);
                XYZ pt0 = nonUsedCurves[0].GetEndPoint(0);
                XYZ pt1 = nonUsedCurves[0].GetEndPoint(1);
                nonUsedCurves.RemoveAt(0);

                tryNumber++;
                bool closedLoop = false;
                int number = 0;




                while (!closedLoop)
                {
                    //for (int i = 1; i < curves.Count; i++)
                    //{
                    //    if (!usedCurves.Contains(i))
                    //    {
                    //        XYZ c1p1 = curveLoop.ElementAt(curveLoop.Count() - 1).GetEndPoint(1);
                    //        //if (adjacentPoints.Where(a => a.X == c1p1.X && a.Y == c1p1.Y && a.Z == c1p1.Z).Count() > 0)
                    //        //{

                    //        //}
                    //        XYZ c2p0 = curves[i].GetEndPoint(0);
                    //        XYZ c2p1 = curves[i].GetEndPoint(1);
                    //        if (c1p1.IsAlmostEqualTo(c2p0))
                    //        {
                    //            curveLoop.Append(curves[i]);
                    //            usedCurves.Add(i);
                    //        }
                    //        else if (c1p1.IsAlmostEqualTo(c2p1))
                    //        {
                    //            curveLoop.Append(curves[i].CreateReversed());
                    //            usedCurves.Add(i);
                    //        }
                    //    }
                    //}
                    for (int i = 0; i < nonUsedCurves.Count; i++)
                    {
                        XYZ c2p0 = nonUsedCurves[i].GetEndPoint(0);
                        XYZ c2p1 = nonUsedCurves[i].GetEndPoint(1);
                        if (pt1.DistanceTo(c2p0) < 0.01)
                        {
                            if (nonUsedCurves[i] is Line)
                            {
                                curveLoop.Append(Line.CreateBound(pt1, c2p1));
                            }
                            else
                            {
                                curveLoop.Append(Arc.Create(pt1, c2p1, nonUsedCurves[i].Evaluate(0.5, true)));
                            }
                            //curveLoop.Append(nonUsedCurves[i]);
                            nonUsedCurves.RemoveAt(i);
                            pt1 = c2p1;
                            number = 0;
                            break;
                        }
                        else if (pt1.DistanceTo(c2p1) < 0.01)
                        {
                            if (nonUsedCurves[i] is Line)
                            {
                                curveLoop.Append(Line.CreateBound(pt1, c2p0));
                            }
                            else
                            {
                                curveLoop.Append(Arc.Create(pt1, c2p0, nonUsedCurves[i].Evaluate(0.5, true)));
                            }
                            //curveLoop.Append(nonUsedCurves[i].CreateReversed());
                            nonUsedCurves.RemoveAt(i);
                            pt1 = c2p0;
                            number = 0;
                            break;
                        }
                    }
                    if (pt0.DistanceTo(pt1) < 0.01)
                    {
                        closedLoop = true;
                        curveLoops.Add(curveLoop);
                    }
                    else
                    {
                        number++;
                        if (number > 1000)
                        {
                            Autodesk.Revit.UI.TaskDialog.Show("erro", "erro");
                            goto finish;
                        }
                    }
                }

            }
        finish:;
            curveLoops = curveLoops.OrderBy(a => a.GetExactLength()).ToList();
            return curveLoops;
        }


        public static Curve JoinCurves(List<Curve> curves)
        {
            List<int> usedCurveIndexes = new List<int>();
            XYZ pt0 = curves[0].GetEndPoint(0);
            XYZ pt1 = curves[0].GetEndPoint(1);
            usedCurveIndexes.Add(0);
            int num = 0;
            while (usedCurveIndexes.Count != curves.Count)
            {
                num++;
                if (num > 1000)
                {
                    return null;
                }
                for (int i = 1; i < curves.Count; i++)
                {
                    if (!usedCurveIndexes.Contains(i))
                    {
                        if (curves[i].GetEndPoint(0).IsAlmostEqualTo(pt0))
                        {
                            usedCurveIndexes.Add(i);
                            pt0 = curves[i].GetEndPoint(1);
                        }
                        else if (curves[i].GetEndPoint(1).IsAlmostEqualTo(pt0))
                        {
                            usedCurveIndexes.Add(i);
                            pt0 = curves[i].GetEndPoint(0);
                        }
                        else if (curves[i].GetEndPoint(0).IsAlmostEqualTo(pt1))
                        {
                            usedCurveIndexes.Add(i);
                            pt1 = curves[i].GetEndPoint(1);
                        }
                        else if (curves[i].GetEndPoint(1).IsAlmostEqualTo(pt1))
                        {
                            usedCurveIndexes.Add(i);
                            pt1 = curves[i].GetEndPoint(0);
                        }
                    }
                }
            }

            if (curves[0] is Arc)
            {
                Curve curve = Arc.Create(pt0, pt1, curves[0].Evaluate(0.5, true));
                return curve;
            }
            else
            {
                Curve curve = Line.CreateBound(pt0, pt1);
                return curve;

            }


        }

        public static List<CurveLoop> OrderCurvesToCurveLoopsClockWise(List<Curve> curves)
        {
            List<CurveLoop> curveLoops = new List<CurveLoop>();
            int tryNumber = 0;
            List<int> usedCurves = new List<int>();

            for (int i = 0; i < curves.Count; i++)
            {
                if (!curves[i].IsBound)
                {
                    Arc arc = curves[i] as Arc;
                    usedCurves.Add(i);
                    CurveLoop unboundCurveLoop = new CurveLoop();
                    XYZ pt1 = arc.Center + XYZ.BasisX * arc.Radius;
                    XYZ pt2 = arc.Center - XYZ.BasisY * arc.Radius;
                    XYZ pt3 = arc.Center - XYZ.BasisX * arc.Radius;
                    XYZ pt4 = arc.Center + XYZ.BasisY * arc.Radius;

                    Curve halfArc1 = Arc.Create(pt1, pt3, pt2);
                    Curve halfArc2 = Arc.Create(pt3, pt1, pt4);
                    unboundCurveLoop.Append(halfArc1);
                    unboundCurveLoop.Append(halfArc2);
                    curveLoops.Add(unboundCurveLoop);
                }
            }



            while (usedCurves.Count != curves.Count)
            {
                CurveLoop curveLoop = new CurveLoop();
                if (curveLoop.Count() == 0)
                {
                    for (int i = 0; i < curves.Count; i++)
                    {
                        if (!usedCurves.Contains(i))
                        {
                            curveLoop.Append(curves[i]);
                            usedCurves.Add(i);
                            break;
                        }
                    }
                }

                tryNumber++;
                bool closedLoop = false;
                int number = 0;

                while (!closedLoop)
                {
                    XYZ curve1Direction = curveLoop.ElementAt(curveLoop.Count() - 1).GetEndPoint(1) - curveLoop.ElementAt(curveLoop.Count() - 1).GetEndPoint(0);

                    for (int i = 1; i < curves.Count; i++)
                    {
                        if (!usedCurves.Contains(i))
                        {
                            XYZ c1p1 = curveLoop.ElementAt(curveLoop.Count() - 1).GetEndPoint(1);
                            XYZ c2p0 = curves[i].GetEndPoint(0);
                            XYZ c2p1 = curves[i].GetEndPoint(1);
                            if (c1p1.IsAlmostEqualTo(c2p0))
                            {
                                curveLoop.Append(curves[i]);
                                usedCurves.Add(i);
                            }
                            else if (c1p1.IsAlmostEqualTo(c2p1))
                            {
                                curveLoop.Append(curves[i].CreateReversed());
                                usedCurves.Add(i);
                            }
                        }
                    }
                    if (curveLoop.Last().GetEndPoint(0).IsAlmostEqualTo(curveLoop.First().GetEndPoint(0)) || curveLoop.Last().GetEndPoint(1).IsAlmostEqualTo(curveLoop.First().GetEndPoint(0)))
                    {
                        closedLoop = true;
                        curveLoops.Add(curveLoop);
                    }
                    else
                    {
                        number++;
                        if (number > 1000)
                        {
                            goto finish;
                        }
                    }
                }

            }
        finish:;
            curveLoops = curveLoops.OrderBy(a => a.GetExactLength()).ToList();
            return curveLoops;
        }

        public static CurveLoop ArrayToLoop(CurveArray array)
        {
            CurveLoop loop = new CurveLoop();
            foreach (Curve item in array)
            {
                loop.Append(item);
            }
            return loop;
        }


    }
    public class FamilyOption : IFamilyLoadOptions
    {
        public bool OnFamilyFound(
          bool familyInUse,
          out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(
          Family sharedFamily,
          bool familyInUse,
          out FamilySource source,
          out bool overwriteParameterValues)
        {
            source = new FamilySource();
            overwriteParameterValues = true;
            return true;
        }
    }

}
