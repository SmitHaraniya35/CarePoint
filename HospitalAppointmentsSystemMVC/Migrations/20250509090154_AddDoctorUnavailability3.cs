using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalAppointmentsSystemMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorUnavailability3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorUnavailabilities_Doctors_DoctorId",
                table: "DoctorUnavailabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorUnavailabilities_Users_AdminUserId",
                table: "DoctorUnavailabilities");

            migrationBuilder.AlterColumn<int>(
                name: "DoctorId",
                table: "DoctorUnavailabilities",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AdminUserId",
                table: "DoctorUnavailabilities",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorUnavailabilities_Doctors_DoctorId",
                table: "DoctorUnavailabilities",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorUnavailabilities_Users_AdminUserId",
                table: "DoctorUnavailabilities",
                column: "AdminUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorUnavailabilities_Doctors_DoctorId",
                table: "DoctorUnavailabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorUnavailabilities_Users_AdminUserId",
                table: "DoctorUnavailabilities");

            migrationBuilder.AlterColumn<int>(
                name: "DoctorId",
                table: "DoctorUnavailabilities",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AdminUserId",
                table: "DoctorUnavailabilities",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorUnavailabilities_Doctors_DoctorId",
                table: "DoctorUnavailabilities",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "DoctorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorUnavailabilities_Users_AdminUserId",
                table: "DoctorUnavailabilities",
                column: "AdminUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
