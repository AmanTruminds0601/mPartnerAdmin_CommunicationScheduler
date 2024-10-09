﻿namespace mPartnerAdmin_CommunicationScheduler.Domain.Models
{
    public class Communication_Custom_Scheduler
    {
        public int SchedulerID { get; set; }
        public int ComID { get; set; }
        public string ChannelType { get; set; }
        public DateTime StartTimestamp { get; set; } // Change from string to DateTime
        public DateTime? EndTimestamp { get; set; } // Change from string? to DateTime?
        public string? FrequencyType { get; set; }
        public string? Frequency { get; set; }
        public string MonthDays { get; set; }
        public int? FrequencyValue { get; set; }
        public int RepeatValue { get; set; } // Change from int? to int
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
