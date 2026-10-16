using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayslipMgtSystem.Migrations
{
    /// <inheritdoc />
    public partial class dgseportal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "afee93c1-7fb9-411d-a83f-3d2486eeb9a1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "Email", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "98e42f67-4eaa-4f73-86e0-c4e24b91c42a", new DateTime(2026, 8, 6, 14, 11, 13, 153, DateTimeKind.Local).AddTicks(4414), "admin.solo.dgse@gmail.com", "ADMIN.SOLO.DGSE@GMAIL.COM", "ADMIN.SOLO.DGSE", "AQAAAAIAAYagAAAAEKF/e2xvMPxk3o3xUhyOfo6FKKt0+0xrKxm64FoF+3pxByC+38m9jLosGOE+oUsOvw==", "bafecb47-3d14-4e4c-9933-6eeb4f7a69fa", "admin.solo.dgse" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "afee93c1-7fb9-411d-a83f-3d2486eeb9a1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "Email", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "0615bcfd-9057-409a-a8c5-61da1c0f6c29", new DateTime(2026, 8, 6, 14, 4, 8, 320, DateTimeKind.Local).AddTicks(6051), "admin.solo.pseb@gmail.com", "ADMIN.SOLO.PSEB@GMAIL.COM", "ADMIN.SOLO.PSEB", "AQAAAAIAAYagAAAAEMBDBGY5IeIEk3JumUuRCZZpImDJIrGXmYIYk+hRcjHMJwxCrxWXvcsUUj7JTMc4LA==", "e59a9cbc-e849-4022-aba4-9b6fc5ccc592", "admin.solo.pseb" });
        }
    }
}
