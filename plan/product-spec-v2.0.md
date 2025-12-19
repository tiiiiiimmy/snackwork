---
title: Product Specification for SnackSpot Auckland v2.0
version: 2.0
date_created: 2025-01-27
last_updated: 2025-01-27
owner: SnackSpot Auckland Team
tags: [product-spec, mobile-first, mysql, self-hosted, social, gamification]
---

# SnackSpot Auckland v2.0 - Product Specification

## 1. 产品概述

### 1.1 产品定位

SnackSpot Auckland 是一个社区驱动的零食发现和分享平台，专注于为奥克兰本地社区用户提供发现、分享和评价零食的社交体验。产品通过游戏化机制和社交功能，鼓励用户探索和分享本地美食文化。

### 1.2 目标用户

- **主要用户群体**：奥克兰本地社区用户
- **用户特征**：
  - 年龄：18-45岁
  - 对本地美食文化感兴趣
  - 喜欢分享和发现新事物
  - 活跃的社交媒体用户

### 1.3 核心价值主张

1. **社区驱动**：用户生成内容，真实分享本地零食体验
2. **社交连接**：通过关注、互动建立美食社区
3. **游戏化激励**：等级系统和排行榜鼓励持续参与
4. **本地化聚焦**：专注于奥克兰本地商店和零食

### 1.4 移动端优先策略

**核心原则**：首先实现手机网页版，确保移动端体验完美

- 所有设计决策优先考虑移动端体验
- 响应式设计，移动端优先，桌面端适配
- 触摸交互优化
- 移动端网络环境性能优化
- 支持PWA，可安装到主屏幕

---

## 2. 核心功能模块

### 2.1 社交功能模块

#### 2.1.1 用户关注/粉丝系统

**功能描述**：
- 用户可以关注其他用户
- 显示关注列表和粉丝列表
- 关注/取消关注操作
- 关注状态实时更新

**业务规则**：
- 用户可以关注任意其他用户
- 不能关注自己
- 关注关系是单向的（非互相关注）
- 显示互相关注标识

**API端点**：
```
POST   /api/v1/users/{userId}/follow      # 关注用户
DELETE /api/v1/users/{userId}/follow      # 取消关注
GET    /api/v1/users/{userId}/following   # 获取关注列表
GET    /api/v1/users/{userId}/followers   # 获取粉丝列表
GET    /api/v1/users/{userId}/follow-status # 获取关注状态
```

#### 2.1.2 相关推荐信息流（Recommendations Feed）

**功能描述**：
- 基于用户行为的个性化推荐系统
- 智能推荐用户可能感兴趣的零食
- 结合多种推荐策略（内容推荐、热门推荐、位置推荐）
- 支持下拉刷新和无限滚动

**推荐策略**（采用方案4：简化混合推荐）：
- **用户偏好推荐**（60%权重）：基于用户历史行为（浏览、评价、点赞）分析偏好，推荐相似特征的零食
- **热门推荐**（40%权重）：推荐高评分、高评价数的热门零食，保证内容质量

**实现方案**：采用简化混合推荐系统，快速实现，适合MVP阶段。详细实现指南请参考 `plan/recommendation-implementation-v4.md`

**业务规则**：
- 排除用户已浏览、已评价、已点赞的零食
- 推荐结果按推荐分数排序，分数相同按时间倒序
- 支持显示推荐理由（如"基于您喜欢的分类"）
- 新用户推荐热门内容和高评分内容
- 推荐结果缓存15分钟，用户行为实时更新

**推荐算法**：
- 用户画像：分析用户喜欢的分类、标签、商店、评分偏好
- 相似度计算：基于分类、标签、商店、评分的加权相似度
- 热门度计算：综合评分、评价数、时间衰减
- 位置分数：基于距离计算，5km内满分，20km外为0

**API端点**：
```
GET /api/v1/feed/recommendations?page={page}&limit={limit}&lat={lat}&lng={lng}
```

**响应格式**：
```json
{
  "success": true,
  "data": [
    {
      "snack": { ... },
      "recommendationReason": "基于您喜欢的分类",
      "score": 0.85,
      "source": "content"
    }
  ],
  "pagination": { ... }
}
```

**详细实现方案**：参考 `plan/recommendation-system-options.md`

#### 2.1.3 私信功能

**功能描述**：
- 用户之间可以发送私信
- 支持文字消息
- 消息列表和对话详情
- 未读消息提示

