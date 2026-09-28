using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    public enum PieceSelectionResult
    {
        Valid,
        NothingSelected,   // ninguna fila seleccionada
        MissingPieces,     // faltan piezas: aviso de formato, sin intento
        NoPieceLeft        // Log NC con todas las filas: debe quedar una para Complete
    }

    // Qué filas exige cada paso en un área Serialized (spec 008, punto 6). En el MES real
    // trabajar un subconjunto no es un error, pero el motor avanza un paso a la vez para toda
    // la sesión: el simulador entrena el flujo común (todas las piezas juntas) y la selección
    // parcial es un aviso de formato, sin penalización.
    public static class PieceSelectionRule
    {
        // availableCount: piezas que el paso puede trabajar (en Start, las de la orden; en el
        // resto, las filas del POD).
        public static PieceSelectionResult Evaluate(StepType stepType, int selectedCount, int availableCount)
        {
            if (selectedCount <= 0)
                return PieceSelectionResult.NothingSelected;

            if (stepType == StepType.LogNC)
                return selectedCount >= availableCount
                    ? PieceSelectionResult.NoPieceLeft
                    : PieceSelectionResult.Valid;

            return selectedCount >= availableCount
                ? PieceSelectionResult.Valid
                : PieceSelectionResult.MissingPieces;
        }

        // Log NC y Complete sin ninguna fila siguen siendo intento fallido (spec 007); en el
        // resto de los casos una selección inválida es solo un aviso de formato.
        public static bool CountsAsFailedAttempt(StepType stepType, PieceSelectionResult result)
        {
            return result == PieceSelectionResult.NothingSelected
                && (stepType == StepType.LogNC || stepType == StepType.Complete);
        }
    }
}
