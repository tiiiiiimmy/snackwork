# SnackSpot Auckland v2.0 - 页面设计文档

## 1. 页面概览

### 1.1 页面列表

本项目共设计 **14个核心页面**，按功能模块分类：

**认证相关（2页）**
1. 登录页
2. 注册页

**主要功能页（4页）**
3. 首页（推荐信息流 + 搜索发现）
4. 创建零食页
5. 消息页
6. 个人中心页

**内容详情页（3页）**
8. 零食详情页
9. 用户资料页
10. 商店详情页

**游戏化相关（2页）**
11. 排行榜页
12. 成就页

**管理相关（2页）**
13. 编辑个人资料页
14. 管理员后台页（可选，未来扩展）

**其他（1页）**
15. 404错误页

---

## 2. 页面详细设计

### 2.1 认证相关页面

#### 2.1.1 登录页 (`/login`)

**页面描述**：
用户登录入口，支持邮箱/用户名和密码登录。

**页面内容**：
- 页面标题："登录"
- 登录表单：
  - 用户名/邮箱输入框
  - 密码输入框（支持显示/隐藏）
  - "记住我"复选框
  - 登录按钮
  - "忘记密码"链接（未来扩展）
- 底部链接：
  - "还没有账号？立即注册"
- 品牌Logo/名称

**涉及组件**：
```
components/
├── common/
│   ├── Button/              # 登录按钮
│   ├── Input/               # 输入框组件
│   └── Checkbox/            # 记住我复选框
└── layout/
    └── AuthLayout/          # 认证页面布局
```

**API接口**：
```typescript
// services/authService.ts
POST /api/v1/auth/login
Request: {
  username: string;  // 用户名或邮箱
  password: string;
  rememberMe?: boolean;
}
Response: {
  success: boolean;
  data: {
    accessToken: string;
    refreshToken: string;
    user: UserResponse;
  };
}
```

**状态管理**：
- 使用 `AuthContext` 管理登录状态
- 登录成功后跳转到首页或原访问页面

**移动端优化**：
- 表单输入框高度至少44px
- 键盘类型适配（邮箱输入框使用email键盘）
- 登录按钮固定在底部，易于点击

---

#### 2.1.2 注册页 (`/register`)

**页面描述**：
新用户注册页面，收集用户基本信息。

**页面内容**：
- 页面标题："注册"
- 注册表单：
  - 用户名输入框（唯一性验证）
  - 邮箱输入框（唯一性验证）
  - 密码输入框（显示强度提示）
  - 确认密码输入框
  - 用户协议复选框（必选）
  - 注册按钮
- 底部链接：
  - "已有账号？立即登录"
- 品牌Logo/名称

**涉及组件**：
```
components/
├── common/
│   ├── Button/
│   ├── Input/
│   ├── Checkbox/
│   └── PasswordStrength/    # 密码强度指示器
└── layout/
    └── AuthLayout/
```

**API接口**：
```typescript
POST /api/v1/auth/register
Request: {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  acceptTerms: boolean;
  latitude?: number;  // 用于确定初始区域
  longitude?: number;
}
Response: {
  success: boolean;
  data: {
    accessToken: string;
    refreshToken: string;
    user: UserResponse;
  };
}
```

**业务逻辑**：
- 实时验证用户名和邮箱唯一性
- 密码强度验证（至少8位，包含字母和数字）
- 注册成功后自动登录并跳转首页
- 根据位置信息自动推荐给用户可以解锁初始区域，用户也可以选择解锁其他初始区域

---

### 2.2 主要功能页面

#### 2.2.1 首页 - 推荐与发现 (`/`)

**页面描述**：
应用主页面，整合推荐信息流和搜索发现功能。支持在推荐模式和搜索模式之间切换。

