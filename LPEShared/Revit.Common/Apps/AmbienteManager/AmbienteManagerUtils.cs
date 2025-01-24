// Decompiled with JetBrains decompiler
// Type: Revit.Common.AmbienteManagerUtils
// Assembly: LPE, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E06EE1B-06C5-4A71-9629-ACF2EB60ECA2
// Assembly location: C:\ProgramData\Autodesk\Revit\Addins\2024\LPE\LPE.dll

using Autodesk.Revit.DB;
using Revit.Common.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Revit.Common
{
    public static class AmbienteManagerUtils
    {
        public static List<TagViewModel> GetTags(Document doc)
        {
            List<TagViewModel> tagViewModels = new List<TagViewModel>();
            List<ViewSchedule> schedules = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>()
                .Where(view => view.LookupParameter("LPE_AMBIENTE").AsString() == "TAG TEXTOS")
                .ToList();
            foreach (var schedule in schedules)
            {
                ScheduleDefinition scheduleDefinition = schedule.Definition;
                var keyName = schedule.KeyScheduleParameterName;
                var tagName = scheduleDefinition.GetField(scheduleDefinition.GetFieldId(1)).GetName();
                List<Element> linhas = new FilteredElementCollector(doc, schedule.Id)
                    .ToList();
                foreach (var linha in linhas)
                {
                    Parameter parameter = linha.LookupParameter(tagName);
                    if (parameter.HasValue)
                    {
                        tagViewModels.Add(new TagViewModel(linha.Name, parameter.AsString()));
                    }
                }

            }
            return tagViewModels;
        }

        public static List<PisoLegendaModel> GetLegendas(Document doc)
        {
            List<PisoLegendaModel> pisoLegendaModels = new List<PisoLegendaModel>();

            ViewPlan templateDocumentacao = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .FirstOrDefault(x => x.Name == "LPE_PLANTA - DOCUMENTAÇÃO") as ViewPlan;

            if (templateDocumentacao != null)
            {
                foreach (ParameterFilterElement filter in templateDocumentacao.GetFilters().Select(x => doc.GetElement(x)))
                {
                    if (filter.Name.Contains("PISO "))
                    {
                        var myColor = templateDocumentacao.GetFilterOverrides(filter.Id).SurfaceForegroundPatternColor;
                        string hexColor = "#" + myColor.Red.ToString("X2") + myColor.Green.ToString("X2") + myColor.Blue.ToString("X2");
                        pisoLegendaModels.Add(new PisoLegendaModel(filter.Name, hexColor));
                    }
                }
            }

            return pisoLegendaModels;
        }

        public static List<FibraViewModel> GetFibras(Document doc)
        {
            List<FibraViewModel> fibraViewModels = new List<FibraViewModel>();
            ViewSchedule schedule = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>()
                .Where(view => view.Name == "TIPO DE FIBRA")
                .FirstOrDefault();
            List<Element> linhas = new FilteredElementCollector(doc, schedule.Id)
                .ToList();
            foreach (var linha in linhas)
            {
                Parameter parameter = linha.LookupParameter("TIPO DE FIBRA");
                if (parameter.HasValue)
                {
                    fibraViewModels.Add(
                        new FibraViewModel(linha.LookupParameter("TIPO DE FIBRA").AsString(),
                                           linha.LookupParameter("Dosagem").AsDouble(),
                                           linha.LookupParameter("Fr1").AsDouble().ToString(),
                                           linha.LookupParameter("Fr4").AsDouble().ToString())
                        );
                }
            }
            return fibraViewModels;
        }

        public static List<int> GetTelas(Document doc)
        {
            List<int> telas = new List<int>();
            ViewSchedule schedule = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>()
                .Where(view => view.Name == "TABELA DE TELAS")
                .FirstOrDefault();
            List<Element> linhas = new FilteredElementCollector(doc, schedule.Id)
                .ToList();
            foreach (var linha in linhas)
            {
                Parameter parameter = linha.LookupParameter("TIPO DE TELA (QXXX)");
                if (parameter.HasValue)
                {
                    telas.Add(parameter.AsInteger());
                }
            }
            return telas;
        }

        public static List<double> GetEmendas(Document doc)
        {
            List<double> emendas = new List<double>();
            ViewSchedule schedule = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>()
                .Where(view => view.Name == "EMENDA DE TELA")
                .FirstOrDefault();
            List<Element> linhas = new FilteredElementCollector(doc, schedule.Id)
                .ToList();
            foreach (var linha in linhas)
            {
                Parameter parameter = linha.LookupParameter("Emenda");
                if (parameter.HasValue)
                {
                    emendas.Add(parameter.AsDouble());
                }
            }
            return emendas;
        }

        public static List<string> GetTratamentoSuperficial(Document doc)
        {
            List<string> tratamentos = new List<string>();
            ViewSchedule schedule = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>()
                .Where(view => view.Name == "TRATAMENTO SUPERFICIAL")
                .FirstOrDefault();
            List<Element> linhas = new FilteredElementCollector(doc, schedule.Id)
                .ToList();
            foreach (var linha in linhas)
            {
                Parameter parameter = linha.LookupParameter("TIPO DE TRATAMENTO SUPERFICIAL");
                if (parameter.HasValue)
                {
                    tratamentos.Add(parameter.AsString());
                }
            }
            return tratamentos;
        }

        public static IList<CompoundStructureLayer> GetCompoundStructureLayer(Document doc, FullAmbienteViewModel ambienteViewModel, FloorType floorType)
        {
            IList<CompoundStructureLayer> orderedExistingLayers = floorType
                .GetCompoundStructure()
                .GetLayers()
                .OrderBy(x => x.LayerId)
                .ToList();

            IList<CompoundStructureLayer> layers = new List<CompoundStructureLayer>();
            for (int i = 0; i < ambienteViewModel.FloorMatriz.Layers.Count; i++)
            {
                var tableLayer = ambienteViewModel.FloorMatriz.Layers.ElementAt(i);
                if (!tableLayer.IsEnabled) continue;
                bool isMatch = false;
                foreach (CompoundStructureLayer existingLayer in orderedExistingLayers)
                {
                    double existingWidthInCm = Math.Round(UnitUtils.ConvertFromInternalUnits(existingLayer.Width, UnitTypeId.Centimeters), 1);
                    if (existingWidthInCm == tableLayer.Width && doc.GetElement(existingLayer.MaterialId).Name == tableLayer.SelectedMaterial)
                    {
                        isMatch = true;
                        layers.Add(existingLayer);
                        break;
                    }
                }
                if (!isMatch)
                {
                    CompoundStructureLayer newLayer = new CompoundStructureLayer();
                    try
                    {
                        newLayer.MaterialId = new FilteredElementCollector(doc)
                            .OfClass(typeof(Material))
                            .First(m => m.Name == tableLayer.SelectedMaterial)
                            .Id;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show($"Erro ao aplicar material {tableLayer.SelectedMaterial}");
                    }
                    newLayer.Width = UnitUtils.ConvertToInternalUnits(tableLayer.Width, UnitTypeId.Centimeters);
                    switch (tableLayer.SelectedCamadaTipo)
                    {
                        case "Bloco Intertravado":
                            newLayer.Function = MaterialFunctionAssignment.Structure;
                            break;
                        case "Concreto":
                            newLayer.Function = MaterialFunctionAssignment.Structure;
                            break;
                        case "Concreto Asfáltico":
                            newLayer.Function = MaterialFunctionAssignment.Structure;
                            break;
                        case "Filme Plástico":
                            newLayer.Function = MaterialFunctionAssignment.Membrane;
                            break;
                        case "Imprimação Asfáltica":
                            newLayer.Function = MaterialFunctionAssignment.Membrane;
                            break;
                        case "Pintura de ligação":
                            newLayer.Function = MaterialFunctionAssignment.Membrane;
                            break;
                        default:
                            newLayer.Function = MaterialFunctionAssignment.Substrate;
                            break;
                    }
                    layers.Add(newLayer);
                }

            }
            return layers;
        }
        public static void SetFloorType(Document doc, FullAmbienteViewModel ambienteViewModel, bool duplicate)
        {
            if (ambienteViewModel.BoolReforcoDeTela)
            {
                return;
            }
            FloorType floorType = null;
            if (ambienteViewModel.PisoId.IntegerValue != -1)
            {
                floorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .FirstOrDefault(a => a.Id == ambienteViewModel.PisoId);
            }
            else
            {
                var existingFloorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .FirstOrDefault(a => a.Name == ambienteViewModel.TipoDePiso);
                if (existingFloorType != null)
                {
                    floorType = existingFloorType;
                }
                else
                {
                    if (ambienteViewModel.StoredFloorMatriz?.FloorName != null)
                    {
                        floorType = new FilteredElementCollector(doc)
                            .OfClass(typeof(FloorType))
                            .Cast<FloorType>()
                            .First(a => a.Name == ambienteViewModel.StoredFloorMatriz.FloorName);
                        floorType = floorType.Duplicate(ambienteViewModel.TipoDePiso) as FloorType;
                    }
                    else
                    {
                        floorType = new FilteredElementCollector(doc)
                            .OfClass(typeof(FloorType))
                            .Cast<FloorType>()
                            .First(a => a.Name == ambienteViewModel.FloorMatriz.FloorName);
                        if (!ambienteViewModel.BoolReforcoDeTela)
                        {
                            floorType = floorType.Duplicate(ambienteViewModel.TipoDePiso) as FloorType;
                        }
                    }
                }
            }
            if (floorType.Name != ambienteViewModel.TipoDePiso && !ambienteViewModel.TipoDePiso.Contains("REF"))
            {
                floorType.Name = ambienteViewModel.TipoDePiso;
            }
            ambienteViewModel.FloorMatriz.FloorName = floorType.Name;

            IList<CompoundStructureLayer> layers = GetCompoundStructureLayer(doc, ambienteViewModel, floorType);
            CompoundStructure compoundStructure1 = CompoundStructure.CreateSimpleCompoundStructure(layers);
            compoundStructure1.StructuralMaterialIndex = layers.ToList().FindIndex(x => x.Function == MaterialFunctionAssignment.Structure);
            int coreLayerIndex = ambienteViewModel.FloorMatriz.Layers.ToList().FindIndex(x => !x.IsEnabled);
            int numberOfInteriorLayers = ambienteViewModel.FloorMatriz.Layers.ToList().Skip(coreLayerIndex).Count(x => x.IsEnabled);
            compoundStructure1.SetNumberOfShellLayers(ShellLayerType.Interior, numberOfInteriorLayers);
            compoundStructure1.EndCap = EndCapCondition.NoEndCap;
            floorType.LookupParameter("Legenda Piso").Set(int.Parse(ambienteViewModel.SelectedLegenda.Name.Replace("PISO ", "")));
            floorType.SetCompoundStructure(compoundStructure1);
        }

        public static Dictionary<string, List<Element>> GetPisoTypes(Document doc)
        {
            List<Element> list = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfCategory(BuiltInCategory.OST_Floors)
                .OrderBy(a => a.Name)
                .ToList();

            Dictionary<string, List<Element>> pisoTypes = new Dictionary<string, List<Element>>();
            foreach (Element element in list)
            {
                string str = element.LookupParameter("LPE_TIPOLOGIA (ART)").AsString();
                switch (str)
                {
                    case "Pavimento Asfáltico":
                        if (pisoTypes.ContainsKey("Pavimento Asfáltico"))
                        {
                            pisoTypes["Pavimento Asfáltico"].Add(element);
                            continue;
                        }
                        pisoTypes.Add("Pavimento Asfáltico", new List<Element>()
            {
              element
            });
                        continue;
                    default:
                        if (str != null && str == "Pavimento Intertravado")
                        {
                            if (pisoTypes.ContainsKey("Pavimento Intertravado"))
                            {
                                pisoTypes["Pavimento Intertravado"].Add(element);
                                continue;
                            }
                            pisoTypes.Add("Pavimento Intertravado", new List<Element>()
              {
                element
              });
                            continue;
                        }
                        if (pisoTypes.ContainsKey("Concreto"))
                        {
                            pisoTypes["Concreto"].Add(element);
                            continue;
                        }
                        pisoTypes.Add("Concreto", new List<Element>()
            {
              element
            });
                        continue;
                }
            }
            return pisoTypes;
        }

        public static Dictionary<string, List<Element>> GetPisoMaterials(Document doc)
        {
            List<Element> list = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Materials)
                .OrderBy(a => a.Name)
                .ToList();

            Dictionary<string, List<Element>> pisoMaterials = new Dictionary<string, List<Element>>();
            foreach (Element element in list)
            {
                bool flag1 = element.LookupParameter("LPE_FIBRA").AsInteger() == 1;
                bool flag2 = element.LookupParameter("LPE_PAVIMENTO FLEXÍVEL").AsInteger() == 1;
                bool flag3 = element.LookupParameter("LPE_TELA DUPLA").AsInteger() == 1;
                bool flag4 = element.LookupParameter("LPE_TELA SIMPLES").AsInteger() == 1;
                if (flag1 || flag2 || flag3 || flag4)
                {
                    if (flag1)
                    {
                        if (pisoMaterials.ContainsKey("LPE_FIBRA"))
                            pisoMaterials["LPE_FIBRA"].Add(element);
                        else
                            pisoMaterials.Add("LPE_FIBRA", new List<Element>()
              {
                element
              });
                    }
                    if (flag2)
                    {
                        if (pisoMaterials.ContainsKey("LPE_PAVIMENTO FLEXÍVEL"))
                            pisoMaterials["LPE_PAVIMENTO FLEXÍVEL"].Add(element);
                        else
                            pisoMaterials.Add("LPE_PAVIMENTO FLEXÍVEL", new List<Element>()
              {
                element
              });
                    }
                    if (flag3)
                    {
                        if (pisoMaterials.ContainsKey("LPE_TELA DUPLA"))
                            pisoMaterials["LPE_TELA DUPLA"].Add(element);
                        else
                            pisoMaterials.Add("LPE_TELA DUPLA", new List<Element>()
              {
                element
              });
                    }
                    if (flag4)
                    {
                        if (pisoMaterials.ContainsKey("LPE_TELA SIMPLES"))
                            pisoMaterials["LPE_TELA SIMPLES"].Add(element);
                        else
                            pisoMaterials.Add("LPE_TELA SIMPLES", new List<Element>()
              {
                element
              });
                    }
                }
            }
            return pisoMaterials;
        }

        public static Dictionary<MaterialClass, List<string>> GetMaterialsByClass(Document doc)
        {
            Dictionary<MaterialClass, List<string>> source = new Dictionary<MaterialClass, List<string>>();
            source.Add(MaterialClass.Concreto, new List<string>());
            source.Add(MaterialClass.FilmePlastico, new List<string>());
            source.Add(MaterialClass.Base, new List<string>());
            source.Add(MaterialClass.SubBase, new List<string>());
            source.Add(MaterialClass.ReforcoSubleito, new List<string>());
            source.Add(MaterialClass.CamadaIsolamento, new List<string>());
            source.Add(MaterialClass.CamadaVentilacao, new List<string>());
            source.Add(MaterialClass.BlocoIntertravado, new List<string>());
            source.Add(MaterialClass.CamadaAssentamento, new List<string>());
            source.Add(MaterialClass.ConcretoAsfaltico, new List<string>());
            source.Add(MaterialClass.PinturaDeLigacao, new List<string>());
            source.Add(MaterialClass.ImprimacaoAsflatica, new List<string>());
            source.Add(MaterialClass.Todos, new List<string>());
            foreach (Material material in new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Materials).Cast<Material>().ToList<Material>())
            {
                source[MaterialClass.Todos].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_CONCRETO").AsInteger() == 1)
                    source[MaterialClass.Concreto].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_FILME PLÁSTICO").AsInteger() == 1)
                    source[MaterialClass.FilmePlastico].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_BASE").AsInteger() == 1)
                    source[MaterialClass.Base].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_SUB-BASE").AsInteger() == 1)
                    source[MaterialClass.SubBase].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_REFORÇO DE SUBLEITO").AsInteger() == 1)
                    source[MaterialClass.ReforcoSubleito].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_CAMADA DE ISOLAMENTO").AsInteger() == 1)
                    source[MaterialClass.CamadaIsolamento].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_CAMADA DE VENTILAÇÃO").AsInteger() == 1)
                    source[MaterialClass.CamadaVentilacao].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_BLOCO INTERTRAVADO").AsInteger() == 1)
                    source[MaterialClass.BlocoIntertravado].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_CAMADA DE ASSENTAMENTO").AsInteger() == 1)
                    source[MaterialClass.CamadaAssentamento].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_CONCRETO ASFÁLTICO").AsInteger() == 1)
                    source[MaterialClass.ConcretoAsfaltico].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_PINTURA DE LIGAÇÃO").AsInteger() == 1)
                    source[MaterialClass.PinturaDeLigacao].Add(((Element)material).Name);
                if (((Element)material).LookupParameter("LPE_MAT_IMPRIMAÇÃO ASFÁLTICA").AsInteger() == 1)
                    source[MaterialClass.ImprimacaoAsflatica].Add(((Element)material).Name);
            }
            for (int index = 0; index < source.Count; ++index)
            {
                MaterialClass key = source.ElementAt<KeyValuePair<MaterialClass, List<string>>>(index).Key;
                source[key] = source[key].OrderBy<string, string>((Func<string, string>)(item => item)).ToList<string>();
            }
            return source;
        }

        public static Dictionary<FloorMatrizClass, List<FloorMatriz>> GetFloorMatrizes(
          Document doc,
          Dictionary<MaterialClass, List<string>> materialsDict)
        {
            Dictionary<FloorMatrizClass, List<FloorMatriz>> source = new Dictionary<FloorMatrizClass, List<FloorMatriz>>();
            source.Add(FloorMatrizClass.ConcretoSobreSolo, new List<FloorMatriz>());
            source.Add(FloorMatrizClass.FundacaoDireta, new List<FloorMatriz>());
            source.Add(FloorMatrizClass.PavIntertravado, new List<FloorMatriz>());
            source.Add(FloorMatrizClass.PavAsfaltico, new List<FloorMatriz>());
            source.Add(FloorMatrizClass.Nenhum, new List<FloorMatriz>());
            foreach (FloorType floorType in ((IEnumerable)new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfClass(typeof(FloorType))
                .Cast<FloorType>().ToList()))
            {
                if (!floorType.Name.Contains("0. MODELO"))
                {
                    continue;
                }
                switch (((Element)floorType).LookupParameter("LPE_TIPOLOGIA (ART)").AsString())
                {
                    case "Concreto Sobre Solo":
                        FloorMatriz floorMatriz1 = new FloorMatriz();
                        floorMatriz1.GetFloorTypeData(floorType, materialsDict);
                        source[FloorMatrizClass.ConcretoSobreSolo].Add(floorMatriz1);
                        continue;
                    case "Fundação Direta":
                        FloorMatriz floorMatriz2 = new FloorMatriz();
                        floorMatriz2.GetFloorTypeData(floorType, materialsDict);
                        source[FloorMatrizClass.FundacaoDireta].Add(floorMatriz2);
                        continue;
                    case "Pavimento Asfáltico":
                        FloorMatriz floorMatriz3 = new FloorMatriz();
                        floorMatriz3.GetFloorTypeData(floorType, materialsDict);
                        source[FloorMatrizClass.PavAsfaltico].Add(floorMatriz3);
                        continue;
                    case "Pavimento Intertravado":
                        FloorMatriz floorMatriz4 = new FloorMatriz();
                        floorMatriz4.GetFloorTypeData(floorType, materialsDict);
                        source[FloorMatrizClass.PavIntertravado].Add(floorMatriz4);
                        continue;
                    default:
                        FloorMatriz floorMatriz5 = new FloorMatriz();
                        floorMatriz5.GetFloorTypeData(floorType, materialsDict);
                        source[FloorMatrizClass.Nenhum].Add(floorMatriz5);
                        continue;
                }
            }
            for (int index = 0; index < source.Count; ++index)
            {
                FloorMatrizClass key = source.ElementAt<KeyValuePair<FloorMatrizClass, List<FloorMatriz>>>(index).Key;
                source[key] = source[key].OrderBy<FloorMatriz, string>((Func<FloorMatriz, string>)(item => item.FloorName)).ToList<FloorMatriz>();
            }
            return source;
        }

        public static void DuplicateViewSchedules(Document doc, string newAmbienteName)
        {
            //ElementId elementId = new ElementId(505819);
            //List<ViewSchedule> list = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.LookupParameter("LPE_AMBIENTE").AsString() == "AMBIENTE XX").ToList();
            //Transaction transaction = new Transaction(doc, "Duplicar tabelas");
            //transaction.Start();
            //foreach (ViewSchedule viewSchedule1 in list)
            //{
            //    ViewSchedule viewSchedule = viewSchedule1;
            //    if (!new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.Name == viewSchedule.Name + " - " + newAmbienteName).ToList().Any())
            //    {
            //        ViewSchedule element = doc.GetElement(((Autodesk.Revit.DB.View)viewSchedule).Duplicate((ViewDuplicateOption.))) as ViewSchedule;
            //        element.Name = viewSchedule.Name + " - " + newAmbienteName;
            //        element.LookupParameter("LPE_AMBIENTE").Set(newAmbienteName);
            //        ScheduleFieldId scheduleFieldId = null;
            //        for (int index = 0; index < viewSchedule.Definition.GetFieldCount(); ++index)
            //        {
            //            if (viewSchedule.Definition.GetField(index).GetName() == "Ambiente")
            //            {
            //                scheduleFieldId = viewSchedule.Definition.GetFieldId(index);
            //                break;
            //            }
            //        }
            //        ScheduleFilter scheduleFilter = new ScheduleFilter(scheduleFieldId, (ScheduleFilterType)2, newAmbienteName);
            //        element.Definition.AddFilter(scheduleFilter);
            //    }
            //}
            //transaction.Commit();
        }

        public static void DeleteViewSchedules(Document doc, string ambienteName)
        {
            ElementId elementId = new ElementId(505819);
            List<ElementId> list = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.LookupParameter("LPE_AMBIENTE").AsString() == ambienteName).Select(view => view.Id).ToList();
            if (!list.Any())
                return;
            Transaction transaction = new Transaction(doc, "Deletar tabelas");
            transaction.Start();
            doc.Delete(list);
            transaction.Commit();
        }

        public static bool GetAmbientes(Document doc, out List<FullAmbienteViewModel> fullAmbienteViewModels)
        {
            ViewSchedule viewSchedule1 = null;
            List<Element> tipoDePisoRows = null;
            ViewSchedule viewSchedule2 = null;
            Dictionary<string, Element> itemDeDetalheDict = null;
            ViewSchedule viewSchedule3 = null;
            Dictionary<string, Element> tipoDeJuntaDict = null;
            Dictionary<string, Element> dictionary4 = null;
            fullAmbienteViewModels = new List<FullAmbienteViewModel>();
            try
            {
                viewSchedule1 = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.Name == "LPE_TIPO DE PISO").FirstOrDefault();
                tipoDePisoRows = new FilteredElementCollector(doc, viewSchedule1.Id).ToList();
                viewSchedule2 = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.Name == "LPE_ITENS DE DETALHE SEÇÕES").FirstOrDefault();
                itemDeDetalheDict = new FilteredElementCollector(doc, viewSchedule2.Id).ToDictionary(e => e.Name);
                viewSchedule3 = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.Name == "LPE_TIPO DE JUNTA").FirstOrDefault();
                tipoDeJuntaDict = new FilteredElementCollector(doc, viewSchedule3.Id).ToDictionary(e => e.Name);
                dictionary4 = new FilteredElementCollector(doc).WhereElementIsElementType().OfCategory(BuiltInCategory.OST_Floors).ToDictionary(e => e.Name);
            }
            catch (Exception EX)
            {
                return false;
            }

            tipoDePisoRows = tipoDePisoRows.OrderBy(x => x.Name.Contains("REF_") ? 1 : 0).ToList();
            foreach (Element tipoDePiso in tipoDePisoRows)
            {
                string name = tipoDePiso.Name;
                Element itensDeDetalheElement = null;
                Element juntaElement = null;
                Element pisoElementType = null;
                if (itemDeDetalheDict.ContainsKey(name))
                    itensDeDetalheElement = itemDeDetalheDict[name];
                if (tipoDeJuntaDict.ContainsKey(name))
                    juntaElement = tipoDeJuntaDict[name];
                if (dictionary4.ContainsKey(name))
                    pisoElementType = dictionary4[name];
                FullAmbienteViewModel ambienteViewModel = new FullAmbienteViewModel(tipoDePiso, itensDeDetalheElement, juntaElement, pisoElementType, doc);
                
                fullAmbienteViewModels.Add(ambienteViewModel);
            }

            foreach (var ambienteViewModel in fullAmbienteViewModels)
            {
                if (ambienteViewModel.BoolReforcoDeTela)
                {
                    try
                    {
                        bool refFIbra = ambienteViewModel.BoolFibra && ambienteViewModel.BoolTelaSuperior;
                        string baseName = refFIbra ?
                            ambienteViewModel.TipoDePiso.Split(new string[] { " // REF_FIBRA" }, StringSplitOptions.None).First() + $" // REF_FIBRA ({ambienteViewModel.TelaSuperior})":
                            ambienteViewModel.TipoDePiso.Split(new string[] { " //" }, StringSplitOptions.None).First();
                        FullAmbienteViewModel parent = fullAmbienteViewModels
                            .Where(x => x.TipoDePiso.Equals(baseName)) ? 
                            .FirstOrDefault();
                        if (parent == null) continue;
                        ambienteViewModel.ParentAmbienteViewModelGUID = parent.GUID;
                        FloorType floorType = new FilteredElementCollector(doc)
                            .OfClass(typeof(FloorType))
                            .Cast<FloorType>()
                            .FirstOrDefault(x => parent.TipoDePiso == x.Name);

                        if (floorType != null)
                        {
                            FloorMatriz floorMatriz = new FloorMatriz();
                            floorMatriz.GetFloorTypeData(floorType, GlobalVariables.MaterialsByClass, ambienteViewModel);
                            ambienteViewModel.FloorMatriz = floorMatriz;
                        }
                        var pisoLegendaModel = ambienteViewModel.Legendas.Where(x => x.Name == $"PISO {floorType?.LookupParameter("Legenda Piso")?.AsInteger()}")?.FirstOrDefault();

                        if (pisoLegendaModel != null)
                        {
                            ambienteViewModel.SelectedLegenda = pisoLegendaModel;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return true;
        }

        public static Element SetAmbienteLPETipoDePiso(
          Element element,
          FullAmbienteViewModel fullAmbienteViewModel)
        {
            element.LookupParameter("Ambiente")?.Set(fullAmbienteViewModel.Ambiente);
            element.LookupParameter("(s/n) Espaçador Inf.")?.Set(fullAmbienteViewModel.BoolEspacadorInferior ? 1 : 0);
            element.LookupParameter("(s/n) Espaçador Sup.")?.Set(fullAmbienteViewModel.BoolEspacadorSuperior ? 1 : 0);
            element.LookupParameter("(s/n) Fibra")?.Set(fullAmbienteViewModel.BoolFibra ? 1 : 0);
            element.LookupParameter("LPE_VEÍCULOS PESADOS")?.Set(fullAmbienteViewModel.BoolLPEVeiculosPesados ? 1 : 0);
            element.LookupParameter("LPE_Carga")?.Set(fullAmbienteViewModel.LPECarga);
            element.LookupParameter("(s/n) Ref. Tela Inferior")?.Set(fullAmbienteViewModel.BoolReforcoTelaInferior ? 1 : 0);
            element.LookupParameter("(s/n) Ref. Tela Superior")?.Set(fullAmbienteViewModel.BoolReforcoTelaSuperior ? 1 : 0);
            element.LookupParameter("(s/n) Tela Inferior")?.Set(fullAmbienteViewModel.BoolTelaInferior ? 1 : 0);
            element.LookupParameter("(s/n) Tela Superior")?.Set(fullAmbienteViewModel.BoolTelaSuperior ? 1 : 0);
            element.LookupParameter("(s/n) Tratamento Superficial")?.Set(fullAmbienteViewModel.BoolTratamentoSuperficial ? 1 : 0);
            element.LookupParameter("Comprimento Placa")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.ComprimentoDaPlaca, UnitTypeId.Meters));
            element.LookupParameter("Dosagem da Fibra (kg/m\u00B3)")?.Set(fullAmbienteViewModel.DosagemFibra);
            element.LookupParameter("Emenda - Ref. Tela Inf")?.Set(fullAmbienteViewModel.EmendaReforcoTelaInferior);
            element.LookupParameter("Emenda - Ref. Tela Sup")?.Set(fullAmbienteViewModel.EmendaReforcoTelaSuperior);
            element.LookupParameter("Emenda - Tela Inferior (\"0,xx\")")?.Set(fullAmbienteViewModel.EmendaTelaInferior);
            element.LookupParameter("Emenda - Tela Superior (\"0,xx\")")?.Set(fullAmbienteViewModel.EmendaTelaSuperior);
            element.LookupParameter("LPE_COBRIMENTO SUPERIOR")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.CobrimentoTelaSuperior, UnitTypeId.Centimeters));
            element.LookupParameter("LPE_COBRIMENTO INFERIOR")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.CobrimentoTelaInferior, UnitTypeId.Centimeters));
            element.LookupParameter("Fibra")?.Set(fullAmbienteViewModel.Fibra);
            element.LookupParameter("Reforço de Tela")?.Set(fullAmbienteViewModel.BoolReforcoDeTela ? 1 : 0);
            element.LookupParameter("Finalidade Ref. Tela Inf")?.Set(fullAmbienteViewModel.FinalidadeReforcoTelaInferior);
            element.LookupParameter("Finalidade Ref. Tela Sup")?.Set(fullAmbienteViewModel.FinalidadeReforcoTelaSuperior);
            element.LookupParameter("Finalidade - Tela Superior")?.Set(fullAmbienteViewModel.FinalidadeTelaSuperior);
            element.LookupParameter("FR1")?.Set(fullAmbienteViewModel.FR1);
            element.LookupParameter("FR4")?.Set(fullAmbienteViewModel.FR4);
            element.LookupParameter("H Concreto")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HConcreto, UnitTypeId.Centimeters));
            element.LookupParameter("H-Espaçador Inferior (cm)")?.Set(fullAmbienteViewModel.HEspacadorInferior);
            element.LookupParameter("H-Espaçador Soldado (cm)")?.Set(fullAmbienteViewModel.HEspacadorSoldado);
            element.LookupParameter("H-Espaçador Superior (cm)")?.Set(fullAmbienteViewModel.HEspacadorSuperior);
            element.LookupParameter("Largura da Placa")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.LarguraDaPlaca, UnitTypeId.Meters));
            element.LookupParameter("Ref. Tela Inferior")?.Set(fullAmbienteViewModel.ReforcoTelaInferior.ToString());
            element.LookupParameter("Ref. Tela Superior")?.Set(fullAmbienteViewModel.ReforcoTelaSuperior.ToString());
            element.LookupParameter("Tela Inferior")?.Set(fullAmbienteViewModel.TelaInferior.ToString());
            element.LookupParameter("TIPO TELA INFERIOR")?.Set(fullAmbienteViewModel.TelaInferior);
            element.LookupParameter("Tela Superior")?.Set(fullAmbienteViewModel.TelaSuperior.ToString());
            element.LookupParameter("TIPO TELA SUPERIOR")?.Set(fullAmbienteViewModel.TelaSuperior);
            element.LookupParameter("Tratamento Superficial")?.Set(fullAmbienteViewModel.TratamentoSuperficial);
            element.LookupParameter("CB_BARRA DE TRANSFERÊNCIA AMARRADA POR CIMA")?.Set(fullAmbienteViewModel.BoolBarraPorCima ? 1 : 0);
            element.get_Parameter(BuiltInParameter.REF_TABLE_ELEM_NAME)?.Set(fullAmbienteViewModel.TipoDePiso);

            element.LookupParameter("TIPO REF. TELA SUPERIOR")?.Set(fullAmbienteViewModel.ReforcoTelaSuperior);
            element.LookupParameter("TIPO REF. TELA INFERIOR")?.Set(fullAmbienteViewModel.ReforcoTelaInferior);
            element.LookupParameter("TAG_EXTRA")?.Set(fullAmbienteViewModel.TagExtra);

            return element;
        }

        public static Element SetAmbienteLPEItensDeDetalhe(
          Element itensDeDetalheElement,
          FullAmbienteViewModel fullAmbienteViewModel)
        {
            itensDeDetalheElement.get_Parameter(BuiltInParameter.REF_TABLE_ELEM_NAME)?.Set(fullAmbienteViewModel.TipoDePiso);
            itensDeDetalheElement.LookupParameter("Ambiente")?.Set(fullAmbienteViewModel.Ambiente);
            itensDeDetalheElement.LookupParameter("CB_CONCRETO")?.Set((fullAmbienteViewModel != null ? (fullAmbienteViewModel.HConcreto != 0.0 ? 1 : 0) : 1) != 0 ? 1 : 0);
            itensDeDetalheElement.LookupParameter("CB_FIBRA")?.Set(fullAmbienteViewModel.BoolFibra ? 1 : 0);
            itensDeDetalheElement.LookupParameter("H Concreto")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HConcreto, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("TAG_CONCRETO")?.Set(fullAmbienteViewModel.TagConcreto);
            itensDeDetalheElement.LookupParameter("CB_SUB_BASE")?.Set(fullAmbienteViewModel.CBSubBase ? 1 : 0);
            itensDeDetalheElement.LookupParameter("H SUB_BASE")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HSubBase, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("TAG_SUB-BASE")?.Set(fullAmbienteViewModel.TagSubBase);
            itensDeDetalheElement.LookupParameter("CB_BASE")?.Set(fullAmbienteViewModel.CBBase ? 1 : 0);
            itensDeDetalheElement.LookupParameter("H BASE")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HBase, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("TAG_BASE")?.Set(fullAmbienteViewModel.TagBase);
            itensDeDetalheElement.LookupParameter("CB_BASE GENÉRICA")?.Set(fullAmbienteViewModel.CBBaseGenerica ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_BASE GENÉRICA")?.Set(fullAmbienteViewModel.TagBaseGenerica);
            itensDeDetalheElement.LookupParameter("CB_REF. SUBLEITO")?.Set(fullAmbienteViewModel.CBRefSubleito ? 1 : 0);
            itensDeDetalheElement.LookupParameter("LPE_COBRIMENTO SUPERIOR")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.CobrimentoTelaSuperior, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("LPE_COBRIMENTO INFERIOR")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.CobrimentoTelaInferior, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("H Ref. Subleito")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HRefSubleito, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("TAG_REF. SUBLEITO")?.Set(fullAmbienteViewModel.TagRefSubleito);
            itensDeDetalheElement.LookupParameter("CB_SUBLEITO")?.Set(fullAmbienteViewModel.CBSubleito ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_SUBLEITO")?.Set(fullAmbienteViewModel.TagSubleito);
            itensDeDetalheElement.LookupParameter("CB_TELA SUPERIOR")?.Set(fullAmbienteViewModel.BoolTelaSuperior ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_TIPO TELA SUPERIOR")?.Set(fullAmbienteViewModel.TelaSuperior);
            itensDeDetalheElement.LookupParameter("CB_TELA INFERIOR")?.Set(fullAmbienteViewModel.BoolTelaInferior ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_TIPO TELA INFERIOR")?.Set(fullAmbienteViewModel.TelaInferior);
            itensDeDetalheElement.LookupParameter("CB_REF. TELA SUPERIOR")?.Set(fullAmbienteViewModel.BoolReforcoTelaSuperior ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_TIPO REF. TELA SUPERIOR")?.Set("REF. TELA SUPERIOR (" + fullAmbienteViewModel.ReforcoTelaSuperior + ")");
            itensDeDetalheElement.LookupParameter("CB_REF. TELA INFERIOR")?.Set(fullAmbienteViewModel.BoolReforcoTelaInferior ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_TIPO REF. TELA INFERIOR")?.Set("REF. TELA INFERIOR (" + fullAmbienteViewModel.ReforcoTelaInferior + ")");
            itensDeDetalheElement.LookupParameter("CB_VEÍCULOS PESADOS")?.Set(fullAmbienteViewModel.BoolLPEVeiculosPesados ? 1 : 0);
            itensDeDetalheElement.LookupParameter("LPE_CARGA")?.Set(fullAmbienteViewModel.LPECarga);
            itensDeDetalheElement.LookupParameter("Espaçamento (BT)")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.EspacamentoBarra, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("CB_BARRA DE TRANSFERÊNCIA AMARRADA POR CIMA")?.Set(fullAmbienteViewModel.BoolBarraPorCima ? 1 : 0);
            itensDeDetalheElement.LookupParameter("CB_CAUQ FAIXA A")?.Set(fullAmbienteViewModel.FloorMatriz.Layers.Where(a => a.SelectedMaterial.Contains("FAIXA A")).Count() > 0 ? 1 : 0);

            
            itensDeDetalheElement.LookupParameter("TAG_TIPO REF. TELA SUPERIOR")?.Set(fullAmbienteViewModel.ReforcoTelaSuperior);
            itensDeDetalheElement.LookupParameter("TAG_TIPO REF. TELA INFERIOR")?.Set(fullAmbienteViewModel.ReforcoTelaInferior);

            return itensDeDetalheElement;
        }

        public static Element SetAmbienteLPETipoDeJunta(
          Element tipoDeJuntaElement,
          FullAmbienteViewModel fullAmbienteViewModel)
        {
            tipoDeJuntaElement.LookupParameter("Ambiente")?.Set(fullAmbienteViewModel.Ambiente);
            tipoDeJuntaElement.LookupParameter("H Concreto")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HConcreto, UnitTypeId.Centimeters));
            tipoDeJuntaElement.LookupParameter("(s/n) Tela Superior")?.Set(fullAmbienteViewModel.BoolTelaSuperior ? 1 : 0);
            tipoDeJuntaElement.LookupParameter("(s/n) Tela Inferior")?.Set(fullAmbienteViewModel.BoolTelaInferior ? 1 : 0);
            tipoDeJuntaElement.LookupParameter("LPE_VEÍCULOS PESADOS")?.Set(fullAmbienteViewModel.BoolLPEVeiculosPesados ? 1 : 0);
            tipoDeJuntaElement.LookupParameter("H-Espaçador Soldado (cm)")?.Set(fullAmbienteViewModel.HEspacadorSoldado);
            tipoDeJuntaElement.LookupParameter("LPE_COBRIMENTO SUPERIOR")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.CobrimentoTelaSuperior, UnitTypeId.Centimeters));
            tipoDeJuntaElement.LookupParameter("LPE_COBRIMENTO INFERIOR")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.CobrimentoTelaInferior, UnitTypeId.Centimeters));
            tipoDeJuntaElement.LookupParameter("Espaçamento (BT)")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.EspacamentoBarra, UnitTypeId.Centimeters));
            tipoDeJuntaElement.LookupParameter("CB_BARRA DE TRANSFERÊNCIA AMARRADA POR CIMA")?.Set(fullAmbienteViewModel.BoolBarraPorCima ? 1 : 0);
            tipoDeJuntaElement.get_Parameter(BuiltInParameter.REF_TABLE_ELEM_NAME)?.Set(fullAmbienteViewModel.TipoDePiso);
            tipoDeJuntaElement.LookupParameter("TIPO TELA INFERIOR")?.Set(fullAmbienteViewModel.TelaInferior);
            tipoDeJuntaElement.LookupParameter("TIPO TELA SUPERIOR")?.Set(fullAmbienteViewModel.TelaSuperior);
            return tipoDeJuntaElement;
        }
    }
}
