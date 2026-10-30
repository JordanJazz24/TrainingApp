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

        // Área (spec 021, P-9): "Producto · Área", "Sin área" o "—" para un administrador.
        public string AreaText { get; set; }

        // Solo un Operator tiene área: Cambiar área no sale en la fila de un admin (AC-10).
        // AreaId: la actual, preseleccionada al cambiarla.
        public bool IsOperator { get; set; }
        public int? AreaId { get; set; }
        public string Who { get; set; }

        public bool IsLocked { get; set; }

        // El admin autenticado: no puede borrarse a sí mismo (AC-13b).
        public bool IsSelf { get; set; }

        // Tiene sesiones de entrenamiento: borrarlo las borra (la confirmación es una alerta).
        public bool HasHistory { get; set; }

        public string ConfirmReset { get; set; }
        public string ConfirmUnlock { get; set; }
        public string ConfirmDelete { get; set; }

        // Consola de cuentas (spec 022, brief 8): textos del directorio y de la ficha.
        public string Initials { get; set; }
        public bool IsAdmin { get; set; }

        // Directorio: la cuenta con problema (bloqueada o con la contraseña por cambiar) lleva su
        // insignia corta; las demás, "Activa". La clasificación es StateOf de la BLL.
        public bool IsProblem { get; set; }
        public string StateShort { get; set; }

        // Sin área: un Operator sin área elegida.
        public bool NoArea { get; set; }
        public string AreaShort { get; set; }
        public string AreaNote { get; set; }

        // Ficha: Acceso, Cuenta y Borrar.
        public string HintIcon { get; set; }
        public string LongHint { get; set; }
        public string LastAgoText { get; set; }
        public string SessionsText { get; set; }
        public string DeleteNote { get; set; }

        // La cuenta abierta en la ficha (HfSelected, o la primera con problema).
        public bool IsSelected { get; set; }
    }
}
