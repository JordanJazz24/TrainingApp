using System;
using System.Collections.Generic;
using System.Globalization;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Guide;
using TrainingApp.Pages.Training.Models;
using static TrainingApp.Pages.Training.GuidedTestHelpers;

namespace TrainingApp.Pages.Training
{
    // Página del POD (AUD-74, E6): Envío de los controles de paso y su continuación (qué se pinta tras cada resultado).
    public partial class GuidedTest
    {
        // ── Step submit handler (cableado en el markup) ───────────────────
        protected void HandleControlSubmit(object sender, StepSubmitEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Guard de sender (spec 006, E12): ASP.NET dispara eventos también en controles
            // invisibles, y el Answer se evalúa contra el paso actual. Un click en el modal
            // de confirmación de un paso ya completado penalizaría el paso siguiente.
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            StepSubmitResult result;
            PodCommandResult logNC = null;
            switch (step.StepType)
            {
                case StepType.ResourceSetup:
                    // La BLL crea la orden simulada con la Shop Order capturada (spec 005).
                    result = Pod.SubmitResourceSetup(_ctx, e.Answer, e.ShopOrder);
                    break;

                case StepType.Search:
                    // Checklist de Start (Moldeo): resuelto el paso, la BLL inicia las piezas.
                    result = Pod.SubmitStartChecklist(_ctx, e.Answer, BrowseSelectedIds());
                    break;

                case StepType.SFC_Split:
                    var close = Pod.CloseSplit(_ctx, e.Answer);
                    if (close.Notice != PodNotice.None)
                    {
                        ShowNotice(close.Notice);
                        ReopenModal("modalSFCSplit");
                        return;
                    }
                    result = close.Step;
                    break;

                default:
                    if (StepTypes.IsLogNC(step.StepType))
                    {
                        // Spec 014: la BLL califica pieza y código contra el hallazgo y, resuelto
                        // el paso, aplica su disposición.
                        logNC  = Pod.SubmitLogNC(_ctx, e.Answer, SelectedIds());
                        result = logNC.Step;
                        break;
                    }
                    result = Pod.SubmitStep(_ctx, e.Answer);
                    break;
            }

            ApplySubmitResult(result, session, guide, area, step, e.IncorrectFeedback);
            var outcome = result.Outcome;

            // Spec 006 (E3): respuesta incorrecta → el control queda abierto en el modal
            // donde se respondió y solo se ve el SweetAlert con el hint (de ApplySubmitResult).
            if (outcome == StepSubmitOutcome.Incorrect)
            {
                // Un Save fallido de Resource Setup puede crear la orden (Setup State "Open", spec
                // 005 y D-193): el Browse Work List la muestra en esta misma respuesta.
                if (step.StepType == StepType.ResourceSetup)
                    BindBrowseList(area, session);

                // Search tiene 4 modales: el control sabe cuál contiene la respuesta a corregir.
                if (step.StepType == StepType.Search)
                {
                    _guideMoment = GuideMoment.Incorrect(step, CtrlSearch.ReopenForRetry());
                    return;
                }

                string answerModalId = GetAnswerModalId(step.StepType);
                if (answerModalId != null) ReopenModal(answerModalId);
                return;
            }

            ContinueAfterStepResolved(session, guide, area, step, outcome, logNC);
        }

