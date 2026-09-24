namespace TrainingApp.Pages.Training.Models
{
    // Answers fijos que la UI envía a la BLL (spec 007).
    public static class StepAnswers
    {
        public const string Complete = "complete";

        // Acción incorrecta del operador (botón equivocado, sin fila válida). Nunca
        // coincide con un código NC del catálogo ni con "complete": StepValidator lo
        // rechaza y la BLL registra el intento fallido.
        public const string Invalid = "__invalid-action__";
    }
}
