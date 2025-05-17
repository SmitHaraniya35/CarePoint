using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalAppointmentsSystemMVC.Migrations
{
    /// <inheritdoc />
    public partial class MoveFieldToDoctorModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFirstLogin",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "IsFirstLogin",
                table: "Doctors",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFirstLogin",
                table: "Doctors");

            migrationBuilder.AddColumn<bool>(
                name: "IsFirstLogin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