        // Paso resuelto (Correct, FailedForced o sesión completada) desde un control: cierra el
        // control, habilita el botón del paso siguiente y aplica lo propio de cada paso. Lo
        // usan el submit de los controles y el ensamble del último componente (spec 008).
        // logNC: el resultado de Log NC resuelto (lo que la BLL hizo con la pieza del hallazgo).
        private void ContinueAfterStepResolved(TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step, StepSubmitOutcome outcome, PodCommandResult logNC = null)
        {
            // Sesión completada: CompleteSession ya redirigió al resultado (o dejó la consola de la
            // limpieza). La sesión ya refleja el intento registrado (PodService actualiza el contexto).
            if (session.Status != SessionStatus.InProgress) return;

            // Modal-overlay: se renderiza en esta respuesta para mostrar su confirmación y
            // queda cerrado para el request siguiente. El resto vuelve al POD de inmediato.
            CloseStepControl(IsModalOverlay(step.StepType) ? step.StepType : (StepType?)null);

            if (ClearSelectionAroundSplit(step.StepType))
                BindWorkList(area, session);

            var nextStep = _ctx.CurrentStepOrDefault;
            if (nextStep != null)
                SetActiveButton(nextStep.StepType, area, _ctx.Policy);

            // El banner de guardado confirma una respuesta correcta; no se muestra en FailedForced.
            bool accepted = outcome == StepSubmitOutcome.Correct;

            if (step.StepType == StepType.ResourceSetup)
            {
                // Con el avance forzado, la BLL ya dejó la orden por defecto.
                BindBrowseList(area, session);
                if (accepted)
                {
                    if (IsCustomResourceSetup) CtrlCustomResourceSetup.ShowSaveConfirmation();
                    else CtrlResourceSetup.ShowSaveConfirmation();
                }
                ReopenModal(ResourceSetupModalId);
            }
            else if (step.StepType == StepType.LineClearance)
            {
                if (accepted) CtrlLineClearance.ShowSaveConfirmation();
                ReopenModal("modalDCStandalone");
            }
            else if (step.StepType == StepType.Search)
            {
                ShowStartedOrder(area, session);
                ShowBanner("Start process completed successfully");
            }
            else if (step.StepType == StepType.Assembly)
            {
                // La Component List reemplazaba el POD: vuelve sin selección, como el real
                // (2026-10-07). Mensaje del MES real al terminar el BOM (Fase 6.5).
                ClearPodSelection(area);
                BindWorkList(area, session);
                ShowBanner("All components processed at this operation");
            }
            else if (step.StepType == StepType.DataCollection)
            {
                // La Data Collection List reemplazaba el POD: vuelve sin selección (2026-10-07).
                ClearPodSelection(area);
                BindWorkList(area, session);
            }
            else if (step.StepType == StepType.SFC_Split)
                ShowBanner("SFC Split completed successfully");
            else if (StepTypes.IsLogNC(step.StepType) && logNC != null)
                ShowLogNCResolved(area, session, logNC);
        }

        // Log NC resuelto (spec 014): el banner de cada método.
        private void ShowLogNCResolved(Area area, TrainingSession session, PodCommandResult logNC)
        {
            if (logNC.Repaired != null)
                ShowRepaired(area, session, logNC.Repaired);
            else if (logNC.Reworked != null)
                ShowReworked(area, session, logNC.Reworked, logNC.ReworkOperation);
            else
                ShowScrapped(area, session, logNC.Scrapped);
        }

        // Reparación extendida resuelta (D-54): Return to any previous ya sacó la pieza del POD;
        // Local Rework la deja, con la operación actual en el banner (V-5, confirmado por el
        // usuario el 2026-10-19).
        private void ShowReworked(Area area, TrainingSession session, NcFinding reworked, string operation)
            => ShowLogNCBanner(area, session,
                string.Format("All of {0} will be processed at {1}", reworked.SFCCode, operation));

        // Vuelta al POD tras Log NC: sin selección, el SFC vacío en Serialized (NonSerialized:
        // TxtSFC es el SFC del lote y no cambia, spec 007), la orden al día y el banner.
        private void ShowLogNCBanner(Area area, TrainingSession session, string message)
        {
            ClearRowSelection();
            if (area.SerializationType == SerializationType.Serialized)
                TxtSFC.Text = string.Empty;
            BindWorkList(area, session);
            ShowBanner(message, hideAfterMs: 5000);
        }

        // Reparación rápida resuelta (spec 014): la pieza sigue en el POD (D-47). Banner de
        // D-55, confirmado por el usuario el 2026-10-19 (V-3).
        private void ShowRepaired(Area area, TrainingSession session, NcFinding repaired)
            => ShowLogNCBanner(area, session,
                string.Format("Component {0} replaced on {1}", repaired.ComponentPartNumber, repaired.SFCCode));