**业务规则**：
- 只能向已关注的用户发送私信（或互相关注）
- 支持消息已读/未读状态
- 消息历史记录保存
- 支持消息删除（仅删除自己的消息）

**API端点**：
```
GET    /api/v1/messages                    # 获取消息列表
GET    /api/v1/messages/{userId}           # 获取与特定用户的对话
POST   /api/v1/messages                    # 发送消息
PUT    /api/v1/messages/{messageId}/read   # 标记为已读
DELETE /api/v1/messages/{messageId}        # 删除消息
GET    /api/v1/messages/unread-count       # 获取未读消息数
```

#### 2.1.4 用户个人资料页面

**功能描述**：
- 显示用户基本信息（头像、用户名、简介）
- 显示用户等级、经验值、成就
- 显示用户发布的零食列表
- 显示用户评价列表
- 显示关注/粉丝数量
- 编辑个人资料功能

**业务规则**：
- 用户可以编辑自己的资料
- 其他用户只能查看公开信息
- 显示用户贡献统计（发布的零食数、评价数等）

**API端点**：
```
GET  /api/v1/users/{userId}              # 获取用户资料
PUT  /api/v1/users/{userId}              # 更新用户资料（仅自己）
GET  /api/v1/users/{userId}/snacks       # 获取用户发布的零食
GET  /api/v1/users/{userId}/reviews       # 获取用户的评价
```

### 2.2 游戏化系统

#### 2.2.1 用户等级系统

**功能描述**：
- 基于用户贡献度的等级系统
- 等级从1级到10级
- 每个等级有对应的经验值要求
- 显示当前等级、经验值和下一级所需经验

**经验值获取规则**：
- 发布零食：+50经验值
- 发布评价：+20经验值
- 获得评价点赞：+5经验值/次
- 获得关注：+10经验值/次
- 每日登录：+5经验值

**等级要求**：
```
等级1: 0-99经验值
等级2: 100-299经验值
等级3: 300-599经验值
等级4: 600-999经验值
等级5: 1000-1499经验值
等级6: 1500-2199经验值
等级7: 2200-2999经验值
等级8: 3000-3999经验值
等级9: 4000-4999经验值
等级10: 5000+经验值
```

**API端点**：
```
GET /api/v1/users/{userId}/level         # 获取用户等级信息
GET /api/v1/levels                        # 获取所有等级配置
```

#### 2.2.2 排行榜

**功能描述**：
- 多种维度的排行榜
- 实时更新排名
- 显示前100名用户

**排行榜类型**：
1. **贡献度排行榜**：基于发布的零食和评价总数
2. **活跃度排行榜**：基于最近30天的活动
3. **影响力排行榜**：基于粉丝数和互动数
4. **经验值排行榜**：基于总经验值

**业务规则**：
- 排行榜每小时更新一次
- 用户可以查看自己在各排行榜中的排名
- 支持查看特定排行榜的详细排名

**API端点**：
```
GET /api/v1/leaderboards?type={type}&limit={limit}  # 获取排行榜
GET /api/v1/leaderboards/my-rank?type={type}         # 获取我的排名
```

#### 2.2.3 功能解锁机制

**功能描述**：
- 高等级用户解锁高级功能
- 解锁提示和引导

**功能解锁规则**：
- **等级2+**：可以创建新分类
- **等级3+**：可以关注其他用户
- **等级5+**：可以发送私信
- **等级7+**：可以创建商店
- **等级10**：解锁所有功能，获得特殊标识

**API端点**：
```
GET /api/v1/users/{userId}/unlocked-features  # 获取已解锁功能
```

### 2.3 内容管理

#### 2.3.1 零食创建、编辑、删除

**功能描述**：
- 用户可以创建零食条目
- 零食所有者可以编辑和删除
- 支持图片上传
- 支持分类和标签

**数据字段**：
- 名称（必填，最大100字符）
- 描述（可选，最大500字符）
- 图片（可选，最多3张）
- 分类（必选）
- 商店（必选）
- 价格（可选）
- 标签（可选，最多5个）

**业务规则**：
- 只有零食创建者可以编辑/删除
- 删除为软删除，30天后物理删除
- 图片大小限制：每张最大5MB，压缩后最大1MB
- 支持图片格式：JPG, PNG, WebP

**API端点**：
```
GET    /api/v1/snacks                      # 获取零食列表
GET    /api/v1/snacks/{id}                 # 获取零食详情
POST   /api/v1/snacks                      # 创建零食
PUT    /api/v1/snacks/{id}                 # 更新零食
DELETE /api/v1/snacks/{id}                 # 删除零食
```

