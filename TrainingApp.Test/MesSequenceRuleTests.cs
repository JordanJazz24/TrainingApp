using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Spec 017 (C-1 a C-3, plan E2): el mensaje del MES para un botón presionado fuera de
    // turno, según el estado del POD. Lista vacía: el MES aceptaría la acción.
    [TestFixture]
    public class MesSequenceRuleTests
    {
        // Después del LC de apertura y del Start: todo lo que el MES revisa está en orden.
        private static MesSequenceState Started() => new MesSequenceState { SfcStarted = true, SfcSelected = true };

        // Start ya hecho, pero sin ninguna fila seleccionada en el POD.
        private static MesSequenceState StartedWithoutSelection() => new MesSequenceState { SfcStarted = true };

        private static MesSequenceState BeforeResourceSetup() => new MesSequenceState
        {
            ResourceSetupPending = true,
            OpeningLineClearancePending = true
        };

        private static MesSequenceState BeforeLineClearance(bool answeredNo = false) => new MesSequenceState
        {
            OpeningLineClearancePending = true,
            LineClearanceAnsweredNo = answeredNo
        };

        private static MesMessageId[] Ids(StepType clicked, MesSequenceState state)
            => MesSequenceRule.For(clicked, state).Select(e => e.Id).ToArray();

        // ── Start ────────────────────────────────────────────────────────

        // Prioridad (C-2): con RS y LC pendientes, M-1.
        [Test]
        public void Start_WithResourceSetupPending_IsM1()
        {
            Assert.That(Ids(StepType.Search, BeforeResourceSetup()),
                Is.EqualTo(new[] { MesMessageId.ResourceSetupMissing }));
        }

        [Test]
        public void Start_WithOpeningLineClearancePending_IsM2()
        {
            Assert.That(Ids(StepType.Search, BeforeLineClearance()),
                Is.EqualTo(new[] { MesMessageId.LineClearanceNotDone }));
        }

        // C-3: la última respuesta del LC fue No.
        [Test]
        public void Start_AfterLineClearanceAnsweredNo_IsM3()
        {
            Assert.That(Ids(StepType.Search, BeforeLineClearance(answeredNo: true)),
                Is.EqualTo(new[] { MesMessageId.LineClearanceStatusNo }));
        }

        // Start no revisa el SFC (es el que lo inicia).
        [Test]
        public void Start_AfterLineClearance_HasNoMessage()
        {
            Assert.That(Ids(StepType.Search, new MesSequenceState()), Is.Empty);
        }

        // ── Assemble y DC Collect ────────────────────────────────────────

        // Prioridad (C-2): el LC se revisa antes que el SFC.
        [TestCase(StepType.Assembly)]
        [TestCase(StepType.DataCollection)]
        public void AssembleOrCollect_WithLineClearancePendingAndNoSfc_IsM2(StepType clicked)
        {
            Assert.That(Ids(clicked, BeforeLineClearance()), Is.EqualTo(new[] { MesMessageId.LineClearanceNotDone }));
        }

        // Con RS pendiente el LC también lo está: M-2, no M-1 (la columna 1 es solo de Start).
        [TestCase(StepType.Assembly)]
        [TestCase(StepType.DataCollection)]
        public void AssembleOrCollect_BeforeResourceSetup_IsM2(StepType clicked)
        {
            Assert.That(Ids(clicked, BeforeResourceSetup()), Is.EqualTo(new[] { MesMessageId.LineClearanceNotDone }));
        }

        [TestCase(StepType.Assembly)]
        [TestCase(StepType.DataCollection)]
        public void AssembleOrCollect_AfterLineClearanceAnsweredNo_IsM3(StepType clicked)
        {
            Assert.That(Ids(clicked, BeforeLineClearance(answeredNo: true)),
                Is.EqualTo(new[] { MesMessageId.LineClearanceStatusNo }));
        }

        [TestCase(StepType.Assembly)]
        [TestCase(StepType.DataCollection)]
        public void AssembleOrCollect_WithoutStartedSfc_IsM5(StepType clicked)
        {
            Assert.That(Ids(clicked, new MesSequenceState()), Is.EqualTo(new[] { MesMessageId.SfcMissing }));
        }

        // C-1: DC Collect durante Assembly con el SFC iniciado pasaría en el MES.
        [TestCase(StepType.Assembly)]
        [TestCase(StepType.DataCollection)]
        public void AssembleOrCollect_WithStartedSfc_HasNoMessage(StepType clicked)
        {
            Assert.That(Ids(clicked, Started()), Is.Empty);
        }

        // ── Log NC y SFC Split ───────────────────────────────────────────

        // C-3: el MES no revisa el LC en Split ni en Log NC; solo el SFC.
        [TestCase(StepType.SFC_Split)]
        [TestCase(StepType.LogNC)]
        [TestCase(StepType.LogNCFastRepair)]
        [TestCase(StepType.LogNCExtendedRepair)]
        public void SplitOrLogNC_WithoutStartedSfc_IsM5EvenWithLineClearancePending(StepType clicked)
        {
            Assert.That(Ids(clicked, BeforeLineClearance()), Is.EqualTo(new[] { MesMessageId.SfcMissing }));
        }

        [TestCase(StepType.SFC_Split)]
        [TestCase(StepType.LogNC)]
        public void SplitOrLogNC_WithStartedSfc_HasNoMessage(StepType clicked)
        {
            Assert.That(Ids(clicked, Started()), Is.Empty);
        }

        // Corrección del usuario al probar la E2: sin una fila seleccionada el MES no tiene SFC.
        [TestCase(StepType.Assembly)]
        [TestCase(StepType.DataCollection)]
        [TestCase(StepType.SFC_Split)]
        [TestCase(StepType.LogNC)]
        public void SfcButtons_StartedButNothingSelected_IsM5(StepType clicked)
        {
            Assert.That(Ids(clicked, StartedWithoutSelection()), Is.EqualTo(new[] { MesMessageId.SfcMissing }));
        }

        [Test]
        public void Complete_StartedButNothingSelected_IsM4()
        {
            var state = StartedWithoutSelection();
            state.PendingDataCollection.Add(new MesPendingDataCollection
            {
                Sfc = "SFC-1",
                Parameters = new List<string> { "TEMP (DCG_A)" }
            });

            Assert.That(Ids(StepType.Complete, state), Is.EqualTo(new[] { MesMessageId.SfcMissingStart }));
        }

        // ── Complete ─────────────────────────────────────────────────────

        [Test]
        public void Complete_WithoutStartedSfc_IsM4()
        {
            Assert.That(Ids(StepType.Complete, BeforeLineClearance()), Is.EqualTo(new[] { MesMessageId.SfcMissingStart }));
        }

        // Área sin DC, o con el DC ya resuelto: el MES deja completar.
        [Test]
        public void Complete_WithStartedSfcAndNoPendingDataCollection_HasNoMessage()
        {
            Assert.That(Ids(StepType.Complete, Started()), Is.Empty);
        }

        // D-113 y D-119: un M-7 por pieza, con todos sus parámetros pendientes y el patrón del MES.
        [Test]
        public void Complete_WithPendingDataCollection_IsOneM7PerPieceWithEveryParameter()
        {
            var state = Started();
            state.PendingDataCollection.Add(new MesPendingDataCollection
            {
                Sfc = "SFC-1",
                Parameters = new List<string> { "TEMP (DCG_A)", "PRESSURE (DCG_B)" }
            });
            state.PendingDataCollection.Add(new MesPendingDataCollection
            {
                Sfc = "SFC-2",
                Parameters = new List<string> { "PRESSURE (DCG_B)" }
            });

            var errors = MesSequenceRule.For(StepType.Complete, state);

            Assert.That(errors.Select(e => e.Text), Is.EqualTo(new[]
            {
                "There are missing required values for data collection parameter(s) TEMP (DCG_A) , PRESSURE (DCG_B) for SFC \"SFC-1\" (Message 15246)",
                "There are missing required values for data collection parameter(s) PRESSURE (DCG_B) for SFC \"SFC-2\" (Message 15246)"
            }));
            Assert.That(errors.All(e => e.Surface == MesSurface.PodBanner), Is.True);
        }

        // Una pieza que ya guardó todos sus grupos obligatorios no lleva mensaje.
        [Test]
        public void Complete_SkipsPiecesWithoutPendingParameters()
        {
            var state = Started();
            state.PendingDataCollection.Add(new MesPendingDataCollection { Sfc = "SFC-1" });
            state.PendingDataCollection.Add(new MesPendingDataCollection
            {
                Sfc = "SFC-2",
                Parameters = new List<string> { "TEMP (DCG_A)" }
            });

            var errors = MesSequenceRule.For(StepType.Complete, state);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0].Text, Does.Contain("for SFC \"SFC-2\""));
        }

        // ── Botones sin chequeo en el MES ────────────────────────────────

        // Resource Setup, Activities → LC (el de cierre incluido) y Logout: nunca un mensaje.
        [TestCase(StepType.ResourceSetup)]
        [TestCase(StepType.LineClearance)]
        [TestCase(StepType.Logout)]
        public void ButtonsWithoutMesCheck_HaveNoMessage(StepType clicked)
        {
            Assert.That(Ids(clicked, BeforeResourceSetup()), Is.Empty);
            Assert.That(Ids(clicked, new MesSequenceState()), Is.Empty);
        }
    }

    // Spec 017 (plan E2): el estado que arma MesSequenceState.Build con lo del PodContext.
    [TestFixture]
    public class MesSequenceStateTests
    {
        // Guía de Moldeo simplificada: RS, LC de apertura, Start, Assembly, DC, LC de cierre,
        // Complete.
        private static readonly IList<TrainingStep> Steps = new List<TrainingStep>
        {
            new TrainingStep { Id = 10, Order = 1, StepType = StepType.ResourceSetup },
            new TrainingStep { Id = 11, Order = 2, StepType = StepType.LineClearance },
            new TrainingStep { Id = 12, Order = 3, StepType = StepType.Search },
            new TrainingStep { Id = 13, Order = 4, StepType = StepType.Assembly },
            new TrainingStep { Id = 14, Order = 5, StepType = StepType.DataCollection },
            new TrainingStep { Id = 15, Order = 6, StepType = StepType.LineClearance },
            new TrainingStep { Id = 16, Order = 7, StepType = StepType.Complete }
        };

        private static readonly IList<DataCollectionGroup> Groups = new List<DataCollectionGroup>
        {
            new DataCollectionGroup { Id = 2, Code = "DCG_B", IsRequired = true,  DisplayOrder = 2 },
            new DataCollectionGroup { Id = 1, Code = "DCG_A", IsRequired = true,  DisplayOrder = 1 },
            new DataCollectionGroup { Id = 3, Code = "DCG_OPT", IsRequired = false, DisplayOrder = 3 }
        };

        private static readonly IList<DataCollectionParameter> Parameters = new List<DataCollectionParameter>
        {
            new DataCollectionParameter { Name = "PRESSURE", DataCollectionGroupId = 2, DisplayOrder = 1 },
            new DataCollectionParameter { Name = "HUMIDITY", DataCollectionGroupId = 1, DisplayOrder = 2 },
            new DataCollectionParameter { Name = "TEMP",     DataCollectionGroupId = 1, DisplayOrder = 1 },
            new DataCollectionParameter { Name = "NOTE",     DataCollectionGroupId = 3, DisplayOrder = 1 }
        };

        private static SimulatedOrder Order(params SimulatedOrderRow[] rows)
            => new SimulatedOrder { Rows = rows.ToList() };

        private static SimulatedOrderRow Row(string id, string status = SimulatedOrderRow.StatusActive)
            => new SimulatedOrderRow { RowId = id, SFCCode = "SFC-" + id, Status = status };

        private static TrainingSessionStepAttempt Attempt(int stepId, AttemptReason reason)
            => new TrainingSessionStepAttempt { StepId = stepId, IsCorrect = false, Reason = reason };

        private static MesSequenceState Build(int currentStepOrder, SimulatedOrder order = null,
            IList<TrainingSessionStepAttempt> attempts = null, DataCollectionProgress progress = null,
            bool withDc = false, ICollection<string> selected = null)
            => MesSequenceState.Build(Steps, currentStepOrder, attempts, order, progress,
                withDc ? Groups : null, withDc ? Parameters : null, selected);

        [Test]
        public void InResourceSetup_EverythingBeforeIsPending()
        {
            var state = Build(currentStepOrder: 1);

            Assert.That(state.ResourceSetupPending, Is.True);
            Assert.That(state.OpeningLineClearancePending, Is.True);
            Assert.That(state.SfcStarted, Is.False);
        }

        [Test]
        public void InOpeningLineClearance_OnlyTheLineClearanceIsPending()
        {
            var state = Build(currentStepOrder: 2, order: Order(Row("1", SimulatedOrderRow.StatusNew)));

            Assert.That(state.ResourceSetupPending, Is.False);
            Assert.That(state.OpeningLineClearancePending, Is.True);
            Assert.That(state.SfcStarted, Is.False, "una fila en New no es un SFC iniciado");
        }

        // NonSerialized: la fila del lote queda Active desde Resource Setup, pero el SFC se inicia
        // con el Start (corrección del usuario al probar la E2).
        [Test]
        public void BeforeStart_AnActiveLotRowIsNotAStartedSfc()
        {
            var state = Build(currentStepOrder: 3, order: Order(Row("1")), selected: new[] { "1" });

            Assert.That(state.SfcStarted, Is.False);
            Assert.That(state.SfcSelected, Is.False);
        }

        // C-3: el LC de cierre sin resolver no cuenta.
        [Test]
        public void InClosingLineClearance_TheOpeningOneIsResolved()
        {
            var state = Build(currentStepOrder: 6, order: Order(Row("1")));

            Assert.That(state.OpeningLineClearancePending, Is.False);
            Assert.That(state.SfcStarted, Is.True);
            Assert.That(state.SfcSelected, Is.False);
        }

        [Test]
        public void SfcSelected_OnlyCountsSelectedActiveRows()
        {
            var order = Order(Row("1"), Row("2", SimulatedOrderRow.StatusNew));

            Assert.That(Build(4, order, selected: new[] { "1" }).SfcSelected, Is.True);
            Assert.That(Build(4, order, selected: new[] { "2" }).SfcSelected, Is.False);
            Assert.That(Build(4, order, selected: new string[0]).SfcSelected, Is.False);
        }

        [Test]
        public void OpeningLineClearance_IsTheFirstOfTheGuide()
        {
            Assert.That(MesSequenceState.OpeningLineClearance(Steps).Id, Is.EqualTo(11));
        }

        // C-3: el No del LC de apertura sale de sus intentos; un clic fuera de secuencia en ese
        // paso no es una respuesta.
        [Test]
        public void LineClearanceAnsweredNo_ComesFromAWrongAnswerOfTheOpeningLineClearance()
        {
            Assert.That(Build(2, attempts: new[] { Attempt(11, AttemptReason.WrongDecision) }).LineClearanceAnsweredNo,
                Is.True);
            Assert.That(Build(2, attempts: new[] { Attempt(11, AttemptReason.OutOfSequence) }).LineClearanceAnsweredNo,
                Is.False);
            Assert.That(Build(2, attempts: new[] { Attempt(15, AttemptReason.WrongDecision) }).LineClearanceAnsweredNo,
                Is.False, "el No del LC de cierre no cuenta");
            Assert.That(Build(2).LineClearanceAnsweredNo, Is.False);
        }

        // M-7 sin catálogos de DC (cualquier botón que no sea Complete): no se calcula.
        [Test]
        public void WithoutDataCollectionCatalogs_NoPendingDataCollection()
        {
            Assert.That(Build(4, Order(Row("1"))).PendingDataCollection, Is.Empty);
        }

        // D-113: los parámetros de los grupos obligatorios, en el orden de la pantalla, con el
        // patrón del MES. El grupo opcional no entra.
        [Test]
        public void PendingDataCollection_ListsTheRequiredParametersInScreenOrder()
        {
            var state = Build(4, Order(Row("1")), withDc: true, selected: new[] { "1" });

            Assert.That(state.PendingDataCollection.Count, Is.EqualTo(1));
            Assert.That(state.PendingDataCollection[0].Sfc, Is.EqualTo("SFC-1"));
            Assert.That(state.PendingDataCollection[0].Parameters,
                Is.EqualTo(new[] { "TEMP (DCG_A)", "HUMIDITY (DCG_A)", "PRESSURE (DCG_B)" }));
        }

        // Sin selección no hay piezas que listar: Complete da M-4 (corrección del usuario al
        // probar la E2, enmienda de D-119).
        [Test]
        public void PendingDataCollection_WithoutSelection_IsEmpty()
        {
            var order = Order(Row("1"), Row("2"));

            Assert.That(Build(5, order, withDc: true).PendingDataCollection, Is.Empty);
        }

        // D-119: una por pieza seleccionada y activa; las filas en New no cuentan.
        [Test]
        public void PendingDataCollection_IsOnePerSelectedActiveRow()
        {
            var order = Order(Row("1"), Row("2"), Row("3"), Row("4", SimulatedOrderRow.StatusNew));

            var state = Build(5, order, withDc: true, selected: new[] { "1", "3", "4" });

            Assert.That(state.PendingDataCollection.Select(p => p.Sfc), Is.EqualTo(new[] { "SFC-1", "SFC-3" }));
        }

        // Los grupos ya guardados de cada pieza no se listan.
        [Test]
        public void PendingDataCollection_SkipsTheGroupsAlreadySaved()
        {
            var progress = new DataCollectionProgress();
            progress.MarkSaved("1", 1);

            var state = Build(5, Order(Row("1")), progress: progress, withDc: true, selected: new[] { "1" });

            Assert.That(state.PendingDataCollection[0].Parameters, Is.EqualTo(new[] { "PRESSURE (DCG_B)" }));
        }

        // Con el paso de DC resuelto, el MES ya tiene los datos.
        [Test]
        public void PendingDataCollection_AfterTheDataCollectionStep_IsEmpty()
        {
            Assert.That(Build(6, Order(Row("1")), withDc: true, selected: new[] { "1" }).PendingDataCollection, Is.Empty);
        }

        // Sin SFC iniciado no hay piezas que listar (Complete da M-4).
        [Test]
        public void PendingDataCollection_WithoutActiveRows_IsEmpty()
        {
            Assert.That(Build(4, Order(Row("1", SimulatedOrderRow.StatusNew)), withDc: true, selected: new[] { "1" })
                .PendingDataCollection, Is.Empty);
        }
    }
}
