using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Guide;
using TrainingApp.Pages.Training.Models;
using TrainingApp.Shared;
using static TrainingApp.Pages.Training.GuidedTestHelpers;

namespace TrainingApp.Pages.Training
{
    // Página del POD (AUD-74, E6): Pintado de barras, botones, encabezado y guía.
    public partial class GuidedTest
    {
        // Mensajes del MES que muestra un chequeo del cliente (spec 017): DC Collect o SFC Split
        // sin fila, un escaneo ajeno en Start y un barcode no numérico. Su explicación va con la
        // página solo si el modo la recibe (D-102): la nota en Guided, Ver pista en Práctica
        // libre; Assessment no recibe nada. Texto del catálogo y nota de MesErrorNotes.
        private void RegisterClientMesNotes()
        {
            if (_ctx == null) return;
            bool guide = _ctx.Policy.ShowsGuide, hint = _ctx.Policy.HintOnDemand;
            if (!guide && !hint) return;

            var ids = new[] { MesMessageId.SfcMissing, MesMessageId.InvalidSfc, MesMessageId.BarcodeNotCompliant };
            var notes = ids.ToDictionary(id => id.ToString(), id => new
            {
                text   = MesMessage.Get(id).Format(),
                anchor = MesErrorNotes.AnchorName(id),
                html   = guide ? MesErrorNotes.Html(id) : null,
                plain  = hint ? MesErrorNotes.Plain(id) : null
            });
            ClientScript.RegisterStartupScript(GetType(), "podMesNotes",
                "window.podMesNotes = " + new JavaScriptSerializer().Serialize(notes) + ";", true);
        }

        // Barra de práctica (spec 012, D-87 a D-89): Ver pista, Saltar paso e Ir a… según la
        // política, en el pie del POD y en la barra del escritorio. Se decide después de los
        // handlers (el paso pudo cambiar en este request); Visible persiste en ViewState: se
        // asigna siempre. La pista y los pasos llegan con la página (JavaScriptSerializer
        // escapa < > & ').
        private void ApplyPracticeBar()
        {
            var current = _ctx != null && _ctx.Session.Status == SessionStatus.InProgress
                ? _ctx.CurrentStepOrDefault
                : null;
            bool hint = current != null && _ctx.Policy.HintOnDemand;
            bool skip = current != null && _ctx.Policy.AllowsSkip;
            BtnHint.Visible = BtnMesHint.Visible = hint;
            BtnSkip.Visible = BtnMesSkip.Visible = BtnGoTo.Visible = BtnMesGoTo.Visible = skip;
            PnlPractice.Visible = PnlMesPractice.Visible = hint || skip;
            if (!hint && !skip) return;

            var steps = _ctx.Guide.Steps;
            var next  = steps.Where(s => s.Order > current.Order).OrderBy(s => s.Order)
                .Select(s => new { order = s.Order, name = StepNames.Display(steps, s) })
                .ToList();
            BtnGoTo.Disabled = BtnMesGoTo.Disabled = next.Count == 0;
            BtnSkip.Attributes["onclick"]    = "return practiceSkip('" + BtnSkip.UniqueID + "');";
            BtnMesSkip.Attributes["onclick"] = "return practiceSkip('" + BtnMesSkip.UniqueID + "');";
            BtnGoTo.Attributes["onclick"]    = "return practiceGoTo('" + BtnSkip.UniqueID + "');";
            BtnMesGoTo.Attributes["onclick"] = "return practiceGoTo('" + BtnMesSkip.UniqueID + "');";

            var data = new
            {
                hint     = StepHints.For(_ctx, current.StepType),
                failure  = _lastFailureFeedback,
                current  = new { order = current.Order, name = StepNames.Display(steps, current) },
                next,
                hiddenId = HfSkipTarget.ClientID
            };
            ClientScript.RegisterStartupScript(GetType(), "podPractice",
                "window.podPractice = " + new JavaScriptSerializer().Serialize(data) + ";", true);
        }

        // Explicación en español de un mensaje del MES para su (?) (spec 017, D-102): solo en
        // Práctica libre (la que tiene Ver pista); null en Guided (lleva la nota de la guía) y
        // en Assessment.
        private string MesHelp(MesError error)
            => _ctx != null && _ctx.Policy.HintOnDemand && MesErrorNotes.Has(error.Id)
                ? MesErrorNotes.Plain(error.Id)
                : null;

