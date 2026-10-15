namespace TrainingApp.Domain
{
    // Usuario en la lista de Gestión de usuarios (spec 013), con cuántas sesiones de
    // entrenamiento tiene: borrarlo borra ese historial (enmienda 2026-10-13).
    public class UserSummary : User
    {
        public int SessionCount { get; set; }
    }
}
