using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace PayslipMgtSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }
        public string? IMEINumber { get; set; }
        [MaxLength(450)]
        public string? FCMToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdateddAt { get; set; }
        public DateTime? PasswordResetAt { get; set; }
        [MaxLength(450)]
        public string? JsonData { get; set; }
        public string? ImageUrl { get; set; }
        public static void Seed (ModelBuilder modelBuilder)
        {
            //User Data seed
            var hasher = new PasswordHasher<ApplicationUser>();
            modelBuilder.Entity<ApplicationUser>().HasData(
                  new ApplicationUser
                  {
                      Id = "afee93c1-7fb9-411d-a83f-3d2486eeb9a1", // primary key
                      UserName = "admin.solo.nitb",
                      NormalizedUserName = "admin.solo.nitb".ToUpper(),
                      Email = "admin.solo.nitb@gmail.com",
                      NormalizedEmail = "admin.solo.nitb@gmail.com".ToUpper(),
                      EmailConfirmed = true,
                      PasswordHash = hasher.HashPassword(null, "Abc@1234"),
                      PhoneNumberConfirmed = true,
                      CreatedAt = DateTime.Now,
                     
                  }
              );

            ////Seeding the relation between our user and role to AspNetUserRoles table
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = "aeb8c506-ff69-4536-a2ae-6ce162ae0dca",
                    UserId = "afee93c1-7fb9-411d-a83f-3d2486eeb9a1"
                }
            );

        }

    }

}
