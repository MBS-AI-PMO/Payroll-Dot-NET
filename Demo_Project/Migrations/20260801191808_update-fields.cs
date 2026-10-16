using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayslipMgtSystem.Migrations
{
    /// <inheritdoc />
    public partial class updatefields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "TravellingAllowance",
                table: "Files",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "MedicalAllowance",
                table: "Files",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "EquipmentAllowance",
                table: "Files",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "CommunicationAllowance",
                table: "Files",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "afee93c1-7fb9-411d-a83f-3d2486eeb9a1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2745ae97-cfce-45ab-84dc-ba04c8ded8f6", new DateTime(2026, 8, 2, 0, 18, 8, 475, DateTimeKind.Local).AddTicks(7902), "AQAAAAIAAYagAAAAEGNpvrraC2p9WnpFSzpVYN0nvLPbzf5mLczQ4TRk4pOFNqWdAQOjmZ8gjTZ4igXqBw==", "06622506-7fec-4a4e-a505-af4ea3b09299" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TravellingAllowance",
                table: "Files",
                type: "int",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MedicalAllowance",
                table: "Files",
                type: "int",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentAllowance",
                table: "Files",
                type: "int",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CommunicationAllowance",
                table: "Files",
                type: "int",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "afee93c1-7fb9-411d-a83f-3d2486eeb9a1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f1d36ad2-f684-4541-aba4-415a3d7d3528", new DateTime(2026, 8, 1, 21, 39, 0, 323, DateTimeKind.Local).AddTicks(2176), "AQAAAAIAAYagAAAAEC2Upi4AbXTBHUKqVZsIMxyqjOBDGFF7KhPaCCZYcdf+HKiXkCA9mFZfsnS6mlSglw==", "ba24b258-f5b6-483c-8234-3a7858c7ba98" });
        }
    }
}
