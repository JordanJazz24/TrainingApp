using System;

namespace TrainingApp.Domain
{
    // Una sesión completada del operador, para la lista "Últimas sesiones" del Inicio (spec 025,
    // P-4): lo que trae spTrainingSessionGetRecentCompleted, sin reglas. El texto "Producto · Área"
    // y cuándo mostrarlo lo decide la BLL (SessionResultService).
    public class CompletedSessionItem
    {
        public int SessionId { get; set; }
        public int TrainingGuideId { get; set; }
        public SessionMode Mode { get; set; }

        // Fecha de cierre, en UTC como en la BD.
        public DateTime CompletedAt { get; set; }

        // El área de la guía y su producto (también de una guía que ya no está publicada).
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public string ProductName { get; set; }
    }
}
