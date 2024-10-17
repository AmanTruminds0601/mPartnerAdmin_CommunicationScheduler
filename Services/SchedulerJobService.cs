using Microsoft.EntityFrameworkCore;
using mPartnerAdmin_CommunicationScheduler.Domain.Models;
using Quartz;
using System.Net.Mail;
using System.Net;

namespace mPartnerAdmin_CommunicationScheduler.Services
{
    public class SchedulerJobService : IJob
    {
        private readonly SchedulerContext _dbContext;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        public SchedulerJobService(ILogger<SchedulerJobService> logger, SchedulerContext dbContext, IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Job executed at: {DateTime.Now}");

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
                case "Notification": // Notification
                    SendNotification(scheduler);
                    break;
                case "Banner": // Banner
                    SendBanner(scheduler);
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
        private void SendNotification(Communication_Custom_Scheduler scheduler)
        {
            // Log the run to Communication_Run_History
            //LogRunHistory(scheduler);
            // Code to send Notification
            Console.WriteLine("Notification Job Triggered");
        }
        private void SendBanner(Communication_Custom_Scheduler scheduler)
        {
            // Log the run to Communication_Run_History
            //LogRunHistory(scheduler);
            // Code to send Banner
            Console.WriteLine("Banner Job Triggered");
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
        private void SendEmail(Communication_Custom_Scheduler scheduler)
        {
            //LogRunHistory(scheduler);
            // Code to send email
            Console.WriteLine("Email Job Triggered");
            SendMail("aman.kumar@truminds.com", "naveena.kunjibettu@truminds.com", "", "Test Scheduler Email", "Testing email through quartz scheduler", "Aman", "");
        }
        private void GenerateMailAddress(MailAddressCollection mailID, string? tomailids)
        {
            if (!string.IsNullOrEmpty(tomailids) && tomailids != "not")
            {
                var splitIDs = tomailids.Split(";");
                foreach (var splitID in splitIDs)
                {
                    mailID.Add(splitID);
                }
            }
        }
        public void SendMail(string? tomailid, string? ccmailid, string? bccmailid, string? subject, string? mailbody, string? dispayname, string? attachments)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 |
                                        (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                MailMessage mMailMessage = new MailMessage();
                mMailMessage.From = new MailAddress(_configuration["mailAdd"].ToString(), dispayname);
                GenerateMailAddress(mMailMessage.To, tomailid);
                GenerateMailAddress(mMailMessage.CC, ccmailid);
                GenerateMailAddress(mMailMessage.Bcc, bccmailid);

                mMailMessage.Subject = subject;
                if (attachments.Length > 0)
                {
                    attachments = attachments.Replace("/", "\\").Replace("\\", "\\\\");
                    var attachmentSplit = attachments.Split(new char[] { ',' });
                    foreach (var attachment in attachmentSplit)
                    {
                        if (!string.IsNullOrEmpty(attachment.Trim()))
                            mMailMessage.Attachments.Add(new Attachment(attachment));
                    }
                }

                mMailMessage.Body = mailbody;
                mMailMessage.IsBodyHtml = true;
                mMailMessage.Priority = MailPriority.High;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.outlook.com";

                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential(_configuration["mailAdd"].ToString(), _configuration["mailPass"].ToString());
                smtp.Send(mMailMessage);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
