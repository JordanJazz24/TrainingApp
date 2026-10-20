using System;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Catálogo de acciones del operador que guarda cada intento (spec 013, registro de
    // actividad; tabla en docs/specs/013-plan.md). El catálogo final se revisa con el diseño
    // del dashboard (Fase 10). Máximo 50 caracteres (TrainingSessionStepAttempts.Action).
    public static class AttemptActions
    {
        // Largo de la columna TrainingSessionStepAttempts.Action.
        public const int MaxLength = 50;

        // Botones del POD (también los que no son un paso: un click fuera de secuencia en
        // Assessment se registra con el botón presionado).
        public const string ResourceSetup        = "POD.ResourceSetup";
        public const string DCStandalone         = "POD.Activities.DCStandalone";
        public const string Start                = "POD.Start";
        public const string Assemble             = "POD.Assemble";
        public const string DCCollect            = "POD.DCCollect";
        public const string SFCSplit             = "POD.SFCSplit";
        public const string LogNC                = "POD.LogNC";
        public const string Complete             = "POD.Complete";
        public const string Logout               = "POD.Logout";

        public const string WorkInstruction       = "POD.WorkInstruction";
        public const string Reports               = "POD.Reports";
        public const string PlaceBackInQueue      = "POD.PlaceBackInQueue";
        public const string ToolList              = "POD.ToolList";
        public const string AsBuiltConfig         = "POD.Activities.AsBuiltConfig";
        public const string LogComment            = "POD.Activities.LogComment";
        public const string SfcPlaceHold          = "POD.Activities.SfcPlaceHold";
        public const string SfcReleaseHold        = "POD.Activities.SfcReleaseHold";
        public const string RemoveFutureHold      = "POD.Activities.RemoveFutureHold";
        public const string ChangeEquipmentStatus = "POD.Activities.ChangeEquipmentStatus";
        public const string TimeSensitiveMaterial = "POD.Activities.TimeSensitiveMaterial";

        // Envíos de los controles de paso.
        public const string ResourceSetupSave    = "ResourceSetup.Save";
        public const string LineClearanceSave    = "LC.Save";
        public const string StartChecklistSave   = "Start.Checklist.Save";
        public const string AssemblyAdd          = "Assembly.Add";
        public const string DataCollectionSave   = "DC.Save";
        public const string SFCSplitClose        = "SFCSplit.Close";
        public const string SFCSplitSplit        = "SFCSplit.Split";
        public const string LogNCAddDone         = "LogNC.AddDone";
        public const string LogNCWrongButton     = "LogNC.WrongButton";

        // Reparación rápida (spec 014): el botón con que terminó el flujo de Remove component.
        public const string LogNCRepairScrap         = "LogNC.Repair.Scrap";
        public const string LogNCRepairReturn        = "LogNC.Repair.Return";
        public const string LogNCRepairSendToRouting = "LogNC.Repair.SendToRouting";
        public const string LogNCRepairNoAdd         = "LogNC.Repair.NoAdd";

        // Reparación extendida (spec 014): el Select de Select Disposition Function.
        public const string LogNCExtendedLocalRework      = "LogNC.Extended.LocalRework";
        public const string LogNCExtendedReturnToPrevious = "LogNC.Extended.ReturnToPrevious";

        // Ingreso a MES (spec 016): la pantalla y el botón que se calificaron.
        public const string MesScript         = "MES.Script.Enter";
        public const string MesRepository     = "MES.Desktop.Repositorio";
        public const string MesHoraHoraStart  = "MES.HoraHora.Iniciar";
        public const string MesRecursosSearch = "MES.Recursos.Buscar";
        public const string MesLogOn          = "MES.Login.LogOn";

        public static string ForMesAction(MesAction action)
        {
            switch (action)
            {
                case MesAction.RunScript:      return MesScript;
                case MesAction.OpenRepository: return MesRepository;
                case MesAction.HoraHoraStart:  return MesHoraHoraStart;
                case MesAction.RecursosSearch: return MesRecursosSearch;
                case MesAction.LogOn:          return MesLogOn;
                // Cerrar Edge no se califica (P-13).
                default: throw new InvalidOperationException("La acción del Ingreso no se califica.");
            }
        }

        // Acción de un envío por respuesta (StepValidator), según el paso y lo enviado.
        public static string ForSubmit(StepType stepType, string answer)
        {
            // Los tres pasos de Log NC envían desde NC Data Entry (spec 014).
            if (StepTypes.IsLogNC(stepType))
                return ForLogNC(answer);

            switch (stepType)
            {
                case StepType.ResourceSetup: return ResourceSetupSave;
                case StepType.LineClearance: return LineClearanceSave;
                case StepType.Search:        return answer == StepAnswers.Start ? Start : StartChecklistSave;
                case StepType.SFC_Split:     return SFCSplitClose;
                case StepType.Complete:      return Complete;
                case StepType.Logout:        return Logout;
                // Assembly y DC no se califican por respuesta (SubmitStep los rechaza).
                default: throw new InvalidOperationException("El paso no tiene acción de envío.");
            }
        }

        // Log NC: Add-Done o, con el flujo de una reparación, el botón con que terminó.
        private static string ForLogNC(string answer)
        {
            if (answer == StepAnswers.Invalid) return LogNCWrongButton;

            var parsed = LogNCAnswer.Parse(answer);
            if (parsed?.Disposition == LogNCAnswer.DispositionLocalRework)      return LogNCExtendedLocalRework;
            if (parsed?.Disposition == LogNCAnswer.DispositionReturnToPrevious) return LogNCExtendedReturnToPrevious;
            switch (parsed?.RemoveAction)
            {
                case LogNCAnswer.RemoveReturn:        return LogNCRepairReturn;
                case LogNCAnswer.RemoveSendToRouting: return LogNCRepairSendToRouting;
                case LogNCAnswer.RemoveScrap:         return parsed.AddComponent ? LogNCRepairScrap : LogNCRepairNoAdd;
                default:                              return LogNCAddDone;
            }
        }
    }
}
