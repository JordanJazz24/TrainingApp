using System;

namespace TrainingApp.Domain
{
    // Una sesión de una guía publicada, para el dashboard (spec 018): lo que el SP agrega, sin
    // reglas (D-139). Qué cuenta como aprobado lo decide ReadinessPolicy en la BLL.
    public class DashboardSession
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int TrainingGuideId { get; set; }
        public SessionMode Mode { get; set; }
        public SessionStatus Status { get; set; }
        public decimal FinalScore { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Intentos con IsCorrect = 0, sin contar los saltos de Práctica libre. Null si no se
        // pidieron (el SP solo los cuenta a pedido: recorrer todos los intentos es caro).
        public int? FailedAttempts { get; set; }
    }
}
