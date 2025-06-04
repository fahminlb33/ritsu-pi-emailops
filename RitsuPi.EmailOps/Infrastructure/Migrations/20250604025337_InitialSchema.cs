using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RitsuPi.EmailOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "message_threads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    thread_hash = table.Column<string>(type: "TEXT", nullable: false),
                    memory_json = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_threads", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    email = table.Column<string>(type: "TEXT", nullable: false),
                    direction = table.Column<string>(type: "TEXT", nullable: false),
                    content = table.Column<string>(type: "TEXT", nullable: false),
                    semantic_kernel_history_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_histories_message_threads_semantic_kernel_history_id",
                        column: x => x.semantic_kernel_history_id,
                        principalTable: "message_threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_histories_semantic_kernel_history_id",
                table: "email_histories",
                column: "semantic_kernel_history_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_threads_thread_hash",
                table: "message_threads",
                column: "thread_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_histories");

            migrationBuilder.DropTable(
                name: "message_threads");
        }
    }
}