        // Nota de hallazgo (spec 014, D-49): solo el hallazgo del paso actual de Log NC, en los
        // dos modos, sin nombrar el código. Se decide después de los handlers (el paso pudo
        // cambiar en este request); Visible persiste en ViewState: se asigna siempre.
        private void ApplyNcFindingNote()
        {
            var finding = _ctx?.CurrentNcFinding;
            PnlNcFinding.Visible = finding != null && _ctx.Session.Status == SessionStatus.InProgress;
            if (!PnlNcFinding.Visible) return;

            // Guided: la guía la muestra al llegar al paso y la deja resaltada (pod-guide.js);
            // Assessment no tiene guía y la ve de inmediato. Los atributos persisten en ViewState.
            if (_ctx.Policy.ShowsGuide)
                PnlNcFinding.Attributes["data-guide-reveal"] = string.Empty;
            else
                PnlNcFinding.Attributes.Remove("data-guide-reveal");

            bool serialized = finding.RowId != null;
            LitFindingTitle.Text = string.Format("Hallazgo {0} de {1}", finding.Index, finding.Total);
            LitFindingPiece.Text = serialized
                ? "SFC <strong>" + HttpUtility.HtmlEncode(finding.SFCCode) + "</strong>"
                : "Lote <strong>" + HttpUtility.HtmlEncode(finding.SFCCode) + "</strong>: " +
                  finding.UnitCount.ToString(CultureInfo.InvariantCulture) + " unidades";
            LitFindingDefect.Text      = HttpUtility.HtmlEncode(finding.Description);
            LitFindingDisposition.Text = HttpUtility.HtmlEncode(FindingDisposition(finding));
        }

        // Etiqueta de la orden (spec 011, enmienda 2026-10-13): visible desde que hay orden
        // hasta el Start, con su Shop Order (la por defecto si Resource Setup avanzó forzado:
        // AC-3). Se decide después de los handlers porque Resource Setup o el Start de este
        // mismo request la cambian. Visible persiste en ViewState: se asigna en cada request.
        private void ApplyOrderLabel()
        {
            var order = Order;
            PnlOrderLabel.Visible = order != null && !PodOrderStarted && _ctx.Session.Status == SessionStatus.InProgress;
            LabelShopOrder = order?.ShopOrderInput ?? string.Empty;
        }

        protected string LabelShopOrder { get; private set; } = string.Empty;

        // Spec 011: TxtSFC se escribe o se escanea hasta el Start (la carga la resuelve el
        // cliente, como Browse Work List); con el Start ya ejecutado vuelve a ser de solo
        // lectura, como antes. Atributo HTML y no la propiedad ReadOnly: con ReadOnly=true
        // ASP.NET ignora el valor posteado y lo cargado se perdería en cada postback. Se decide
        // en PreRender porque el Start de este mismo request ya lo cambia (los atributos
        // persisten en ViewState).
        private void ApplySfcFieldMode()
        {
            if (PodOrderStarted)
                TxtSFC.Attributes["readonly"] = "readonly";
            else
                TxtSFC.Attributes.Remove("readonly");
        }

        // Mensajes del MES de los chequeos de formato del cliente (spec 017), los mismos del
        // catálogo de la BLL, ya codificados para un string de JS: un número que no es de la
        // orden (22507, spec 011 punto 5) y DC Collect o SFC Split sin fila (12906).
        protected string InvalidSfcMesErrorJs => MesErrorJs(MesMessageId.InvalidSfc);

        protected string SfcMissingMesErrorJs => MesErrorJs(MesMessageId.SfcMissing);

        // Guía visual (spec 010): solo en Guided. Assessment no recibe el motor, el guion, los
        // textos ni el estado (AC-2). Pasos sin guion todavía: la guía no se muestra.
        private void RegisterGuide()
        {
            // Visible persiste en ViewState: se decide en cada request.
            PhGuideAssets.Visible = false;
            if (_ctx == null || !_ctx.Policy.ShowsGuide) return;

            // Spec 017 (D-102): con un mensaje del MES en pantalla, su nota en español primero.
            var moment = _lastMesError != null ? _guideMoment.WithMesError(_lastMesError.Id) : _guideMoment;
            var script = new GuideScriptBuilder().Build(_ctx, moment);
            if (script.IsEmpty) return;

            PhGuideAssets.Visible = true;
            // JavaScriptSerializer escapa < > & ' : el HTML de los textos no puede cerrar el <script>.
            ClientScript.RegisterStartupScript(GetType(), "podGuide",
                "PodGuide.start(" + new JavaScriptSerializer().Serialize(script.ToClient()) + ");", true);
        }

