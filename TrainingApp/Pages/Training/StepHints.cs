using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training
{
    public static class StepHints
    {
        // Pista del paso: la de Guided tras un intento fallido y la de Ver pista en Práctica libre
        // (spec 012, D-87). En español, con los nombres del MES en inglés (spec 010, R7). Log NC
        // solo nombra el Component en la reparación rápida.
        public static string For(StepType stepType)
        {
            // Spec 014: la pieza y el código son los del hallazgo.
            if (stepType == StepType.LogNCFastRepair)
                return "Seleccione la pieza del hallazgo, presione Log NC en el código del hallazgo, elija en Component el componente del hallazgo, presione Add-Done y reemplácelo (Yes, Scrap, Yes y Add).";
            if (stepType == StepType.LogNCExtendedRepair)
                return "Seleccione la pieza del hallazgo, presione Log NC en el código del hallazgo, presione Add-Done y presione Select en la disposición del hallazgo.";
            if (StepTypes.IsLogNC(stepType))
                return "Seleccione la pieza del hallazgo, presione Log NC en el código del hallazgo y presione Add-Done.";

            switch (stepType)
            {
                case StepType.ResourceSetup:  return "Presione Retrieve, quite los equipos anteriores (REMOVE ALL), agregue sus filas, verifique que Setup State esté en 'Set Up' y presione Save.";
                case StepType.LineClearance:  return "Responda 'Si' para confirmar la limpieza de línea.";
                case StepType.Search:         return "Responda 'Sí' a las verificaciones y presione Save.";
                case StepType.DataCollection: return "Complete todos los parámetros dentro de su rango y presione Save (no Save Draft).";
                case StepType.SFC_Split:      return "Seleccione una fila del POD, divida la cantidad del hallazgo y presione Close.";
                case StepType.Assembly:       return "Ensamble cada componente de la Component List: escanee su código y presione Add.";
                case StepType.Complete:       return "Presione Complete para cerrar la orden.";
                case StepType.Logout:         return "Presione el ícono Logout para cerrar su sesión en MES.";
                case StepType.MesLogin:       return "Ejecute el script de limpieza con su Code1, abra el Repositorio, inicie Hora a Hora 3.0 y Recursos MES con su Code1 y entre a MES con Log On.";
                default:                      return string.Empty;
            }
        }
    }
}
