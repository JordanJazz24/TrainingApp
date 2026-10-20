using System;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Pantallas del Ingreso a MES (spec 016, P-2). Un intento fallido vuelve a la pantalla que
    // falló (D-68, P-4).
    public enum MesScreen
    {
        Desktop,
        Console,
        ScriptOutput,    // la consola con la salida del script y "Press any key to continue"
        Repository,
        HoraHora,
        Recursos,
        MesLogin
    }

    // Acciones del Ingreso que viajan al servidor (D-77): calificables o que cambian el avance.
    // Cambiar de pestaña, abrir una tarjeta y cerrar la consola se resuelven en el cliente.
    public enum MesAction
    {
        RunScript,       // Enter en la consola con el Code1
        OpenRepository,  // doble clic en Repositorio (abre Edge)
        HoraHoraStart,   // Iniciar en Hora a Hora 3.0 con el Code1
        RecursosSearch,  // Buscar en Recursos MES con el recurso y el Code1
        LogOn,           // Log On en el login de MES: cierra el paso
        CloseEdge        // ✕ de Edge: vuelve al escritorio y conserva lo hecho (P-13)
    }

    // Avance del Ingreso a MES (D-78), en PodState: sobrevive a F5 (P-7) y se descarta al
    // resolver el paso. El login de MES está abierto cuando Recursos MES ya se hizo (Buscar
    // pasa esa pestaña al login).
    [Serializable]
    public class MesLoginProgress
    {
        public bool ScriptDone   { get; set; }
        public bool EdgeOpen     { get; set; }
        public bool HoraHoraDone { get; set; }
        public bool RecursosDone { get; set; }

        // Aplica una acción aceptada por MesLoginRule.
        public void Apply(MesAction action)
        {
            switch (action)
            {
                case MesAction.RunScript:      ScriptDone   = true;  break;
                case MesAction.OpenRepository: EdgeOpen     = true;  break;
                case MesAction.HoraHoraStart:  HoraHoraDone = true;  break;
                case MesAction.RecursosSearch: RecursosDone = true;  break;
                case MesAction.CloseEdge:      EdgeOpen     = false; break;
            }
        }

        // Pantalla que retoma el Ingreso sin una acción que la decida (F5, D-78; o un envío
        // ignorado): Edge cerrado → escritorio; con Recursos MES hecho, el login de MES; si no,
        // la pestaña del Repositorio.
        public MesScreen ResumeScreen()
        {
            if (!EdgeOpen) return MesScreen.Desktop;
            return RecursosDone ? MesScreen.MesLogin : MesScreen.Repository;
        }
    }

    public enum MesOutcomeKind
    {
        Accepted,   // se aplica y sigue en Screen; LogOn cierra el paso (Screen null)
        Ignored,    // ya hecho (doble envío, el script por segunda vez, P-14): no cambia nada
        Failed,     // intento fallido (Reason) y se vuelve a Screen, la que falló (P-4)
        Invalid     // no corresponde al avance guardado: el cliente quedó desfasado (Session
                    // perdida, otra pestaña) o el envío fue manipulado. No es un intento
    }

    public sealed class MesOutcome
    {
        private MesOutcome(MesOutcomeKind kind, AttemptReason reason, MesScreen? screen)
        {
            Kind   = kind;
            Reason = reason;
            Screen = screen;
        }

        public MesOutcomeKind Kind { get; }
        public AttemptReason Reason { get; }

        // Pantalla que sigue (Accepted) o a la que se vuelve (Failed); null en el resto.
        public MesScreen? Screen { get; }

        internal static readonly MesOutcome Ignored = new MesOutcome(MesOutcomeKind.Ignored, AttemptReason.None, null);
        internal static readonly MesOutcome Invalid = new MesOutcome(MesOutcomeKind.Invalid, AttemptReason.None, null);

        internal static MesOutcome Accepted(MesScreen? next)
            => new MesOutcome(MesOutcomeKind.Accepted, AttemptReason.None, next);

        internal static MesOutcome Failed(AttemptReason reason, MesScreen returnScreen)
            => new MesOutcome(MesOutcomeKind.Failed, reason, returnScreen);
    }

    // Calificación del Ingreso a MES (spec 016, D-67, P-3, P-4): la secuencia y el Code1 propio.
    // No se califican la estación, la contraseña (solo formato, D-73) ni el orden entre Hora a
    // Hora y Recursos MES. Sin estado: PodService aplica el resultado y registra el intento.
    public static class MesLoginRule
    {
        // Largo exacto que pide el script real (P-2).
        public const int ScriptCode1Length = 9;

        public static MesOutcome Evaluate(MesLoginProgress progress, MesAction action,
            string ownCode1, string code1 = null, string resource = null, string password = null)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            switch (action)
            {
                case MesAction.RunScript:
                    if (progress.ScriptDone) return MesOutcome.Ignored;
                    if (!IsScriptCode1(code1)) return MesOutcome.Invalid;
                    return Code1Outcome(ownCode1, code1, MesScreen.Console, MesScreen.ScriptOutput);

                case MesAction.OpenRepository:
                    if (!progress.ScriptDone)
                        return MesOutcome.Failed(AttemptReason.OutOfSequence, MesScreen.Desktop);
                    return progress.EdgeOpen ? MesOutcome.Ignored : MesOutcome.Accepted(MesScreen.Repository);

                case MesAction.HoraHoraStart:
                    if (progress.HoraHoraDone) return MesOutcome.Ignored;
                    if (!progress.EdgeOpen || IsBlank(code1)) return MesOutcome.Invalid;
                    return Code1Outcome(ownCode1, code1, MesScreen.HoraHora, MesScreen.HoraHora);

                case MesAction.RecursosSearch:
                    if (progress.RecursosDone) return MesOutcome.Ignored;
                    if (!progress.EdgeOpen || IsBlank(code1) || IsBlank(resource)) return MesOutcome.Invalid;
                    // Buscar pasa la misma pestaña al login de MES (P-2).
                    return Code1Outcome(ownCode1, code1, MesScreen.Recursos, MesScreen.MesLogin);

                case MesAction.LogOn:
                    // El login de MES solo se abre con Recursos MES hecho.
                    if (!progress.EdgeOpen || !progress.RecursosDone || IsBlank(code1) || IsBlank(password))
                        return MesOutcome.Invalid;
                    if (!IsOwnCode1(ownCode1, code1))
                        return MesOutcome.Failed(AttemptReason.WrongDecision, MesScreen.MesLogin);
                    // Sin el registro de Hora a Hora: vuelve a la pestaña del Repositorio (P-4).
                    return progress.HoraHoraDone
                        ? MesOutcome.Accepted(null)
                        : MesOutcome.Failed(AttemptReason.OutOfSequence, MesScreen.Repository);

                case MesAction.CloseEdge:
                    return progress.EdgeOpen ? MesOutcome.Accepted(MesScreen.Desktop) : MesOutcome.Ignored;

                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        // Code1 propio sin distinguir mayúsculas ni espacios alrededor (D-79).
        public static bool IsOwnCode1(string ownCode1, string code1)
            => !IsBlank(ownCode1) && !IsBlank(code1) &&
               string.Equals(ownCode1.Trim(), code1.Trim(), StringComparison.OrdinalIgnoreCase);

        // Formato del script real: exactamente 9 dígitos (P-3).
        public static bool IsScriptCode1(string code1)
            => code1 != null && code1.Trim().Length == ScriptCode1Length && code1.Trim().All(c => c >= '0' && c <= '9');

        private static MesOutcome Code1Outcome(string ownCode1, string code1, MesScreen failedScreen,
            MesScreen nextScreen)
            => IsOwnCode1(ownCode1, code1)
                ? MesOutcome.Accepted(nextScreen)
                : MesOutcome.Failed(AttemptReason.WrongDecision, failedScreen);

        private static bool IsBlank(string value) => string.IsNullOrWhiteSpace(value);
    }
}
