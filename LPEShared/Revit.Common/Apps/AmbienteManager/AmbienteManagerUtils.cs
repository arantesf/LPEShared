using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.Revit.DB;

namespace Revit.Common
{
    public static class AmbienteManagerUtils
    {
        public static void DuplicateViewSchedules(Document doc, string newAmbienteName)
        {
            ElementId ambienteParameterId = new ElementId(505819);

            List<ViewSchedule> defaultViewSchedules = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules)
                .Cast<ViewSchedule>()
                .Where(view => view.LookupParameter("LPE_AMBIENTE").AsString() == "AMBIENTE XX")
                .ToList();

            Transaction tx = new Transaction(doc, "Duplicar tabelas");
            tx.Start();
            foreach (ViewSchedule viewSchedule in defaultViewSchedules)
            {
                List<ViewSchedule> viewScheduleWithNewName = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules)
                .Cast<ViewSchedule>()
                .Where(view => view.Name == $"{viewSchedule.Name} - {newAmbienteName}")
                .ToList();

                if (viewScheduleWithNewName.Any())
                {
                    continue;
                }

                ViewSchedule newView = doc.GetElement(viewSchedule.Duplicate(ViewDuplicateOption.Duplicate)) as ViewSchedule;
                newView.Name = $"{viewSchedule.Name} - {newAmbienteName}";
                newView.LookupParameter("LPE_AMBIENTE").Set(newAmbienteName);
                ScheduleFieldId ambienteFieldId = null;
                for (int i = 0; i < viewSchedule.Definition.GetFieldCount(); i++)
                {
                    if (viewSchedule.Definition.GetField(i).GetName() == "Ambiente")
                    {
                        ambienteFieldId = viewSchedule.Definition.GetFieldId(i); break;
                    }
                }

