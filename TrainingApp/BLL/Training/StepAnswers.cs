namespace TrainingApp.BLL.Training
{
    // Answers fijos que la UI envía a la BLL (spec 007).
    public static class StepAnswers
    {
        public const string Complete = "complete";

        // Clic en el ícono Logout del POD en el último paso (spec 015).
        public const string Logout = "logout";

        // Start directo (área sin checklist, spec 008): el paso se califica solo por secuencia.
        public const string Start = "start";

        // Botón equivocado dentro de un control (ej. Add o Done en NC Data Entry). Nunca
        // coincide con un código NC del catálogo ni con "complete": StepValidator lo
        // rechaza y la BLL registra el intento fallido. Las acciones sin fila válida (Log NC,
        // Complete) las decide PodService sin pasar por el validador.
        public const string Invalid = "__invalid-action__";
    }
}