**页面内容**：
- 顶部导航栏：
  - Logo/品牌名
  - 搜索栏（支持实时搜索，点击展开搜索模式）
  - 筛选按钮（打开筛选面板）
  - 消息图标（显示未读数，跳转消息页）
  - **偏好筛选按钮**：打开偏好面板，用户可定制个人偏好标签/分类（如无麸质、无乳糖、低糖、素食等），用于个性化推荐和搜索默认过滤
- 内容区域（支持两种模式切换）：
  
  **推荐模式（默认）**：
  - 推荐信息流
  - 下拉刷新功能
  - 无限滚动加载
  - 推荐零食卡片列表
  - 每个卡片显示：
    - 零食图片（懒加载）
    - 零食名称
    - 推荐理由（如"基于您喜欢的分类"）
    - 商店名称
    - 评分和评价数
    - 快速操作按钮（点赞、收藏）
  
  **搜索/浏览模式**（当用户输入搜索关键词或应用筛选时自动切换）：
  - 显示当前筛选条件标签（可快速清除）
  - 零食列表（网格布局，移动端2列）
  - 零食卡片
  - 无限滚动加载
  - 空状态：无搜索结果时的提示
  
- 筛选面板（抽屉式，从右侧滑出）：
  - 区域筛选（显示已解锁区域）
  - 分类筛选
  - 价格范围筛选
  - 评分筛选
  - 排序选项（最新、最高评分、最多评价、推荐度）
  - 偏好标签筛选（无麸质、无乳糖、低糖、素食等）
- 底部导航栏（固定在底部）

**涉及组件**：
```
components/
├── layout/
│   ├── Header/              # 顶部导航（包含搜索栏）
│   └── BottomNav/           # 底部导航
├── feed/
│   ├── HomeFeed/            # 首页内容容器（统一管理推荐/搜索模式）
│   ├── RecommendationFeed/  # 推荐信息流容器
│   ├── SearchResults/       # 搜索结果容器
│   └── FeedItem/            # 单个推荐项
├── snack/
│   └── SnackCard/           # 零食卡片组件
├── filters/
│   ├── FilterDrawer/        # 筛选抽屉面板
│   ├── FilterBar/           # 筛选条件标签栏
│   ├── AreaFilter/          # 区域筛选
│   ├── CategoryFilter/      # 分类筛选
│   ├── PriceRangeFilter/    # 价格筛选
│   └── PreferenceFilter/    # 偏好标签筛选
└── common/
    ├── SearchInput/         # 搜索输入框
    └── ModeToggle/          # 推荐/搜索模式切换（可选）
```

**API接口**：
```typescript
// 推荐模式
// services/recommendationService.ts
GET /api/v1/feed/recommendations?page={page}&limit={limit}&preferences={tags}
Response: {
  success: boolean;
  data: RecommendationItem[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

// 搜索模式
// services/snackService.ts
GET /api/v1/snacks/search?q={keyword}&categoryId={id}&areaId={id}&minPrice={min}&maxPrice={max}&minRating={rating}&preferences={tags}&sort={sort}&page={page}&limit={limit}
Response: {
  success: boolean;
  data: Snack[];
  pagination: Pagination;
}

// 获取所有分类
GET /api/v1/categories

// 获取已解锁区域
GET /api/v1/users/{userId}/unlocked-areas

// 获取偏好标签列表
GET /api/v1/preferences/tags

// 记录浏览行为
POST /api/v1/behaviors/view
Request: {
  snackId: string;
}
```

**状态管理**：
- 页面模式：`'recommendation' | 'search'`（根据是否有搜索关键词/筛选条件自动切换）
- 搜索关键词：本地状态或URL查询参数
- 筛选条件：URL查询参数（支持分享和浏览器前进后退）
- 推荐数据：React Query缓存（15分钟）
- 搜索结果：React Query缓存（5分钟）
- 无限滚动：使用 `useInfiniteQuery`

