# SnackSpot Auckland v2.0 — 架构设计文档

**日期**：2026-04-06  
**状态**：已批准  
**规模目标**：日访问 5000+，注册用户 2 万+

---

## 1. 技术选型

| 层面 | 选择 | 说明 |
|---|---|---|
| 后端 | .NET 9 Web API + C# 12 | 固定不可更改 |
| ORM | Entity Framework Core 9 | Code First + Migrations |
| 数据库 | MySQL 8.0+ (InnoDB, utf8mb4) | 主数据存储 |
| 缓存 | Redis | 排行榜、推荐缓存、速率限制、SSE路由 |
| 图片存储 | Cloudflare R2 | 对象存储，预签名直传，自带CDN |
| 实时通知 | SSE（Server-Sent Events） | 私信新消息推送 |
| 前端 | Next.js 15 (App Router) + TypeScript | SSR/ISR/CSR 混合渲染 |
| 表单验证 | React Hook Form + Zod | |
| HTTP 客户端 | Axios | |
| 部署 | 单台 VPS，Nginx 反向代理 | systemd 管理服务 |

---

## 2. 项目结构（Monorepo）

```
snackwork/
├── src/
│   ├── backend/
│   │   └── SnackSpot.Api/
│   │       ├── Controllers/          # 路由入口，参数绑定，响应格式
│   │       │   └── V1/
│   │       ├── Services/             # 业务逻辑，EF Core 查询
│   │       ├── Models/
│   │       │   ├── Entities/         # DB 映射实体
│   │       │   └── DTOs/             # 请求/响应 DTO
│   │       ├── Data/
│   │       │   ├── SnackSpotDbContext.cs
│   │       │   └── Migrations/
│   │       └── Infrastructure/
│   │           ├── Redis/            # StackExchange.Redis
│   │           ├── Storage/          # Cloudflare R2 (AWS SDK S3 兼容)
│   │           └── Sse/              # SSE 连接管理器
│   └── frontend/
│       └── snackspot-web/            # Next.js 15
│           ├── app/                  # App Router 路由
│           ├── components/
│           ├── lib/
│           │   ├── api/              # API client (Axios)
│           │   └── hooks/            # React Query hooks
│           └── public/
├── docs/
│   └── superpowers/specs/
└── spec.md
```

---

## 3. 前端路由 & 渲染策略

| 路由 | 渲染方式 | 原因 |
|---|---|---|
| `/` | CSR | 个性化 Feed，无需 SEO |
| `/discover` | CSR | 实时搜索交互 |
| `/snacks/[id]` | SSR (动态) | SEO，零食详情被搜索引擎收录 |
| `/stores/[id]` | SSR (动态) | SEO |
| `/profile/[userId]` | SSR (动态) | SEO |
| `/messages` | CSR | 私密内容 |
| `/messages/[userId]` | CSR | 私密内容 |
| `/create` | CSR | 需要认证 |
| `/leaderboard` | ISR (1小时重验) | 排行榜每小时更新 |

**底部导航**（移动端固定）：首页 / 发现 / 发布 / 消息 / 我的

---

## 4. 数据库设计

### 4.1 与 spec 的差异

**SnackImages 表** — 去掉 `LONGBLOB`，改为存 URL：

