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
    // Página del POD (AUD-74, E6): Handlers de los botones del POD y fin de sesión.
    public partial class GuidedTest
    {
        // Saltar paso / Ir a… (spec 012, D-83, D-84 enmendada): el destino viaja en
        // HfSkipTarget. La BLL ignora uno que ya no está adelante (doble clic, F5). Si el salto
        // cierra la sesión, el resultado; si no (también si se ignoró), el POD se vuelve a armar
        // desde su estado como en un F5 (sin selección ni control abierto): los pasos saltados
        // pueden ser varios y de tipos distintos, y el Ingreso a MES saltado muestra el POD.
        protected void BtnSkip_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, _) = GetContext();
            if (session == null) return;

            string raw = HfSkipTarget.Value;
            HfSkipTarget.Value = string.Empty;
            if (int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int target) &&
                Pod.Skip(_ctx, target).Step?.Outcome == StepSubmitOutcome.SessionCompleted)
            {
                CompleteSession(session);
                return;
            }
            Response.Redirect(Request.RawUrl);
        }

        // ── Button ServerClick handlers ───────────────────────────────────

        protected void BtnResourceSetup_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(Pod.OpenResourceSetup(_ctx), session, guide, area, step)) return;
            OpenStepControl(StepType.ResourceSetup, area, session, guide, ResourceSetupModalId);
        }

        protected void BtnActivities_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            if (HandleOutOfSequence(Pod.OpenLineClearance(_ctx), session, guide, area, step)) return;
            // LineClearance se abre como "Data Collection Standalone" desde Activities.
            OpenStepControl(StepType.LineClearance, area, session, guide, "modalDCStandalone");
        }

        protected void BtnStart_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            var result = Pod.ClickStart(_ctx, BrowseSelectedIds(), ScannedSfc());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            // El mensaje del MES y, si lo hay, el aviso del simulador (Serialized sin piezas:
            // los dos).
            if (result.MesErrors != null || result.Notice != PodNotice.None)
            {
                if (result.MesErrors != null) ShowMesErrors(result.MesErrors);
                if (result.Notice != PodNotice.None) ShowNotice(result.Notice);
                return;
            }

            // Checklist (Moldeo): SearchControl abre su propio Modal 1 desde Initialize()
            // (secuencia de 4 modales); el Start se completa en HandleControlSubmit.
            if (result.OpenChecklist)
            {
                OpenStepControl(StepType.Search, area, session, guide, modalId: null);
                return;
            }

            // Start directo (spec 008): sin modales, se califica solo por secuencia.
            ApplySubmitResult(result.Step, session, guide, area, step, incorrectFeedback: null);
            if (result.Step.Outcome == StepSubmitOutcome.Incorrect) return;
            ShowStartedOrder(area, session);
            ShowBanner("Start process completed successfully");
        }

        // SFC capturado para el Start de NonSerialized: escaneado en TxtSFC o elegido en Browse.
        private string ScannedSfc()
        {
            string sfcValue = TxtSFC.Text.Trim();
            return string.IsNullOrEmpty(sfcValue) ? HfSelectedSFC.Value.Trim() : sfcValue;
        }

        // Start completado (directo, con checklist o forzado): la BLL ya pasó las piezas a
        // Active y el POD las carga sin seleccionar, como el POD real: el operador las
        // selecciona para la actividad siguiente (corrección del usuario, 2026-10-07).
        // NonSerialized: el SFC del lote queda fijo en el header, tomado de la orden (servidor).
        private void ShowStartedOrder(Area area, TrainingSession session)
        {
            var order = Order;
            ClearPodSelection(area);
            if (order != null)
            {
                if (area.SerializationType == SerializationType.NonSerialized)
                    TxtSFC.Text = order.SFCCode;

                SetSearchHeader(order.ShopOrderInput, SimulatedOrderRow.StatusActive);
            }

            BindWorkList(area, session);
        }

        // El POD vuelve sin selección (tras Start, Assembly y DC, como el real). Serialized: el
        // campo SFC refleja la selección, así que queda vacío. NonSerialized: es el SFC del lote
        // y no cambia (spec 007).
        private void ClearPodSelection(Area area)
        {
            ClearRowSelection();
            if (area.SerializationType == SerializationType.Serialized)
                TxtSFC.Text = string.Empty;
        }

        protected void BtnAssemble_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            var result = Pod.OpenAssembly(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (ShowSelectionError(result, area, session)) return;

            // Abre la Component List; el modal Assemble se abre desde cada componente.
            OpenStepControl(StepType.Assembly, area, session, guide, modalId: null);
        }

        // Selección inválida al abrir Assembly, DC o SFC Split (lo decide la BLL, sin intento):
        // sin la fila del lote, el 12906 del MES y la selección del POD se limpia; en Serialized,
        // el aviso del simulador. Devuelve si hubo alguno.
        private bool ShowSelectionError(PodCommandResult result, Area area, TrainingSession session)
        {
            if (result.MesErrors != null)
            {
                ClearRowSelection();
                BindWorkList(area, session);
                ShowMesErrors(result.MesErrors);
                return true;
            }
            if (result.Notice != PodNotice.None)
            {
                ShowNotice(result.Notice);
                return true;
            }
            return false;
        }

        protected void BtnDCCollection_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            // Defensa en servidor: el HiddenField puede faltar, estar obsoleto o ser
            // manipulado aunque la validación JavaScript haya sido omitida. La BLL decide la
            // selección y, en la primera apertura, fija las piezas a recorrer (spec 008).
            var result = Pod.OpenDataCollection(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (ShowSelectionError(result, area, session)) return;

            OpenStepControl(StepType.DataCollection, area, session, guide, modalId: null);
        }

        // Spec 010: se eligió un código NC y se abrió NC Data Entry; la guía entra en el
        // formulario. Mismo guard de sender que los demás handlers del control.
        protected void HandleNCCodeSelected(object sender, EventArgs e)
        {
            if (_ctx == null || !ReferenceEquals(sender, CtrlLogNC)) return;
            _guideMoment = GuideMoment.Opened(_ctx.CurrentStep, "entry");
        }

        // Spec 010: Collect abrió el Data Collection Entry de un grupo; la guía entra en sus
        // parámetros. Mismo guard de sender que los demás handlers del control.
        protected void HandleGroupOpened(object sender, EventArgs e)
        {
            if (_ctx == null || !ReferenceEquals(sender, CtrlDataCollection)) return;
            _guideMoment = GuideMoment.Opened(_ctx.CurrentStep, CtrlDataCollection.OpenGroupCode);
        }

        // Data Collection (spec 008): Save de un DC Group para la pieza actual.
        protected void HandleGroupSubmit(object sender, DataCollectionGroupSubmitEventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;

            // Mismo guard de sender que HandleControlSubmit (spec 006, E12).
            if (!ReferenceEquals(sender, GetStepControl(step.StepType))) return;

            // Pieza para la que se guarda el grupo (la guía entra en su banner, spec 010).
            string pieceRowId = GetDataCollectionProgress()?.CurrentPiece?.RowId;
            var result = Pod.SubmitDataCollectionGroup(_ctx, e.GroupCode, e.AnswersJson);
            switch (result.Outcome)
            {
                case DataCollectionOutcome.Incorrect:
                    // El control queda abierto en el modal donde se respondió (spec 006, E3);
                    // grupos y piezas ya guardados se conservan (AC-8).
                    // Spec 017 (C-6): el mensaje del MES en la franja, junto al feedback de hoy.
                    if (result.MesError != null)
                        CtrlDataCollection.ShowMesError(NoteMesError(result.MesError), MesHelp(result.MesError));
                    ApplySubmitResult(result.StepResult, session, guide, area, step, incorrectFeedback: null);
                    _guideMoment = GuideMoment.Incorrect(step, e.GroupCode);
                    ReopenModal("modalDCEntry");
                    return;

                case DataCollectionOutcome.GroupSaved:
                    RefreshDataCollectionControl(session, guide, area, step);
                    _guideMoment = GuideMoment.Progress(step, pieceRowId + "|" + e.GroupCode);
                    ShowBanner("All data for DC group " + e.GroupCode + " has been collected", hideAfterMs: 4000);
                    return;

                case DataCollectionOutcome.PieceCompleted:
                    RefreshDataCollectionControl(session, guide, area, step);
                    _guideMoment = GuideMoment.Progress(step, pieceRowId + "|" + e.GroupCode);
                    ShowBanner(string.Format("SFC {0} completed. Next: {1}",
                        result.PieceSfcCode, DescribeCurrentPiece(GetDataCollectionProgress())), hideAfterMs: 5000);
                    return;

                case DataCollectionOutcome.StepResolved:
                    // Paso completo, forzado (E5: la BLL descarta el avance parcial) o sesión terminada.
                    ApplySubmitResult(result.StepResult, session, guide, area, step, incorrectFeedback: null);
                    ContinueAfterStepResolved(session, guide, area, step, result.StepResult.Outcome);
                    return;
            }
        }

        private void RefreshDataCollectionControl(TrainingSession session, TrainingGuide guide,
            Area area, TrainingStep step)
        {
            CtrlDataCollection.ViewModel = BuildStepViewModel(step, area, session, guide);
            CtrlDataCollection.ShowGroupList();
        }

        // Avance de DC (estado del POD, lo maneja la BLL): sobrevive a Return to POD y a F5.
        private DataCollectionProgress GetDataCollectionProgress() => _ctx.State.DataCollection;

        protected void BtnSFCSplit_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            var result = Pod.OpenSplit(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (ShowSelectionError(result, area, session)) return;

            OpenStepControl(StepType.SFC_Split, area, session, guide, "modalSFCSplit");
        }

        protected void BtnLogNC_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            // La BLL decide: aviso si en Serialized no queda pieza para Complete; intento
            // fallido si no hay una fila scrapeable (spec 007/008).
            var result = Pod.OpenLogNC(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (result.Notice != PodNotice.None)
            {
                ShowNotice(result.Notice);
                return;
            }
            if (result.Step != null)
            {
                // Spec 017 (M-5): sin ninguna fila, el 12906 junto al feedback del intento.
                if (result.MesErrors != null) ShowMesErrors(result.MesErrors);
                ApplySubmitResult(result.Step, session, guide, area, step,
                    AttemptFeedback(result.FeedbackReason, area));
                // Avance forzado (spec 014, P-5): la BLL aplicó la disposición del hallazgo.
                if (result.Step.Outcome == StepSubmitOutcome.FailedForced)
                    ShowLogNCResolved(area, session, result);
                return;
            }

            // El paso de Log NC actual (spec 014: el botón abre cualquiera de los tres).
            OpenStepControl(step.StepType, area, session, guide, modalId: null);
        }

        // Acciones de MES que nunca son un paso de la guía (Work Instructions, Reports, Place
        // Back in Queue, Tool List y las opciones de relleno de Activities). No tienen lógica:
        // en Assessment el click es un intento fallido del paso actual (S2).
        protected void BtnNonStepAction_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            HandleOutOfSequence(Pod.ClickNonStepAction(_ctx, NonStepAction(sender)), session, guide, area, step);
        }

        // Botón de relleno → su acción en el registro de actividad (spec 013). Es también la lista
        // de botones de relleno (SetActiveButton): un botón nuevo se agrega aquí una sola vez.
        private Dictionary<System.Web.UI.HtmlControls.HtmlButton, string> NonStepActions =>
            new Dictionary<System.Web.UI.HtmlControls.HtmlButton, string>
            {
                { BtnWorkInstructions,      AttemptActions.WorkInstruction },
                { BtnReports,               AttemptActions.Reports },
                { BtnPlaceBackInQueue,      AttemptActions.PlaceBackInQueue },
                { BtnToolList,              AttemptActions.ToolList },
                { BtnAsBuiltConfig,         AttemptActions.AsBuiltConfig },
                { BtnLogComment,            AttemptActions.LogComment },
                { BtnSfcPlaceHold,          AttemptActions.SfcPlaceHold },
                { BtnSfcReleaseHold,        AttemptActions.SfcReleaseHold },
                { BtnRemoveFutureHold,      AttemptActions.RemoveFutureHold },
                { BtnChangeEquipmentStatus, AttemptActions.ChangeEquipmentStatus },
                { BtnTimeSensitiveMaterial, AttemptActions.TimeSensitiveMaterial }
            };

        private string NonStepAction(object sender)
        {
            var button = sender as System.Web.UI.HtmlControls.HtmlButton;
            if (button == null || !NonStepActions.TryGetValue(button, out string action))
                throw new InvalidOperationException("Botón de relleno sin acción en el registro de actividad.");
            return action;
        }

        private IEnumerable<System.Web.UI.HtmlControls.HtmlButton> NonStepActionButtons => NonStepActions.Keys;

        protected void BtnComplete_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            // La BLL decide: aviso por selección parcial (Serialized), intento fallido sin la
            // selección del paso (spec 007/008) o el Complete.
            var result = Pod.Complete(_ctx, SelectedIds());
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            if (result.Notice != PodNotice.None)
            {
                ShowNotice(result.Notice);
                return;
            }

            // Spec 017 (M-4): sin ninguna fila, el 22506 junto al feedback del intento.
            if (result.MesErrors != null) ShowMesErrors(result.MesErrors);
            ApplySubmitResult(result.Step, session, guide, area, step,
                AttemptFeedback(result.FeedbackReason, area));
            ShowCompleted(area, session, result.Completed, banner: result.Step.Outcome == StepSubmitOutcome.Correct);
        }

        // Spec 015 (D-22): las filas completadas salen del POD (las quitó la BLL). Banner
        // genérico PROVISIONAL hasta tener la captura real (general-flow → Paso 9).
        private void ShowCompleted(Area area, TrainingSession session, IList<SimulatedOrderRow> rows, bool banner)
        {
            if (rows == null || rows.Count == 0 || session.Status != SessionStatus.InProgress) return;

            RepaintCompletedPod(area, session);
            if (banner)
                ShowBanner(area.SerializationType == SerializationType.Serialized
                    ? rows.Count + " SFCs completed"
                    : "SFC " + rows[0].SFCCode + " completed", hideAfterMs: 5000);
        }

        // El POD después de Complete (normal o forzado): sin selección, sin SFC ni header.
        private void RepaintCompletedPod(Area area, TrainingSession session)
        {
            ClearRowSelection();
            TxtSFC.Text = string.Empty;
            BindWorkList(area, session);
            BindSearchHeader();
        }

        protected void BtnAbandon_Click(object sender, EventArgs e)
        {
            if (_ctx == null) return;
            // Abandona la sesión y borra su estado del POD. Al completar, el estado queda en la
            // Session por diseño (Deferred Decisions de la spec 005).
            Pod.Abandon(_ctx);
            Response.Redirect("~/Pages/Training/StartSession.aspx");
        }

        // Logout con la sesión de entrenamiento en curso (spec 015): antes del último paso es un
        // botón más del POD (S2). En el último paso empieza la limpieza (spec 016, P-6): "You
        // have logged off" en Edge, sin intento; el script la termina (HandleMesAction). Sin
        // sesión en curso no llega acá: LoadContext ya salió.
        protected void BtnLogout_ServerClick(object sender, EventArgs e)
        {
            var (session, guide, area, step) = GetContext();
            if (session == null) return;
            var result = Pod.ClickLogout(_ctx);
            if (HandleOutOfSequence(result, session, guide, area, step)) return;
            _mesScreen = result.MesScreen;
        }

        // ── Fin de sesión (spec 025, D-338) ──────────────────────────────
        // La sesión se completó en este request (el último paso, un salto o un avance forzado):
        // redirige al resultado. Con la limpieza al salir cerrada por el script (P-12) la consola
        // muestra primero su salida y, al cerrarla, el cliente navega (MesLoginView.CloseUrl); el
        // POD queda cerrado detrás.
        private void CompleteSession(TrainingSession session)
        {
            if (!_cleanupCompleted)
            {
                Response.Redirect(ResultUrl(session));
                return;
            }
            CloseStepControl();
            PnlWorkList.Visible = false;
            PnlScore.Visible    = false;
            BtnAbandon.Disabled = true;
        }
    }
}
