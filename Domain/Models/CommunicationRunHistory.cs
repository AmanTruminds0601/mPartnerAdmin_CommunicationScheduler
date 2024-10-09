namespace mPartnerAdmin_CommunicationScheduler.Domain.Models
{
    public class CommunicationRunHistory
    {
        public int SchHistoryID { get; set; }
        public int ComID { get; set; }
        public string ChannelType { get; set; }
        public int SchedulerID { get; set; }
        public string FrequencyType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string UpdatedBy { get; set; }
    }
}
