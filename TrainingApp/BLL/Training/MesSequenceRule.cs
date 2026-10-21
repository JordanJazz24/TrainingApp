using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Parámetros obligatorios de DC sin guardar de una pieza (M-7): el SFC y cada parámetro
    // como "<PARAM> (<DC_GROUP>)", en el orden de la pantalla.
    public class MesPendingDataCollection
    {
        public string Sfc { get; set; }
        public IList<string> Parameters { get; set; } = new List<string>();
    }

    // Lo que el MES mira del estado del POD para rechazar un botón (spec 017, C-2). Lo arma
    // Build con lo que PodService saca del PodContext.
    public class MesSequenceState
    {
        // steps: los de la guía; currentStepOrder: el paso actual de la sesión; attempts: los
        // intentos de la sesión (de ellos sale la última respuesta del LC, que así sobrevive a la
        // reconstrucción del estado, D-10). dcGroups/dcParameters: null si el botón no los
        // necesita (solo Complete, M-7). selectedRowIds: la selección del POD; el M-7 es de las
        // piezas seleccionadas (D-119, corregido por el usuario al probar la E2: sin selección
        // el MES dice "SFC is missing").
        public static MesSequenceState Build(IList<TrainingStep> steps, int currentStepOrder,
            IList<TrainingSessionStepAttempt> attempts, SimulatedOrder order, DataCollectionProgress dcProgress,
            IList<DataCollectionGroup> dcGroups, IList<DataCollectionParameter> dcParameters,
            ICollection<string> selectedRowIds)
        {
            bool Pending(TrainingStep s) => s != null && currentStepOrder <= s.Order;

            var openingLc = OpeningLineClearance(steps);
            // En NonSerialized la fila del lote queda Active desde Resource Setup: el SFC está
            // iniciado recién con el paso de Start resuelto.
            var active    = !Pending(FirstOf(steps, StepType.Search))
                ? order?.ActiveRows() ?? new List<SimulatedOrderRow>()
                : new List<SimulatedOrderRow>();
            var selected  = active.Count > 0 ? order.FindActiveRows(selectedRowIds) : new List<SimulatedOrderRow>();
            var state = new MesSequenceState
            {
                ResourceSetupPending        = Pending(FirstOf(steps, StepType.ResourceSetup)),
                OpeningLineClearancePending = Pending(openingLc),
                // Responder Sí resuelve el paso: mientras siga pendiente, un No registrado es la
                // última respuesta. Los clics fuera de secuencia en el LC no son respuestas.
                LineClearanceAnsweredNo     = openingLc != null && (attempts ?? new List<TrainingSessionStepAttempt>())
                    .Any(a => a.StepId == openingLc.Id && a.Reason == AttemptReason.WrongDecision),
                SfcStarted                  = active.Count > 0,
                SfcSelected                 = selected.Count > 0
            };

            if (dcGroups == null || dcParameters == null || selected.Count == 0 ||
                !Pending(FirstOf(steps, StepType.DataCollection)))
                return state;

            var required = dcGroups.Where(g => g.IsRequired).OrderBy(g => g.DisplayOrder).ToList();
            if (required.Count == 0) return state;

            foreach (var row in selected)
            {
                var saved = dcProgress?.SavedGroupIds(row.RowId) ?? new HashSet<int>();
                state.PendingDataCollection.Add(new MesPendingDataCollection
                {
                    Sfc = row.SFCCode ?? order.SFCCode,
                    Parameters = required
                        .Where(g => !saved.Contains(g.Id))
                        .SelectMany(g => dcParameters
                            .Where(p => p.DataCollectionGroupId == g.Id)
                            .OrderBy(p => p.DisplayOrder)
                            .Select(p => p.Name + " (" + g.Code + ")"))
                        .ToList()
                });
            }
            return state;
        }

        // El LC de apertura: el primero de la guía. El de cierre no lo revisa el MES (C-3).
        public static TrainingStep OpeningLineClearance(IList<TrainingStep> steps)
            => FirstOf(steps, StepType.LineClearance);

        private static TrainingStep FirstOf(IList<TrainingStep> steps, StepType type)
            => steps.Where(s => s.StepType == type).OrderBy(s => s.Order).FirstOrDefault();

        public bool ResourceSetupPending { get; set; }

        // El primer Line Clearance de la guía (el de apertura) sin resolver; el de cierre no
        // cuenta (C-3).
        public bool OpeningLineClearancePending { get; set; }

        // La última respuesta del LC de apertura fue No (C-3).
        public bool LineClearanceAnsweredNo { get; set; }

        // El paso de Start ya se resolvió y quedan filas en Active.
        public bool SfcStarted { get; set; }

        // Alguna fila iniciada está seleccionada en el POD (lo que el MES toma como el SFC).
        public bool SfcSelected { get; set; }

        // El SFC con el que el MES trabaja el botón: iniciado y seleccionado.
        public bool HasSfc => SfcStarted && SfcSelected;

        // Un elemento por pieza seleccionada con DC obligatorio pendiente (D-119); vacío si no
        // hay o el área no tiene DC.
        public IList<MesPendingDataCollection> PendingDataCollection { get; set; } = new List<MesPendingDataCollection>();
    }

    // Mensaje del MES para un botón del POD presionado fuera de turno (spec 017, C-1 a C-3):
    // solo si en el MES esa acción, en ese estado, daría ese error. Lista vacía si el MES la
    // aceptaría (se queda el aviso de hoy). No decide la calificación (D-92).
    public static class MesSequenceRule
    {
        public static IList<MesError> For(StepType clicked, MesSequenceState state)
        {
            var none = new List<MesError>();

            switch (clicked)
            {
                case StepType.Search:
                    if (state.ResourceSetupPending)
                        return One(MesMessageId.ResourceSetupMissing);
                    return state.OpeningLineClearancePending ? LineClearance(state) : none;

                case StepType.Assembly:
                case StepType.DataCollection:
                    if (state.OpeningLineClearancePending)
                        return LineClearance(state);
                    return state.HasSfc ? none : One(MesMessageId.SfcMissing);

                case StepType.SFC_Split:
                    return state.HasSfc ? none : One(MesMessageId.SfcMissing);

                case StepType.Complete:
                    if (!state.HasSfc)
                        return One(MesMessageId.SfcMissingStart);
                    // Un M-7 por pieza (D-119), con todos sus parámetros pendientes (D-113).
                    return state.PendingDataCollection
                        .Where(p => p.Parameters.Count > 0)
                        .Select(p => MesMessage.Error(MesMessageId.MissingDataCollection,
                            ("list", string.Join(" , ", p.Parameters)), ("sfc", p.Sfc)))
                        .ToList();

                default:
                    // Log NC (los tres métodos, spec 014) como SFC Split; el resto no tiene
                    // chequeo en el MES (Resource Setup, Line Clearance, Logout, sin lógica).
                    if (StepTypes.IsLogNC(clicked))
                        return state.HasSfc ? none : One(MesMessageId.SfcMissing);
                    return none;
            }
        }

        private static IList<MesError> LineClearance(MesSequenceState state)
            => One(state.LineClearanceAnsweredNo ? MesMessageId.LineClearanceStatusNo : MesMessageId.LineClearanceNotDone);

        private static IList<MesError> One(MesMessageId id)
            => new List<MesError> { MesMessage.Error(id) };
    }
}
