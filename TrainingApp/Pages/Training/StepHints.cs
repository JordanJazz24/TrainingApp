using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training
{
    // Pista del paso: la de Guided tras un intento fallido y la de Ver pista en Práctica libre
    // (spec 012, D-87). En español, con los nombres del MES en inglés (spec 010, R7). Log NC
    // solo nombra el Component en la reparación rápida. El Ingreso a MES y Logout la eligen por
    // tramo, con el avance que ya guarda PodState, el mismo de los ResumeName de la guía
    // (enmienda 2, textos aprobados por el usuario).
    public static class StepHints
    {
        public static string For(PodContext ctx, StepType stepType)
            => For(stepType, ctx.MesLogin, ctx.State.Logout);

        public static string For(StepType stepType, MesLoginProgress mesLogin, LogoutProgress logout)
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
                case StepType.Logout:         return LogoutHint(logout);
                case StepType.MesLogin:       return MesLoginHint(mesLogin ?? new MesLoginProgress());
                default:                      return string.Empty;
            }
        }

        private static string MesLoginHint(MesLoginProgress progress)
        {
            if (!progress.ScriptDone)
                return "Haga doble clic en MES Limpiar historial y cache, escriba su Code1, presione Enter y luego cualquier tecla para cerrar la ventana.";
            if (!progress.EdgeOpen)
                return "Cierre la consola con cualquier tecla y abra el Repositorio con doble clic.";
            if (!progress.RecursosDone)
                return "En el Repositorio seleccione Recursos MES, escanee la etiqueta de su estación, escriba su Code1 y presione Buscar.";
            return "En el login de MES ingrese su usuario (Code1) y su contraseña y presione Log On.";
        }

        private static string LogoutHint(LogoutProgress cleanup)
        {
            if (cleanup == null)
                return "Presione el ícono Logout para cerrar su sesión en MES.";
            if (!cleanup.EdgeClosed)
                return "Cierre el navegador con la ✕.";
            return "Haga doble clic en MES Limpiar historial y cache, escriba su Code1, presione Enter y luego cualquier tecla para terminar.";
        }
    }
}
