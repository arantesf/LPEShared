using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Resources;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExternalApplication : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            AddRibbonButtons(application);

            return Result.Succeeded;
        }

        private void AddRibbonButtons(UIControlledApplication application)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string executingAssemblyPath = assembly.Location;
            Debug.Print(executingAssemblyPath);
            string executingAssemblyName = assembly.GetName().Name;
            Console.WriteLine(executingAssemblyName);
            string tabName = "LPE";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // tab already exists
            }
            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Pisos e Juntas");

            //////////// SPLIT FLOORS ////////////

            PushButtonData splitFloorsPBD = new PushButtonData("SplitFloors", "Dividir Pisos", executingAssemblyPath, typeof(SplitFloors).FullName)
            {
                ToolTip = "Divide o piso de um ambiente com base nas suas juntas.",
                LongDescription = "O parâmetro \"Ambiente\" deve ser preenchido tanto nos elementos de pisos quanto nos respectivos elementos de juntas.",
                LargeImage = ResourceImage.GetIcon("DividirPisosLPE.png")
            };
            panel.AddItem(splitFloorsPBD);

            //////////// SPLIT JOINTS ////////////

            PushButtonData splitJointsPBD = new PushButtonData("SplitJoints", "Dividir Juntas", executingAssemblyPath, typeof(SplitJoints).FullName)
            {
                ToolTip = "Divide as juntas do modelo pelos encontros com outras juntas ou outros pisos do mesmo ambiente.",
                LongDescription = "O parâmetro \"Ambiente\" deve ser preenchido tanto nos elementos de pisos quanto nos respectivos elementos de juntas.",
                LargeImage = ResourceImage.GetIcon("DividirJuntasLPE.png")
            };
            panel.AddItem(splitJointsPBD);

            //////////// DIMENSION FLOORS ////////////

            PushButtonData dimensionPBD = new PushButtonData("DimensionFloors", "Cotar Pisos", executingAssemblyPath, typeof(DimensionFloors).FullName)
            {
                ToolTip = "Cota os pisos divididos em suas duas direções principais.",
                LongDescription = "O comando utilizará o comando Aligned Dimension para criar 2 cotas em cada piso do modelo.",
                LargeImage = ResourceImage.GetIcon("CotarPisosLPE.png")
            };
            panel.AddItem(dimensionPBD);

            //////////// MESH REINFORCEMENT ////////////

            PushButtonData meshReinforcementPBD = new PushButtonData("MeshReinforcement", "Reforçar com Tela", executingAssemblyPath, typeof(SplitJoints).FullName)
            {
                ToolTip = "Caracteriza os pisos de fibra (Parâmetro ''(s/n) Fibra'') que possuem dimensões com porporção maiores que 1,5:1 com uma hachura e o preenchimento dos parâmetros de espaçadores.",
                LongDescription = "Escolha, na janela que se abre, a tela que deseja inserir como reforço e o cobrimento a ser aplicado.",
                LargeImage = ResourceImage.GetIcon("ReforçarPisosLPE.png")
            };
            panel.AddItem(splitJointsPBD);

            //////////// TAG JOINTS ////////////

            PushButtonData tagJointsPBD = new PushButtonData("TagJoints", "Tagear Juntas", executingAssemblyPath, typeof(TagJoints).FullName)
            {
                ToolTip = "Etiqueta as juntas dos ambientes escolhidos na janela.",
                LongDescription = "As juntas serão etiquetadas alternadamente.",
                LargeImage = ResourceImage.GetIcon("TagearJuntasLPE.png")
            };
            panel.AddItem(splitJointsPBD);

            //////////// RESTORE FLOORS ////////////

            PushButtonData restoreFloorsPBD = new PushButtonData("RestoreFloors", "Restaurar Pisos", executingAssemblyPath, typeof(RestoreFloors).FullName)
            {
                ToolTip = "Restaura os pisos divididos pelo comando \"Dividir Pisos\" ao seu formato original.",
                LongDescription = "O comando utilizará o valor prenchido no parâmetro \"Ambiente\" para identificar quais pisos se unirão.",
                LargeImage = ResourceImage.GetIcon("TagearJuntasLPE.png")
            };
            panel.AddItem(splitJointsPBD);

            //////////// ABOUT ////////////

            PushButtonData aboutPBD = new PushButtonData("About", "Sobre", executingAssemblyPath, typeof(About).FullName)
            {
                ToolTip = "",
                LongDescription = "",
                LargeImage = ResourceImage.GetIcon("TagearJuntasLPE.png")
            };
            panel.AddItem(splitJointsPBD);
        }
    }
}