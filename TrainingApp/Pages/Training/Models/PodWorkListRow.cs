namespace TrainingApp.Pages.Training.Models
{
    public class PodWorkListRow
    {
        public string SFCCode             { get; set; }
        public string MaterialDescription { get; set; }
        public int    Qty                 { get; set; }
        public string Status              { get; set; }
        public string ProcessLot          { get; set; }
        public string ShopOrder           { get; set; }
        public bool   IsSelected          { get; set; }

        // Browse Work List extra columns
        public string ScheduleStart       { get; set; }
        public string PlannedCompletion   { get; set; }
    }
}
