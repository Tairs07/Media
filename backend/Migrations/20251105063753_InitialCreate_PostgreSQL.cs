// using System;
// using Microsoft.EntityFrameworkCore.Migrations;
// using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

// #nullable disable

// namespace backend.Migrations
// {
//     /// <inheritdoc />
//     public partial class InitialCreate_PostgreSQL : Migration
//     {
//         /// <inheritdoc />
//         protected override void Up(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.CreateTable(
//                 name: "Users",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
//                     Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
//                     PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
//                     AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
//                     Bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
//                     CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "datetime('now', 'utc')"),
//                     UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_Users", x => x.Id);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "MediaFiles",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
//                     FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
//                     FileType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
//                     MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
//                     FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
//                     ThumbnailPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
//                     FileSize = table.Column<long>(type: "bigint", nullable: false),
//                     Width = table.Column<int>(type: "integer", nullable: true),
//                     Height = table.Column<int>(type: "integer", nullable: true),
//                     Duration = table.Column<int>(type: "integer", nullable: true),
//                     UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "GETUTCDATE()"),
//                     UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
//                     Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
//                     Tags = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
//                     ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
//                     LikeCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
//                     IsPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
//                     UserId = table.Column<int>(type: "integer", nullable: false)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_MediaFiles", x => x.Id);
//                     table.ForeignKey(
//                         name: "FK_MediaFiles_Users",
//                         column: x => x.UserId,
//                         principalTable: "Users",
//                         principalColumn: "Id",
//                         onDelete: ReferentialAction.Cascade);
//                 });

//             migrationBuilder.CreateTable(
//                 name: "UserSessions",
//                 columns: table => new
//                 {
//                     Id = table.Column<int>(type: "integer", nullable: false)
//                         .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
//                     UserId = table.Column<int>(type: "integer", nullable: false),
//                     Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
//                     IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
//                     UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
//                     CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "datetime('now', 'utc')"),
//                     ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
//                     IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
//                 },
//                 constraints: table =>
//                 {
//                     table.PrimaryKey("PK_UserSessions", x => x.Id);
//                     table.ForeignKey(
//                         name: "FK_UserSessions_Users",
//                         column: x => x.UserId,
//                         principalTable: "Users",
//                         principalColumn: "Id",
//                         onDelete: ReferentialAction.Cascade);
//                 });

//             migrationBuilder.CreateIndex(
//                 name: "IX_MediaFiles_FileType",
//                 table: "MediaFiles",
//                 column: "FileType");

//             migrationBuilder.CreateIndex(
//                 name: "IX_MediaFiles_UploadedAt",
//                 table: "MediaFiles",
//                 column: "UploadedAt");

//             migrationBuilder.CreateIndex(
//                 name: "IX_MediaFiles_UserId",
//                 table: "MediaFiles",
//                 column: "UserId");

//             migrationBuilder.CreateIndex(
//                 name: "IX_MediaFiles_UserId_IsPublic",
//                 table: "MediaFiles",
//                 columns: new[] { "UserId", "IsPublic" });

//             migrationBuilder.CreateIndex(
//                 name: "IX_Users_Email",
//                 table: "Users",
//                 column: "Email",
//                 unique: true);

//             migrationBuilder.CreateIndex(
//                 name: "IX_Users_Username",
//                 table: "Users",
//                 column: "Username",
//                 unique: true);

//             migrationBuilder.CreateIndex(
//                 name: "IX_UserSessions_Token",
//                 table: "UserSessions",
//                 column: "Token",
//                 unique: true);

//             migrationBuilder.CreateIndex(
//                 name: "IX_UserSessions_UserId",
//                 table: "UserSessions",
//                 column: "UserId");

//             migrationBuilder.CreateIndex(
//                 name: "IX_UserSessions_UserId_IsActive",
//                 table: "UserSessions",
//                 columns: new[] { "UserId", "IsActive" });
//         }

//         /// <inheritdoc />
//         protected override void Down(MigrationBuilder migrationBuilder)
//         {
//             migrationBuilder.DropTable(
//                 name: "MediaFiles");

//             migrationBuilder.DropTable(
//                 name: "UserSessions");

//             migrationBuilder.DropTable(
//                 name: "Users");
//         }
//     }
// }
