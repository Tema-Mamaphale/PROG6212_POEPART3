using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimSystem.Migrations
{
    /// <inheritdoc />
    public partial class SyncLecturerClaimModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Lecturers",
                columns: new[] { "Id", "Department", "Email", "HourlyRate", "IsActive", "Name", "Phone" },
                values: new object[] { 1, "Business", "naledi.dlamini@gmail.com", 80m, true, "Naledi Dlamini", "0724438909" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Lecturers",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
