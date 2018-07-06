using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace YAOJ.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Problem",
                columns: table => new
                {
                    ProblemID = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    Format = table.Column<string>(nullable: false),
                    SampleInput = table.Column<string>(nullable: false),
                    SampleOutput = table.Column<string>(nullable: false),
                    Note = table.Column<string>(nullable: true),
                    Limitation = table.Column<string>(nullable: true),
                    Data = table.Column<byte[]>(nullable: true),
                    DataHash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problem", x => x.ProblemID);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Name = table.Column<string>(nullable: true),
                    Password = table.Column<string>(nullable: true),
                    AcceptanceCount = table.Column<int>(nullable: false),
                    IsAdmin = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Record",
                columns: table => new
                {
                    RecordID = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Status = table.Column<int>(nullable: false),
                    UserID = table.Column<int>(nullable: false),
                    ProblemID = table.Column<string>(nullable: true),
                    Language = table.Column<string>(nullable: true),
                    SourceCode = table.Column<string>(nullable: true),
                    UsedTime = table.Column<double>(nullable: false),
                    UsedMemory = table.Column<double>(nullable: false),
                    JudgeText = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Record", x => x.RecordID);
                    table.ForeignKey(
                        name: "FK_Record_Problem_ProblemID",
                        column: x => x.ProblemID,
                        principalTable: "Problem",
                        principalColumn: "ProblemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Record_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Record_ProblemID",
                table: "Record",
                column: "ProblemID");

            migrationBuilder.CreateIndex(
                name: "IX_Record_UserID",
                table: "Record",
                column: "UserID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Record");

            migrationBuilder.DropTable(
                name: "Problem");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