        // Assembly (spec 008): el operador ensambló un componente del BOM. Mientras falten
        // componentes la Component List se actualiza y el modal queda abierto con la
        // confirmación del Add (Fase 6.5); con el último, el paso se resuelve y vuelve el POD.
        protected void HandleComponentAssembled(object sender, ComponentAssembledEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Mismo guard de sender que HandleControlSubmit (spec 006, E12).
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            var result = Pod.AssembleComponent(_ctx, e.ComponentId, e.Barcode);
            // Spec 017 (M-8): el 17150 en la franja de la ventana, que queda abierta.
            if (result.MesError != null)
            {
                CtrlAssembly.ShowMesError(e.ComponentId, NoteMesError(result.MesError), MesHelp(result.MesError));
                // La guía vuelve al escaneo de ese componente después de la nota.
                _guideMoment = GuideMoment.Incorrect(step, e.ComponentId.ToString(CultureInfo.InvariantCulture));
                return;
            }
            if (result.StepResult == null)
            {
                CtrlAssembly.ViewModel = BuildStepViewModel(step, area, session, guide);
                CtrlAssembly.BindComponents();
                // La confirmación solo si la BLL aceptó el componente (pertenece al BOM y el
                // paso está activo); si no, un aviso para que no parezca un click perdido.
                if (result.ComponentAssembled)
                {
                    CtrlAssembly.ShowAddConfirmation(e.ComponentId);
                    // Spec 010: la guía entra en la confirmación de este componente.
                    _guideMoment = GuideMoment.Progress(step, e.ComponentId.ToString(CultureInfo.InvariantCulture));
                }
                else
                    ShowSweetAlert("warning", "Componente no válido",
                        "El componente no pertenece a la Component List de esta orden.");
                return;
            }

            ApplySubmitResult(result.StepResult, session, guide, area, step, incorrectFeedback: null);
            ContinueAfterStepResolved(session, guide, area, step, result.StepResult.Outcome);
        }

        // Spec 007: el scrap elimina la fila de la orden simulada, tanto en Correct como en
        // FailedForced (lo hace la BLL). Sin fila válida no se tocó nada. Spec 008: en
        // Serialized se scrapean todas las filas seleccionadas de una vez.
        private void ShowScrapped(Area area, TrainingSession session, IList<SimulatedOrderRow> rows)
        {
            if (rows == null || rows.Count == 0) return;

            string scrapped = rows.Count == 1
                ? "All of " + (rows[0].SFCCode ?? Order.SFCCode)
                : rows.Count + " SFCs";

            ShowLogNCBanner(area, session, scrapped + " scrapped");
        }

        // El operador sale del control sin responder (no cuenta como intento). Mismo
        // criterio que el guard de sender: solo el control abierto puede cerrarse.
        protected void HandleReturnToPod(object sender, EventArgs e)
        {
            var open = OpenStepType;
            if (!open.HasValue || !ReferenceEquals(sender, GetStepControl(open.Value))) return;

            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            CloseStepControl();
            BindWorkList(area, session);
        }

        protected void HandleSplitRequested(object sender, SFCSplitRequestedEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            var result = Pod.Split(_ctx, e.RowId, e.Qty);
            // Spec 014 (enmienda 3): una cantidad distinta de la del hallazgo es un intento. El
            // control queda abierto en el modal del split; forzado, el paso se resuelve con el
            // split de N que dejó la BLL.
            if (result.Step != null)
            {
                e.Handled = true;
                ApplySubmitResult(result.Step, session, guide, area, step, incorrectFeedback: null);
                if (result.Step.Outcome != StepSubmitOutcome.Incorrect)
                {
                    e.StepResolved = true;
                    ContinueAfterStepResolved(session, guide, area, step, result.Step.Outcome);
                }
                return;
            }
            // El modal del split muestra el mensaje del MES en lugar del formulario (spec 017) o
            // el aviso del simulador en su propio mensaje de error.
            if (result.MesErrors != null)
            {
                e.MesError = NoteMesError(result.MesErrors[0]);
                e.MesHelp  = MesHelp(result.MesErrors[0]);
                return;
            }
            if (result.Notice != PodNotice.None)
            {
                e.ErrorMessage = NoticeText(result.Notice).Text;
                return;
            }

            e.IsApplied = true;
            // Spec 010: la guía entra en el mensaje "New SFC … created".
            _guideMoment = GuideMoment.Progress(step, "split");
            e.RemainingQty = result.RemainingQty;
            e.NewSfcCode = result.NewSfcCode;
            BindWorkList(area, session);
        }

        private void ApplySubmitResult(StepSubmitResult result, TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step, string incorrectFeedback)
        {
            UpdateScoreWidget(session);

            switch (result.Outcome)
            {
                case StepSubmitOutcome.Correct:
                    _guideMoment = GuideMoment.StepResolved(step);
                    HandleCorrectSubmit(session, guide, area, step.StepType);
                    break;

                case StepSubmitOutcome.Incorrect:
                    _guideMoment = GuideMoment.Incorrect(step);
                    _lastFailureFeedback = incorrectFeedback;
                    ShowSweetAlert("warning", "Incorrect",
                        BuildIncorrectFeedback(_ctx, step.StepType, result.RemainingScoredAttempts, incorrectFeedback));
                    break;

                case StepSubmitOutcome.FailedForced:
                    _guideMoment = GuideMoment.StepResolved(step);
                    ShowSweetAlert("error", "Step Failed",
                        "Paso fallido — el sistema avanza automáticamente.");
                    HandleCorrectSubmit(session, guide, area, step.StepType);
                    break;

                case StepSubmitOutcome.SessionCompleted:
                    _guideMoment = GuideMoment.SessionCompleted(step);
                    // PodService ya dejó en la sesión el puntaje final guardado.
                    CompleteSession(session);
                    break;
            }
        }