#### 2.3.2 商店/商家管理

**功能描述**：
- 用户可以创建商店信息
- 商店关联到地理位置
- 显示商店的零食列表

**数据字段**：
- 名称（必填，最大80字符）
- 地址（可选，最大120字符）
- 纬度（必填）
- 经度（必填）
- 描述（可选，最大200字符）

**业务规则**：
- 等级7+用户可以创建商店
- 商店创建后不可编辑（防止数据混乱）
- 商店可以软删除（如果没有关联零食）
- 同一名称可以在不同位置存在（如连锁店）

**API端点**：
```
GET    /api/v1/stores?search={keyword}     # 搜索商店
GET    /api/v1/stores/{id}                 # 获取商店详情
POST   /api/v1/stores                      # 创建商店
DELETE /api/v1/stores/{id}                 # 删除商店
GET    /api/v1/stores/{id}/snacks          # 获取商店的零食列表
```

#### 2.3.3 分类系统

**功能描述**：
- 零食分类管理
- 用户可以创建新分类
- 分类列表和筛选

**业务规则**：
- 等级2+用户可以创建分类
- 分类名称唯一（不区分大小写）
- 如果分类没有关联零食，可以删除
- 分类名称自动规范化（去空格、转小写）

**API端点**：
```
GET  /api/v1/categories                   # 获取所有分类
POST /api/v1/categories                   # 创建分类
DELETE /api/v1/categories/{id}            # 删除分类
```

#### 2.3.4 搜索和高级筛选

**功能描述**：
- 全文搜索零食
- 多条件筛选
- 搜索结果排序

**搜索功能**：
- 按零食名称搜索
- 按商店名称搜索
- 按标签搜索

**筛选条件**：
- 分类筛选
- 价格范围筛选
- 评分筛选（最低评分）
- 距离筛选（如果提供位置）

**排序选项**：
- 最新发布
- 最高评分
- 最多评价
- 距离最近（如果提供位置）

**API端点**：
```
GET /api/v1/snacks/search?q={keyword}&category={id}&minPrice={min}&maxPrice={max}&minRating={rating}&sort={sort}&page={page}&limit={limit}
```

### 2.4 评价系统

#### 2.4.1 评分功能

**功能描述**：
- 1-5星评分系统
- 每个零食显示平均评分和总评分数
- 用户只能对同一零食评分一次（可修改）

**业务规则**：
- 评分范围：1-5星
- 用户可以对已评分的零食修改评分
- 删除评分后可以重新评分
- 平均评分保留1位小数

**API端点**：
```
POST   /api/v1/snacks/{snackId}/ratings   # 提交/更新评分
DELETE /api/v1/snacks/{snackId}/ratings   # 删除评分
GET    /api/v1/snacks/{snackId}/ratings   # 获取评分统计
```

#### 2.4.2 文字评论

**功能描述**：
- 用户可以发表文字评论
- 评论可以编辑和删除
- 显示评论时间

**业务规则**：
- 评论长度：10-500字符
- 只有评论作者可以编辑/删除
- 评论支持软删除
- 评论按时间倒序显示

**API端点**：
```
GET    /api/v1/snacks/{snackId}/reviews   # 获取评论列表
POST   /api/v1/snacks/{snackId}/reviews   # 发表评论
PUT    /api/v1/reviews/{reviewId}          # 更新评论
DELETE /api/v1/reviews/{reviewId}          # 删除评论
```

#### 2.4.3 评论互动

**功能描述**：
- 评论点赞功能
- 评论回复功能（可选，未来扩展）

**业务规则**：
- 用户可以点赞/取消点赞评论
- 不能给自己的评论点赞
- 点赞状态实时更新

**API端点**：
```
POST   /api/v1/reviews/{reviewId}/like     # 点赞评论
DELETE /api/v1/reviews/{reviewId}/like     # 取消点赞
```

### 2.5 可选功能

#### 2.5.1 地图展示（可选）

**功能描述**：
- 在地图上显示零食位置
- 标记商店位置
- 地图筛选和搜索

**实现优先级**：低（非核心功能，后续版本实现）

---

## 3. 移动端优先设计

### 3.1 响应式设计

**屏幕适配**：
- 移动端优先：320px - 768px
- 平板适配：768px - 1024px
- 桌面适配：1024px+

**断点定义**：
```scss
$mobile: 320px;
$mobile-large: 480px;
$tablet: 768px;
$desktop: 1024px;
$desktop-large: 1440px;
```

