#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System;
using System.Collections.Generic;

#endregion

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class TagJoints : IExternalCommand
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
            View initialView = uidoc.ActiveView;

            bool OK = true;
            string error = "";
            if (!Utils.VerifyIfProjectParameterExists(doc, "Ambiente"))
            {
                error += "\n- Ambiente";
                OK = false;
            }
            if (!OK)
            {
                TaskDialog.Show("ATENÇÃO!", $"Não foi possível executar o comando por não existir no modelo os seguintes parâmetros:\n {error}");
                return Result.Cancelled;
            }

            try
            {

                Element e100 = new FilteredElementCollector(doc)
                    .WhereElementIsElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralFramingTags)
                    .Where(a => a.Name == "100%")
                    .First();

                Element e80 = new FilteredElementCollector(doc)
                    .WhereElementIsElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralFramingTags)
                    .Where(a => a.Name == "80%")
                    .First();

                var window = new SelectAmbienteMVVM(uidoc, SelectAmbientMVVMExecuteCommand.TagJoints);
                //var window = new SelectAmbienteMVVM(floors, "TAGEAR JUNTAS", System.Windows.Visibility.Collapsed);
                window.ShowDialog();
                if (!window.Execute)
                {
                    return Result.Cancelled;
                }

                List<string> selectedAmbientes = new List<string>();
                foreach (string item in window.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name))
                {
                    selectedAmbientes.Add(item);
                }

                XYZ rightDirection = initialView.RightDirection;
                XYZ upDirection = initialView.UpDirection;
                Options options = new Options
                {
                    View = initialView,
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = true
                };
                ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);

                TransactionGroup tg = new TransactionGroup(doc, "Tagear Juntas");
                tg.Start();

                foreach (var ambiente in selectedAmbientes)
                {
                    List<Element> joints = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .Where(a => a.LookupParameter("Ambiente").AsString() == ambiente && !a.Name.Contains("JEP") && (a.Location as LocationCurve).Curve is Line)
                        .ToList();

                    List<Element> curveJoints = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .Where(a => a.LookupParameter("Ambiente").AsString() == ambiente && !a.Name.Contains("JEP") && (a.Location as LocationCurve).Curve is Arc)
                        .ToList();

                    List<GroupOfJoints> groups = new List<GroupOfJoints>();

                    List<int> usedJoints = new List<int>();
                    groups.Add(new GroupOfJoints(joints[0]));
                    usedJoints.Add(0);
                    int groupContador = 0;
                    while (usedJoints.Count != joints.Count)
                    {
                        for (int j = 1; j < joints.Count; j++)
                        {
                            if (!usedJoints.Contains(j))
                            {
                                if (groups[groupContador].TryInsertNewJoint(joints[j], 3.28084))
                                {
                                    usedJoints.Add(j);
                                    j = 0;
                                }
                            }
                        }
                        for (int k = 1; k < joints.Count; k++)
                        {
                            if (!usedJoints.Contains(k))
                            {
                                groups.Add(new GroupOfJoints(joints[k]));
                                groupContador++;
                                usedJoints.Add(k);
                                break;
                            }
                        }

                    }

                    List<GroupOfJoints> verticalGroups = groups.Where(a => a.Orientation == JointOrientation.Vertical).OrderBy(a => a.DoubleX).ToList();
                    foreach (var group in verticalGroups)
                    {
                        if (group.Ep1.Y > group.Ep0.Y)
                        {
                            group.OrderedJoints.Reverse();
                        }
                    }
                    List<GroupOfJoints> horizontalGroups = groups.Where(a => a.Orientation == JointOrientation.Horizontal).OrderBy(a => a.DoubleY).ToList();
                    foreach (var group in horizontalGroups)
                    {
                        if (group.Ep0.X > group.Ep1.X)
                        {
                            group.OrderedJoints.Reverse();
                        }
                    }

                    using (Transaction tx = new Transaction(doc, "Create Tags"))
                    {
                        tx.Start();
                        int jump = 0;
                        foreach (var group in verticalGroups)
                        {
                            for (int i = jump; i < group.OrderedJoints.Count; i++)
                            {
                                GeometryElement wholeGridGeometry = group.OrderedJoints[i].get_Geometry(options);
                                foreach (GeometryObject geomObj in wholeGridGeometry)
                                {
                                    if (geomObj is Line)
                                    {
                                        Line line = geomObj as Line;
                                        if (line.Length > 2)
                                        {
                                            IndependentTag tag = null;
                                            if (doc.GetElement(group.OrderedJoints[i].GetTypeId()).LookupParameter("Legenda").AsString().Length > 1)
                                            {
                                                tag = IndependentTag.Create(doc, e80.Id, initialView.Id, new Reference(group.OrderedJoints[i]), false, TagOrientation.Vertical, (geomObj as Line).Evaluate(0.5, true));
                                            }
                                            else
                                            {
                                                tag = IndependentTag.Create(doc, e100.Id, initialView.Id, new Reference(group.OrderedJoints[i]), false, TagOrientation.Vertical, (geomObj as Line).Evaluate(0.5, true));
                                            }
                                            if (group.OrderedJoints[index: i].Name.Contains("JE"))
                                            {
                                                tag.TagHeadPosition -= XYZ.BasisX;
                                            }

                                        }
                                        break;
                                    }
                                }
                                i++;
                            }
                            if (jump == 0)
                            {
                                jump = 1;
                            }
                            else
                            {
                                jump = 0;
                            }
                        }
                        jump = 0;
                        foreach (var group in horizontalGroups)
                        {
                            for (int i = jump; i < group.OrderedJoints.Count; i++)
                            {
                                GeometryElement wholeGridGeometry = group.OrderedJoints[i].get_Geometry(options);
                                foreach (GeometryObject geomObj in wholeGridGeometry)
                                {
                                    if (geomObj is Line)
                                    {
                                        Line line = geomObj as Line;
                                        if (line.Length > 2)
                                        {
                                            IndependentTag tag = null;
                                            if (doc.GetElement(group.OrderedJoints[i].GetTypeId()).LookupParameter("Legenda").AsString().Length > 1)
                                            {
                                                tag = IndependentTag.Create(doc, e80.Id, initialView.Id, new Reference(group.OrderedJoints[i]), false, TagOrientation.Vertical, (geomObj as Line).Evaluate(0.5, true));
                                            }
                                            else
                                            {
                                                tag = IndependentTag.Create(doc, e100.Id, initialView.Id, new Reference(group.OrderedJoints[i]), false, TagOrientation.Vertical, (geomObj as Line).Evaluate(0.5, true));
                                            }
                                            if (group.OrderedJoints[i].Name.Contains("JE"))
                                            {
                                                tag.TagHeadPosition += XYZ.BasisY;
                                            }
                                        }
                                    }
                                }
                                i++;
                            }
                            if (jump == 0)
                            {
                                jump = 1;
                            }
                            else
                            {
                                jump = 0;
                            }
                        }
                        tx.Commit();
                    }
                }

                tg.Assimilate();

                return Result.Succeeded;

            }
            catch (Exception e)
            {
                TaskDialog.Show("ERRO", $"Houve um erro não mapeado na execução do plug-in, contate os desenvolvedores.\n\n{e.Message}");
                return Result.Cancelled;
            }
        }
    }
}
