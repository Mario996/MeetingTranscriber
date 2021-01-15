using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebAPI.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transcription",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    TranscriptionARN = table.Column<string>(nullable: true),
                    MeetingPlatform = table.Column<string>(nullable: true),
                    DateOfMeeting = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transcription", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(nullable: true),
                    Password = table.Column<string>(nullable: true),
                    Role = table.Column<string>(nullable: true),
                    Token = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptionUser",
                columns: table => new
                {
                    TranscriptionId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptionUser", x => new { x.TranscriptionId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TranscriptionUser_Transcription_TranscriptionId",
                        column: x => x.TranscriptionId,
                        principalTable: "Transcription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TranscriptionUser_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionUser_UserId",
                table: "TranscriptionUser",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranscriptionUser");

            migrationBuilder.DropTable(
                name: "Transcription");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
