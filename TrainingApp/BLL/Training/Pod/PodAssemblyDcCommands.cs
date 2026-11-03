using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;
using static TrainingApp.BLL.Training.Pod.PodCore;
using static TrainingApp.BLL.Training.Pod.PodSelection;

namespace TrainingApp.BLL.Training.Pod
{
    // Assembly y Data Collection (AUD-74, E5).
    internal sealed class PodAssemblyDcCommands
    {
        private readonly PodCore _core;
        private readonly PodSequence _sequence;

        public PodAssemblyDcCommands(PodCore core, PodSequence sequence)
        {
            _core = core;
            _sequence = sequence;
        }

        // Abrir la Component List exige la selección del paso (aviso de formato, sin intento).
        public PodCommandResult OpenAssembly(PodContext ctx, ICollection<string> selectedRowIds)
        {
            return _sequence.CheckSequence(ctx, StepType.Assembly, AttemptActions.Assemble, selectedRowIds)
                ?? CheckPodSelection(ctx, StepType.Assembly, selectedRowIds)
                ?? new PodCommandResult();
        }

        // Assembly (spec 008): ensambla un componente del BOM para todas las piezas
        // seleccionadas. El paso se completa (intento correcto) cuando todo el BOM quedó
        // ensamblado. No tiene camino incorrecto: se califica solo por secuencia (S2). Un barcode
        // que no es numérico es formato, sin intento: el 17150 del MES (spec 017, M-8). No se
        // valida contra el número de parte (D-97).
        public AssemblySubmitResult AssembleComponent(PodContext ctx, int componentId, string barcode)
        {
            var result = new AssemblySubmitResult();
            if (ctx.Session.Status != SessionStatus.InProgress) return result;

            var step = ctx.CurrentStep;
            if (step.StepType != StepType.Assembly) return result;

            if (!IsCompliantBarcode(barcode))
            {
                result.MesError = MesMessage.Error(MesMessageId.BarcodeNotCompliant);
                return result;
            }

            var bom = ctx.AssemblyComponents;
            if (!bom.Any(c => c.Id == componentId)) return result;

            // Con el último componente, el intento se guarda antes de tocar el avance: si el SP
            // falla, la Session no queda adelantada y el Add se puede repetir (AUD-06).
            // RecordAttempt descarta el avance al resolver el paso.
            var progress = ctx.State.Assembly ?? new AssemblyProgress();
            if (progress.CoversWith(componentId, bom))
            {
                result.StepResult = _core.RecordAttempt(ctx, step, isCorrect: true,
                    AttemptReason.None, AttemptActions.AssemblyAdd);
            }
            else
            {
                progress.AssembledComponentIds.Add(componentId);
                ctx.State.Assembly = progress;
                _core.SaveState(ctx);
            }

            result.ComponentAssembled = true;
            return result;
        }

        // ── Data Collection ──────────────────────────────────────────────

        // Abrir Data Collection exige la selección del paso (aviso de formato, sin intento). La
        // primera apertura fija las piezas a recorrer; al reabrir se sigue donde quedó (AC-9,
        // spec 008).
        public PodCommandResult OpenDataCollection(PodContext ctx, ICollection<string> selectedRowIds)
        {
            var sequence = _sequence.CheckSequence(ctx, StepType.DataCollection, AttemptActions.DCCollect, selectedRowIds);
            if (sequence != null) return sequence;

            var selection = CheckPodSelection(ctx, StepType.DataCollection, selectedRowIds);
            if (selection != null) return selection;

            if (ctx.State.DataCollection == null)
                StartDataCollection(ctx, selectedRowIds);
            return new PodCommandResult();
        }