```sql
CREATE TABLE SnackImages (
    Id CHAR(36) PRIMARY KEY,
    SnackId CHAR(36) NOT NULL,
    ImageUrl VARCHAR(500) NOT NULL,
    DisplayOrder INT DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SnackId) REFERENCES Snacks(Id) ON DELETE CASCADE,
    INDEX idx_snack (SnackId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

**新增 UserViewHistory 表** — 推荐系统排除已看内容：

```sql
CREATE TABLE UserViewHistory (
    UserId CHAR(36) NOT NULL,
    SnackId CHAR(36) NOT NULL,
    ViewedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (UserId, SnackId),
    INDEX idx_user_viewed (UserId, ViewedAt),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (SnackId) REFERENCES Snacks(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### 4.2 其余表结构

按 spec.md 第 4 节保持不变（Users, Snacks, Stores, Categories, Reviews, ReviewLikes, Follows, Messages, UserLevels, Leaderboards, Achievements, UserAchievements, SnackTags）。

---

## 5. Redis 使用规划

| Key 模式 | 数据结构 | 用途 | TTL |
|---|---|---|---|
| `rec:{userId}` | String (JSON) | 推荐结果缓存 | 15分钟 |
| `lb:contribution` | Sorted Set | 贡献度排行榜 | 1小时 |
| `lb:active` | Sorted Set | 活跃度排行榜 | 1小时 |
| `lb:influence` | Sorted Set | 影响力排行榜 | 1小时 |
| `lb:exp` | Sorted Set | 经验值排行榜 | 1小时 |
| `sse:{userId}` | String | SSE 连接 ID 路由 | 会话期 |
| `rate:{ip}:{endpoint}` | String (计数) | 速率限制 | 1分钟 |

排行榜用 `ZADD` 写入，`ZREVRANGE` 取 Top 100，`ZREVRANK` 查自己排名，无需遍历 DB。

---

## 6. 图片上传流程（Cloudflare R2）

```
1. 前端选图 → 压缩为 WebP，限制 <1MB
2. POST /api/v1/images/presign
   → 后端验证身份，生成 R2 预签名 PUT URL（有效期 5分钟）
   → 返回 { uploadUrl, imageUrl }
3. 前端直接 PUT 到 R2（不经过 .NET 服务器）
4. 前端拿到 imageUrl，随零食数据一并提交
5. .NET 只做 imageUrl 字符串的 DB 写入
```

R2 Bucket 策略：仅允许 `image/jpeg`、`image/png`、`image/webp`，最大 5MB。

---

## 7. SSE 私信通知流程

```
1. 用户登录后建立 SSE 长连接：
   GET /api/v1/sse/connect (Authorization: Bearer {token})

2. .NET SSE 管理器将 UserId → ConnectionId 写入 Redis

3. 发送者 POST /api/v1/messages
   → 写入 MySQL
   → 从 Redis 查接收方连接 ID
   → 推送 SSE 事件：{ type: "new_message", senderId, preview }

4. 前端收到 SSE 事件 → 更新未读数角标
   → 用户打开对话 → GET /api/v1/messages/{userId} 拉取详情

5. Token 过期或连接断开时，Redis 中的连接记录自动清除
```

**SSE 限制**：每用户最多 3 个并发连接（防多标签页资源浪费）。

---

## 8. 安全设计

### 认证
- Access Token：15分钟有效期（JWT）
- Refresh Token：30天，存 HttpOnly Cookie
- SSE 连接：握手时验证 JWT，过期主动断开

### 速率限制（Redis 实现）
- 全局 API：100次/分钟/IP
- 登录/注册：10次/分钟/IP
- 图片预签名：10次/分钟/用户
- 搜索：30次/分钟/用户
- SSE 连接：3个/用户

### 文件上传
- 类型验证：MIME + 文件头双重校验
- 大小限制：5MB（R2 策略层强制）
- 预签名 URL 5分钟过期

### 其他
- FluentValidation 服务端验证
- 参数化查询防 SQL 注入
- 输出转义防 XSS
- HTTPS 强制 + HSTS
- 严格 CORS（仅允许前端域名）

---

## 9. 部署架构

### VPS 推荐配置
- CPU：2 核
- RAM：4GB
- 存储：40GB SSD（代码 + 日志；图片在 R2）
- OS：Ubuntu 22.04 LTS

### 进程架构
```
Internet
    │
    ▼
Nginx (:80/:443)  ← SSL 终止，静态文件，反代
    ├── /         → Next.js (:3000)        # SSR + 静态
    ├── /api/     → .NET Kestrel (:5000)   # REST API
    └── /sse/     → .NET Kestrel (:5000)   # SSE 长连接

本地（仅 localhost）：
    ├── MySQL (:3306)
    └── Redis (:6379)
```

### systemd 服务
- `snackspot-api.service` — .NET 9 API
- `snackspot-web.service` — Next.js (`next start`)

### Nginx 关键配置
- SSE 端点需关闭 `proxy_buffering`，设置 `X-Accel-Buffering: no`
- 图片静态资源走 R2 CDN，不经过 Nginx
- `client_max_body_size 6m`（预签名接口不需要，但保留余量）

---

## 10. 新增 API 端点

在 spec.md 定义的所有端点基础上，新增：

```
GET  /api/v1/images/presign     # 获取 R2 预签名上传 URL（需认证）
GET  /api/v1/sse/connect        # SSE 长连接入口（需认证）
```

---

## 11. 开发阶段规划

| Phase | 内容 | 关键产出 |
|---|---|---|
| 1 | 后端基础：项目搭建、DB 迁移、JWT 认证 | 可注册登录，API 文档可访问 |
| 2 | 零食 / 商店 / 分类 CRUD + R2 图片上传 | 可发布带图零食 |
| 3 | 评价系统（评分 + 评论 + 点赞） | 完整内容互动 |
| 4 | 关注系统 + Feed + 推荐算法 + Redis | 社交功能闭环 |
| 5 | 游戏化：等级 + 经验值 + 排行榜（Redis Sorted Set） | 激励机制上线 |
| 6 | 私信 + SSE 实时通知 | 消息系统完整 |
| 7 | Next.js 前端（与后端并行可开始） | 移动端 UI 完成 |
| 8 | PWA + 性能优化 + VPS 部署 + 监控 | 正式上线 |
