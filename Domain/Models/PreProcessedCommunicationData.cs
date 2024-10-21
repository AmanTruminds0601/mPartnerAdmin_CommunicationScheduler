namespace mPartnerAdmin_CommunicationScheduler.Domain.Models
{
    public class PreProcessedCommunicationData
    {
        public int ComID { get; set; }
        public string ChannelType { get; set; } // string (50)
        public string Subject { get; set; } // string (500)
        public string CC { get; set; } // string (100)
        public string BCC { get; set; } // string (100)
        public string Content { get; set; } // string (Max)
        public DateTime StartDate { get; set; } // DateTime
        public DateTime EndDate { get; set; } // DateTime
        public string Frequency { get; set; } // string (100)
        public int TemplateId { get; set; } // int
        public string PhoneType { get; set; } // string (20)
        public string Language { get; set; } // string (200)
        public string SourcePage { get; set; } // string (30)
        public bool? IsClickable { get; set; } // bool
        public string? Redirection { get; set; } // bool
        public string? Module { get; set; } // string (200)
        public string? Weblink { get; set; } // string (200)
        public string? ButtonName { get; set; } // bool
        public string? UserType { get; set; } // int
        public int? sequence { get; set; } // int
        public bool? IsActive { get; set; } // Nullable DateTime
        public DateTime? CreatedOn { get; set; } // Nullable int
        public string? CreatedBy { get; set; } // Nullable int
        public DateTime? UpdatedOn { get; set; } // Nullable int
        public string? UpdatedBy { get; set; } // Nullable int
        public string? UserID { get; set; } // Nullable int
        public string? UserMobileNo { get; set; } // Nullable DateTime
        public string? UserEmail { get; set; } // Nullable int
        public int SchedulerID { get; set; } // Nullable int
    }
}
