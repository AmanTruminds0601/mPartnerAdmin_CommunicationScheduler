using Microsoft.EntityFrameworkCore;
using mPartnerAdmin_CommunicationScheduler.Domain.Models;
using Quartz;

namespace mPartnerAdmin_CommunicationScheduler.Services
{
    public class SchedulerJobService : IJob
    {
        private readonly SchedulerContext _dbContext;
        private readonly ILogger _logger;
        public SchedulerJobService(ILogger<SchedulerJobService> logger, SchedulerContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Job executed at: {DateTime.Now}");
            //// Fetch active schedulers from the database
            //var activeSchedulers = await _dbContext.CommunicationCustomSchedulers
            //    .Where(s => s.IsActive && DateTime.Parse(s.StartTimestamp) <= DateTime.Now &&
            //                (s.EndTimestamp == null || DateTime.Parse(s.EndTimestamp) >= DateTime.Now))
            //    .ToListAsync();

            //foreach (var scheduler in activeSchedulers)
            //{
            //    // Schedule based on FrequencyType (e.g., Daily, Weekly, etc.)
            //    TriggerChannel(scheduler);
            //}

            // Retrieve the scheduler from JobDataMap
            var schedulerData = context.MergedJobDataMap.GetString("SchedulerData");
            if (string.IsNullOrEmpty(schedulerData))
            {
                Console.WriteLine("No scheduler data found.");
                return;
            }

            var scheduler = Newtonsoft.Json.JsonConvert.DeserializeObject<Communication_Custom_Scheduler>(schedulerData);

            // Trigger based on ChannelType
            TriggerChannel(scheduler);
        }

        private void TriggerChannel(Communication_Custom_Scheduler scheduler)
        {
            // Handle different channel types and send communication
            switch (scheduler.ChannelType)
            {
                case "SMS": // SMS
                    SendSMS(scheduler);
                    break;
                case "WhatsApp": // WhatsApp
                    SendWhatsAppMessage(scheduler);
                    break;
                case "Email": // Email
                    SendEmail(scheduler);
                    break;
            }
        }

        private void SendSMS(Communication_Custom_Scheduler scheduler)
        {
            // Log the run to Communication_Run_History
            //LogRunHistory(scheduler);
            // Code to send SMS
            Console.WriteLine("SMS Job Triggered");
        }

        private void SendWhatsAppMessage(Communication_Custom_Scheduler scheduler)
        {
            //LogRunHistory(scheduler);
            // Code to send WhatsApp message
            Console.WriteLine("WhatsApp Job Triggered");
        }

        private void SendEmail(Communication_Custom_Scheduler scheduler)
        {
            //LogRunHistory(scheduler);
            // Code to send email
            Console.WriteLine("Email Job Triggered");
        }

        private void LogRunHistory(Communication_Custom_Scheduler scheduler)
        {
            // Insert a new entry in the Communication_Run_History table
            var history = new CommunicationRunHistory
            {
                ComID = scheduler.ComID,
                ChannelType = scheduler.ChannelType,
                SchedulerID = scheduler.SchedulerID,
                FrequencyType = scheduler.FrequencyType,
                IsActive = scheduler.IsActive,
                CreatedOn = DateTime.Now,
                CreatedBy = "SchedulerJobService",
            };

            _dbContext.CommunicationRunHistory.Add(history);
            _dbContext.SaveChanges();
        }
    }
}
