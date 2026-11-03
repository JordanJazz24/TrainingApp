using System;
using System.Linq;
using System.Web.UI;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;
using TrainingApp.Pages.Training.Guide;
using TrainingApp.Pages.Training.Models;
using static TrainingApp.Pages.Training.GuidedTestHelpers;

namespace TrainingApp.Pages.Training
{
    // Página del POD (AUD-74, E6): Visibilidad y apertura de los controles de paso (spec 006).
    public partial class GuidedTest
    {
        // ── Controles de paso estáticos (spec 006) ───────────────────────
        // Los 8 controles están declarados en el .aspx, así que el árbol es idéntico en
        // todos los requests. Qué paso está abierto vive en el ViewState de la página
        // (firmado con MAC) y ApplyStepControlVisibility es el único que toca Visible.
        private const string OpenStepTypeKey = "GDT_OpenStepType";

        // Pasos con control propio: Logout es el ícono del header (spec 015). Los pasos de Log
        // NC comparten un control (spec 014, D-60). Ingreso a MES no se abre como un control
        // de paso: es una pantalla completa mientras es el paso actual (spec 016, P-1).
        private static readonly StepType[] AllStepTypes =
            ((StepType[])Enum.GetValues(typeof(StepType)))
                .Where(t => t != StepType.Logout && t != StepType.MesLogin).ToArray();

        // Ingreso a MES (spec 016): la pantalla completa reemplaza al POD en esta respuesta.
        protected bool MesScreenActive { get; private set; }

        // Pantalla que decidió la BLL en el handler del Ingreso (la que falló o la que sigue);
        // null: la del avance guardado (F5, D-78).
        private MesScreen? _mesScreen;

        // La limpieza al salir cerró la sesión en este request con el script (spec 016, P-12):
        // la consola muestra su salida y, al cerrarla, el cliente navega al resultado (spec 025,
        // D-338). En cualquier otro cierre la página redirige en el mismo request.
        private bool _cleanupCompleted;

        // Quién valida que una sesión sea propia y completada antes de llevar a su resultado.
        private readonly SessionResultService _results = new SessionResultService();

        private StepType? OpenStepType
        {
            get { return ViewState[OpenStepTypeKey] as StepType?; }
            set { ViewState[OpenStepTypeKey] = value; }
        }

        // Serialized (spec 008): el POD y Browse Work List permiten varias filas y TxtSFC
        // muestra "[N SELECTED]". NonSerialized: un click selecciona una fila (spec 007); el
        // ícono Select All selecciona todas en ambos tipos.
        protected bool PodMultiSelect { get; private set; }

        // Search/Start ya completado: el POD Search muestra la orden Active y Browse Work
        // List ya no la cambia a In Queue en el cliente.
        protected bool PodOrderStarted { get; private set; }

        // Status (SFC) de las piezas elegidas en Browse Work List antes del Start.
        private const string SfcStatusInQueue = "In Queue";

        // Ícono Select All del header del POD y de Browse Work List: cuadros apilados, como
        // en el MES real. Los colores salen del CSS (.select-all-btn).
        protected const string SelectAllIcon =
            "<svg width=\"15\" height=\"15\" viewBox=\"0 0 15 15\" aria-hidden=\"true\">" +
            "<rect class=\"sa-back\" x=\"0.5\" y=\"0.5\" width=\"9\" height=\"9\" />" +
            "<rect class=\"sa-back\" x=\"2.5\" y=\"2.5\" width=\"9\" height=\"9\" />" +
            "<rect class=\"sa-front\" x=\"5\" y=\"5\" width=\"9\" height=\"9\" />" +
            "</svg>";

        // Pantalla de Resource Setup de la sesión (spec 020): la estándar o Custom RS.
        private bool IsCustomResourceSetup => _ctx?.ResourceSetupType == ResourceSetupType.Custom;

        private string ResourceSetupModalId
            => IsCustomResourceSetup ? "modalCustomResourceSetup" : "modalResourceSetup";

        private IStepControl GetStepControl(StepType stepType)
        {
            if (StepTypes.IsLogNC(stepType)) return CtrlLogNC;
            switch (stepType)
            {
                case StepType.ResourceSetup:
                    return IsCustomResourceSetup ? (IStepControl)CtrlCustomResourceSetup : CtrlResourceSetup;
                case StepType.LineClearance:  return CtrlLineClearance;
                case StepType.Search:         return CtrlSearch;
                case StepType.Assembly:       return CtrlAssembly;
                case StepType.DataCollection: return CtrlDataCollection;
                case StepType.SFC_Split:      return CtrlSFCSplit;
                case StepType.Complete:       return CtrlComplete;
                // Sin control: los guards de sender comparan contra null y no aceptan nada.
                // MesLoginControl tiene su propio evento (HandleMesAction).
                case StepType.Logout:         return null;
                case StepType.MesLogin:       return null;
                default: throw new ArgumentOutOfRangeException(nameof(stepType), stepType, null);
            }
        }

        // renderOnce: control ya cerrado que igual se renderiza en esta respuesta para
        // mostrar su confirmación; en el request siguiente deja de estar visible.
        private void ApplyStepControlVisibility(StepType? open, StepType? renderOnce = null)
        {
            StepType? shown = open ?? renderOnce;
            var shownControl = shown.HasValue ? GetStepControl(shown.Value) : null;
            foreach (var type in AllStepTypes)
            {
                var control = GetStepControl(type);
                ((Control)control).Visible = ReferenceEquals(control, shownControl);
            }

            bool replacesWorkList = shown.HasValue && !IsModalOverlay(shown.Value);
            PnlModalHost.Visible     = shown.HasValue && IsModalOverlay(shown.Value);
            PnlActiveControl.Visible = replacesWorkList;
            PnlWorkList.Visible      = !replacesWorkList;
        }

        private void OpenStepControl(StepType stepType, Area area, TrainingSession session,
            TrainingGuide guide, string modalId)
        {
            var step = _ctx.CurrentStep;
            var ctrl = GetStepControl(stepType);
            ctrl.ViewModel = BuildStepViewModel(step, area, session, guide);
            ctrl.Initialize();

            OpenStepType = stepType;
            ApplyStepControlVisibility(stepType);
            _guideMoment = GuideMoment.Opened(step);

            if (!string.IsNullOrEmpty(modalId))
                ReopenModal(modalId);
        }

        private void CloseStepControl(StepType? renderOnce = null)
        {
            OpenStepType = null;
            ApplyStepControlVisibility(null, renderOnce);
        }

        // Modal donde el operador ingresa la respuesta de cada paso. LineClearance responde
        // en modalDCEntry, apilado sobre modalDCStandalone (que el control ya reabre).
        // LogNC no usa uno: su entrada es un panel de servidor que conserva su estado.
        private string GetAnswerModalId(StepType stepType)
        {
            switch (stepType)
            {
                case StepType.ResourceSetup:  return ResourceSetupModalId;
                case StepType.LineClearance:  return "modalDCEntry";
                case StepType.Search:         return "modalSearch4";
                case StepType.Assembly:       return "modalAssemble";
                case StepType.DataCollection: return "modalDCEntry";
                case StepType.SFC_Split:      return "modalSFCSplit";
                default:                      return null;
            }
        }
    }
}