        // POD Search: Shop Order y Status (SFC) de la orden cargada. Antes del Start, las
        // piezas elegidas en Browse Work List (In Queue); con el Start, Active. Se recalcula
        // en cada request porque el OK de Browse solo lo adelanta en el cliente.
        private void BindSearchHeader()
        {
            var order = Order;
            if (order != null && _ctx.IsResolved(StepType.Complete))
            {
                // Spec 015 (D-22): la orden ya salió del POD. Sin header; el campo SFC sigue de
                // solo lectura y la etiqueta de la orden no vuelve (PodOrderStarted).
                SetSearchHeader(string.Empty, string.Empty);
                PodOrderStarted = true;
            }
            else if (order != null && _ctx.IsResolved(StepType.Search))
                SetSearchHeader(order.ShopOrderInput, SimulatedOrderRow.StatusActive);
            else if (order != null && FindBrowseSelectedRows(order).Count > 0)
                SetSearchHeader(order.ShopOrderInput, SfcStatusInQueue);
            else
                SetSearchHeader(string.Empty, string.Empty);
        }

        private void SetSearchHeader(string shopOrder, string sfcStatus)
        {
            LitShopOrder.Text = HttpUtility.HtmlEncode(shopOrder);
            LitSFCStatus.Text = HttpUtility.HtmlEncode(sfcStatus);
            PodOrderStarted   = sfcStatus == SimulatedOrderRow.StatusActive;
        }

