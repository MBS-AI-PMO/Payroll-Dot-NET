using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayslipMgtSystem.Migrations
{
    /// <inheritdoc />
    public partial class nitbportal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "afee93c1-7fb9-411d-a83f-3d2486eeb9a1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "Email", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "cb780739-5fcc-4eb5-a1d0-e79008796653", new DateTime(2026, 9, 25, 11, 44, 51, 531, DateTimeKind.Local).AddTicks(4619), "admin.solo.nitb@gmail.com", "ADMIN.SOLO.NITB@GMAIL.COM", "ADMIN.SOLO.NITB", "AQAAAAIAAYagAAAAEOop8rzaoYsw0FOGCJSsKVW415ACJcgyEEV2/qrJh8uZk3z/QjZjQ6nh1tWeaKzBug==", "a6708c4e-6787-4a03-8561-e6086e395469", "admin.solo.nitb" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "afee93c1-7fb9-411d-a83f-3d2486eeb9a1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "Email", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "98e42f67-4eaa-4f73-86e0-c4e24b91c42a", new DateTime(2026, 8, 6, 14, 11, 13, 153, DateTimeKind.Local).AddTicks(4414), "admin.solo.dgse@gmail.com", "ADMIN.SOLO.DGSE@GMAIL.COM", "ADMIN.SOLO.DGSE", "AQAAAAIAAYagAAAAEKF/e2xvMPxk3o3xUhyOfo6FKKt0+0xrKxm64FoF+3pxByC+38m9jLosGOE+oUsOvw==", "bafecb47-3d14-4e4c-9933-6eeb4f7a69fa", "admin.solo.dgse" });
        }
    }
}
