using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Nombre de un paso para el operador (spec 012, D-85), con su variante cuando el tipo se
    // repite en la guía, como en docs/domain.md → "Ponderación de pasos". Lo usan Ir a… y el
    // resumen de Práctica libre.
    public static class StepNames
    {
        public static string Display(IList<TrainingStep> steps, TrainingStep step)
        {
            switch (step.StepType)
            {
                case StepType.MesLogin:            return "Ingreso a MES";
                case StepType.ResourceSetup:       return "Resource Setup";
                case StepType.LineClearance:
                    return steps.Any(s => s.StepType == StepType.LineClearance && s.Order < step.Order)
                        ? "Line Clearance (cierre)"
                        : "Line Clearance (apertura)";
                case StepType.Search:              return "Search / Start";
                case StepType.Assembly:            return "Assembly";
                case StepType.DataCollection:      return "Data Collection";
                case StepType.SFC_Split:           return "SFC Split";
                case StepType.LogNC:               return "Log NC · desecho rápido";
                case StepType.LogNCFastRepair:     return "Log NC · reparación rápida";
                case StepType.LogNCExtendedRepair: return "Log NC · reparación extendida";
                case StepType.Complete:            return "Complete";
                case StepType.Logout:              return "Logout";
                default: throw new ArgumentOutOfRangeException(nameof(step), step.StepType, "Paso sin nombre.");
            }
        }
    }
}