**业务逻辑**：
- 默认显示推荐模式
- 当用户输入搜索关键词或应用任何筛选条件时，自动切换到搜索模式
- 清除所有搜索和筛选条件后，自动切换回推荐模式
- 推荐模式支持偏好标签过滤（用户设置的偏好会自动应用到推荐算法）
- 搜索模式支持所有筛选条件组合

**移动端优化**：
- 搜索栏支持点击展开全屏搜索
- 筛选面板使用抽屉式设计（从右侧滑出）
- 下拉刷新（推荐模式和搜索模式都支持）
- 图片懒加载
- 卡片点击区域优化（整个卡片可点击）
- 筛选条件标签支持快速清除（点击X）

---

#### 2.2.2 创建零食页 (`/create`)

**页面描述**：
用户创建新零食条目的表单页面。

**页面内容**：
- 页面标题："发布零食"
- 创建表单：
  - 零食名称（必填）
  - 描述（可选，多行文本）
  - 图片上传（最多5张，支持拍照和相册）
  - 分类选择（下拉选择或搜索）
  - 商店选择（搜索商店或创建新商店）
  - 价格输入（可选）
  - 标签输入（可选择已有标签，也可以直接输入创建新标签，最多99个）
- 底部操作栏：
  - 取消按钮
  - 发布按钮（禁用状态直到必填项完成）

**涉及组件**：
```
components/
├── forms/
│   ├── CreateSnackForm/     # 创建零食表单
│   ├── ImageUpload/         # 图片上传组件
│   ├── CategorySelector/    # 分类选择器
│   ├── StoreSelector/       # 商店选择器
│   └── TagInput/            # 标签输入组件
└── common/
    ├── Button/
    └── Input/
```

**API接口**：
```typescript
// services/snackService.ts
POST /api/v1/snacks
Request: {
  name: string;
  description?: string;
  categoryId: string;
  storeId: string;
  price?: number;
  tags?: string[];
  images: File[];  // 多文件上传
}
Response: {
  success: boolean;
  data: Snack;
}

// 创建商店（如果需要）
POST /api/v1/stores
Request: {
  name: string;
  address?: string;
  latitude: number;
  longitude: number;
  description?: string;
}
```

**业务逻辑**：
- 表单验证（使用React Hook Form + Zod）
- 图片压缩和预览
- 商店搜索和创建
- 发布成功后跳转到零食详情页

**移动端优化**：
- 表单输入框大尺寸，易于触摸
- 图片上传支持拍照
- 提交按钮固定在底部

---

#### 2.2.4 消息页 (`/messages`)

**页面描述**：
私信列表页面，显示所有对话。

**页面内容**：
- 页面标题："消息"
- 消息列表：
  - 对话项（显示对方头像、用户名、最后一条消息、时间、未读数）
  - 下拉刷新
  - 点击进入对话详情
- 空状态：
  - 无消息时的提示和引导

**涉及组件**：
```
components/
├── layout/
│   └── Header/
├── social/
│   ├── MessageList/         # 消息列表
│   └── MessageItem/         # 单个对话项
└── common/
    └── Avatar/              # 头像组件
```

**API接口**：
```typescript
// services/messageService.ts
GET /api/v1/messages
Response: {
  success: boolean;
  data: Conversation[];
}

// 获取未读消息数
GET /api/v1/messages/unread-count
Response: {
  success: boolean;
  data: {
    unreadCount: number;
  };
}
```

**对话详情（子页面/模态框）**：
- 消息历史记录（时间倒序）
- 输入框和发送按钮
- 实时更新（WebSocket）

**API接口（对话详情）**：
```typescript
GET /api/v1/messages/{userId}
Response: {
  success: boolean;
  data: Message[];
}

POST /api/v1/messages
Request: {
  receiverId: string;
  content: string;
}
Response: {
  success: boolean;
  data: Message;
}

PUT /api/v1/messages/{messageId}/read
```

**移动端优化**：
- 对话项支持左滑删除
- 输入框固定在底部
- 消息气泡优化显示

---