        // Data Collection (spec 008): guarda un DC Group para la pieza actual del avance.
        //   - Valores incorrectos: intento fallido (penalización y force-advance normales); el
        //     avance se conserva salvo que el paso avance forzado (E5).
        //   - Correcto: la pieza se completa según DataCollectionCompletionRule y el avance
        //     pasa a la siguiente; con la última, se registra el intento correcto del paso.
        // Sin avance (DC no se abrió con OpenDataCollection) el Save no aplica.
        public DataCollectionSubmitResult SubmitDataCollectionGroup(PodContext ctx,
            string groupCode, string answersJson)
        {
            var result = new DataCollectionSubmitResult { Outcome = DataCollectionOutcome.Ignored };
            if (ctx.Session.Status != SessionStatus.InProgress) return result;

            var step = ctx.CurrentStep;
            if (step.StepType != StepType.DataCollection) return result;

            var progress = ctx.State.DataCollection;
            if (progress == null || progress.IsFinished) return result;

            var groups = ctx.DataCollectionGroups;
            var group  = groups.FirstOrDefault(g => g.Code == groupCode);
            if (group == null) return result;

            // Grupo ya guardado por la pieza actual: su Collect está deshabilitado, así que un
            // segundo Save solo llega manipulando el cliente y no cuenta (E3, enmienda 2026-09-29).
            if (progress.CurrentPieceSavedGroupIds.Contains(group.Id)) return result;

            var piece = progress.CurrentPiece;
            result.PieceSfcCode = piece.SFCCode;

            var validator = BuildValidator(ctx, StepType.DataCollection);
            // Nombre del grupo Trainer inválido (spec 015): el control lo bloquea como formato;
            // si llega es una manipulación y no cuenta (auditoría AUD-36).
            if (validator.HasInvalidPersonName(group.Id, answersJson))
                return new DataCollectionSubmitResult { Outcome = DataCollectionOutcome.Ignored };

            if (!validator.ValidateDataCollectionGroup(group.Id, answersJson))
                return RecordInvalidGroup(ctx, step, validator, group, answersJson, result);

            return SaveValidGroup(ctx, step, progress, piece, group, groups, result);
        }

        // Valores incorrectos: intento fallido; el avance se conserva salvo que el paso avance
        // forzado.
        private DataCollectionSubmitResult RecordInvalidGroup(PodContext ctx, TrainingStep step,
            StepValidator validator, DataCollectionGroup group, string answersJson,
            DataCollectionSubmitResult result)
        {
            // Avance forzado (E5): RecordAttempt descarta el avance parcial. Registro de
            // actividad: un valor vacío se distingue de uno fuera de rango (spec 013).
            var reason = validator.HasMissingDataCollectionValue(group.Id, answersJson)
                ? AttemptReason.EmptyRequiredField : AttemptReason.WrongDecision;
            result.StepResult = _core.RecordAttempt(ctx, step, isCorrect: false,
                reason, AttemptActions.DataCollectionSave);
            result.Outcome = result.StepResult.Outcome == StepSubmitOutcome.Incorrect
                ? DataCollectionOutcome.Incorrect
                : DataCollectionOutcome.StepResolved;
            // Spec 017 (C-6): el mensaje del MES del primer parámetro con fallo; no cambia
            // el intento.
            result.MesError = validator.FirstDataCollectionError(group.Id, answersJson);
            return result;
        }

        // Valores correctos: el grupo queda guardado; si completa la pieza, el avance pasa a la
        // siguiente y, con la última, se registra el intento correcto del paso.
        private DataCollectionSubmitResult SaveValidGroup(PodContext ctx, TrainingStep step,
            DataCollectionProgress progress, DataCollectionPiece piece, DataCollectionGroup group,
            IList<DataCollectionGroup> groups, DataCollectionSubmitResult result)
        {
            var savedAfter = new HashSet<int>(progress.SavedGroupIds(piece.RowId)) { group.Id };
            if (!DataCollectionCompletionRule.IsPieceComplete(savedAfter, groups))
            {
                progress.MarkSaved(piece.RowId, group.Id);
                _core.SaveState(ctx);
                result.Outcome = DataCollectionOutcome.GroupSaved;
                return result;
            }

            // Con la última pieza, el intento se guarda antes de tocar el avance: si el SP falla,
            // la Session no queda adelantada y el Save se puede repetir (AUD-06). RecordAttempt
            // descarta el avance al resolver el paso.
            if (progress.CurrentIndex == progress.Pieces.Count - 1)
            {
                result.StepResult = _core.RecordAttempt(ctx, step, isCorrect: true,
                    AttemptReason.None, AttemptActions.DataCollectionSave);
                result.Outcome = DataCollectionOutcome.StepResolved;
                return result;
            }

            progress.MarkSaved(piece.RowId, group.Id);
            progress.CurrentIndex++;
            _core.SaveState(ctx);
            result.Outcome = DataCollectionOutcome.PieceCompleted;
            return result;
        }

        // Las piezas de DC son las filas seleccionadas del POD, en su orden (NonSerialized: el lote).
        private void StartDataCollection(PodContext ctx, ICollection<string> selectedRowIds)
        {
            var progress = new DataCollectionProgress();
            foreach (var row in SelectedRows(ctx, selectedRowIds))
                progress.Pieces.Add(new DataCollectionPiece { RowId = row.RowId, SFCCode = row.SFCCode });

            ctx.State.DataCollection = progress;
            _core.SaveState(ctx);
        }
    }
}
