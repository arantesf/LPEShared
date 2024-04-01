// Decompiled with JetBrains decompiler
// Type: Revit.Common.AmbienteManagerUtils
// Assembly: LPE, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E06EE1B-06C5-4A71-9629-ACF2EB60ECA2
// Assembly location: C:\ProgramData\Autodesk\Revit\Addins\2024\LPE\LPE.dll

using Autodesk.Revit.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Revit.Common
{
    public static class AmbienteManagerUtils
    {
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
                    newLayer.MaterialId = new FilteredElementCollector(doc)
                        .OfClass(typeof(Material))
                        .First(m => m.Name == tableLayer.SelectedMaterial)
                        .Id;
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
            FloorType floorType = null;
            if (ambienteViewModel.FloorMatriz.FloorName == null)
            {
                floorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .First(a => a.Name == ambienteViewModel.SelectedfloorMatriz.FloorName);
                floorType = floorType.Duplicate(ambienteViewModel.TipoDePiso) as FloorType;
            }
            else
            {
                floorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .First(a => a.Name == ambienteViewModel.FloorMatriz.FloorName);
            }
            if (floorType.Name != ambienteViewModel.TipoDePiso)
            {
                floorType.Name = ambienteViewModel.TipoDePiso;
            }
            IList<CompoundStructureLayer> layers = GetCompoundStructureLayer(doc, ambienteViewModel, floorType);
            CompoundStructure compoundStructure1 = CompoundStructure.CreateSimpleCompoundStructure(layers);
            compoundStructure1.StructuralMaterialIndex = layers.ToList().FindIndex(x => x.Function == MaterialFunctionAssignment.Structure);
            int coreLayerIndex = ambienteViewModel.FloorMatriz.Layers.ToList().FindIndex(x => !x.IsEnabled);
            int numberOfInteriorLayers = ambienteViewModel.FloorMatriz.Layers.ToList().Skip(coreLayerIndex).Count(x => x.IsEnabled);
            compoundStructure1.SetNumberOfShellLayers(ShellLayerType.Interior, numberOfInteriorLayers);
            compoundStructure1.EndCap = EndCapCondition.NoEndCap;
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
            foreach (FloorType floorType in ((IEnumerable)new FilteredElementCollector(doc).WhereElementIsElementType().OfCategory(BuiltInCategory.OST_Floors).Cast<FloorType>().ToList()))
            {
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
            ElementId elementId = new ElementId(505819);
            List<ViewSchedule> list = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.LookupParameter("LPE_AMBIENTE").AsString() == "AMBIENTE XX").ToList();
            Transaction transaction = new Transaction(doc, "Duplicar tabelas");
            transaction.Start();
            foreach (ViewSchedule viewSchedule1 in list)
            {
                ViewSchedule viewSchedule = viewSchedule1;
                if (!new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.Name == viewSchedule.Name + " - " + newAmbienteName).ToList().Any())
                {
                    ViewSchedule element = doc.GetElement(((View)viewSchedule).Duplicate((ViewDuplicateOption)0)) as ViewSchedule;
                    element.Name = viewSchedule.Name + " - " + newAmbienteName;
                    element.LookupParameter("LPE_AMBIENTE").Set(newAmbienteName);
                    ScheduleFieldId scheduleFieldId = null;
                    for (int index = 0; index < viewSchedule.Definition.GetFieldCount(); ++index)
                    {
                        if (viewSchedule.Definition.GetField(index).GetName() == "Ambiente")
                        {
                            scheduleFieldId = viewSchedule.Definition.GetFieldId(index);
                            break;
                        }
                    }
                    ScheduleFilter scheduleFilter = new ScheduleFilter(scheduleFieldId, (ScheduleFilterType)2, newAmbienteName);
                    element.Definition.AddFilter(scheduleFilter);
                }
            }
            transaction.Commit();
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

        public static List<FullAmbienteViewModel> GetAmbientes(Document doc)
        {
            ViewSchedule viewSchedule1 = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.Name == "LPE_TIPO DE PISO").FirstOrDefault();
            Dictionary<string, Element> dictionary1 = new FilteredElementCollector(doc, viewSchedule1.Id).ToDictionary(e => e.Name);
            ViewSchedule viewSchedule2 = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.Name == "LPE_TIPO DE JUNTA").FirstOrDefault();
            new FilteredElementCollector(doc, viewSchedule2.Id).ToDictionary(e => e.Name);
            ViewSchedule viewSchedule3 = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Schedules).Cast<ViewSchedule>().Where(view => view.Name == "LPE_ITENS DE DETALHE SEÇÕES").FirstOrDefault();
            Dictionary<string, Element> dictionary2 = new FilteredElementCollector(doc, viewSchedule3.Id).ToDictionary(e => e.Name);
            List<FullAmbienteViewModel> ambientes = new List<FullAmbienteViewModel>();
            foreach (KeyValuePair<string, Element> keyValuePair in dictionary1)
            {
                Element tipoDePisoElement = keyValuePair.Value;
                Element itensDeDetalheElement = (Element)null;
                if (dictionary2.ContainsKey(tipoDePisoElement.Name))
                    itensDeDetalheElement = dictionary2.First(a => a.Key == tipoDePisoElement.LookupParameter("Key Name")?.AsString()).Value;
                FullAmbienteViewModel ambienteViewModel = new FullAmbienteViewModel(tipoDePisoElement, itensDeDetalheElement, doc);
                ambientes.Add(ambienteViewModel);
            }
            return ambientes;
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
            element.LookupParameter("Ref. Tela Inferior")?.Set(fullAmbienteViewModel.ReforcoTelaInferior);
            element.LookupParameter("Ref. Tela Superior")?.Set(fullAmbienteViewModel.ReforcoTelaSuperior);
            element.LookupParameter("Tela Inferior")?.Set(fullAmbienteViewModel.TelaInferior);
            element.LookupParameter("Tela Superior")?.Set(fullAmbienteViewModel.TelaSuperior);
            element.LookupParameter("Tratamento Superficial")?.Set(fullAmbienteViewModel.TratamentoSuperficial);
            element.LookupParameter("Key Name")?.Set(fullAmbienteViewModel.TipoDePiso);
            return element;
        }

        public static Element SetAmbienteLPEItensDeDetalhe(
          Element itensDeDetalheElement,
          FullAmbienteViewModel fullAmbienteViewModel)
        {
            itensDeDetalheElement.LookupParameter("Key Name")?.Set(fullAmbienteViewModel.TipoDePiso);
            itensDeDetalheElement.LookupParameter("Ambiente")?.Set(fullAmbienteViewModel.Ambiente);
            itensDeDetalheElement.LookupParameter("CB_CONCRETO").Set((fullAmbienteViewModel != null ? (fullAmbienteViewModel.HConcreto != 0.0 ? 1 : 0) : 1) != 0 ? 1 : 0);
            itensDeDetalheElement.LookupParameter("CB_FIBRA").Set(fullAmbienteViewModel.BoolFibra ? 1 : 0);
            itensDeDetalheElement.LookupParameter("H Concreto")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HConcreto, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("TAG_CONCRETO").Set(fullAmbienteViewModel.TagConcreto);
            itensDeDetalheElement.LookupParameter("CB_SUB_BASE").Set(fullAmbienteViewModel.CBSubBase ? 1 : 0);
            itensDeDetalheElement.LookupParameter("H SUB_BASE").Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HSubBase, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("TAG_SUB-BASE").Set(fullAmbienteViewModel.TagSubBase);
            itensDeDetalheElement.LookupParameter("CB_BASE GENÉRICA").Set(fullAmbienteViewModel.CBBaseGenerica ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_BASE GENÉRICA").Set(fullAmbienteViewModel.TagBaseGenerica);
            itensDeDetalheElement.LookupParameter("CB_REF. SUBLEITO").Set(fullAmbienteViewModel.CBRefSubleito ? 1 : 0);
            try
            {
                itensDeDetalheElement.LookupParameter("H Ref. Subleito").Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HRefSubleito, UnitTypeId.Centimeters));
            }
            catch (Exception ex)
            {
            }
            itensDeDetalheElement.LookupParameter("TAG_REF. SUBLEITO").Set(fullAmbienteViewModel.TagRefSubleito);
            itensDeDetalheElement.LookupParameter("CB_SUBLEITO").Set(fullAmbienteViewModel.CBSubleito ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_SUBLEITO").Set(fullAmbienteViewModel.TagSubleito);
            try
            {
                itensDeDetalheElement.LookupParameter("CB_TELA SUPERIOR").Set(fullAmbienteViewModel.BoolTelaSuperior ? 1 : 0);
            }
            catch (Exception ex)
            {
            }
            try
            {
                itensDeDetalheElement.LookupParameter("TAG_TIPO TELA SUPERIOR").Set(int.Parse(fullAmbienteViewModel.TelaSuperior.Replace("Q", "")));
            }
            catch (Exception ex)
            {
            }
            try
            {
                itensDeDetalheElement.LookupParameter("CB_TELA INFERIOR").Set(fullAmbienteViewModel.BoolTelaInferior ? 1 : 0);
            }
            catch (Exception ex)
            {
            }
            try
            {
                itensDeDetalheElement.LookupParameter("TAG_TIPO TELA INFERIOR").Set(int.Parse(fullAmbienteViewModel.TelaInferior.Replace("Q", "")));
            }
            catch (Exception ex)
            {
            }
            try
            {
                itensDeDetalheElement.LookupParameter("CB_REF. TELA SUPERIOR").Set(fullAmbienteViewModel.BoolReforcoTelaSuperior ? 1 : 0);
            }
            catch (Exception ex)
            {
            }
            try
            {
                itensDeDetalheElement.LookupParameter("TAG_TIPO REF. TELA SUPERIOR").Set("REF. TELA SUPERIOR (" + fullAmbienteViewModel.ReforcoTelaSuperior + ")");
            }
            catch (Exception ex)
            {
            }
            try
            {
                itensDeDetalheElement.LookupParameter("CB_REF. TELA INFERIOR").Set(fullAmbienteViewModel.BoolReforcoTelaInferior ? 1 : 0);
            }
            catch (Exception ex)
            {
            }
            try
            {
                itensDeDetalheElement.LookupParameter("TAG_TIPO REF. TELA INFERIOR").Set("REF. TELA INFERIOR (" + fullAmbienteViewModel.ReforcoTelaInferior + ")");
            }
            catch (Exception ex)
            {
            }
            itensDeDetalheElement.LookupParameter("CB_VEÍCULOS PESADOS")?.Set(fullAmbienteViewModel.BoolLPEVeiculosPesados ? 1 : 0);
            itensDeDetalheElement.LookupParameter("LPE_CARGA").Set(fullAmbienteViewModel.LPECarga);
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
            tipoDeJuntaElement.LookupParameter("Key Name")?.Set(fullAmbienteViewModel.TipoDePiso);
            return tipoDeJuntaElement;
        }

        public static Element SetFloorType(Document doc, FullAmbienteViewModel fullAmbienteViewModel)
        {
            FloorType floorType = (FloorType)null;
            try
            {
                ElementId elementId = new ElementId(-1);
                string tipoDeSolucao = fullAmbienteViewModel.TipoDeSolucao;
                if (!(tipoDeSolucao == "TELA SIMPLES"))
                {
                    if (!(tipoDeSolucao == "TELA DUPLA"))
                    {
                        if (!(tipoDeSolucao == "FIBRA"))
                        {
                            if (!(tipoDeSolucao == "PAV. INTERTRAVADO"))
                            {
                                int num = tipoDeSolucao == "PAV. ASFÁLTICO" ? 1 : 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return floorType;
        }
    }
}
