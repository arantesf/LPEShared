using Autodesk.Revit.DB;

namespace Revit.Common
{
    public class CurveAndProjection
    {
        public Curve Curve { get; set; }
        public Curve Projection { get; set; }
        public CurveAndProjection(Curve curve)
        {
            Curve = curve;
            Projection = Utils.GetCurveInPlane(curve, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero));
        }
        public CurveAndProjection(Curve curve, Floor floor)
        {
            Projection = Utils.GetCurveInPlane(curve, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero));
            Curve = Line.CreateBound(floor.GetVerticalProjectionPoint(curve.GetEndPoint(0), FloorFace.Top), floor.GetVerticalProjectionPoint(curve.GetEndPoint(1), FloorFace.Top));
        }
    }
       
}
