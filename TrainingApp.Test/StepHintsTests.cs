using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training;

namespace TrainingApp.Test
{
    // Spec 012, enmienda 2: pistas por tramo del Ingreso a MES y de Logout, con los textos
    // aprobados y el mismo avance que usan los ResumeName de la guía.
    [TestFixture]
    public class StepHintsTests
    {
        private static string Login(bool script, bool edge, bool horaHora, bool recursos)
            => StepHints.For(StepType.MesLogin, new MesLoginProgress
            {
                ScriptDone = script, EdgeOpen = edge, HoraHoraDone = horaHora, RecursosDone = recursos
            }, null);

        [Test]
        public void MesLogin_HintPerStretch()
        {
            Assert.Multiple(new System.Action(() =>
            {
                Assert.That(Login(false, false, false, false), Does.StartWith("Haga doble clic en MES Limpiar historial y cache"));
                Assert.That(Login(true, false, false, false), Does.StartWith("Cierre la consola"));
                Assert.That(Login(true, true, false, false), Does.StartWith("Seleccione Hora-Hora (3.0)"));
                Assert.That(Login(true, true, true, false), Does.StartWith("Vuelva a la pestaña del Repositorio"));
                Assert.That(Login(true, true, true, true), Does.StartWith("En el login de MES"));
            }));
        }

        // Sin avance guardado (Session nueva): el primer tramo.
        [Test]
        public void MesLogin_WithoutProgress_IsTheFirstStretch()
        {
            Assert.That(StepHints.For(StepType.MesLogin, null, null), Does.StartWith("Haga doble clic"));
        }

        [Test]
        public void Logout_HintPerStretch()
        {
            Assert.Multiple(new System.Action(() =>
            {
                Assert.That(StepHints.For(StepType.Logout, null, null), Does.StartWith("Presione el ícono Logout"));
                Assert.That(StepHints.For(StepType.Logout, null, new LogoutProgress()), Does.StartWith("Cierre el navegador"));
                Assert.That(StepHints.For(StepType.Logout, null, new LogoutProgress { EdgeClosed = true }),
                    Does.EndWith("cualquier tecla para terminar."));
            }));
        }

        // Los demás pasos no dependen del avance del Ingreso ni de la limpieza.
        [Test]
        public void OtherSteps_IgnoreTheProgress()
        {
            var progress = new MesLoginProgress { ScriptDone = true };
            Assert.That(StepHints.For(StepType.ResourceSetup, progress, new LogoutProgress()),
                Is.EqualTo(StepHints.For(StepType.ResourceSetup, null, null)));
        }
    }

    // Spec 012 (P-10, D-86): el resumen de Práctica libre por paso.
    [TestFixture]
    public class StepAttemptSummaryTests
    {
        private static TrainingSessionStepAttempt Attempt(int stepId, int n, bool ok, AttemptReason reason)
            => new TrainingSessionStepAttempt { StepId = stepId, AttemptNumber = n, IsCorrect = ok, Reason = reason };

        [Test]
        public void Build_CountsAttemptsAndSkips()
        {
            var steps = ScoreCalculatorGuideTests.MoldeoGuide();
            var attempts = new List<TrainingSessionStepAttempt>
            {
                Attempt(1, 1, true,  AttemptReason.None),
                Attempt(2, 1, false, AttemptReason.WrongDecision),
                Attempt(2, 2, true,  AttemptReason.None),
                Attempt(3, 1, false, AttemptReason.Skipped),
                Attempt(4, 1, false, AttemptReason.WrongDecision),
                Attempt(4, 2, false, AttemptReason.EmptyRequiredField),
                Attempt(4, 3, false, AttemptReason.Skipped)
            };

            var rows = StepAttemptSummary.Build(steps, attempts);

            Assert.That(rows.Select(r => r.Order), Is.EqualTo(steps.Select(s => s.Order)));
            Assert.Multiple(new System.Action(() =>
            {
                Assert.That(rows[0].StepName, Is.EqualTo("Ingreso a MES"));
                Assert.That(rows[0].Attempts, Is.EqualTo(1));
                Assert.That(rows[0].Skipped, Is.False);

                Assert.That(rows[1].Attempts, Is.EqualTo(2));
                Assert.That(rows[1].Failures, Is.EqualTo(1));

                Assert.That(rows[2].StepName, Is.EqualTo("Line Clearance (apertura)"));
                Assert.That(rows[2].Skipped, Is.True);
                Assert.That(rows[2].Failures, Is.EqualTo(0));

                Assert.That(rows[3].Skipped, Is.True);
                Assert.That(rows[3].Failures, Is.EqualTo(2));

                Assert.That(rows[4].Attempts, Is.EqualTo(0));
            }));
        }
    }
}
