using BlogEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogEngine.Infrastructure.Data
{
    public class BlogDbContext : DbContext
    {
        public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options)
        {
        }

        public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
        public DbSet<PostSection> PostSections => Set<PostSection>();
        public DbSet<ReaderReaction> ReaderReactions => Set<ReaderReaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BlogPost>(entity =>
            {
                entity.ToTable("BlogPosts");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Guid).IsUnique();
                entity.HasIndex(e => e.RootPostId);

                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Author).HasMaxLength(150).IsRequired();

                entity.HasOne(e => e.PreviousVersion)
                      .WithMany()
                      .HasForeignKey(e => e.PreviousVersionId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.NextVersion)
                      .WithMany()
                      .HasForeignKey(e => e.NextVersionId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.Sections)
                      .WithOne(s => s.BlogPost)
                      .HasForeignKey(s => s.BlogPostId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Reactions)
                      .WithOne(r => r.BlogPost)
                      .HasForeignKey(r => r.BlogPostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PostSection>(entity =>
            {
                entity.ToTable("PostSections");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.BlogPostId, e.SortOrder });

                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Provider).HasMaxLength(100);
                entity.Property(e => e.ShareUrl).HasMaxLength(1000);
            });

            modelBuilder.Entity<ReaderReaction>(entity =>
            {
                entity.ToTable("ReaderReactions");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BlogPostId);
                entity.HasIndex(e => e.PostSectionId);

                entity.Property(e => e.Classification).HasConversion<string>();
                entity.Property(e => e.ReaderName).HasMaxLength(100);
                entity.Property(e => e.Comment).HasMaxLength(2000);
            });
        }
    }
}
