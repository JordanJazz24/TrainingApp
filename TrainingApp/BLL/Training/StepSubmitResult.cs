namespace TrainingApp.BLL.Training
{
    public enum StepSubmitOutcome
    {
        Correct,         // answer accepted; advance to next step
        Incorrect,       // wrong answer; retry allowed (Guided, or Assessment < 4 attempts)
        FailedForced,    // Assessment mode: 4th+ attempt, step failed, forced advance
        SessionCompleted // last step done; session score finalized
    }

    public class StepSubmitResult
    {
        public StepSubmitOutcome Outcome { get; set; }
        public int AttemptNumber { get; set; }
        public int NextStepOrder { get; set; }
        public decimal CurrentScore { get; set; }
        public decimal SessionScore { get; set; }

        // Intentos que todavía puntúan antes del avance forzado (Assessment): 0 si este era
        // el último. La página lo muestra sin revelar la respuesta.
        public int RemainingScoredAttempts
            => System.Math.Max(0, Scoring.ScoreCalculator.MaxScoredAttempts - AttemptNumber);
    }
}
