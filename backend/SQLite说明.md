# SQLite 数据库说明

## 为什么使用 SQLite？

SQLite 是一个轻量级、文件型的关系数据库，非常适合：
- 开发和测试环境
- 小型到中型应用
- 不需要多用户并发访问的场景
- 易于部署和备份（只需复制单个文件）

## SQLite vs SQL Server

| 特性 | SQLite | SQL Server |
|------|--------|------------|
| 部署 | 无需安装，零配置 | 需要安装数据库服务器 |
| 文件 | 单个 .db 文件 | 复杂的数据文件和日志 |
| 性能 | 适合中小型应用 | 适合大型企业应用 |
| 并发 | 支持读并发，写有锁定 | 支持高并发读写 |
| 成本 | 免费 | 需要许可证（企业版） |
| 移动端 | 原生支持 | 不支持 |

## 数据库文件位置

数据库文件：`backend/MediaShareDB.db`

首次运行应用并执行迁移后，会自动创建此文件。

## 数据类型映射

SQLite 使用动态类型系统，EF Core 会自动处理类型映射：

- `int` → INTEGER
- `string` → TEXT
- `bool` → INTEGER (0/1)
- `DateTime` → TEXT (ISO8601格式)
- `long` → INTEGER

## 注意事项

### 1. 默认值语法
- SQL Server: `GETUTCDATE()`
- SQLite: `datetime('now', 'utc')`

### 2. 字符串长度限制
SQLite 的 TEXT 类型没有固定长度限制，但 EF Core 的 MaxLength 属性仍会生效（应用层验证）。

### 3. 并发控制
SQLite 支持读并发，但写入时会锁定整个数据库。对于多用户应用，建议：
- 使用 SQLite WAL 模式（Write-Ahead Logging）
- 或者迁移到 PostgreSQL/SQL Server

### 4. 备份
SQLite 数据库备份非常简单，只需复制 `.db` 文件即可。

## 启用 WAL 模式（可选，提升并发性能）

在 `ApplicationDbContext` 的 `OnConfiguring` 中或连接字符串中启用：

```csharp
options.UseSqlite(connectionString, opt => 
{
    opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
});

// 或者在 Program.cs 中：
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
    
    // 启用 WAL 模式（提升并发性能）
    options.UseSqlite(connectionString, sqliteOptions =>
    {
        sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

// 然后在应用启动时执行：
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}
```

## 迁移到生产环境

如果将来需要迁移到 SQL Server 或 PostgreSQL：

1. 安装新的数据库提供程序包
2. 更新连接字符串
3. 更新 `Program.cs` 中的 `UseSqlServer()` 或 `UseNpgsql()`
4. 重新生成迁移（可能需要调整 SQL 语法）
5. 导出 SQLite 数据并导入到新数据库

## 数据库工具

推荐的 SQLite 管理工具：
- **DB Browser for SQLite** - 免费的图形化工具
- **SQLiteStudio** - 跨平台的 SQLite 管理工具
- **Visual Studio Code 扩展** - SQLite Viewer

## 连接字符串示例

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=MediaShareDB.db"
  }
}
```

也可以使用相对路径或绝对路径：
- 相对路径：`Data Source=./Data/MediaShareDB.db`
- 绝对路径：`Data Source=C:/Data/MediaShareDB.db`
- 内存数据库（仅测试）：`Data Source=:memory:`




