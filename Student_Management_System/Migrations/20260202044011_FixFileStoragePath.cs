using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Student_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class FixFileStoragePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix forward slashes to backslashes in existing StoragePath values for Windows
            migrationBuilder.Sql(@"
                UPDATE Files 
                SET StoragePath = REPLACE(StoragePath, '/', '\')
                WHERE StoragePath LIKE '%/%'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert backslashes to forward slashes
            migrationBuilder.Sql(@"
                UPDATE Files 
                SET StoragePath = REPLACE(StoragePath, '\', '/')
                WHERE StoragePath LIKE '%\%'
            ");
        }
    }
}
