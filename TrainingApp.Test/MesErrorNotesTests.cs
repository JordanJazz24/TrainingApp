using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NUnit.Framework;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Guide;

namespace TrainingApp.Test
{
    // Spec 017 (D-102, D-114): la explicación en español de cada mensaje del MES que el
    // simulador puede mostrar, para la guía de Guided y Ver pista de Práctica libre.
    [TestFixture]
    public class MesErrorNotesTests
    {
        [Test]
        public void EveryMessageOfTheCatalog_HasItsNote()
        {
            Assert.Multiple(new Action(() =>
            {
                foreach (var message in MesMessage.All)
                {
                    Assert.That(MesErrorNotes.Has(message.Id), Is.True, message.Id.ToString());
                    Assert.That(MesErrorNotes.Plain(message.Id), Is.Not.Empty, message.Id.ToString());
                }
            }));
        }

        // Textos literales de la spec (D-114).
        [Test]
        public void Notes_AreTheApprovedTexts()
        {
            Assert.That(MesErrorNotes.Plain(MesMessageId.LineClearanceStatusNo), Is.EqualTo(
                "El último Line Clearance quedó en NO: el MES bloquea el proceso hasta que la línea esté liberada. Repita el Line Clearance."));
            Assert.That(MesErrorNotes.Plain(MesMessageId.SfcMissing),
                Is.EqualTo(MesErrorNotes.Plain(MesMessageId.SfcMissingStart)));
            Assert.That(MesErrorNotes.Plain(MesMessageId.SplitQtyMissing), Is.EqualTo(
                "Revise la cantidad a separar. Al presionar OK el MES cierra la ventana: abra SFC Split otra vez."));
        }

        // El globo recibe HTML: el texto codificado y lo marcado en negrita.
        [Test]
        public void Html_IsEncodedWithBold()
        {
            // WebUtility codifica también los acentos (como el resto de los textos de la guía).
            Assert.That(WebUtility.HtmlDecode(MesErrorNotes.Html(MesMessageId.ResourceSetupMissing)), Is.EqualTo(
                "Falta el <strong>Resource Setup</strong> de la estación: hágalo antes de iniciar."));
            Assert.That(MesErrorNotes.Html(MesMessageId.ResourceSetupMissing), Does.StartWith(
                "Falta el <strong>Resource Setup</strong> de la estaci&#243;n"));
            Assert.That(MesErrorNotes.Html(MesMessageId.FieldMustBeNumber), Does.Not.Contain("<strong>"));
        }

        // La nota va sobre la superficie donde se ve el mensaje (enmienda 1 del plan).
        [Test]
        public void Anchor_FollowsTheSurface()
        {
            Assert.Multiple(new Action(() =>
            {
                foreach (var message in MesMessage.All)
                {
                    string anchor = MesErrorNotes.AnchorName(message.Id);
                    switch (message.Surface)
                    {
                        case MesSurface.PodBanner:
                            Assert.That(anchor, Is.EqualTo("pod-mes-error"), message.Id.ToString());
                            break;
                        case MesSurface.SplitForm:
                            Assert.That(anchor, Is.EqualTo("split-mes-error"), message.Id.ToString());
                            break;
                        default:
                            Assert.That(anchor, Is.AnyOf("asm-mes-error", "dc-mes-error"), message.Id.ToString());
                            break;
                    }
                }
            }));
            Assert.That(MesErrorNotes.AnchorName(MesMessageId.BarcodeNotCompliant), Is.EqualTo("asm-mes-error"));
            Assert.That(MesErrorNotes.AnchorName(MesMessageId.ValueAboveUpperLimit), Is.EqualTo("dc-mes-error"));
        }
    }

