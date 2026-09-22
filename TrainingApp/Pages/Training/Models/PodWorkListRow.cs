namespace TrainingApp.Pages.Training.Models
{
    public class PodWorkListRow
    {
        // Distingue filas hermanas con el mismo SFCCode tras un SFC Split (spec 005).
        public string RowId               { get; set; }
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