        // ── StepViewModel builder ────────────────────────────────────────
        private StepViewModel BuildStepViewModel(TrainingStep step, Area area,
            TrainingSession session, TrainingGuide guide)
        {
            var vm = new StepViewModel();

            // Spec 005: BuildSimulatedSFCCode ya no existe — el SFC real vive en la
            // SimulatedOrder de sesión. Antes de Resource Setup no hay orden aún,
            // así que se usa un placeholder en vez de fallar.
            // NonSerialized: el SFC del lote. Serialized: el SFC o "[N SELECTED]" de la selección
            // (ej. NC Data Entry de varias piezas, spec 008).
            string sfcCode = Order?.SFCCode;
            if (string.IsNullOrEmpty(sfcCode))
                sfcCode = DescribeSelection(FindSelectedOrderRows());
            if (string.IsNullOrEmpty(sfcCode))
                sfcCode = "—";
            string resourceName = PodDefaults.ResourceName(area);
            string opName     = guide.Title;

            vm.ExtraData["SFCCode"]      = sfcCode;
            vm.ExtraData["ResourceName"] = resourceName;
            vm.ExtraData["OperationName"] = opName;
            vm.ExtraData["Site"]         = PodDefaults.Site;

            if (StepTypes.IsLogNC(step.StepType))
            {
                vm.ExtraData["NCCodes"] = _ctx.NCCodes;
                // "SFC (1/N)" y "Apply to all SFCs" de NC Data Entry (Fase 6.5, entrega 8).
                var ncPieces = FindSelectedOrderRows();
                vm.ExtraData["SelectedPieceCount"] = ncPieces.Count;
                vm.ExtraData["FirstSelectedSFC"]   = ncPieces.Count > 0
                    ? ncPieces[0].SFCCode ?? Order?.SFCCode
                    : null;
                // Guided (spec 014): NC Selection abre en la página del código del hallazgo, que
                // señala la guía. Assessment no lo recibe.
                if (_ctx.Policy.ShowsGuide)
                    vm.ExtraData["GuideNCCode"] = _ctx.NcFindingFor(step)?.Code;
                // Browse y reparaciones (spec 014): lo ensamblado y As-Built Configuration (P-11:
                // de la orden simulada; Routing y BOM son simulados; la operación, la actual del
                // área, D-58).
                vm.ExtraData["Assembled"] = _ctx.State.AssembledComponents ?? new List<AssembledComponent>();
                vm.ExtraData["RepairInfo"] = new Dictionary<string, string>
                {
                    ["sfc"]         = vm.ExtraData["FirstSelectedSFC"] as string ?? sfcCode,
                    ["material"]    = PodDefaults.MaterialVersion(Order),
                    ["description"] = Order?.MaterialDescription ?? string.Empty,
                    ["routing"]     = PodDefaults.SimRouting(area),
                    ["bom"]         = PodDefaults.SimBom(Order),
                    ["status"]      = SimulatedOrderRow.StatusActive,
                    ["operation"]   = _ctx.CurrentOperationName,
                    ["resource"]    = resourceName
                };
                return vm;
            }

            switch (step.StepType)
            {
                case StepType.ResourceSetup:
                    vm.ExtraData["Preloaded"] = _ctx.State.PreloadedEquipment;
                    if (IsCustomResourceSetup)
                    {
                        vm.ExtraData["CustomResourceSetup"] = _ctx.CustomResourceSetup;
                        vm.ExtraData["Code1"] = _ctx.Code1;
                    }
                    break;

                case StepType.LineClearance:
                    vm.ExtraData["QuestionText"] = PodDefaults.LineClearanceQuestion;
                    break;

                case StepType.DataCollection:
                    vm.ExtraData["Parameters"] = _ctx.DataCollectionParameters;
                    vm.ExtraData["Groups"]     = _ctx.DataCollectionGroups;
                    var dcProgress = GetDataCollectionProgress();
                    vm.ExtraData["PieceLabel"] = DescribeCurrentPiece(dcProgress);
                    // Grupos ya guardados por la pieza actual: su Collect se deshabilita.
                    vm.ExtraData["SavedGroupIds"] = dcProgress?.CurrentPieceSavedGroupIds ?? new HashSet<int>();
                    break;

                case StepType.SFC_Split:
                    var selectedRow = FindSelectedOrderRow();
                    // "SFC to Split" es el SFC de la fila elegida: una fila que ya salió de otro
                    // split tiene su propio SFC, distinto del lote.
                    if (selectedRow?.SFCCode != null)
                        vm.ExtraData["SFCCode"] = selectedRow.SFCCode;
                    vm.ExtraData["AvailableQty"] = selectedRow?.Qty ?? 0;
                    vm.ExtraData["SelectedRowId"] = selectedRow?.RowId;
                    vm.ExtraData["HasSuccessfulSplit"] = Order != null && Order.HasSplit;
                    break;

                case StepType.Assembly:
                    // BOM del área y componentes ya ensamblados (spec 008).
                    vm.ExtraData["Components"]         = _ctx.AssemblyComponents;
                    vm.ExtraData["AssembledIds"]       = _ctx.State.Assembly?.AssembledComponentIds
                                                         ?? new HashSet<int>();
                    vm.ExtraData["SelectedPieceCount"] = FindSelectedOrderRows().Count;
                    vm.ExtraData["ProcessLot"]         = Order?.ProcessLot;
                    break;

                case StepType.Search:
                    // SearchControl owns its own 4-modal flow (labels hardcoded in the .ascx);
                    // it only needs ResourceName, already populated above.
                    break;
            }

            return vm;
        }

        // ── UI helpers ───────────────────────────────────────────────────
        private void PopulateHeader(TrainingSession session, Area area, TrainingGuide guide)
        {
            // El MES real muestra el Code1 del empleado, no su nombre (Fase 6.5).
            LitUserName.Text      = HttpUtility.HtmlEncode(
                Session[Constants.SessionKeys.Code1]?.ToString() ?? "");
            LitOperationName.Text = HttpUtility.HtmlEncode(guide.Title);
            LitResourceName.Text  = HttpUtility.HtmlEncode(PodDefaults.ResourceName(area));
            LitBrowseContext.Text = string.Format("{0}, {1}, {2}",
                area.Name, PodDefaults.ResourceName(area),
                Session[Constants.SessionKeys.Code1]?.ToString() ?? "");
        }

