using InterviewWizard.Models.Session;
using InterviewWizard.Models.User;
using Microsoft.EntityFrameworkCore;

namespace InterviewWizard
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionObject> SessionObjects { get; set; }
        public DbSet<Question> QuestionObjects { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ResetRequest> ResetRequests { get; set; }
        public DbSet<VerifyRequest> VerifyRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Session
            modelBuilder.Entity<Session>()
                .HasKey(s => s.SessionKey);

            modelBuilder.Entity<Session>()
                .HasMany(s => s.SessionObjects)
                .WithOne(so => so.Session)
                .HasForeignKey(so => so.SessionKey);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.User) // Session has one User.
                .WithMany() // No need to specify a collection of Sessions in User.
                .HasForeignKey(s => s.UserId) // Specifies the foreign key.
                .IsRequired(false); // Makes the User association optional.

            modelBuilder.Entity<Session>()
                .HasMany(s => s.Questions)
                .WithOne(Question => Question.Session)
                .HasForeignKey(Question => Question.SessionKey);

            // Configure Question
            modelBuilder.Entity<Question>()
                .HasKey(q => q.Id);

            modelBuilder.Entity<Question>()
                .Property(q => q.AnswerRating)
                .HasDefaultValue(null);

            modelBuilder.Entity<Question>()
                .Property(q => q.AnswerText)
                .HasDefaultValue(null);

            // Configure SessionObject
            modelBuilder.Entity<SessionObject>()
                .HasKey(so => so.Id);

            // Configure User
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<User>()
                .Property(u => u.Verified)
                .HasDefaultValue(false);

            modelBuilder.Entity<User>()
                .Property(u => u.SubscriptionType)
                .HasDefaultValue(SubscriptionType.Free); // Assuming SubscriptionType is an enum and defaults to Free

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique(); // Enforce unique email addresses

            // Configure ResetRequest
            modelBuilder.Entity<ResetRequest>()
                .HasKey(rr => rr.Id);

            // Configure VerifyRequest
            modelBuilder.Entity<VerifyRequest>()
                .HasKey(vr => vr.Id);

            modelBuilder.Entity<VerifyRequest>()
                .HasOne(vr => vr.User)
                .WithMany(u => u.VerifyRequests)
                .HasForeignKey(vr => vr.UserId);
        }
    }
}
