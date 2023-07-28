using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Revit.Common
{
    public enum JointOrientation
    {
        Vertical,
        Horizontal
    }

    public class GroupOfJoints
    {
        public XYZ Direction { get; set; }
        public XYZ Ep0 { get; set; }
        public XYZ Ep1 { get; set; }
        public List<Element> OrderedJoints { get; set; } = new List<Element>();
        public ElementType Type { get; set; }
        public double DoubleY { get; set; } = 0;
        public double DoubleX { get; set; } = 0;

        public JointOrientation Orientation { get; set; } = JointOrientation.Vertical;
        public GroupOfJoints(Element joint)
        {
            Curve curve = (joint.Location as LocationCurve).Curve;
            Direction = ((curve as Line).Direction - (curve as Line).Direction.Z * XYZ.BasisZ).Normalize();
            Ep0 = curve.GetEndPoint(0) - curve.GetEndPoint(0).Z * XYZ.BasisZ;
            Ep1 = curve.GetEndPoint(1) - curve.GetEndPoint(1).Z * XYZ.BasisZ;
            OrderedJoints.Add(joint);
            Type = joint.Document.GetElement(joint.GetTypeId()) as ElementType;
            if (Direction.Y > 0.8 || Direction.Y < -0.8)
            {
                DoubleX = Ep0.X;
            }
            else
            {
                Orientation = JointOrientation.Horizontal;
                DoubleY = Ep0.Y;
            }
        }
        public bool TryInsertNewJoint(Element joint, double tolerance)
        {
            if (Type.Id == joint.GetTypeId())
            {
                Curve curve = (joint.Location as LocationCurve).Curve;
                XYZ curveDirection = ((curve as Line).Direction - (curve as Line).Direction.Z * XYZ.BasisZ).Normalize();
                if (curveDirection.IsAlmostEqualTo(Direction) || curveDirection.IsAlmostEqualTo(-Direction))
                {
                    XYZ ep0 = curve.GetEndPoint(0) - curve.GetEndPoint(0).Z * XYZ.BasisZ;
                    XYZ ep1 = curve.GetEndPoint(1) - curve.GetEndPoint(1).Z * XYZ.BasisZ;
                    if (ep0.DistanceTo(Ep1) < tolerance)
                    {
                        OrderedJoints.Add(joint);
                        Ep1 = ep1;
                        return true;
                    }
                    else if (ep1.DistanceTo(Ep1) < tolerance)
                    {
                        OrderedJoints.Add(joint);
                        Ep1 = ep0;
                        return true;
                    }
                    else if (ep0.DistanceTo(Ep0) < tolerance)
                    {
                        List<Element> newOrderedJoints = new List<Element>
                        {
                            joint
                        };
                        newOrderedJoints.AddRange(OrderedJoints);
                        OrderedJoints = newOrderedJoints;
                        Ep0 = ep1;
                        return true;
                    }
                    else if (ep1.DistanceTo(Ep0) < tolerance)
                    {
                        List<Element> newOrderedJoints = new List<Element>
                        {
                            joint
                        };
                        newOrderedJoints.AddRange(OrderedJoints);
                        OrderedJoints = newOrderedJoints;
                        Ep0 = ep0;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