        private void UpdateScoreWidget(TrainingSession session)
        {
            // Puntaje del contexto: el de la carga o el del último intento registrado (AUD-25).
            LitScore.Text = FormatScore(_ctx.Score);
            PnlScore.Visible = _ctx.Policy.ShowsScore;

            var (label, badgeClass) = ModeLabel(_ctx.Policy);
            LitModeBadge.Text = string.Format(
                "<span class=\"{0}\">{1}</span>", badgeClass, label);
        }

        private void SetActiveButton(StepType stepType, Area area, ModePolicy policy)
        {
            // Assessment (S2): todo habilitado — elegir el botón correcto es parte de la evaluación.
            bool enableAll = policy.EnablesAllButtons;

            BtnResourceSetup.Disabled = !enableAll;
            BtnStart.Disabled         = !enableAll;
            BtnAssemble.Disabled      = !enableAll;
            BtnDCCollection.Disabled  = !enableAll;
            BtnComplete.Disabled      = !enableAll;
            BtnActivities.Disabled    = !enableAll;
            BtnSFCSplit.Disabled      = !enableAll;
            BtnLogNC.Disabled         = !enableAll;
            BtnLogout.Disabled        = !enableAll;
            foreach (var decoy in NonStepActionButtons)
                decoy.Disabled = !enableAll;

            // La validación de fila en cliente (formato) solo aplica al botón del paso actual:
            // un click fuera de secuencia tiene que llegar al servidor para contar.
            SetClientRowCheck(BtnDCCollection, "podValidateDCCollect", stepType == StepType.DataCollection);
            SetClientRowCheck(BtnSFCSplit, "podValidateSFCSplit", stepType == StepType.SFC_Split);
            // Spec 011 (enmienda E-1): con Search como paso actual, Start carga primero lo escrito
            // en TxtSFC sin Enter. Fuera de turno no hay chequeo: el click cuenta en Assessment (S2).
            SetClientRowCheck(BtnStart, "podLoadBeforeStart", stepType == StepType.Search);

            if (enableAll)
            {
                EnableActivitiesToggle();
                return;
            }

            // Spec 014 (P-7): los tres pasos de Log NC se abren con [Log NC].
            if (StepTypes.IsLogNC(stepType))
            {
                BtnLogNC.Disabled = false;
                return;
            }

            switch (stepType)
            {
                case StepType.ResourceSetup:
                    BtnResourceSetup.Disabled = false;
                    break;
                case StepType.LineClearance:
                    BtnActivities.Disabled = false;
                    EnableActivitiesToggle();
                    break;
                case StepType.Search:
                    BtnStart.Disabled = false;
                    break;
                case StepType.Assembly:
                    BtnAssemble.Disabled = false;
                    break;
                case StepType.DataCollection:
                    BtnDCCollection.Disabled = false;
                    break;
                case StepType.SFC_Split:
                    if (area.SerializationType == SerializationType.NonSerialized)
                        BtnSFCSplit.Disabled = false;
                    break;
                case StepType.Complete:
                    BtnComplete.Disabled = false;
                    break;
                case StepType.Logout:
                    BtnLogout.Disabled = false;
                    break;
            }
        }

        // Enable the dropdown toggle so the Activities menu can open
        private void EnableActivitiesToggle()
        {
            Page.ClientScript.RegisterStartupScript(GetType(), "enableActivitiesToggle",
                "var t=document.getElementById('BtnActivitiesToggle');" +
                "if(t) t.disabled=false;", true);
        }

        // Se asigna en cada request: los atributos de un HtmlControl persisten en ViewState.
        private static void SetClientRowCheck(System.Web.UI.HtmlControls.HtmlButton button,
            string validatorFunction, bool isCurrentStep)
        {
            if (isCurrentStep)
                button.Attributes["onclick"] = "if (!" + validatorFunction + "()) return false;";
            else
                button.Attributes.Remove("onclick");
        }

        // Mensaje del MES entre el header y el POD Search (con ícono verde, como el real).
        // Ocupa su propia franja y desaparece solo después de hideAfterMs, salvo que la guía de
        // Guided lo esté mostrando (spec 010, R9).
        private void ShowBanner(string message, int hideAfterMs = 3000)
        {
            PnlBannerZone.Visible = true;
            LitBanner.Text        = HttpUtility.HtmlEncode(message);
            ClientScript.RegisterStartupScript(GetType(), "hideBanner",
                "podAutoHide('.pod-banner', " + hideAfterMs + ");", true);
        }
    }
}
