using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Familias de tipos de paso (spec 014, P-7): los pasos de Log NC se abren con el mismo
    // botón [Log NC] y el mismo control; cada uno acepta solo códigos de su método.
    public static class StepTypes
    {
        public static bool IsLogNC(StepType stepType) => MethodOf(stepType).HasValue;

        // Método de NC de un paso de Log NC; null si el paso no es de Log NC.
        public static NCMethod? MethodOf(StepType stepType)
        {
            switch (stepType)
            {
                case StepType.LogNC:           return NCMethod.FastScrap;
                case StepType.LogNCFastRepair: return NCMethod.FastRepair;
                case StepType.LogNCExtendedRepair: return NCMethod.ExtendedRepair;
                default:                       return null;
            }
        }
    }
}
