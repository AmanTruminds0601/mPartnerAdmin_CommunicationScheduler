using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using mPartnerAdmin_CommunicationScheduler.Domain.Models;
using mPartnerAdmin_CommunicationScheduler.Services;
using mPartnerAdmin_CommunicationScheduler.Utilities;

public class SchedulerPollingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SchedulerPollingService> _logger;
    private readonly int _pollingIntervalInSeconds = 30; // Check every 30 seconds

    public SchedulerPollingService(IServiceProvider serviceProvider, ILogger<SchedulerPollingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_pollingIntervalInSeconds * 1000, stoppingToken);
            await CheckForNewSchedulers();
        }
    }

    private async Task CheckForNewSchedulers()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<mPartnerAdmin_CommunicationScheduler.SchedulerContext>();
            var schedulerFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
            var quartzScheduler = await schedulerFactory.GetScheduler();

            // Fetch active schedulers
            var schedulers = dbContext.Communication_Custom_Scheduler
                .Where(s => s.IsActive && s.IsScheduled == false)
                .ToList();

            foreach (var scheduler in schedulers)
            {
                var jobKey = new JobKey($"SchedulerJob_{scheduler.SchedulerID}");
                if (!await quartzScheduler.CheckExists(jobKey))
                {
                    // Register new job and trigger
                    var jobDataMap = new JobDataMap
                    {
                        { "SchedulerData", Newtonsoft.Json.JsonConvert.SerializeObject(scheduler) }
                    };
                    var (cronTrigger, simpleTrigger) = CreateTriggers(scheduler, jobKey, jobDataMap);
                    if (cronTrigger != null)
                    {
                        await quartzScheduler.ScheduleJob(CreateJobDetail(jobKey), cronTrigger);
                    }

                    if (simpleTrigger != null)
                    {
                        await quartzScheduler.ScheduleJob(CreateJobDetail(jobKey), simpleTrigger);
                    }
                    _logger.LogInformation($"New job scheduled: {scheduler.SchedulerID}");
                }
            }
        }
    }

    private IJobDetail CreateJobDetail(JobKey jobKey)
    {
        return JobBuilder.Create<SchedulerJobService>()
            .WithIdentity(jobKey)
            .Build();
    }

    private (ITrigger? cronTrigger, ITrigger? simpleTrigger) CreateTriggers(Communication_Custom_Scheduler scheduler, JobKey jobKey, JobDataMap jobDataMap)
    {
        // Default values for triggers
        ITrigger cronTrigger = null;
        ITrigger simpleTrigger = null;

        if (scheduler.FrequencyType == "OneTime")
        {
            simpleTrigger = TriggerBuilder.Create()
                .ForJob(jobKey)
                .WithIdentity($"SchedulerTrigger_{scheduler.SchedulerID}")
                .StartAt(DateTimeOffset.Parse(Convert.ToString(scheduler.StartTimestamp)))
                .WithSimpleSchedule(x => x.WithRepeatCount(0))
                .UsingJobData(jobDataMap)
                .Build();
        }        
        if (scheduler.FrequencyType == "Daily")
        {
            DateTimeOffset startTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.StartTimestamp));
            DateTimeOffset endTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.EndTimestamp));

            int repeatValueInMinutes = scheduler.RepeatValue; // Repeat interval (in minutes), e.g., 1 minute
            int maxExecutionsPerDay = scheduler.FrequencyValue; // Maximum executions per day, e.g., 100

            simpleTrigger = TriggerBuilder.Create()
                .ForJob(jobKey)
                .WithIdentity($"DailyTrigger_{scheduler.SchedulerID}")
                .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(repeatValueInMinutes)  // Repeat every x minutes (from DB)
                        .WithRepeatCount(maxExecutionsPerDay - 1))   // Repeat (n-1) times, so it runs 'maxExecutionsPerDay' times
                .StartAt(startTimestamp)
                .EndAt(endTimestamp)
                .UsingJobData(jobDataMap)
                .Build();
        }        
        if (scheduler.FrequencyType == "Weekly")
        {
            // Get the days of the week from the "Mon.Wed" format to "MON,WED"
            var daysOfWeek = CronUtility.ConvertDaysOfWeek(scheduler.Frequency); // Convert "Mon.Wed" to "MON,WED"

            DateTimeOffset startTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.StartTimestamp));
            DateTimeOffset endTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.EndTimestamp));

            int startMinute = startTimestamp.Minute;
            int startHour = startTimestamp.Hour;

            // Construct a Cron expression for the start time on specific days (Not yet handling repetition)
            string cronExpression = $"{startMinute} {startHour} ? * {daysOfWeek}";

            cronTrigger = TriggerBuilder.Create()
                .ForJob(jobKey)
                .WithIdentity($"WeeklyCronTrigger_{scheduler.SchedulerID}")
                .WithCronSchedule(cronExpression)
                .StartAt(startTimestamp)
                .EndAt(endTimestamp)
                .UsingJobData(jobDataMap)
                .Build();

            simpleTrigger = TriggerBuilder.Create()
                .ForJob(jobKey)
                .WithIdentity($"WeeklySimpleTrigger_{scheduler.SchedulerID}")
                .StartAt(startTimestamp)
                .EndAt(endTimestamp)
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(scheduler.RepeatValue) // Repeat every 'RepeatValue' minutes
                    .WithRepeatCount(scheduler.FrequencyValue - 1)) // Repeat 'FrequencyValue' times (since first run is included)
                .UsingJobData(jobDataMap)
                .Build();
        }        
        if (scheduler.FrequencyType == "Monthly")
        {
            // Convert months and days into Cron compatible formats
            var months = CronUtility.ConvertMonths(scheduler.Frequency); // Convert "Jan.Feb" to "JAN,FEB"
            var monthDays = string.Join(",", scheduler.MonthDays.Split('.')); // Convert "1.15" to "1,15"

            DateTimeOffset startTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.StartTimestamp));
            DateTimeOffset endTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.EndTimestamp));

            int startMinute = startTimestamp.Minute;
            int startHour = startTimestamp.Hour;

            // Cron expression to schedule the job to run on specific days of the month and months
            string cronExpression = $"{startMinute} {startHour} {monthDays} {months} ?";

            // Add the CronTrigger to start the job on specific days and months
            cronTrigger = TriggerBuilder.Create()
                .ForJob(jobKey)
                .WithIdentity($"MonthlyCronTrigger_{scheduler.SchedulerID}")
                .WithCronSchedule(cronExpression)
                .StartAt(startTimestamp)
                .EndAt(endTimestamp)
                .UsingJobData(jobDataMap)
                .Build();

            // SimpleSchedule for repetitions on the same day
            simpleTrigger = TriggerBuilder.Create()
                .ForJob(jobKey)
                .WithIdentity($"MonthlySimpleTrigger_{scheduler.SchedulerID}")
                .StartAt(startTimestamp) // Start at the specified time
                .EndAt(endTimestamp) // End at the specified end time
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(scheduler.RepeatValue) // Repeat every 'RepeatValue' minutes
                    .WithRepeatCount(scheduler.FrequencyValue - 1)) // Repeat 'FrequencyValue' times (excluding the initial trigger)
                .UsingJobData(jobDataMap)
                .Build();
        }
        return (cronTrigger, simpleTrigger);
    }
}
