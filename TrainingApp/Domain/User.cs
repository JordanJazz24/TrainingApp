using System;

namespace TrainingApp.Domain
{
    // Cuenta local de un Employee de la API (primer acceso completado o en curso). Antes se
    // llamaba Operator; "operador" queda como término del MES (D-17).
    public class User
    {
        public int Id { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public bool IsFirstLogin { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
