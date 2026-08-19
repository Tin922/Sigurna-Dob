using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SigurnaDob.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedLookupData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ActivityTypes",
                columns: new[] { "Id", "Name", "Note" },
                values: new object[,]
                {
                    { 1, "Društvena", null },
                    { 2, "Kreativna", null },
                    { 3, "Tjelovježba", null },
                    { 4, "Edukativna", null },
                    { 5, "Izlet", null }
                });

            migrationBuilder.InsertData(
                table: "AppRoles",
                columns: new[] { "Id", "DisplayName", "Name" },
                values: new object[,]
                {
                    { 1, "Korisnik", "User" },
                    { 2, "Administrator", "Admin" },
                    { 3, "Koordinator", "Coordinator" },
                    { 4, "Njegovatelj", "Caregiver" },
                    { 5, "Član obitelji", "FamilyMember" }
                });

            migrationBuilder.InsertData(
                table: "CareTaskStatuses",
                columns: new[] { "Id", "Name", "Note" },
                values: new object[,]
                {
                    { 1, "Novo", null },
                    { 2, "Dodijeljeno", null },
                    { 3, "U tijeku", null },
                    { 4, "Izvršeno", null },
                    { 5, "Otkazano", null }
                });

            migrationBuilder.InsertData(
                table: "CareTaskTypes",
                columns: new[] { "Id", "Name", "Note" },
                values: new object[,]
                {
                    { 1, "Terapija", null },
                    { 2, "Prehrana", null },
                    { 3, "Higijena", null },
                    { 4, "Pratnja", null },
                    { 5, "Administrativno", null }
                });

            migrationBuilder.InsertData(
                table: "EmployeePositions",
                columns: new[] { "Id", "Name", "Note" },
                values: new object[,]
                {
                    { 1, "Koordinator", null },
                    { 2, "Njegovatelj", null }
                });

            migrationBuilder.InsertData(
                table: "EmployeeStatuses",
                columns: new[] { "Id", "Name", "Note" },
                values: new object[,]
                {
                    { 1, "Aktivan", null },
                    { 2, "Neaktivan", null }
                });

            migrationBuilder.InsertData(
                table: "ResidentStatuses",
                columns: new[] { "Id", "Name", "Note" },
                values: new object[,]
                {
                    { 1, "U pripremi za prijem", null },
                    { 2, "Aktivan", null },
                    { 3, "Privremeno odsutan", null },
                    { 4, "Premješten iz doma", null },
                    { 5, "Arhiviran", null }
                });

            migrationBuilder.InsertData(
                table: "RoomStatuses",
                columns: new[] { "Id", "Name", "Note" },
                values: new object[,]
                {
                    { 1, "U uporabi", null },
                    { 2, "Održavanje", null },
                    { 3, "Izvan uporabe", null }
                });

            migrationBuilder.InsertData(
                table: "VisitRequestStatuses",
                columns: new[] { "Id", "Name", "Note" },
                values: new object[,]
                {
                    { 1, "Zaprimljeno", null },
                    { 2, "Odobreno", null },
                    { 3, "Odbijeno", null },
                    { 4, "Održano", null },
                    { 5, "Otkazano", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ActivityTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ActivityTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ActivityTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ActivityTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ActivityTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "CareTaskStatuses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CareTaskStatuses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CareTaskStatuses",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CareTaskStatuses",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CareTaskStatuses",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "CareTaskTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CareTaskTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CareTaskTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CareTaskTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CareTaskTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "EmployeePositions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "EmployeePositions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EmployeeStatuses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "EmployeeStatuses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ResidentStatuses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ResidentStatuses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ResidentStatuses",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ResidentStatuses",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ResidentStatuses",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "RoomStatuses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RoomStatuses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RoomStatuses",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "VisitRequestStatuses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "VisitRequestStatuses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "VisitRequestStatuses",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "VisitRequestStatuses",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "VisitRequestStatuses",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