        // Botón de otro paso sin intento (D-88).
        private void ShowNotCurrentStepNotice()
            => ShowSweetAlert("info", "Otro paso", "Ese no es el paso actual.");

        // Assessment (S2): todos los botones del POD están habilitados y un click en uno que
        // no es el del paso actual es un intento fallido de ese paso (lo decide la BLL antes que
        // cualquier aviso de formato). Devuelve true si el click fue fuera de secuencia; en
        // Guided esos botones están deshabilitados y el click se ignora; en Práctica libre, el
        // aviso sin intento (D-88).
        private bool HandleOutOfSequence(PodCommandResult result, TrainingSession session,
            TrainingGuide guide, Area area, TrainingStep step)
        {
            if (!result.OutOfSequence) return false;
            // Spec 017: el mensaje del MES para ese botón, si lo hay, antes del feedback de hoy
            // (Assessment) o en lugar de "Ese no es el paso actual." (Práctica libre, D-101).
            if (result.MesErrors != null) ShowMesErrors(result.MesErrors);
            if (result.Step == null)
            {
                if (result.NotCurrentStepNotice) ShowNotCurrentStepNotice();
                return true;
            }

            ApplySubmitResult(result.Step, session, guide, area, step, incorrectFeedback: null);
            if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                RepaintAfterForcedAdvance(step.StepType, session, guide, area, result.PodChanged);
            return true;
        }

        // Paso forzado por errores de secuencia: la BLL ya dejó el POD como si el paso se
        // hubiera hecho (orden por defecto, piezas iniciadas, split por defecto). La página lo
        // vuelve a pintar como en el submit normal (B-1): cierra el control que seguía abierto
        // (Component List o Data Collection List) y, si el paso forzado o el siguiente es SFC
        // Split, limpia la selección.
        private void RepaintAfterForcedAdvance(StepType forcedStep, TrainingSession session,
            TrainingGuide guide, Area area, bool podChanged)
        {
            bool rebind = false;
            if (OpenStepType.HasValue)
            {
                CloseStepControl();
                rebind = true;
            }

            if (podChanged && forcedStep == StepType.ResourceSetup)
                BindBrowseList(area, session);

            if (ClearSelectionAroundSplit(forcedStep))
                rebind = true;

            // Complete forzado (spec 015, D-22): la BLL quitó las filas del POD.
            if (podChanged && forcedStep == StepType.Complete)
            {
                RepaintCompletedPod(area, session);
                return;
            }

            // Log NC forzado (spec 014, P-5): la BLL desechó la pieza del hallazgo.
            if (podChanged && StepTypes.IsLogNC(forcedStep))
            {
                ClearRowSelection();
                if (area.SerializationType == SerializationType.Serialized)
                    TxtSFC.Text = string.Empty;
                rebind = true;
            }

            // Como en el submit normal: tras Assembly y DC el POD vuelve sin selección.
            if (forcedStep == StepType.Assembly || forcedStep == StepType.DataCollection)
            {
                ClearPodSelection(area);
                rebind = true;
            }

            // ShowStartedOrder ya vuelve a pintar el POD.
            if (podChanged && forcedStep == StepType.Search)
                ShowStartedOrder(area, session);
            else if (rebind)
                BindWorkList(area, session);
        }

        // Si el paso resuelto o el siguiente es SFC Split, se limpia la selección del POD: el
        // split pide elegir la fila de nuevo. Devuelve true si hay que volver a pintar el POD.
        private bool ClearSelectionAroundSplit(StepType resolvedStep)
        {
            var nextStep = _ctx.CurrentStepOrDefault;
            if (resolvedStep != StepType.SFC_Split && nextStep?.StepType != StepType.SFC_Split)
                return false;

            ClearRowSelection();
            return true;
        }

        private void HandleCorrectSubmit(TrainingSession session, TrainingGuide guide,
            Area area, StepType submittedStepType)
        {
            if (session.Status == SessionStatus.Completed)
            {
                CompleteSession(session);
                return;
            }

            var nextStep = _ctx.CurrentStepOrDefault;
            if (nextStep == null) return;

            SetActiveButton(nextStep.StepType, area, _ctx.Policy);
        }
    }
}
