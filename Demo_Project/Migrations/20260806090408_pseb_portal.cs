using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayslipMgtSystem.Migrations
{
    /// <inheritdoc />
    public partial class psebportal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "afee93c1-7fb9-411d-a83f-3d2486eeb9a1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "Email", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "0615bcfd-9057-409a-a8c5-61da1c0f6c29", new DateTime(2026, 8, 6, 14, 4, 8, 320, DateTimeKind.Local).AddTicks(6051), "admin.solo.pseb@gmail.com", "ADMIN.SOLO.PSEB@GMAIL.COM", "ADMIN.SOLO.PSEB", "AQAAAAIAAYagAAAAEMBDBGY5IeIEk3JumUuRCZZpImDJIrGXmYIYk+hRcjHMJwxCrxWXvcsUUj7JTMc4LA==", "e59a9cbc-e849-4022-aba4-9b6fc5ccc592", "admin.solo.pseb" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "afee93c1-7fb9-411d-a83f-3d2486eeb9a1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "Email", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "2745ae97-cfce-45ab-84dc-ba04c8ded8f6", new DateTime(2026, 8, 2, 0, 18, 8, 475, DateTimeKind.Local).AddTicks(7902), "admin.solo.clrmis@gmail.com", "ADMIN.SOLO.CLRMIS@GMAIL.COM", "ADMIN.SOLO.CLRMIS", "AQAAAAIAAYagAAAAEGNpvrraC2p9WnpFSzpVYN0nvLPbzf5mLczQ4TRk4pOFNqWdAQOjmZ8gjTZ4igXqBw==", "06622506-7fec-4a4e-a505-af4ea3b09299", "admin.solo.clrmis" });
        }
    }
}