    // Spec 017 (D-102): la nota de la guía de Guided para un mensaje del MES del servidor.
    [TestFixture]
    public class GuideMesErrorNoteTests
    {
        private static GuideScript Script()
        {
            var script = new GuideScript { EntryKey = "4:qty" };
            script.Substeps.Add(new GuideSubstep { Key = "4:open", Mode = GuideMode.Click, StepLabel = "Paso 4 · SFC Split", Index = 1, Total = 3 });
            script.Substeps.Add(new GuideSubstep
            {
                Key = "4:qty", Mode = GuideMode.Capture, StepLabel = "Paso 4 · SFC Split", Index = 2, Total = 3,
                Free = new List<string> { "[data-guide=\"nc-finding\"]" }
            });
            script.Substeps.Add(new GuideSubstep { Key = "4:split", Mode = GuideMode.Click, StepLabel = "Paso 4 · SFC Split", Index = 3, Total = 3 });
            return script;
        }

        private static readonly TrainingStep Step = new TrainingStep { Order = 4, StepType = StepType.SFC_Split };

        // Va justo antes de la entrada: "Continuar" sigue a ella. Lleva su número y sus libres.
        [Test]
        public void Note_GoesRightBeforeTheEntry()
        {
            var script = Script();

            GuideScriptBuilder.AddMesErrorNote(script, GuideMoment.Load.WithMesError(MesMessageId.SfcMissing));

            Assert.That(script.Substeps.Select(s => s.Key),
                Is.EqualTo(new[] { "4:open", "4:qty:mes-error", "4:qty", "4:split" }));
            var note = script.Substeps[1];
            Assert.That(script.EntryKey, Is.EqualTo(note.Key));
            Assert.That(note.Mode, Is.EqualTo(GuideMode.Info));
            Assert.That(note.Anchor, Does.Contain("pod-mes-error"));
            Assert.That(note.Html, Is.EqualTo(MesErrorNotes.Html(MesMessageId.SfcMissing)));
            Assert.That(note.Index, Is.EqualTo(2));
            Assert.That(note.Total, Is.EqualTo(3));
            Assert.That(note.StepLabel, Is.EqualTo("Paso 4 · SFC Split"));
            Assert.That(note.Free, Is.EqualTo(new[] { "[data-guide=\"nc-finding\"]" }));
        }

        [Test]
        public void StripMessages_AnchorToTheirWindow()
        {
            var script = Script();

            GuideScriptBuilder.AddMesErrorNote(script, GuideMoment.Load.WithMesError(MesMessageId.ValueAboveUpperLimit));

            Assert.That(script.Substeps[1].Anchor, Does.Contain("dc-mes-error"));
        }

        // El mensaje de SFC Split se cierra con su OK, que cierra la ventana: la nota espera ese
        // clic.
        [Test]
        public void SplitMessage_WaitsForItsOk()
        {
            var script = Script();

            GuideScriptBuilder.AddMesErrorNote(script, GuideMoment.Load.WithMesError(MesMessageId.SplitQtyMissing));

            var note = script.Substeps[1];
            Assert.That(note.Mode, Is.EqualTo(GuideMode.Click));
            Assert.That(note.Anchor, Does.Contain("split-mes-ok"));
            Assert.That(note.Postback, Is.False);
            Assert.That(note.Free, Has.Some.Contains("split-mes-error"));
        }

        [Test]
        public void WithoutMesError_NothingChanges()
        {
            var script = Script();

            GuideScriptBuilder.AddMesErrorNote(script, GuideMoment.Load);

            Assert.That(script.Substeps.Count, Is.EqualTo(3));
            Assert.That(script.EntryKey, Is.EqualTo("4:qty"));
        }

        // Paso recién resuelto: la entrada es la cola de confirmación; no hay nota.
        [Test]
        public void StepResolved_HasNoNote()
        {
            var script = Script();

            GuideScriptBuilder.AddMesErrorNote(script, GuideMoment.StepResolved(Step).WithMesError(MesMessageId.SfcMissing));

            Assert.That(script.Substeps.Count, Is.EqualTo(3));
        }

        // Sin entrada (paso sin guion), no hay nota.
        [Test]
        public void WithoutEntry_HasNoNote()
        {
            var script = new GuideScript();

            GuideScriptBuilder.AddMesErrorNote(script, GuideMoment.Load.WithMesError(MesMessageId.SfcMissing));

            Assert.That(script.Substeps, Is.Empty);
        }
    }
}
