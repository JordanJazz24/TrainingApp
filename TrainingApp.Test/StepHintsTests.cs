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
        private static string Login(bool script, bool edge, bool recursos)
            => StepHints.For(StepType.MesLogin, new MesLoginProgress
            {
                ScriptDone = script, EdgeOpen = edge, RecursosDone = recursos
            }, null);

        [Test]
        public void MesLogin_HintPerStretch()
        {
            Assert.Multiple(new System.Action(() =>
            {
                Assert.That(Login(false, false, false), Does.StartWith("Haga doble clic en MES Limpiar historial y cache"));
                Assert.That(Login(true, false, false), Does.StartWith("Cierre la consola"));
                Assert.That(Login(true, true, false), Does.StartWith("En el Repositorio seleccione Recursos MES"));
                Assert.That(Login(true, true, true), Does.StartWith("En el login de MES"));
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
}
