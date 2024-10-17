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

    //for App Crash
    // Enable persistent store
    q.UsePersistentStore(options =>
    {
        options.UseSqlServer(ConnectionString); // Using SQL Server for persistence
        options.UseNewtonsoftJsonSerializer();  // Using JSON to serialize the job data
        options.UseClustering();  // Enable clustering if you are running multiple instances of the app
    });

    //var jobKey = new JobKey("SchedulerJob");
    //q.AddJob<SchedulerJobService>(opts => opts.WithIdentity(jobKey));

    // Fetch schedules from the database and add triggers dynamically
    using (var scope = builder.Services.BuildServiceProvider().CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<mPartnerAdmin_CommunicationScheduler.SchedulerContext>();
        var schedulers = dbContext.Communication_Custom_Scheduler.Where(s => s.IsActive && s.IsScheduled == false).ToList();

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
                    .StartAt(DateTimeOffset.Parse(Convert.ToString(scheduler.StartTimestamp)))
                    .WithSimpleSchedule(x => x.WithRepeatCount(0))
                    .UsingJobData(jobDataMap));
            }
            if (scheduler.FrequencyType == "Daily")
            {
                DateTimeOffset startTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.StartTimestamp));
                DateTimeOffset endTimestamp = DateTimeOffset.Parse(Convert.ToString(scheduler.EndTimestamp));

                int repeatValueInMinutes = scheduler.RepeatValue; // Repeat interval (in minutes), e.g., 1 minute
                int maxExecutionsPerDay = scheduler.FrequencyValue; // Maximum executions per day, e.g., 100

                // Add the job trigger dynamically
                    q.AddTrigger(opts => opts
                        .ForJob(jobKey)
                    .WithIdentity($"DailyTrigger_{scheduler.SchedulerID}")
                        .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(repeatValueInMinutes)  // Repeat every x minutes (from DB)
                        .WithRepeatCount(maxExecutionsPerDay - 1))    // Repeat (n-1) times, so it runs 'maxExecutionsPerDay' times
                    .StartAt(startTimestamp)                          // Job starts at this time
                    .EndAt(endTimestamp)                              // Job ends at this time
                    .UsingJobData(jobDataMap));                       // Attach the serialized scheduler data as job data
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

                // Adding the CronTrigger for the weekly schedule (this sets the job to start on the correct days)
                    q.AddTrigger(opts => opts
                        .ForJob(jobKey)
                    .WithIdentity($"WeeklyCronTrigger_{scheduler.SchedulerID}")
                    .WithCronSchedule(cronExpression)
                    .StartAt(startTimestamp)
                    .EndAt(endTimestamp)
                        .UsingJobData(jobDataMap));

                // SimpleSchedule for repetitions on each day the job is triggered
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity($"WeeklySimpleTrigger_{scheduler.SchedulerID}")
                    .StartAt(startTimestamp) // Start at the start time
                    .EndAt(endTimestamp) // End at the end time
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(scheduler.RepeatValue) // Repeat every 'RepeatValue' minutes
                        .WithRepeatCount(scheduler.FrequencyValue - 1)) // Repeat 'FrequencyValue' times (since first run is included)
                    .UsingJobData(jobDataMap));
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
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity($"MonthlyCronTrigger_{scheduler.SchedulerID}")
                    .WithCronSchedule(cronExpression)
                    .StartAt(startTimestamp)
                    .EndAt(endTimestamp)
                    .UsingJobData(jobDataMap));

                // SimpleSchedule for repetitions on the same day
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity($"MonthlySimpleTrigger_{scheduler.SchedulerID}")
                    .StartAt(startTimestamp) // Start at the specified time
                    .EndAt(endTimestamp) // End at the specified end time
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(scheduler.RepeatValue) // Repeat every 'RepeatValue' minutes
                        .WithRepeatCount(scheduler.FrequencyValue - 1)) // Repeat 'FrequencyValue' times (excluding the initial trigger)
                    .UsingJobData(jobDataMap));
            }

            // Updating IsScheduled after scheduling
            scheduler.IsScheduled = true;
            dbContext.Communication_Custom_Scheduler.Update(scheduler);
            dbContext.SaveChanges();
        }
    }
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddHostedService<SchedulerPollingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
