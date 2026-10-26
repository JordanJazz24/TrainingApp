namespace TrainingApp.Pages.Admin
{
    // Fila de la lista de Gestión de usuarios (spec 013, AC-7), con los textos ya armados para
    // la tabla. Solo presentación.
    public class UserListRow
    {
        public int Id { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }
        public string RoleText { get; set; }
        public string RoleCss { get; set; }
        public string RoleIcon { get; set; }
        public string StateText { get; set; }
        public string StateCss { get; set; }
        public string StateIcon { get; set; }
        public string CreatedText { get; set; }
        public string LastLoginText { get; set; }

        public bool IsLocked { get; set; }

        // El admin autenticado: no puede borrarse a sí mismo (AC-13b).
        public bool IsSelf { get; set; }

        // Tiene sesiones de entrenamiento: borrarlo las borra (la confirmación es una alerta).
        public bool HasHistory { get; set; }

        public string ConfirmReset { get; set; }
        public string ConfirmUnlock { get; set; }
        public string ConfirmDelete { get; set; }
    }
}
