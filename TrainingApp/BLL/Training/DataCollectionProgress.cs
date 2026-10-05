using System;
using System.Collections.Generic;

namespace TrainingApp.BLL.Training
{
    // Pieza que recorre Data Collection: una fila del POD (en NonSerialized, el lote).
    [Serializable]
    public class DataCollectionPiece
    {
        public string RowId   { get; set; }
        public string SFCCode { get; set; }
    }

    // Avance del paso Data Collection de una sesión (spec 008): las piezas se procesan una
    // por una en el orden del POD y cada una guarda sus DC Groups. Vive en el PodState
    // (Session), sobrevive a Return to POD y solo lo modifica PodService.
    [Serializable]
    public class DataCollectionProgress
    {
        public List<DataCollectionPiece> Pieces { get; } = new List<DataCollectionPiece>();
        public int CurrentIndex { get; set; }

        // Grupos guardados correctamente por pieza (RowId → ids de DataCollectionGroup).
        // Guardar dos veces el mismo grupo no cuenta doble (E3).
        private readonly Dictionary<string, HashSet<int>> _savedGroups = new Dictionary<string, HashSet<int>>();

        public DataCollectionPiece CurrentPiece
            => CurrentIndex < Pieces.Count ? Pieces[CurrentIndex] : null;

        public bool IsFinished => CurrentIndex >= Pieces.Count;

        public ICollection<int> SavedGroupIds(string rowId)
        {
            HashSet<int> saved;
            return _savedGroups.TryGetValue(rowId, out saved) ? saved : (ICollection<int>)new HashSet<int>();
        }

        // Grupos ya guardados por la pieza actual: su Collect queda deshabilitado hasta pasar
        // a la pieza siguiente, como Assemble en la Component List (Fase 6.5, entrega 1).
        public ICollection<int> CurrentPieceSavedGroupIds
            => CurrentPiece != null ? SavedGroupIds(CurrentPiece.RowId) : new HashSet<int>();

        public void MarkSaved(string rowId, int groupId)
        {
            HashSet<int> saved;
            if (!_savedGroups.TryGetValue(rowId, out saved))
            {
                saved = new HashSet<int>();
                _savedGroups[rowId] = saved;
            }
            saved.Add(groupId);
        }
    }

    public enum DataCollectionOutcome
    {
        Ignored,        // el Save no aplica (paso distinto, grupo desconocido, sin avance)
        Incorrect,      // valores incorrectos: intento fallido, el avance se conserva
        GroupSaved,     // grupo correcto; la pieza todavía no está completa
        PieceCompleted, // la pieza quedó completa; se pasa a la siguiente
        StepResolved    // última pieza completa, o avance forzado / sesión completada
    }

    public class DataCollectionSubmitResult
    {
        public DataCollectionOutcome Outcome { get; set; }

        // Intento registrado (Incorrect y StepResolved); null en el resto.
        public StepSubmitResult StepResult { get; set; }

        // SFC de la pieza para la que se guardó el grupo (banner "SFC … completed").
        public string PieceSfcCode { get; set; }
    }
}
