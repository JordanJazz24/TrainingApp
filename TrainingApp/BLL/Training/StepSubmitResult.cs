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
        public int NextStepOrder { get; set; }
        public decimal SessionScore { get; set; }
    }
}
