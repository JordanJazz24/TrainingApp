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
        private Product                        _product;
        private bool                           _productLoaded;

        internal PodContext(int userId, TrainingSession session, TrainingGuide guide, Area area,
            IList<TrainingSessionStepAttempt> attempts, PodState state, decimal score,
            TrainingGuideService catalog)
        {
            UserId = userId;
            Session    = session;
            Guide      = guide;
            Area       = area;
            Attempts   = attempts;
            State      = state;
            Score      = score;
            _catalog   = catalog;
        }

        public int UserId { get; }
        public TrainingSession Session { get; }
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
