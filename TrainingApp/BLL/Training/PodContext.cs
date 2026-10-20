using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Todo lo que un click del POD necesita, cargado una vez por request por
    // PodService.LoadContext (spec 009, AUD-25). Cuando PodService registra un intento,
    // actualiza el contexto (paso actual, estado, puntaje), así que la página no recarga la
    // sesión.
    public class PodContext
    {
        private readonly TrainingGuideService _catalog;

        // Catálogos del área: se leen al primer uso y una sola vez por request (auditoría
        // AUD-39). La página y PodService los comparten en lugar de volver a consultarlos.
        private IList<DataCollectionGroup>     _dcGroups;
        private IList<DataCollectionParameter> _dcParameters;
        private IList<NCCode>                  _ncCodes;
        private IList<AssemblyComponent>       _assemblyComponents;
        private IList<Operation>               _operations;
        private Product                        _product;
        private bool                           _productLoaded;
        private readonly Func<string>          _code1Loader;
        private string                         _code1;

        internal PodContext(int userId, TrainingSession session, TrainingGuide guide, Area area,
            IList<TrainingSessionStepAttempt> attempts, PodState state, decimal score,
            TrainingGuideService catalog, Func<string> code1Loader)
        {
            UserId = userId;
            Session    = session;
            Guide      = guide;
            Area       = area;
            Attempts   = attempts;
            State      = state;
            Score      = score;
            _catalog   = catalog;
            _code1Loader = code1Loader;
        }

        public int UserId { get; }

        // Code1 del usuario autenticado (spec 016, D-79): el Ingreso a MES lo compara con lo que
        // escribe el operador. Sale de la BD por el UserId, nunca del cliente; se lee al primer uso.
        public string Code1 => _code1 ?? (_code1 = _code1Loader() ?? string.Empty);

        // Avance del Ingreso a MES (spec 016); vacío antes de la primera acción.
        public MesLoginProgress MesLogin => State.MesLogin ?? new MesLoginProgress();

        // Limpieza al salir en curso (spec 016, P-6): el paso actual es Logout y el operador ya
        // hizo clic en el ícono. Mientras dura, la página muestra Edge o el escritorio.
        public bool IsLogoutCleanup
            => Session.Status == SessionStatus.InProgress && State.Logout != null &&
               CurrentStepOrDefault?.StepType == StepType.Logout;

        // Recurso del área: el del POD y el de la etiqueta de la estación en Recursos MES (D-70).
        public string ResourceName => PodDefaults.ResourceName(Area);
        public TrainingSession Session { get; }
        public ModePolicy Policy => ModePolicy.For(Session.Mode);
        public TrainingGuide Guide { get; }
        public Area Area { get; }
        public PodState State { get; }

        // Puntaje actual de la sesión (0..1), con los intentos registrados hasta ahora.
        public decimal Score { get; internal set; }

        // El estado del POD se perdió (Session reciclada) y la BLL lo reconstruyó en este
        // request con la orden por defecto (spec 009, D-10).
        public bool Rebuilt { get; internal set; }

        internal IList<TrainingSessionStepAttempt> Attempts { get; }

        public TrainingStep CurrentStep
            => Guide.Steps.First(s => s.Order == Session.CurrentStepOrder);

        // Paso actual, o null si la sesión ya no tiene uno (se completó con el último paso).
        public TrainingStep CurrentStepOrDefault
            => Guide.Steps.FirstOrDefault(s => s.Order == Session.CurrentStepOrder);

        // El paso de ese tipo ya se resolvió (correcto o forzado). false si la guía no lo tiene.
        public bool IsResolved(StepType stepType)
        {
            var step = Guide.Steps.FirstOrDefault(s => s.StepType == stepType);
            return step != null && Session.CurrentStepOrder > step.Order;
        }

        // Hallazgo del paso de Log NC (spec 014); null si el paso no tiene o todavía no hay Start.
        public NcFinding NcFindingFor(TrainingStep step)
            => step == null ? null : State.NcFindings?.FirstOrDefault(f => f.StepOrder == step.Order);

        // Hallazgo que muestran la nota y la guía en el paso actual: el del paso de Log NC o, en
        // NonSerialized, durante el SFC Split, el del Log NC que le sigue (enmienda 2 de la spec
        // 014: el hallazgo dicta la cantidad del split).
        public NcFinding CurrentNcFinding
        {
            get
            {
                var step = CurrentStepOrDefault;
                if (step == null || Session.Status != SessionStatus.InProgress) return null;
                if (StepTypes.IsLogNC(step.StepType)) return NcFindingFor(step);
                return step.StepType == StepType.SFC_Split ? SplitFinding : null;
            }
        }

        // NonSerialized: el hallazgo del primer paso de Log NC después del SFC Split; su
        // cantidad es la que se divide (enmienda 2 de la spec 014). null si no hay.
        public NcFinding SplitFinding
        {
            get
            {
                if (Area.SerializationType != SerializationType.NonSerialized) return null;
                var split = Guide.Steps.FirstOrDefault(s => s.StepType == StepType.SFC_Split);
                if (split == null) return null;
                var logNC = Guide.Steps.Where(s => s.Order > split.Order && StepTypes.IsLogNC(s.StepType))
                    .OrderBy(s => s.Order).FirstOrDefault();
                return NcFindingFor(logNC);
            }
        }

        // Resultado de una evaluación terminada: puntaje final contra el mínimo de aprobación.
        public bool IsPassed => ScoreCalculator.IsPassed(Session.FinalScore);

        public IList<DataCollectionGroup> DataCollectionGroups
            => _dcGroups ?? (_dcGroups = _catalog.GetDataCollectionGroups(Area.Id));

        public IList<DataCollectionParameter> DataCollectionParameters
            => _dcParameters ?? (_dcParameters = _catalog.GetDataCollectionParameters(Area.Id));

        public IList<NCCode> NCCodes
            => _ncCodes ?? (_ncCodes = _catalog.GetNCCodes(Area.Id));

        public IList<AssemblyComponent> AssemblyComponents
            => _assemblyComponents ?? (_assemblyComponents = _catalog.GetAssemblyComponents(Area.Id));

        // Operaciones simuladas del área (spec 014, D-58), de la actual a las anteriores.
        public IList<Operation> Operations
            => _operations ?? (_operations = _catalog.GetOperations(Area.Id));

        // La operación actual (mayor Sequence) y la anterior (P-14); null si el área no las tiene.
        public Operation CurrentOperation  => Operations.FirstOrDefault();
        public Operation PreviousOperation => Operations.Skip(1).FirstOrDefault();

        // Nombres que muestran Remove component y el banner de la extendida. Sin operaciones
        // en el catálogo del área, la simulada de PodDefaults.
        public string CurrentOperationName  => CurrentOperation?.Name ?? PodDefaults.SimOperation(Area);
        public string PreviousOperationName => PreviousOperation?.Name ?? CurrentOperationName;

        // Product del área (texto del material y panel final); null si no existe.
        public Product Product
        {
            get
            {
                if (!_productLoaded)
                {
                    _product       = _catalog.GetAllProducts().FirstOrDefault(p => p.Id == Area.ProductId);
                    _productLoaded = true;
                }
                return _product;
            }
        }
    }
}
