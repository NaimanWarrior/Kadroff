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

        public DbSet<Position> Positions { get; set; }

        public DbSet<Cv> Cvs { get; set; }

        public DbSet<CandidateProject> CandidateProjects { get; set; }

        public DbSet<PositionComment> PositionComments { get; set; }

        public DbSet<CvLike> CvLikes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Position>().UseXminAsConcurrencyToken();
            builder.Entity<Cv>().UseXminAsConcurrencyToken();

            builder.Entity<CvLike>()
                .HasKey(l => new { l.CvId, l.RecruiterId });
        }
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

    public class Cv
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int PositionId { get; set; }

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public uint Version { get; set; }
    }

    public class Position
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public uint Version { get; set; }
    }

    public class CandidateProject
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DescriptionMarkdown { get; set; } = string.Empty;
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