#### 2.2.5 个人中心页 (`/profile`)

**页面描述**：
用户个人中心，显示个人信息、统计数据、我的内容。

**页面内容**：
- 用户信息卡片：
  - 头像
  - 用户名
  - 等级徽章
  - 经验值进度条
  - 关注/粉丝数
- 统计信息：
  - 发布的零食数
  - 发表的评价数
  - 获得的点赞数
- 功能入口：
  - "我的零食"（跳转到我的零食列表）
  - "我的评价"
  - "我的收藏"（未来扩展）
  - "编辑资料"
  - "设置"（未来扩展）
- 成就展示：
  - 已获得成就列表
- 退出登录按钮

**涉及组件**：
```
components/
├── layout/
│   └── Header/
├── user/
│   ├── UserProfileCard/    # 用户信息卡片
│   ├── LevelBadge/          # 等级徽章
│   ├── ExperienceBar/       # 经验值进度条
│   └── StatsCard/           # 统计卡片
└── gamification/
    └── AchievementList/     # 成就列表
```

**API接口**：
```typescript
// services/userService.ts
GET /api/v1/users/{userId}
Response: {
  success: boolean;
  data: {
    id: string;
    username: string;
    email: string;
    avatarUrl?: string;
    bio?: string;
    level: number;
    experiencePoints: number;
    nextLevelExperience: number;
    followerCount: number;
    followingCount: number;
    snackCount: number;
    reviewCount: number;
    unlockedAreas: Area[];
  };
}

GET /api/v1/users/{userId}/snacks
GET /api/v1/users/{userId}/reviews
GET /api/v1/users/{userId}/achievements
```

**移动端优化**：
- 信息卡片使用卡片式设计
- 功能入口使用图标+文字布局
- 支持下拉刷新

---

### 2.3 内容详情页面

#### 2.3.1 零食详情页 (`/snacks/{id}`)

**页面描述**：
零食详细信息页面，包含图片、描述、评价等。

**页面内容**：
- 顶部导航栏（返回按钮、分享按钮）
- 零食图片轮播（支持缩放）
- 零食信息：
  - 名称
  - 描述
  - 分类标签
  - 商店信息（可点击跳转商店详情）
  - 价格（如有）
  - 评分和评价数
- 操作按钮：
  - 点赞按钮
  - 收藏按钮（未来扩展）
  - 分享按钮
- 评价区域：
  - 评价列表（分页加载）
  - 发表评价表单（已登录用户）
- 相关推荐：
  - 同分类零食推荐
  - 同商店零食推荐

**涉及组件**：
```
components/
├── layout/
│   └── Header/
├── snack/
│   ├── SnackDetail/         # 零食详情容器
│   ├── SnackImageCarousel/  # 图片轮播
│   ├── SnackInfo/           # 零食信息
│   └── SnackActions/        # 操作按钮组
├── review/
│   ├── ReviewList/          # 评价列表
│   ├── ReviewCard/          # 评价卡片
│   └── AddReviewForm/       # 发表评价表单
└── store/
    └── StoreCard/           # 商店卡片（可点击）
```

**API接口**：
```typescript
// services/snackService.ts
GET /api/v1/snacks/{id}
Response: {
  success: boolean;
  data: {
    id: string;
    name: string;
    description?: string;
    category: Category;
    store: Store;
    price?: number;
    averageRating: number;
    totalReviews: number;
    images: string[];
    tags: string[];
    createdAt: string;
    createdBy: User;
  };
}

// 评价相关
GET /api/v1/snacks/{snackId}/reviews?page={page}&limit={limit}
POST /api/v1/snacks/{snackId}/reviews
POST /api/v1/snacks/{snackId}/ratings

// 点赞
POST /api/v1/reviews/{reviewId}/like
```

**业务逻辑**：
- 记录浏览行为（用于推荐系统）
- 评价需要登录
- 已评价用户可以编辑/删除自己的评价
- 图片支持手势缩放

