using System;
using System.Collections.Generic;

namespace TrainingApp.Domain
{
    public class TrainingGuide
    {
        public int Id { get; set; }
        public int AreaId { get; set; }
        public int? OperationId { get; set; }  // FK preparado para el futuro; hoy siempre null
        public string Title { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TrainingStep> Steps { get; set; }
    }
}
