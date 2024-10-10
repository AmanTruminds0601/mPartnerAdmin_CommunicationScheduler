using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mPartnerAdmin_CommunicationScheduler.Domain.Models;
using mPartnerAdmin_CommunicationScheduler.Services;

public class SchedulerPollingService : BackgroundService
{
    private readonly SchedulerContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISchedulerFactory _schedulerFactory;

    public SchedulerPollingService(IServiceProvider serviceProvider, ISchedulerFactory schedulerFactory, SchedulerContext dbContext)
    {
        _serviceProvider = serviceProvider;
        _schedulerFactory = schedulerFactory;
        _dbContext = dbContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<mPartnerAdmin_CommunicationScheduler.SchedulerContext>();

                    // Fetching new schedulers from the database
                    var newSchedulers = dbContext.Communication_Custom_Scheduler
                        .Where(s => s.IsActive && !s.IsScheduled)
                        .ToList();

                    foreach (var scheduler in newSchedulers)
                    {
                        // Adding job to Quartz scheduler
                        await ScheduleJob(scheduler);

                        // Marking the scheduler as scheduled to avoid duplication
                        scheduler.IsScheduled = true;
                        dbContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                // Logging the exception
                Console.WriteLine(ex.Message);
            }

            // Poll every 30 seconds 
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ScheduleJob(Communication_Custom_Scheduler scheduler)
    {
        var jobKey = new JobKey($"SchedulerJob_{scheduler.SchedulerID}");
        var triggerKey = new TriggerKey($"SchedulerTrigger_{scheduler.SchedulerID}");

        var jobDataMap = new JobDataMap
        {
            { "SchedulerData", Newtonsoft.Json.JsonConvert.SerializeObject(scheduler) }
        };

        var schedulerFactory = await _schedulerFactory.GetScheduler();

        var job = JobBuilder.Create<SchedulerJobService>()
                            .WithIdentity(jobKey)
                            .UsingJobData(jobDataMap)
                            .Build();

        // Adding triggers based on FrequencyType TO DO....
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"Trigger_{scheduler.SchedulerID}")
            .StartNow()
            .Build();

        await schedulerFactory.ScheduleJob(job, trigger);
    }
}