**移动端优化**：
- 图片轮播支持手势滑动
- 评价表单使用底部抽屉
- 相关推荐使用横向滚动

---

#### 2.3.2 用户资料页 (`/users/{id}`)

**页面描述**：
查看其他用户的公开资料和内容。

**页面内容**：
- 用户信息卡片：
  - 头像
  - 用户名
  - 简介
  - 等级和成就
  - 关注/粉丝数
- 操作按钮：
  - 关注/取消关注按钮
  - 私信按钮（如果已关注）
- 内容标签页：
  - "零食"标签：用户发布的零食（网格布局）
  - "评价"标签：用户发表的评价
- 统计信息：
  - 发布的零食数
  - 发表的评价数

**涉及组件**：
```
components/
├── user/
│   ├── UserProfileCard/
│   ├── UserContentTabs/     # 内容标签页
│   └── UserSnackGrid/       # 用户零食网格
├── social/
│   └── FollowButton/        # 关注按钮
└── snack/
    └── SnackCard/
```

**API接口**：
```typescript
GET /api/v1/users/{userId}
GET /api/v1/users/{userId}/snacks
GET /api/v1/users/{userId}/reviews
GET /api/v1/users/{userId}/follow-status
POST /api/v1/users/{userId}/follow
DELETE /api/v1/users/{userId}/follow
```

**业务逻辑**：
- 如果查看自己的资料，显示编辑按钮
- 关注状态实时更新
- 内容按时间倒序排列

---

#### 2.3.3 商店详情页 (`/stores/{id}`)

**页面描述**：
商店详细信息页面，显示商店信息和该商店的零食列表。

**页面内容**：
- 商店信息：
  - 商店名称
  - 地址
  - 图片
  - 位置地图（可选）
  - 描述
- 商店统计：
  - 零食数量
- 零食列表：
  - 该商店的所有零食（网格布局）
  - 支持筛选和排序

**涉及组件**：
```
components/
├── store/
│   ├── StoreDetail/         # 商店详情容器
│   ├── StoreInfo/          # 商店信息
│   └── StoreSnackList/     # 商店零食列表
└── snack/
    └── SnackCard/
```

**API接口**：
```typescript
// services/storeService.ts
GET /api/v1/stores/{id}
Response: {
  success: boolean;
  data: {
    id: string;
    name: string;
    address?: string;
    latitude: number;
    longitude: number;
    area: Area;
    description?: string;
    snackCount: number;
    averageRating: number;
  };
}

GET /api/v1/stores/{id}/snacks?page={page}&limit={limit}
```

---

### 2.4 游戏化相关页面

#### 2.4.1 排行榜页 (`/leaderboards`)

**页面描述**：
显示各种排行榜，用户可以查看排名。

**页面内容**：
- 排行榜类型标签：
  - 贡献度排行榜
  - 活跃度排行榜
  - 影响力排行榜
  - 经验值排行榜
- 排行榜列表：
  - 排名（1-100）
  - 用户头像和用户名
  - 分数/数值
  - 我的排名高亮显示
- 我的排名卡片：
  - 显示当前用户在排行榜中的位置

**涉及组件**：
```
components/
├── gamification/
│   ├── Leaderboard/         # 排行榜容器
│   ├── LeaderboardTabs/     # 排行榜类型标签
│   ├── LeaderboardItem/     # 排行榜项
│   └── MyRankCard/          # 我的排名卡片
└── user/
    └── UserCard/            # 用户卡片
```

**API接口**：
```typescript
// services/leaderboardService.ts
GET /api/v1/leaderboards?type={type}&limit={limit}
Response: {
  success: boolean;
  data: LeaderboardItem[];
}

GET /api/v1/leaderboards/my-rank?type={type}
Response: {
  success: boolean;
  data: {
    rank: number;
    score: number;
  };
}
```

