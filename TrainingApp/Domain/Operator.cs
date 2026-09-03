using System;

namespace TrainingApp.Domain
{
    public class Operator
    {
        public int Id { get; set; }
        public string Code1 { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public bool IsFirstLogin { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
