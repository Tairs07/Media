# API 接口设计文档

## 基础信息

- **基础URL**: `/api`
- **协议**: HTTPS/HTTP
- **数据格式**: JSON
- **认证方式**: JWT Bearer Token

## 统一响应格式

### 成功响应
```json
{
  "success": true,
  "data": {},
  "message": "操作成功"
}
```

### 错误响应
```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "错误描述"
  }
}
```

## API 端点

### 1. 认证相关 (Auth)

#### 1.1 用户注册
- **POST** `/api/auth/register`
- **请求体**:
```json
{
  "username": "string",
  "email": "string",
  "password": "string"
}
```
- **响应**:
```json
{
  "success": true,
  "data": {
    "token": "string",
    "user": {
      "id": 1,
      "username": "string",
      "email": "string"
    }
  }
}
```

#### 1.2 用户登录
- **POST** `/api/auth/login`
- **请求体**:
```json
{
  "username": "string",
  "password": "string"
}
```
- **响应**: 同注册响应

#### 1.3 获取当前用户信息
- **GET** `/api/auth/me`
- **需要认证**: 是
- **响应**:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "username": "string",
    "email": "string",
    "avatarUrl": "string",
    "bio": "string",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

#### 1.4 刷新Token
- **POST** `/api/auth/refresh`
- **需要认证**: 是
- **响应**: 返回新的token

---

### 2. 媒体文件相关 (Media)

#### 2.1 上传文件
- **POST** `/api/media/upload`
- **需要认证**: 是
- **请求**: multipart/form-data
  - `files`: File[] (多个文件)
  - `description`: string (可选)
  - `tags`: string (可选，逗号分隔)
  - `isPublic`: boolean (可选，默认true)
- **响应**:
```json
{
  "success": true,
  "data": {
    "files": [
      {
        "id": 1,
        "fileName": "string",
        "fileType": "image",
        "filePath": "string",
        "thumbnailPath": "string",
        "fileSize": 1024,
        "width": 1920,
        "height": 1080
      }
    ]
  }
}
```

#### 2.2 获取文件列表
- **GET** `/api/media`
- **查询参数**:
  - `page`: int (页码，默认1)
  - `pageSize`: int (每页数量，默认20)
  - `fileType`: string (可选，image/video)
  - `userId`: int (可选，按用户筛选)
  - `orderBy`: string (可选，uploadedAt/viewCount/likeCount)
  - `order`: string (可选，asc/desc，默认desc)
- **响应**:
```json
{
  "success": true,
  "data": {
    "items": [],
    "total": 100,
    "page": 1,
    "pageSize": 20,
    "totalPages": 5
  }
}
```

#### 2.3 获取文件详情
- **GET** `/api/media/{id}`
- **响应**:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "fileName": "string",
    "fileType": "image",
    "filePath": "string",
    "thumbnailPath": "string",
    "fileSize": 1024,
    "width": 1920,
    "height": 1080,
    "duration": null,
    "description": "string",
    "tags": "tag1,tag2",
    "viewCount": 100,
    "likeCount": 50,
    "isPublic": true,
    "uploadedAt": "2024-01-01T00:00:00Z",
    "user": {
      "id": 1,
      "username": "string",
      "avatarUrl": "string"
    }
  }
}
```

#### 2.4 删除文件
- **DELETE** `/api/media/{id}`
- **需要认证**: 是（仅文件所有者）
- **响应**: 成功消息

#### 2.5 更新文件信息
- **PUT** `/api/media/{id}`
- **需要认证**: 是（仅文件所有者）
- **请求体**:
```json
{
  "description": "string",
  "tags": "string",
  "isPublic": true
}
```

---

### 3. 用户相关 (User)

#### 3.1 获取用户信息
- **GET** `/api/users/{id}`
- **响应**: 用户详细信息

#### 3.2 更新用户信息
- **PUT** `/api/users/{id}`
- **需要认证**: 是（仅本人）
- **请求体**:
```json
{
  "avatarUrl": "string",
  "bio": "string"
}
```

#### 3.3 获取用户上传的文件
- **GET** `/api/users/{id}/media`
- **查询参数**: 同媒体文件列表
- **响应**: 文件列表

---

### 4. 文件访问

#### 4.1 获取文件流
- **GET** `/api/files/{id}`
- **响应**: 文件二进制流

#### 4.2 获取缩略图
- **GET** `/api/files/{id}/thumbnail`
- **响应**: 缩略图二进制流

---

## HTTP 状态码

- `200 OK`: 成功
- `201 Created`: 创建成功
- `400 Bad Request`: 请求参数错误
- `401 Unauthorized`: 未认证
- `403 Forbidden`: 无权限
- `404 Not Found`: 资源不存在
- `500 Internal Server Error`: 服务器错误

## 认证头

需要在请求头中添加：
```
Authorization: Bearer <token>
```