**业务逻辑**：
- 排行榜每小时更新
- 支持下拉刷新
- 我的排名固定在顶部或底部

---

#### 2.4.2 成就页 (`/achievements`)

**页面描述**：
显示所有成就和用户已获得的成就。

**页面内容**：
- 成就分类：
  - 全部
  - 已获得
  - 未获得
- 成就列表：
  - 成就图标
  - 成就名称
  - 成就描述
  - 完成条件
  - 奖励经验值
  - 获得时间（已获得）
  - 进度条（未完成但可显示进度）

**涉及组件**：
```
components/
├── gamification/
│   ├── AchievementList/     # 成就列表
│   ├── AchievementCard/     # 成就卡片
│   └── AchievementProgress/ # 成就进度
└── common/
    └── Badge/               # 徽章组件
```

**API接口**：
```typescript
GET /api/v1/achievements
Response: {
  success: boolean;
  data: Achievement[];
}

GET /api/v1/users/{userId}/achievements
Response: {
  success: boolean;
  data: UserAchievement[];
}
```

---

### 2.5 管理相关页面

#### 2.5.1 编辑个人资料页 (`/profile/edit`)

**页面描述**：
用户编辑个人信息的表单页面。

**页面内容**：
- 编辑表单：
  - 头像上传（支持裁剪）
  - 用户名（唯一性验证）
  - 简介（最大200字符）
  - 邮箱（只读，不可修改）
  - 密码修改（可选）
- 底部操作栏：
  - 取消按钮
  - 保存按钮

**涉及组件**：
```
components/
├── forms/
│   └── EditProfileForm/     # 编辑资料表单
├── common/
│   ├── AvatarUpload/        # 头像上传组件
│   └── Input/
└── layout/
    └── Header/
```

**API接口**：
```typescript
PUT /api/v1/users/{userId}
Request: {
  username?: string;
  bio?: string;
  avatarUrl?: string;
}
Response: {
  success: boolean;
  data: User;
}
```

---

#### 2.5.2 管理员后台页 (`/admin`) - 未来扩展

**页面描述**：
管理员后台管理界面（可选功能，未来版本实现）。

**页面内容**：
- 管理导航：
  - 用户管理
  - 内容管理
  - 区域管理
  - 系统统计
- 数据表格和操作按钮

**注意**：此页面在MVP阶段不实现，仅做规划。

---

### 2.6 其他页面

#### 2.6.1 404错误页 (`/404`)

**页面描述**：
页面未找到的错误提示页。

**页面内容**：
- 404错误提示
- 返回首页按钮
- 友好的错误信息

---

## 3. 组件复用设计

### 3.1 通用组件

以下组件在多个页面中复用：

**布局组件**：
- `Header`：顶部导航栏（用于所有页面）
- `BottomNav`：底部导航栏（用于主要功能页）
- `Layout`：页面布局容器

**内容组件**：
- `SnackCard`：零食卡片（用于首页、用户资料页等）
- `UserCard`：用户卡片（用于排行榜、关注列表等）
- `ReviewCard`：评价卡片（用于零食详情页、用户资料页）

**交互组件**：
- `Button`：按钮（所有页面）
- `Input`：输入框（表单页面）
- `Modal`：模态框（各种确认对话框）
- `Toast`：提示消息（全局使用）

---

## 4. API接口汇总

### 4.1 按功能模块分类