### 3.2 触摸优化

**交互规范**：
- 按钮最小尺寸：44px × 44px
- 触摸目标间距：至少8px
- 支持手势：滑动、长按、下拉刷新
- 防止误触：重要操作需要确认

**手势支持**：
- 左滑：删除/取消关注
- 右滑：返回
- 下拉：刷新内容
- 上拉：加载更多

### 3.3 性能优化

**性能指标**：
- 首屏加载时间：< 2秒（4G网络）
- 页面交互响应：< 100ms
- 图片加载：懒加载，渐进式加载
- 代码分割：按路由分割

**优化策略**：
- 图片压缩和WebP格式
- API响应压缩（Gzip）
- 静态资源CDN
- 服务端渲染关键内容（SSR，可选）

### 3.4 PWA支持

**功能**：
- 可安装到主屏幕
- 离线缓存（Service Worker）
- 推送通知（未来扩展）
- 应用图标和启动画面

**缓存策略**：
- 静态资源：Cache First
- API数据：Network First，失败时使用缓存
- 图片：Cache First，过期时间7天

### 3.5 移动端UI/UX

#### 3.5.1 底部导航栏

**导航项**：
1. **首页**：信息流（Feed）
2. **发现**：搜索和浏览
3. **发布**：创建零食
4. **消息**：私信列表
5. **我的**：个人中心

**设计规范**：
- 固定在底部
- 图标 + 文字标签
- 当前页面高亮
- 消息未读数提示

#### 3.5.2 交互模式

**下拉刷新**：
- 信息流页面支持下拉刷新
- 显示刷新动画和状态

**无限滚动**：
- 列表页面支持无限滚动
- 显示加载状态
- 到达底部自动加载

**移动端表单**：
- 大输入框，易于触摸
- 输入验证实时反馈
- 键盘类型适配（数字、邮箱等）
- 提交按钮固定在底部

**图片上传**：
- 支持拍照和相册选择
- 图片压缩和裁剪
- 上传进度显示
- 多图上传支持

---

## 4. 数据库架构设计

### 4.1 数据库选择

**数据库**：MySQL 8.0+
- 使用InnoDB存储引擎
- 支持事务和外键约束
- UTF8MB4字符集

### 4.2 核心表结构

#### 4.2.1 Users（用户表）

```sql
CREATE TABLE Users (
    Id CHAR(36) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    AvatarUrl VARCHAR(500),
    Bio VARCHAR(200),
    Level INT DEFAULT 1 NOT NULL,
    ExperiencePoints INT DEFAULT 0 NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    IsDeleted TINYINT(1) DEFAULT 0 NOT NULL,
    INDEX idx_username (Username),
    INDEX idx_email (Email),
    INDEX idx_level (Level),
    INDEX idx_experience (ExperiencePoints)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.2 Snacks（零食表）

```sql
CREATE TABLE Snacks (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500),
    CategoryId CHAR(36) NOT NULL,
    StoreId CHAR(36) NOT NULL,
    CreatedByUserId CHAR(36) NOT NULL,
    Price DECIMAL(10,2),
    AverageRating DECIMAL(3,2) DEFAULT 0.00,
    TotalRatings INT DEFAULT 0,
    TotalReviews INT DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    IsDeleted TINYINT(1) DEFAULT 0 NOT NULL,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    FOREIGN KEY (StoreId) REFERENCES Stores(Id),
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id),
    INDEX idx_category (CategoryId),
    INDEX idx_store (StoreId),
    INDEX idx_creator (CreatedByUserId),
    INDEX idx_rating (AverageRating),
    INDEX idx_created (CreatedAt),
    FULLTEXT INDEX idx_search (Name, Description)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.3 SnackImages（零食图片表）

```sql
CREATE TABLE SnackImages (
    Id CHAR(36) PRIMARY KEY,
    SnackId CHAR(36) NOT NULL,
    ImageData LONGBLOB NOT NULL,
    ImageType VARCHAR(20) NOT NULL,
    FileSize INT NOT NULL,
    DisplayOrder INT DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SnackId) REFERENCES Snacks(Id) ON DELETE CASCADE,
    INDEX idx_snack (SnackId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.4 Stores（商店表）

```sql
CREATE TABLE Stores (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(80) NOT NULL,
    Address VARCHAR(120),
    Latitude DECIMAL(9,6) NOT NULL,
    Longitude DECIMAL(9,6) NOT NULL,
    Description VARCHAR(200),
    CreatedByUserId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted TINYINT(1) DEFAULT 0 NOT NULL,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id),
    INDEX idx_location (Latitude, Longitude),
    INDEX idx_name (Name),
    FULLTEXT INDEX idx_search (Name, Address)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.5 Categories（分类表）

