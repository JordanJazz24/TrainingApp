using NUnit.Framework;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 016 (AC-2 a AC-8, P-3, P-4; enmienda 1, D-151): el Ingreso a MES se califica por
    // secuencia y por el Code1 propio; cada fallo vuelve a la pantalla que falló.
    [TestFixture]
    public class MesLoginRuleTests
    {
        private const string Own   = "320102684";
        private const string Other = "320999999";

        private static MesLoginProgress Progress(bool script = false, bool edge = false, bool recursos = false)
            => new MesLoginProgress { ScriptDone = script, EdgeOpen = edge, RecursosDone = recursos };

        private static MesOutcome Evaluate(MesLoginProgress progress, MesAction action,
            string code1 = null, string resource = null, string password = null)
            => MesLoginRule.Evaluate(progress, action, Own, code1, resource, password);

        private static void AssertFailed(MesOutcome outcome, AttemptReason reason, MesScreen screen)
        {
            Assert.That(outcome.Kind, Is.EqualTo(MesOutcomeKind.Failed));
            Assert.That(outcome.Reason, Is.EqualTo(reason));
            Assert.That(outcome.Screen, Is.EqualTo(screen));
        }

        private static void AssertAccepted(MesOutcome outcome, MesScreen? next)
        {
            Assert.That(outcome.Kind, Is.EqualTo(MesOutcomeKind.Accepted));
            Assert.That(outcome.Screen, Is.EqualTo(next));
        }

        // La pantalla que sigue a cada acción aceptada; Log On cierra el paso (sin pantalla).
        [Test]
        public void AcceptedActions_SayTheNextScreen()
        {
            AssertAccepted(Evaluate(Progress(), MesAction.RunScript, Own), MesScreen.ScriptOutput);
            AssertAccepted(Evaluate(Progress(script: true), MesAction.OpenRepository), MesScreen.Repository);
            AssertAccepted(Evaluate(Progress(true, true), MesAction.RecursosSearch, Own, "LoFlo-01"), MesScreen.MesLogin);
            AssertAccepted(Evaluate(Progress(true, true), MesAction.CloseEdge), MesScreen.Desktop);
            AssertAccepted(Evaluate(Progress(true, true, true), MesAction.LogOn, Own, password: "1"), null);
        }

        // F5 (D-78): Edge cerrado → escritorio; con Recursos MES hecho, el login; si no, el Repositorio.
        [Test]
        public void ResumeScreen_FollowsSavedProgress()
        {
            Assert.That(Progress(script: true).ResumeScreen(), Is.EqualTo(MesScreen.Desktop));
            Assert.That(Progress(true, true).ResumeScreen(), Is.EqualTo(MesScreen.Repository));
            Assert.That(Progress(true, true, recursos: true).ResumeScreen(), Is.EqualTo(MesScreen.MesLogin));
        }

        // D-151: Hora a Hora salió del Ingreso. Su acción ya no existe: MesLoginControl descarta
        // la que llegue de una página abierta antes (Enum.TryParse + IsDefined), sin intento.
        [Test]
        public void HoraHoraStart_IsNotAnAction()
            => Assert.That(System.Enum.IsDefined(typeof(MesAction), "HoraHoraStart"), Is.False);

        // ── Script ───────────────────────────────────────────────────────

        [Test]
        public void Script_OwnCode1_IsAccepted()
            => Assert.That(Evaluate(Progress(), MesAction.RunScript, Own).Kind, Is.EqualTo(MesOutcomeKind.Accepted));

        [Test]
        public void Script_OtherCode1_FailsAndStaysInConsole()
            => AssertFailed(Evaluate(Progress(), MesAction.RunScript, Other), AttemptReason.WrongDecision, MesScreen.Console);

        // AC-3: un Code1 que no tiene 9 dígitos es formato; el control lo bloquea y al
        // servidor no llega como intento.
        [TestCase("12345678")]
        [TestCase("1234567890")]
        [TestCase("32010268A")]
        [TestCase("")]
        public void Script_NotNineDigits_IsInvalid(string code1)
            => Assert.That(Evaluate(Progress(), MesAction.RunScript, code1).Kind, Is.EqualTo(MesOutcomeKind.Invalid));

        // P-14: después del Ingreso el script no hace nada.
        [Test]
        public void Script_AlreadyDone_IsIgnored()
            => Assert.That(Evaluate(Progress(script: true), MesAction.RunScript, Own).Kind, Is.EqualTo(MesOutcomeKind.Ignored));

        // D-79: sin distinguir mayúsculas ni espacios alrededor.
        [Test]
        public void Code1_IgnoresSurroundingSpaces()
            => Assert.That(MesLoginRule.IsOwnCode1(Own, "  " + Own + " "), Is.True);

        // ── Repositorio (secuencia) ──────────────────────────────────────

        [Test]
        public void Repository_BeforeScript_FailsOutOfSequenceToDesktop()
            => AssertFailed(Evaluate(Progress(), MesAction.OpenRepository), AttemptReason.OutOfSequence, MesScreen.Desktop);

        [Test]
        public void Repository_AfterScript_IsAccepted()
            => Assert.That(Evaluate(Progress(script: true), MesAction.OpenRepository).Kind, Is.EqualTo(MesOutcomeKind.Accepted));

        // ── Recursos MES ─────────────────────────────────────────────────

        [Test]
        public void Recursos_OtherCode1_FailsAndStaysThere()
            => AssertFailed(Evaluate(Progress(true, true), MesAction.RecursosSearch, Other, "LoFlo-01"),
                AttemptReason.WrongDecision, MesScreen.Recursos);

        // D-67: la estación no se califica (cualquier valor no vacío).
        [Test]
        public void Recursos_AnyStation_IsAccepted()
            => Assert.That(Evaluate(Progress(true, true), MesAction.RecursosSearch, Own, "OTRA-99").Kind,
                Is.EqualTo(MesOutcomeKind.Accepted));

        [Test]
        public void Recursos_EmptyStation_IsInvalid()
            => Assert.That(Evaluate(Progress(true, true), MesAction.RecursosSearch, Own, " ").Kind,
                Is.EqualTo(MesOutcomeKind.Invalid));

        // ── Log On ───────────────────────────────────────────────────────

        // AC-7 (enmienda 1): Log On exige solo el registro de Recursos MES.
        [Test]
        public void LogOn_WithRecursos_IsAccepted()
            => Assert.That(Evaluate(Progress(true, true, true), MesAction.LogOn, Own, password: "1").Kind,
                Is.EqualTo(MesOutcomeKind.Accepted));

        [Test]
        public void LogOn_OtherCode1_FailsAndStaysInLogin()
            => AssertFailed(Evaluate(Progress(true, true, true), MesAction.LogOn, Other, password: "1"),
                AttemptReason.WrongDecision, MesScreen.MesLogin);

        // D-73: la contraseña es solo formato.
        [Test]
        public void LogOn_EmptyPassword_IsInvalid()
            => Assert.That(Evaluate(Progress(true, true, true), MesAction.LogOn, Own, password: "").Kind,
                Is.EqualTo(MesOutcomeKind.Invalid));

        // El login de MES solo se abre con Recursos MES hecho: sin él, el cliente está desfasado.
        [Test]
        public void LogOn_WithoutRecursos_IsInvalid()
            => Assert.That(Evaluate(Progress(true, true), MesAction.LogOn, Own, password: "1").Kind,
                Is.EqualTo(MesOutcomeKind.Invalid));

        // ── Cerrar Edge (P-13) ───────────────────────────────────────────

        [Test]
        public void CloseEdge_KeepsRegistrations()
        {
            var progress = Progress(true, true, true);
            Assert.That(Evaluate(progress, MesAction.CloseEdge).Kind, Is.EqualTo(MesOutcomeKind.Accepted));

            progress.Apply(MesAction.CloseEdge);

            Assert.That(progress.EdgeOpen, Is.False);
            Assert.That(progress.RecursosDone, Is.True);
            Assert.That(progress.ScriptDone, Is.True);
        }

        // ── Limpieza al salir (AC-10, AC-11, P-6) ────────────────────────

        private static MesOutcome Cleanup(bool edgeClosed, MesAction action, string code1 = null)
            => LogoutCleanupRule.Evaluate(new LogoutProgress { EdgeClosed = edgeClosed }, action, Own, code1);

        [Test]
        public void Cleanup_StartsInLoggedOffAndCloseEdgeGoesToDesktop()
        {
            var progress = new LogoutProgress();
            Assert.That(progress.ResumeScreen(), Is.EqualTo(MesScreen.LoggedOff));

            AssertAccepted(Cleanup(false, MesAction.CloseEdge), MesScreen.Desktop);
            progress.Apply(MesAction.CloseEdge);

            Assert.That(progress.ResumeScreen(), Is.EqualTo(MesScreen.Desktop));
        }

        [Test]
        public void Cleanup_ScriptWithOwnCode1_ClosesTheStep()
            => AssertAccepted(Cleanup(true, MesAction.RunScript, Own), MesScreen.ScriptOutput);

        [Test]
        public void Cleanup_ScriptWithOtherCode1_FailsAndStaysInConsole()
            => AssertFailed(Cleanup(true, MesAction.RunScript, Other), AttemptReason.WrongDecision, MesScreen.Console);

        // P-6: en el escritorio, abrir el Repositorio es intento fallido de Logout.
        [Test]
        public void Cleanup_Repository_FailsOutOfSequence()
            => AssertFailed(Cleanup(true, MesAction.OpenRepository), AttemptReason.OutOfSequence, MesScreen.Desktop);

        // Con Edge todavía abierto el escritorio no se ve; Recursos y Log On no existen en la
        // limpieza.
        [TestCase(false, MesAction.RunScript)]
        [TestCase(false, MesAction.OpenRepository)]
        [TestCase(true, MesAction.RecursosSearch)]
        [TestCase(true, MesAction.LogOn)]
        public void Cleanup_UnreachableActions_AreInvalid(bool edgeClosed, MesAction action)
            => Assert.That(Cleanup(edgeClosed, action, Own).Kind, Is.EqualTo(MesOutcomeKind.Invalid));

        [Test]
        public void Cleanup_CloseEdgeTwice_IsIgnored()
            => Assert.That(Cleanup(true, MesAction.CloseEdge).Kind, Is.EqualTo(MesOutcomeKind.Ignored));

        // ── Avance forzado (AC-8, P-5) ───────────────────────────────────

        // El 4.º intento fallido en Assessment fuerza el avance; en Guided nunca.
        [Test]
        public void FourthFailedAttempt_ForcesAdvanceOnlyInAssessment()
        {
            Assert.That(ScoreCalculator.ShouldForceAdvance(4, SessionMode.Assessment), Is.True);
            Assert.That(ScoreCalculator.ShouldForceAdvance(3, SessionMode.Assessment), Is.False);
            Assert.That(ScoreCalculator.ShouldForceAdvance(4, SessionMode.Guided), Is.False);
        }
    }
}
