using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PayslipMgtSystem.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
          : base(options)
        {
        }
        public DbSet<v_Accounts> v_Accounts { get; set; }
        public DbSet<UploadFile> Files { get; set; }
        public DbSet<TaxCertificateRequest> ITCRequests { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Roles Data Seed
            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole[]{
                                new IdentityRole{
                                    Id = "aeb8c506-ff69-4536-a2ae-6ce162ae0dca",
                                    Name = "Admin",
                                    NormalizedName = "admin".ToUpper() },

            });
            #endregion
            ApplicationUser.Seed(modelBuilder);

            base.OnModelCreating(modelBuilder);


        }
    }
}