```sql
CREATE TABLE Categories (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL,
    Description VARCHAR(200),
    Icon VARCHAR(50),
    CreatedByUserId CHAR(36),
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted TINYINT(1) DEFAULT 0 NOT NULL,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id),
    INDEX idx_name (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.6 Reviews（评价表）

```sql
CREATE TABLE Reviews (
    Id CHAR(36) PRIMARY KEY,
    SnackId CHAR(36) NOT NULL,
    UserId CHAR(36) NOT NULL,
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    Comment VARCHAR(500),
    LikeCount INT DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    IsDeleted TINYINT(1) DEFAULT 0 NOT NULL,
    FOREIGN KEY (SnackId) REFERENCES Snacks(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    UNIQUE KEY uk_snack_user (SnackId, UserId),
    INDEX idx_snack (SnackId),
    INDEX idx_user (UserId),
    INDEX idx_rating (Rating),
    INDEX idx_created (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.7 ReviewLikes（评价点赞表）

```sql
CREATE TABLE ReviewLikes (
    Id CHAR(36) PRIMARY KEY,
    ReviewId CHAR(36) NOT NULL,
    UserId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ReviewId) REFERENCES Reviews(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    UNIQUE KEY uk_review_user (ReviewId, UserId),
    INDEX idx_review (ReviewId),
    INDEX idx_user (UserId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.8 Follows（关注关系表）

```sql
CREATE TABLE Follows (
    Id CHAR(36) PRIMARY KEY,
    FollowerId CHAR(36) NOT NULL,
    FollowingId CHAR(36) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (FollowerId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (FollowingId) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE KEY uk_follow (FollowerId, FollowingId),
    INDEX idx_follower (FollowerId),
    INDEX idx_following (FollowingId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.9 Messages（私信表）

```sql
CREATE TABLE Messages (
    Id CHAR(36) PRIMARY KEY,
    SenderId CHAR(36) NOT NULL,
    ReceiverId CHAR(36) NOT NULL,
    Content VARCHAR(1000) NOT NULL,
    IsRead TINYINT(1) DEFAULT 0 NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeletedBySender TINYINT(1) DEFAULT 0 NOT NULL,
    IsDeletedByReceiver TINYINT(1) DEFAULT 0 NOT NULL,
    FOREIGN KEY (SenderId) REFERENCES Users(Id),
    FOREIGN KEY (ReceiverId) REFERENCES Users(Id),
    INDEX idx_sender (SenderId),
    INDEX idx_receiver (ReceiverId),
    INDEX idx_conversation (SenderId, ReceiverId),
    INDEX idx_created (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.10 UserLevels（用户等级配置表）

```sql
CREATE TABLE UserLevels (
    Level INT PRIMARY KEY,
    MinExperience INT NOT NULL,
    MaxExperience INT NOT NULL,
    Title VARCHAR(50) NOT NULL,
    Description VARCHAR(200),
    UnlockedFeatures JSON,
    INDEX idx_experience (MinExperience, MaxExperience)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.11 Leaderboards（排行榜缓存表）

```sql
CREATE TABLE Leaderboards (
    Id CHAR(36) PRIMARY KEY,
    Type VARCHAR(50) NOT NULL,
    UserId CHAR(36) NOT NULL,
    Score INT NOT NULL,
    Rank INT NOT NULL,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    UNIQUE KEY uk_type_user (Type, UserId),
    INDEX idx_type_rank (Type, Rank),
    INDEX idx_user (UserId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.12 Achievements（成就表）

```sql
CREATE TABLE Achievements (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(200),
    Icon VARCHAR(50),
    ConditionType VARCHAR(50) NOT NULL,
    ConditionValue INT NOT NULL,
    RewardExperience INT DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.13 UserAchievements（用户成就关联表）

```sql
CREATE TABLE UserAchievements (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    AchievementId CHAR(36) NOT NULL,
    UnlockedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (AchievementId) REFERENCES Achievements(Id),
    UNIQUE KEY uk_user_achievement (UserId, AchievementId),
    INDEX idx_user (UserId),
    INDEX idx_achievement (AchievementId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### 4.2.14 SnackTags（零食标签表）

```sql
CREATE TABLE SnackTags (
    Id CHAR(36) PRIMARY KEY,
    SnackId CHAR(36) NOT NULL,
    TagName VARCHAR(50) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SnackId) REFERENCES Snacks(Id) ON DELETE CASCADE,
    INDEX idx_snack (SnackId),
    INDEX idx_tag (TagName)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### 4.3 索引设计

**主要索引策略**：
- 主键索引：所有表的主键
- 外键索引：所有外键字段
- 查询优化索引：常用查询字段（如CreatedAt, Rating等）
- 全文索引：搜索字段（Name, Description等）
- 复合索引：多条件查询（如Type + Rank）

### 4.4 数据关系

**核心关系**：
- Users 1:N Snacks（一个用户可创建多个零食）
- Users 1:N Reviews（一个用户可发表多个评价）
- Users N:M Follows（用户之间的关注关系）
- Users 1:N Messages（用户之间的私信）
- Snacks N:1 Categories（多个零食属于一个分类）
- Snacks N:1 Stores（多个零食属于一个商店）
- Snacks 1:N Reviews（一个零食有多个评价）
- Reviews 1:N ReviewLikes（一个评价有多个点赞）

---

## 5. 技术架构

### 5.1 前端技术栈

**核心框架**：
- React 19+ with TypeScript
- React Router v7（客户端路由）
- Vite（构建工具）

**状态管理**：
- React Context API（全局状态）
- React Query / SWR（服务端状态，可选）

**UI组件库**：
- 自定义组件 + SCSS
- 移动端UI库（可选：Ant Design Mobile, Vant等）

**工具库**：
- Axios（HTTP客户端）
- React Hook Form + Zod（表单验证）
- Date-fns（日期处理）

**移动端优化**：
- react-spring / framer-motion（动画）
- react-intersection-observer（懒加载）
- workbox（PWA Service Worker）

### 5.2 后端技术栈

**核心框架**：
- .NET 9.0 Web API
- C# 12

**数据访问**：
- Entity Framework Core 9.0
- MySQL Connector/NET

**认证授权**：
- JWT Bearer Authentication
- ASP.NET Core Identity（可选）

**其他组件**：
- Swagger/OpenAPI（API文档）
- Serilog（日志）
- FluentValidation（输入验证）

### 5.3 数据库

**数据库**：MySQL 8.0+
- InnoDB存储引擎
- UTF8MB4字符集
- 支持事务和外键约束

### 5.4 部署架构

**自部署方案**：
- 前端：Nginx静态文件服务
- 后端：.NET应用（Kestrel + Nginx反向代理）
- 数据库：MySQL服务器
- 所有服务部署在同一服务器或分布式部署

**服务器要求**：
- 操作系统：Linux (Ubuntu 20.04+ / CentOS 7+)
- 内存：至少2GB（推荐4GB+）
- 存储：至少20GB（推荐50GB+）
- 网络：支持HTTPS（SSL证书）

**部署流程**：
1. 服务器环境准备（.NET Runtime, Node.js, MySQL, Nginx）
2. 数据库初始化（创建数据库、执行迁移）
3. 后端应用部署（编译、配置、启动服务）
4. 前端应用构建和部署（构建静态文件、配置Nginx）
5. SSL证书配置（Let's Encrypt）
6. 监控和日志配置

---

## 6. API设计

### 6.1 API规范

**基础URL**：`https://api.snackspot.co.nz/api/v1`

**认证方式**：JWT Bearer Token
```
Authorization: Bearer {token}
```

**响应格式**：JSON

**HTTP方法**：
- GET：查询数据
- POST：创建资源
- PUT：更新资源
- DELETE：删除资源

### 6.2 通用响应格式

**成功响应**：
```json
{
  "success": true,
  "data": { ... },
  "message": "操作成功"
}
```

**分页响应**：
```json
{
  "success": true,
  "data": [ ... ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 100,
    "totalPages": 5
  }
}
```

**错误响应**：
```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "错误描述",
    "details": { ... }
  }
}
```

### 6.3 主要API端点

#### 6.3.1 认证相关

```
POST   /auth/register          # 注册
POST   /auth/login             # 登录
POST   /auth/refresh            # 刷新Token
POST   /auth/logout             # 登出
```

#### 6.3.2 用户相关

```
GET    /users/{userId}         # 获取用户资料
PUT    /users/{userId}          # 更新用户资料
GET    /users/{userId}/snacks   # 获取用户的零食
GET    /users/{userId}/reviews  # 获取用户的评价
GET    /users/{userId}/level    # 获取用户等级信息
```

#### 6.3.3 关注相关

```
POST   /users/{userId}/follow      # 关注用户
DELETE /users/{userId}/follow      # 取消关注
GET    /users/{userId}/following   # 获取关注列表
GET    /users/{userId}/followers   # 获取粉丝列表
```

#### 6.3.4 信息流

```
GET    /feed                      # 获取信息流
```

#### 6.3.5 零食相关

```
GET    /snacks                    # 获取零食列表
GET    /snacks/{id}               # 获取零食详情
POST   /snacks                    # 创建零食
PUT    /snacks/{id}               # 更新零食
DELETE /snacks/{id}               # 删除零食
GET    /snacks/search             # 搜索零食
```

#### 6.3.6 评价相关

```
GET    /snacks/{snackId}/reviews  # 获取评价列表
POST   /snacks/{snackId}/reviews  # 发表评价
PUT    /reviews/{reviewId}         # 更新评价
DELETE /reviews/{reviewId}         # 删除评价
POST   /reviews/{reviewId}/like    # 点赞评价
DELETE /reviews/{reviewId}/like    # 取消点赞
```

#### 6.3.7 商店相关

```
GET    /stores                    # 搜索商店
GET    /stores/{id}               # 获取商店详情
POST   /stores                    # 创建商店
DELETE /stores/{id}               # 删除商店
GET    /stores/{id}/snacks        # 获取商店的零食
```

#### 6.3.8 分类相关

```
GET    /categories                # 获取所有分类
POST   /categories                # 创建分类
DELETE /categories/{id}           # 删除分类
```

#### 6.3.9 私信相关

```
GET    /messages                  # 获取消息列表
GET    /messages/{userId}         # 获取与特定用户的对话
POST   /messages                  # 发送消息
PUT    /messages/{messageId}/read # 标记为已读
DELETE /messages/{messageId}      # 删除消息
GET    /messages/unread-count     # 获取未读消息数
```

#### 6.3.10 排行榜相关

```
GET    /leaderboards              # 获取排行榜
GET    /leaderboards/my-rank      # 获取我的排名
```

### 6.4 移动端API优化

**分页参数**：
- `page`：页码（从1开始）
- `limit`：每页数量（默认20，最大100）

**字段筛选**：
- `fields`：指定返回字段（逗号分隔）

**响应压缩**：
- 启用Gzip压缩
- 移动端优先返回必要字段

---

## 7. 安全要求

### 7.1 认证和授权

**JWT认证**：
- Access Token：15分钟有效期
- Refresh Token：30天有效期，存储在HttpOnly Cookie
- Token刷新机制：自动刷新即将过期的Token

**密码安全**：
- 使用BCrypt加密存储
- 密码强度要求：至少8位，包含字母和数字
- 密码重置功能（未来扩展）

### 7.2 输入验证

**验证规则**：
- 所有用户输入必须验证
- 使用FluentValidation进行服务端验证
- 防止SQL注入（使用参数化查询）
- 防止XSS攻击（输入转义）

**文件上传安全**：
- 文件类型验证（MIME类型 + 文件头）
- 文件大小限制（5MB）
- 图片压缩和病毒扫描（未来扩展）

### 7.3 速率限制

**限制规则**：
- 全局API：100请求/分钟/IP
- 认证相关：10请求/分钟/IP
- 图片上传：5请求/秒/用户
- 搜索API：30请求/分钟/用户

### 7.4 数据保护

**敏感数据**：
- 密码：BCrypt加密
- 邮箱：可加密存储（可选）
- 日志：PII数据脱敏

**HTTPS**：
- 强制HTTPS
- HSTS头设置
- 安全Cookie设置

**CORS**：
- 严格CORS策略
- 仅允许指定域名

---

## 8. 部署架构

### 8.1 服务器架构

**单服务器部署**（初期）：
```
┌─────────────────────────────────┐
│        Nginx (80, 443)          │
│  ┌──────────┐  ┌──────────────┐ │
│  │ Frontend │  │ Reverse Proxy│ │
│  │ (Static) │  │  to Backend  │ │
│  └──────────┘  └──────┬───────┘ │
└───────────────────────┼─────────┘
                        │
            ┌───────────▼──────────┐
            │  .NET API (Kestrel)  │
            │    (localhost:5000)   │
            └───────────┬──────────┘
                        │
            ┌───────────▼──────────┐
            │   MySQL Database     │
            │    (localhost:3306)  │
            └──────────────────────┘
```

**分布式部署**（扩展）：
- 前端：CDN或独立服务器
- 后端：应用服务器集群
- 数据库：主从复制

### 8.2 部署流程

**1. 环境准备**：
```bash
# 安装.NET Runtime
# 安装Node.js
# 安装MySQL
# 安装Nginx
# 配置防火墙
```

**2. 数据库初始化**：
```bash
# 创建数据库
# 执行EF Core迁移
# 初始化种子数据
```

**3. 后端部署**：
```bash
# 构建应用
dotnet publish -c Release
# 配置appsettings.json
# 配置systemd服务
# 启动服务
```

**4. 前端部署**：
```bash
# 构建前端
npm run build
# 复制到Nginx目录
# 配置Nginx
```

**5. SSL配置**：
```bash
# 使用Let's Encrypt
certbot --nginx -d snackspot.co.nz
```

### 8.3 监控和日志

**应用监控**：
- 健康检查端点：`/health`
- 性能监控（可选：Application Insights替代方案）
- 错误追踪（可选：Sentry）

**日志管理**：
- 结构化日志（Serilog）
- 日志文件轮转
- 错误日志告警（可选）

**数据库监控**：
- 慢查询日志
- 连接池监控
- 备份监控

### 8.4 备份策略

**数据库备份**：
- 每日自动备份
- 保留最近30天备份
- 定期恢复测试

**应用备份**：
- 配置文件备份
- 上传文件备份（如果有）

---

## 9. 开发计划

### 9.1 开发阶段

**Phase 1: 基础功能（4周）**
- 用户认证系统
- 零食CRUD
- 商店管理
- 分类系统
- 基础UI框架

**Phase 2: 社交功能（3周）**
- 关注系统
- 信息流
- 私信功能
- 用户资料

**Phase 3: 游戏化系统（2周）**
- 等级系统
- 排行榜
- 功能解锁

**Phase 4: 评价系统（2周）**
- 评分功能
- 评论功能
- 评论互动

**Phase 5: 移动端优化（2周）**
- 响应式设计
- 触摸优化
- PWA支持
- 性能优化

**Phase 6: 测试和部署（2周）**
- 单元测试
- 集成测试
- 部署上线
- 监控配置

### 9.2 里程碑

- **M1**: 基础功能完成，可发布零食
- **M2**: 社交功能完成，用户可互动
- **M3**: 游戏化系统上线，用户有动力
- **M4**: 移动端优化完成，体验流畅
- **M5**: 正式上线，稳定运行

---

## 10. 验收标准

### 10.1 功能验收

- [ ] 用户可以注册、登录、登出
- [ ] 用户可以创建、编辑、删除零食
- [ ] 用户可以关注其他用户
- [ ] 用户可以查看信息流
- [ ] 用户可以发送和接收私信
- [ ] 用户可以评价零食
- [ ] 等级系统正常工作
- [ ] 排行榜正常更新
- [ ] 搜索和筛选功能正常

### 10.2 性能验收

- [ ] 首屏加载时间 < 2秒（4G网络）
- [ ] API响应时间 < 200ms（平均）
- [ ] 图片加载优化，支持懒加载
- [ ] 移动端交互流畅，无卡顿

### 10.3 安全验收

- [ ] JWT认证正常工作
- [ ] 输入验证有效
- [ ] 速率限制生效
- [ ] HTTPS配置正确
- [ ] 敏感数据加密存储

### 10.4 移动端验收

- [ ] 移动端UI完整，体验良好
- [ ] 触摸交互友好
- [ ] PWA可安装
- [ ] 响应式设计适配不同屏幕
- [ ] 性能指标达标

---

## 11. 附录

### 11.1 术语表

- **Feed**: 信息流，基于关注关系的动态内容流
- **PWA**: Progressive Web App，渐进式网页应用
- **JWT**: JSON Web Token，用于认证的令牌
- **CRUD**: Create, Read, Update, Delete，基本数据操作
- **软删除**: 逻辑删除，数据标记为删除但不物理删除

### 11.2 参考文档

- React官方文档：https://react.dev/
- .NET官方文档：https://learn.microsoft.com/dotnet/
- MySQL官方文档：https://dev.mysql.com/doc/
- PWA指南：https://web.dev/progressive-web-apps/

---

**文档版本**: 2.0  
**最后更新**: 2025-01-27  
**维护者**: SnackSpot Auckland Team

