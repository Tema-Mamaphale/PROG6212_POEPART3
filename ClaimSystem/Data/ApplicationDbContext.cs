using Microsoft.EntityFrameworkCore;
using ClaimSystem.Models;

namespace ClaimSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Claim> Claims => Set<Claim>();
        public DbSet<Lecturer> Lecturers => Set<Lecturer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

           
            modelBuilder.Entity<Claim>(e =>
            {
                e.Property(x => x.LecturerName).HasMaxLength(120).IsRequired();
                e.Property(x => x.Month).HasMaxLength(40).IsRequired();

                
                e.Property(x => x.HoursWorked).HasColumnType("decimal(10,2)").HasPrecision(10, 2);
                e.Property(x => x.HourlyRate).HasColumnType("decimal(10,2)").HasPrecision(10, 2);

                e.Property(x => x.Status).HasConversion<int>();

                
                e.HasOne(x => x.Lecturer)
                 .WithMany()
                 .HasForeignKey(x => x.LecturerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

 
            modelBuilder.Entity<Lecturer>(e =>
            {
                e.Property(x => x.Name).IsRequired().HasMaxLength(120);
                e.Property(x => x.Email).HasMaxLength(200);
                e.Property(x => x.Phone).HasMaxLength(50);
                e.Property(x => x.Department).HasMaxLength(200);

           
                e.Property(x => x.HourlyRate).HasColumnType("decimal(18,2)").HasPrecision(18, 2);

              
                e.HasData(new Lecturer
                {
                    Id = 1,
                    Name = "Naledi Dlamini",
                    Email = "naledi.dlamini@gmail.com",
                    Phone = "0724438909",
                    Department = "Business",
                    HourlyRate = 80m,
                    IsActive = true
                });
            });
        }
    }
}
