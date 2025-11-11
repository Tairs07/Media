#r "nuget: Npgsql, 8.0.3"

using Npgsql;

var connectionString = "Host=120.27.211.83;Port=5432;Database=mediashare;Username=mediashare_user;Password=mediashare_password";

Console.WriteLine("正在连接数据库...");

using var connection = new NpgsqlConnection(connectionString);
connection.Open();

var sql = @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('20251105063753_InitialCreate_PostgreSQL', '9.0.0')
ON CONFLICT (""MigrationId"") DO NOTHING;";

using var command = new NpgsqlCommand(sql, connection);
var rows = command.ExecuteNonQuery();

Console.WriteLine($"✓ 迁移历史已修复! (影响 {rows} 行)");
Console.WriteLine("");
Console.WriteLine("现在可以创建和应用新迁移了:");
Console.WriteLine("  dotnet ef migrations add AddChatFeature");
Console.WriteLine("  dotnet ef database update");



