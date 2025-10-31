# 数据库设计文档

## 概述
本文档描述了图片视频分享网站的数据库设计方案，使用 SQLite 作为数据库。

## 表结构

### 1. Users（用户表）

用户基本信息表，存储所有注册用户的信息。

| 字段名 | 类型 | 长度 | 允许空 | 说明 | 索引 |
|--------|------|------|--------|------|------|
| Id | int | - | 否 | 主键，自增 | PK |
| Username | nvarchar | 50 | 否 | 用户名，唯一 | 唯一索引 |
| Email | nvarchar | 100 | 否 | 邮箱，唯一 | 唯一索引 |
| PasswordHash | nvarchar | 255 | 否 | 密码哈希值 | - |
| AvatarUrl | nvarchar | 500 | 是 | 头像URL | - |
| Bio | nvarchar | 500 | 是 | 个人简介 | - |
| CreatedAt | datetime2 | - | 否 | 创建时间，默认UTC当前时间 | - |
| UpdatedAt | datetime2 | - | 是 | 更新时间 | - |
| IsActive | bit | - | 否 | 是否激活状态，默认true | - |

**关系：**
- 一对多：一个用户可以有多个媒体文件（MediaFiles）
- 一对多：一个用户可以有多个会话（UserSessions）

---

### 2. MediaFiles（媒体文件表）

存储所有上传的图片和视频文件信息。

| 字段名 | 类型 | 长度 | 允许空 | 说明 | 索引 |
|--------|------|------|--------|------|------|
| Id | int | - | 否 | 主键，自增 | PK |
| FileName | nvarchar | 255 | 否 | 文件名 | - |
| FileType | nvarchar | 50 | 否 | 文件类型（image/video） | 索引 |
| MimeType | nvarchar | 100 | 否 | MIME类型（如image/jpeg） | - |
| FilePath | nvarchar | 500 | 否 | 文件存储路径 | - |
| ThumbnailPath | nvarchar | 500 | 是 | 缩略图路径 | - |
| FileSize | bigint | - | 否 | 文件大小（字节） | - |
| Width | int | - | 是 | 宽度（像素） | - |
| Height | int | - | 是 | 高度（像素） | - |
| Duration | int | - | 是 | 视频时长（秒） | - |
| UploadedAt | datetime2 | - | 否 | 上传时间，默认UTC当前时间 | 索引 |
| UpdatedAt | datetime2 | - | 是 | 更新时间 | - |
| Description | nvarchar | 500 | 是 | 文件描述 | - |
| Tags | nvarchar | 100 | 是 | 标签（逗号分隔） | - |
| ViewCount | int | - | 否 | 浏览次数，默认0 | - |
| LikeCount | int | - | 否 | 点赞数，默认0 | - |
| IsPublic | bit | - | 否 | 是否公开，默认true | 复合索引 |
| UserId | int | - | 否 | 用户ID，外键 | 索引，FK |

**关系：**
- 多对一：多个媒体文件属于一个用户（Users）

**索引策略：**
- 在 UserId 上建立索引，加速按用户查询
- 在 FileType 上建立索引，加速按类型筛选
- 在 UploadedAt 上建立索引，加速按时间排序
- 在 (UserId, IsPublic) 上建立复合索引，优化用户公开文件查询

---

### 3. UserSessions（用户会话表）

存储用户登录会话信息，用于管理JWT Token。

| 字段名 | 类型 | 长度 | 允许空 | 说明 | 索引 |
|--------|------|------|--------|------|------|
| Id | int | - | 否 | 主键，自增 | PK |
| UserId | int | - | 否 | 用户ID，外键 | 索引，FK |
| Token | nvarchar | 500 | 否 | JWT Token，唯一 | 唯一索引 |
| IpAddress | nvarchar | 45 | 否 | IP地址（支持IPv6） | - |
| UserAgent | nvarchar | 500 | 是 | 用户代理字符串 | - |
| CreatedAt | datetime2 | - | 否 | 创建时间，默认UTC当前时间 | - |
| ExpiresAt | datetime2 | - | 否 | 过期时间 | - |
| IsActive | bit | - | 否 | 是否激活，默认true | 复合索引 |

**关系：**
- 多对一：多个会话属于一个用户（Users）

**索引策略：**
- 在 UserId 上建立索引，加速按用户查询会话
- 在 Token 上建立唯一索引，快速验证Token
- 在 (UserId, IsActive) 上建立复合索引，优化查找用户活跃会话

---

## 数据库关系图

```
Users (1) ────────< (N) MediaFiles
  │
  │ (1)
  │
  └───────< (N) UserSessions
```

## 索引优化说明

1. **唯一索引**：确保用户名、邮箱、Token的唯一性
2. **普通索引**：加速常用查询条件（UserId、FileType、UploadedAt）
3. **复合索引**：优化组合查询（如用户公开文件、用户活跃会话）

## 默认值设计

- **时间字段**：使用 `GETUTCDATE()` 自动设置UTC时间
- **布尔字段**：默认设置为 `true`（IsActive、IsPublic）
- **计数字段**：默认设置为 `0`（ViewCount、LikeCount）

## 外键约束

- 所有外键使用 `CASCADE` 删除策略
- 删除用户时，自动删除其媒体文件和会话记录

## 迁移脚本

迁移文件位置：`backend/Migrations/20251031101941_InitialCreate.cs`

应用迁移命令：
```bash
dotnet ef database update
```

数据库文件将自动创建在项目根目录：`backend/MediaShareDB.db`

**注意**：SQLite 是轻量级文件数据库，无需安装数据库服务器，数据库文件会自动创建。

## 后续扩展建议

1. **Tags表**：如需要更强大的标签功能，可单独创建标签表
2. **Likes表**：如需要记录点赞用户，可创建点赞关系表
3. **Comments表**：评论功能需要单独的评论表
4. **Collections表**：收藏功能可创建收藏夹表