                ScheduleFilter ambienteFilter = new ScheduleFilter(ambienteFieldId, ScheduleFilterType.Equal, newAmbienteName);
                newView.Definition.AddFilter(ambienteFilter);
            }
            tx.Commit();

        }

        public static void DeleteViewSchedules(Document doc, string ambienteName)
        {
            ElementId ambienteParameterId = new ElementId(505819);

            List<ElementId> viewScheduleIdsWithAmbienteName = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules)
                .Cast<ViewSchedule>()
                .Where(view => view.LookupParameter("LPE_AMBIENTE").AsString() == ambienteName)
                .Select(view => view.Id)
                .ToList();

            if (viewScheduleIdsWithAmbienteName.Any())
            {
                Transaction tx = new Transaction(doc, "Deletar tabelas");
                tx.Start();
                doc.Delete(viewScheduleIdsWithAmbienteName);
                tx.Commit();
            }

        }

        public static List<FullAmbienteViewModel> GetAmbientes(Document doc)
        {
            ViewSchedule keyScheduleTipoDePiso = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules)
                .Cast<ViewSchedule>()
                .Where(view => view.Name == "LPE_TIPO DE PISO")
                .FirstOrDefault();

            Dictionary<string, Element> tiposDePiso = new FilteredElementCollector(doc, keyScheduleTipoDePiso.Id).ToDictionary(e => e.Name);

            ViewSchedule keyScheduleTipoDeJunta = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules)
                .Cast<ViewSchedule>()
                .Where(view => view.Name == "LPE_TIPO DE JUNTA")
                .FirstOrDefault();
            Dictionary<string, Element> tiposDeJunta = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).ToDictionary(e => e.Name);


            ViewSchedule keyScheduleItensDeDetalhe = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Schedules)
                .Cast<ViewSchedule>()
                .Where(view => view.Name == "LPE_ITENS DE DETALHE SEÇÕES")
                .FirstOrDefault();

            Dictionary<string, Element> itensDeDetalhe = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).ToDictionary(e => e.Name);

            List<FullAmbienteViewModel> fullAmbienteViewModels = new List<FullAmbienteViewModel>();

            foreach (var tipoDePisoDict in tiposDePiso)
            {

                Element tipoDePisoElement = tipoDePisoDict.Value;
                Element itensDeDetalheElement = null;
                if (itensDeDetalhe.ContainsKey(tipoDePisoElement.LookupParameter("Ambiente")?.AsString()))
                {
                    itensDeDetalheElement = itensDeDetalhe.First(a => a.Key == tipoDePisoElement.LookupParameter("Key Name")?.AsString()).Value;
                }
                FullAmbienteViewModel fullAmbienteViewModel = new FullAmbienteViewModel(tipoDePisoElement, itensDeDetalheElement);
                fullAmbienteViewModels.Add(fullAmbienteViewModel);
            }
            return fullAmbienteViewModels;
        }

        public static Element SetAmbienteLPETipoDePiso(Element element, FullAmbienteViewModel fullAmbienteViewModel)
        {
            element.LookupParameter("Ambiente")?.Set(fullAmbienteViewModel.Ambiente);
            element.LookupParameter("(s/n) Espaçador Inf.")?.Set(fullAmbienteViewModel.BoolEspacadorInferior ? 1 : 0);
            element.LookupParameter("(s/n) Espaçador Sup.")?.Set(fullAmbienteViewModel.BoolEspacadorSuperior ? 1 : 0);
            element.LookupParameter("(s/n) Fibra")?.Set(fullAmbienteViewModel.BoolFibra ? 1 : 0);
            element.LookupParameter("LPE_VEÍCULOS PESADOS")?.Set(fullAmbienteViewModel.BoolLPEVeiculosPesados ? 1 : 0);
            element.LookupParameter("(s/n) Ref. Tela Inferior")?.Set(fullAmbienteViewModel.BoolReforcoTelaInferior ? 1 : 0);
            element.LookupParameter("(s/n) Ref. Tela Superior")?.Set(fullAmbienteViewModel.BoolReforcoTelaSuperior ? 1 : 0);
            element.LookupParameter("(s/n) Tela Inferior")?.Set(fullAmbienteViewModel.BoolTelaInferior ? 1 : 0);
            element.LookupParameter("(s/n) Tela Superior")?.Set(fullAmbienteViewModel.BoolTelaSuperior ? 1 : 0);
            element.LookupParameter("(s/n) Tratamento Superficial")?.Set(fullAmbienteViewModel.BoolTratamentoSuperficial ? 1 : 0);
            element.LookupParameter("Comprimento Placa")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.ComprimentoDaPlaca, UnitTypeId.Meters));
            element.LookupParameter("Dosagem da Fibra (kg/m³)")?.Set(fullAmbienteViewModel.DosagemFibra);
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

        public static Element SetAmbienteLPEItensDeDetalhe(Element itensDeDetalheElement, FullAmbienteViewModel fullAmbienteViewModel)
        {
            itensDeDetalheElement.LookupParameter("Key Name")?.Set(fullAmbienteViewModel.TipoDePiso);
            itensDeDetalheElement.LookupParameter("Ambiente")?.Set(fullAmbienteViewModel.Ambiente);
            itensDeDetalheElement.LookupParameter("CB_CONCRETO").Set((fullAmbienteViewModel?.HConcreto != 0) ? 1 : 0);
            itensDeDetalheElement.LookupParameter("CB_FIBRA").Set(fullAmbienteViewModel.BoolFibra ? 1 : 0);
            itensDeDetalheElement.LookupParameter("H Concreto")?.Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HConcreto, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("TAG_CONCRETO").Set(fullAmbienteViewModel.TagConcreto);
            itensDeDetalheElement.LookupParameter("CB_SUB_BASE").Set(fullAmbienteViewModel.CBSubBase ? 1 : 0);
            itensDeDetalheElement.LookupParameter("H SUB_BASE").Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HSubBase, UnitTypeId.Centimeters));
            itensDeDetalheElement.LookupParameter("TAG_SUB-BASE").Set(fullAmbienteViewModel.TagSubBase);
            itensDeDetalheElement.LookupParameter("CB_BASE GENÉRICA").Set(fullAmbienteViewModel.CBBaseGenerica ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_BASE GENÉRICA").Set(fullAmbienteViewModel.TagBaseGenerica);
            itensDeDetalheElement.LookupParameter("CB_REF. SUBLEITO").Set(fullAmbienteViewModel.CBRefSubleito ? 1 : 0);
            try { itensDeDetalheElement.LookupParameter("H Ref. Subleito").Set(UnitUtils.ConvertToInternalUnits(fullAmbienteViewModel.HRefSubleito, UnitTypeId.Centimeters)); } catch (Exception) { }
            itensDeDetalheElement.LookupParameter("TAG_REF. SUBLEITO").Set(fullAmbienteViewModel.TagRefSubleito);
            itensDeDetalheElement.LookupParameter("CB_SUBLEITO").Set(fullAmbienteViewModel.CBSubleito ? 1 : 0);
            itensDeDetalheElement.LookupParameter("TAG_SUBLEITO").Set(fullAmbienteViewModel.TagSubleito);
            try { itensDeDetalheElement.LookupParameter("CB_TELA SUPERIOR").Set(fullAmbienteViewModel.BoolTelaSuperior ? 1 : 0); } catch (Exception) { }
            try { itensDeDetalheElement.LookupParameter("TAG_TIPO TELA SUPERIOR").Set(int.Parse(fullAmbienteViewModel.TelaSuperior.Replace("Q", ""))); } catch (Exception) { }
            try { itensDeDetalheElement.LookupParameter("CB_TELA INFERIOR").Set(fullAmbienteViewModel.BoolTelaInferior ? 1 : 0); } catch (Exception) { }
            try { itensDeDetalheElement.LookupParameter("TAG_TIPO TELA INFERIOR").Set(int.Parse(fullAmbienteViewModel.TelaInferior.Replace("Q", ""))); } catch (Exception) { }
            try { itensDeDetalheElement.LookupParameter("CB_REF. TELA SUPERIOR").Set(fullAmbienteViewModel.BoolReforcoTelaSuperior ? 1 : 0); } catch (Exception) { }
            try { itensDeDetalheElement.LookupParameter("TAG_TIPO REF. TELA SUPERIOR").Set($"REF. TELA SUPERIOR ({fullAmbienteViewModel.ReforcoTelaSuperior})"); } catch (Exception) { }
            try { itensDeDetalheElement.LookupParameter("CB_REF. TELA INFERIOR").Set(fullAmbienteViewModel.BoolReforcoTelaInferior ? 1 : 0); } catch (Exception) { }
            try { itensDeDetalheElement.LookupParameter("TAG_TIPO REF. TELA INFERIOR").Set($"REF. TELA INFERIOR ({fullAmbienteViewModel.ReforcoTelaInferior})"); } catch (Exception) { }
            itensDeDetalheElement.LookupParameter("CB_VEÍCULOS PESADOS")?.Set(fullAmbienteViewModel.BoolLPEVeiculosPesados ? 1 : 0);
            itensDeDetalheElement.LookupParameter("LPE_CARGA").Set(fullAmbienteViewModel.LPECarga);

            return itensDeDetalheElement;
        }

        public static Element SetAmbienteLPETipoDeJunta(Element tipoDeJuntaElement, FullAmbienteViewModel fullAmbienteViewModel)
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

    }
}
