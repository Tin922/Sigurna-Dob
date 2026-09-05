using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SigurnaDob.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareTaskChangeHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CareTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                    FromStatusId = table.Column<int>(type: "INTEGER", nullable: true),
                    ToStatusId = table.Column<int>(type: "INTEGER", nullable: true),
                    FromCaregiverId = table.Column<int>(type: "INTEGER", nullable: true),
                    ToCaregiverId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedByAppUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChangedByName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareTaskChangeHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareTaskChangeHistories_CareTaskStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "CareTaskStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CareTaskChangeHistories_CareTaskStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "CareTaskStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CareTaskChangeHistories_CareTasks_CareTaskId",
                        column: x => x.CareTaskId,
                        principalTable: "CareTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CareTaskChangeHistories_Employees_FromCaregiverId",
                        column: x => x.FromCaregiverId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CareTaskChangeHistories_Employees_ToCaregiverId",
                        column: x => x.ToCaregiverId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ResidentStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResidentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromStatusId = table.Column<int>(type: "INTEGER", nullable: true),
                    ToStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedByAppUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChangedByName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResidentStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResidentStatusHistories_ResidentStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "ResidentStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ResidentStatusHistories_ResidentStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "ResidentStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResidentStatusHistories_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareTaskChangeHistories_CareTaskId",
                table: "CareTaskChangeHistories",
                column: "CareTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CareTaskChangeHistories_FromCaregiverId",
                table: "CareTaskChangeHistories",
                column: "FromCaregiverId");

            migrationBuilder.CreateIndex(
                name: "IX_CareTaskChangeHistories_FromStatusId",
                table: "CareTaskChangeHistories",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CareTaskChangeHistories_ToCaregiverId",
                table: "CareTaskChangeHistories",
                column: "ToCaregiverId");

            migrationBuilder.CreateIndex(
                name: "IX_CareTaskChangeHistories_ToStatusId",
                table: "CareTaskChangeHistories",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentStatusHistories_FromStatusId",
                table: "ResidentStatusHistories",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentStatusHistories_ResidentId",
                table: "ResidentStatusHistories",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidentStatusHistories_ToStatusId",
                table: "ResidentStatusHistories",
                column: "ToStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareTaskChangeHistories");

            migrationBuilder.DropTable(
                name: "ResidentStatusHistories");
        }
    }
}
