using Microsoft.EntityFrameworkCore;
using mPartnerAdmin_CommunicationScheduler;
using mPartnerAdmin_CommunicationScheduler.Services;
using mPartnerAdmin_CommunicationScheduler.Utilities;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var ConnectionString = builder.Configuration.GetSection("ConnectionString:SchedulerDb").Value;
builder.Services.AddDbContext<mPartnerAdmin_CommunicationScheduler.SchedulerContext>(options => options.UseSqlServer(ConnectionString));
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    //var jobKey = new JobKey("SchedulerJob");
    //q.AddJob<SchedulerJobService>(opts => opts.WithIdentity(jobKey));

    // Fetch schedules from the database and add triggers dynamically
    using (var scope = builder.Services.BuildServiceProvider().CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<mPartnerAdmin_CommunicationScheduler.SchedulerContext>();
        var schedulers = dbContext.Communication_Custom_Scheduler.Where(s => s.IsActive).ToList();

        foreach (var scheduler in schedulers)
        {
            var jobKey = new JobKey($"SchedulerJob_{scheduler.SchedulerID}"); 
            var triggerKey = new TriggerKey($"SchedulerTrigger_{scheduler.SchedulerID}");

            var jobDataMap = new JobDataMap
            {
                { "SchedulerData", Newtonsoft.Json.JsonConvert.SerializeObject(scheduler) } // Serialize scheduler data
            };

            /*** Named Job Deactivation Logic  ***/
            if (!scheduler.IsActive)
            {
                // Remove inactive scheduler's job and trigger
                var schedulerFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
                var quartzScheduler = schedulerFactory.GetScheduler().Result;
                
                if (quartzScheduler.CheckExists(jobKey).Result)
                {
                    quartzScheduler.DeleteJob(jobKey).Wait();

                    Console.WriteLine($"Scheduler with ID {scheduler.SchedulerID} has been deactivated and removed from Quartz.");
                    continue; // Skip to next scheduler
                }
            }

            // Add the job to Quartz if it's active
            q.AddJob<SchedulerJobService>(opts => opts.WithIdentity(jobKey));


            if (scheduler.FrequencyType == "OneTime")
            {
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity($"SchedulerTrigger_{scheduler.SchedulerID}")
                    .WithCronSchedule(Convert.ToString(scheduler.StartTimestamp))
                    .UsingJobData(jobDataMap));
            }
            if (scheduler.FrequencyType == "Daily")
            {
                DateTimeOffset startTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.StartTimestamp));
                DateTimeOffset endTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.EndTimestamp));
                string cronExpression = $"0 0 {startTimestamp.Hour}/{scheduler.RepeatValue} * * ?";
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity($"DailyTrigger_{scheduler.SchedulerID}")
                    .WithCronSchedule(cronExpression)
                    .StartAt(startTimestamp)
                    .EndAt(endTimestamp)
                    .UsingJobData(jobDataMap));
            }
            if (scheduler.FrequencyType == "Weekly")
            {
                var daysOfWeek = CronUtility.ConvertDaysOfWeek(scheduler.Frequency); // Convert "Mon.Wed" to "MON,WED"
                DateTimeOffset startTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.StartTimestamp));
                DateTimeOffset endTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.EndTimestamp));
                string cronExpression = $"0 0 {startTimestamp.Hour}/{scheduler.RepeatValue} ? * {daysOfWeek}";
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity($"WeeklyTrigger_{scheduler.SchedulerID}")
                    .WithCronSchedule(cronExpression)
                    .StartAt(startTimestamp)
                    .EndAt(endTimestamp)
                    .UsingJobData(jobDataMap));
            }
            if (scheduler.FrequencyType == "Monthly")
            {
                var months = CronUtility.ConvertMonths(scheduler.Frequency); // Convert "Jan,Jun,Oct" to "1,6,10"
                var days = scheduler.MonthDays; // e.g., "1,14,25,30"
                DateTimeOffset startTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.StartTimestamp));
                DateTimeOffset endTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.EndTimestamp));
                string cronExpression = $"0 0 {startTimestamp.Hour}/{scheduler.RepeatValue} {days} {months} ?";
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity($"MonthlyTrigger_{scheduler.SchedulerID}")
                    .WithCronSchedule(cronExpression)
                    .StartAt(startTimestamp)
                    .EndAt(endTimestamp)
                    .UsingJobData(jobDataMap));
            }
        }
    }
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