**认证模块**：
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`

**用户模块**：
- `GET /api/v1/users/{userId}`
- `PUT /api/v1/users/{userId}`
- `GET /api/v1/users/{userId}/snacks`
- `GET /api/v1/users/{userId}/reviews`
- `GET /api/v1/users/{userId}/level`
- `GET /api/v1/users/{userId}/unlocked-areas`

**零食模块**：
- `GET /api/v1/snacks`
- `GET /api/v1/snacks/{id}`
- `POST /api/v1/snacks`
- `PUT /api/v1/snacks/{id}`
- `DELETE /api/v1/snacks/{id}`
- `GET /api/v1/snacks/search`

**推荐模块**：
- `GET /api/v1/feed/recommendations`
- `POST /api/v1/behaviors/view`

**评价模块**：
- `GET /api/v1/snacks/{snackId}/reviews`
- `POST /api/v1/snacks/{snackId}/reviews`
- `PUT /api/v1/reviews/{reviewId}`
- `DELETE /api/v1/reviews/{reviewId}`
- `POST /api/v1/reviews/{reviewId}/like`

**商店模块**：
- `GET /api/v1/stores`
- `GET /api/v1/stores/{id}`
- `POST /api/v1/stores`
- `GET /api/v1/stores/{id}/snacks`

**区域模块**：
- `GET /api/v1/areas`
- `GET /api/v1/areas/{areaId}`
- `POST /api/v1/users/{userId}/unlock-area`

**社交模块**：
- `POST /api/v1/users/{userId}/follow`
- `GET /api/v1/users/{userId}/following`
- `GET /api/v1/messages`
- `POST /api/v1/messages`

**游戏化模块**：
- `GET /api/v1/leaderboards`
- `GET /api/v1/achievements`

---

## 5. 路由设计

### 5.1 路由配置

```typescript
// config/routes.ts
export const routes = {
  // 认证
  login: '/login',
  register: '/register',
  
  // 主要功能
  home: '/',  // 首页包含推荐和搜索发现功能
  create: '/create',
  messages: '/messages',
  profile: '/profile',
  
  // 内容详情
  snackDetail: (id: string) => `/snacks/${id}`,
  userProfile: (id: string) => `/users/${id}`,
  storeDetail: (id: string) => `/stores/${id}`,
  
  // 游戏化
  leaderboards: '/leaderboards',
  achievements: '/achievements',
  
  // 管理
  editProfile: '/profile/edit',
  admin: '/admin',
  
  // 其他
  notFound: '/404',
};
```

### 5.2 路由守卫

- **认证守卫**：未登录用户访问需要登录的页面，重定向到登录页
- **权限守卫**：管理员页面需要管理员权限
- **区域守卫**：访问未解锁区域的零食，显示解锁提示

---

## 6. 状态管理设计

### 6.1 全局状态（Context API）

- `AuthContext`：用户认证状态
- `ThemeContext`：主题设置（未来扩展）
- `NotificationContext`：通知消息

### 6.2 服务端状态（React Query）

- 推荐数据
- 零食列表
- 用户数据
- 评价数据
- 排行榜数据

### 6.3 本地状态（useState）

- 表单输入
- UI状态（模态框开关、菜单展开等）
- 筛选条件

---

## 7. 移动端优化要点

### 7.1 导航设计

- **底部导航栏**：固定在底部，用于主要功能页
- **顶部导航栏**：显示标题和操作按钮
- **返回按钮**：详情页左上角

### 7.2 交互优化

- **下拉刷新**：列表页面支持
- **无限滚动**：替代分页按钮
- **手势操作**：左滑删除、右滑返回
- **触摸优化**：按钮最小44px

### 7.3 性能优化

- **图片懒加载**：使用 `react-intersection-observer`
- **代码分割**：按路由分割
- **缓存策略**：React Query缓存

---

## 8. 页面开发优先级

### Phase 1: MVP核心页面（4周）
1. 登录页
2. 注册页
3. 首页（推荐信息流 + 搜索发现）
4. 零食详情页
5. 创建零食页
6. 个人中心页

### Phase 2: 社交功能页面（2周）
7. 用户资料页
8. 消息页

### Phase 3: 游戏化页面（1周）
9. 排行榜页
10. 成就页

### Phase 4: 完善功能（1周）
11. 编辑个人资料页
12. 商店详情页
13. 404错误页

---

**文档版本**: 1.0  
**创建日期**: 2025-01-27  
**最后更新**: 2025-01-27

