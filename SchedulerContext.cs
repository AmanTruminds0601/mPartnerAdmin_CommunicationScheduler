using Microsoft.EntityFrameworkCore;
using mPartnerAdmin_CommunicationScheduler.Domain.Models;

namespace mPartnerAdmin_CommunicationScheduler
{
    public class SchedulerContext : DbContext
    {
        public SchedulerContext(DbContextOptions<SchedulerContext> options) : base(options)
        {

        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Communication_Custom_Scheduler>()
            //     .HasKey(c => c.SchedulerID); ;

            //modelBuilder.Entity<CommunicationRunHistory>()
            //    .HasNoKey();
            modelBuilder.Entity<Communication_Custom_Scheduler>()
                .ToTable("Communication_Custom_Scheduler", "mpadmin") // Specify schema
                .HasNoKey();

            modelBuilder.Entity<CommunicationRunHistory>()
                .ToTable("CommunicationRunHistory", "mpadmin") // Specify schema if necessary
                .HasNoKey();
        }

        public DbSet<Communication_Custom_Scheduler> Communication_Custom_Scheduler { get; set; }
        public DbSet<CommunicationRunHistory> CommunicationRunHistory { get; set; }
    }
}
