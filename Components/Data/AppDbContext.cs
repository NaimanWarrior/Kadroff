using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kadroff.Components.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppAttribute> Attributes { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<UserAttributeValue> UserAttributeValues { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<PositionAttribute> PositionAttributes { get; set; }
        public DbSet<Cv> Cvs { get; set; }
        public DbSet<CandidateProject> CandidateProjects { get; set; }
        public DbSet<PositionComment> PositionComments { get; set; }
        public DbSet<CvLike> CvLikes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Position>().UseXminAsConcurrencyToken();
            builder.Entity<Cv>().UseXminAsConcurrencyToken();
            builder.Entity<UserAttributeValue>().UseXminAsConcurrencyToken();
            builder.Entity<JobApplication>()
                .Property(j => j.Status)
                .HasConversion<string>();
            builder.Entity<CvLike>()
                .HasKey(l => new { l.CvId, l.RecruiterId });

            builder.Entity<PositionAttribute>()
                .HasKey(pa => new { pa.PositionId, pa.AttributeId });

            builder.Entity<AppAttribute>()
                .HasIndex(a => a.Name).IsUnique();

            builder.Entity<UserAttributeValue>()
                .HasIndex(u => new { u.UserId, u.AttributeId }).IsUnique();
        }
    }

    public class JobApplication
    {
        public int Id { get; set; }

        [Required]
        public int PositionId { get; set; }
        public Position Position { get; set; }

        [Required]
        public string CandidateId { get; set; }
        public IdentityUser Candidate { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        public JobApplicationStatus Status { get; set; } = JobApplicationStatus.UnderReview;

        public string CoverLetter { get; set; }
        public DateTime? AutoAcceptDate { get; set; }
        public string? ResponseComment { get; set; }
    }
        public static class RefJobApplicationStatus{
            public static string ToDisplayText(this JobApplicationStatus status) => status switch
            {
                JobApplicationStatus.Submitted => "Подан",
                JobApplicationStatus.UnderReview => "На рассмотрении",
                JobApplicationStatus.Interview => "Приглашен на собеседование",
                JobApplicationStatus.Rejected => "Отказ",
                JobApplicationStatus.Accepted => "Принят",
                _ => throw new NotImplementedException()
            };
        }

    public enum JobApplicationStatus
    {
        Submitted = 0,
        UnderReview = 1,
        Interview = 2,
        Rejected = 3,
        Accepted = 4
    }

    public class AppAttribute
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Category { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = "String";
    }

    public class UserAttributeValue
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        public int AttributeId { get; set; }
        public AppAttribute? Attribute { get; set; }
        public string StringValue { get; set; } = string.Empty;
        public uint Version { get; set; }
    }

    public class Cv
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        public int PositionId { get; set; }
        public Position? Position { get; set; }
        public bool IsPublished { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public byte[]? PdfByte { get; set; }
        public uint Version { get; set; }
    }

    public class Position
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int MaxProjects { get; set; } = 3;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public uint Version { get; set; }

        public ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    }

    public class PositionAttribute
    {
        public int PositionId { get; set; }
        public Position? Position { get; set; }
        public int AttributeId { get; set; }
        public AppAttribute? Attribute { get; set; }
    }

    public class CandidateProject
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DescriptionMarkdown { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class PositionComment
    {
        [Key]
        public int Id { get; set; }
        public int PositionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ContentMarkdown { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CvLike
    {
        public long CvId { get; set; }
        public string RecruiterId { get; set; } = string.Empty;
    }
}