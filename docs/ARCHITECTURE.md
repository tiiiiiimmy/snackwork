# SnackSpot Auckland v2.0 - 文件架构设计文档

## 1. 项目整体架构

### 1.1 项目结构

本项目采用 **Monorepo** 结构，前端和后端代码在同一个仓库中，便于统一管理和版本控制。

```
snackwork/
├── .github/                    # GitHub配置
│   └── workflows/              # CI/CD工作流
├── docs/                       # 项目文档
│   ├── ARCHITECTURE.md         # 架构设计文档（本文件）
│   ├── CODING_STANDARDS.md     # 开发规范文档
│   └── DEPLOYMENT.md           # 部署文档
├── plan/                       # 产品规划和规格文档
│   ├── product-spec-v2.0.md
│   └── recommendation-implementation-v4.md
├── scripts/                    # 脚本文件
│   ├── setup-dev.sh            # 开发环境设置
│   ├── setup-db.sh             # 数据库初始化
│   └── deploy.sh               # 部署脚本
├── src/                        # 源代码目录
│   ├── frontend/               # 前端应用（React + TypeScript）
│   └── backend/                # 后端应用（.NET 9.0）
├── .gitignore                  # Git忽略文件
├── README.md                   # 项目说明
└── LICENSE                     # 许可证
```

### 1.2 目录命名规范

- **小写字母 + 连字符**：用于目录名（如 `src/frontend/`）
- **PascalCase**：用于文件名（如 `UserProfile.tsx`）
- **camelCase**：用于变量和函数名
- **UPPER_CASE**：用于常量

---

## 2. 前端文件架构

### 2.1 前端目录结构

```
src/frontend/
├── public/                     # 静态资源
│   ├── favicon.ico
│   ├── manifest.json           # PWA配置
│   ├── robots.txt
│   └── icons/                  # PWA图标
├── src/
│   ├── assets/                 # 静态资源（图片、字体等）
│   │   ├── images/
│   │   └── fonts/
│   ├── components/             # React组件
│   │   ├── common/             # 通用组件
│   │   │   ├── Button/
│   │   │   │   ├── Button.tsx
│   │   │   │   ├── Button.module.scss
│   │   │   │   └── index.ts
│   │   │   ├── LoadingSpinner/
│   │   │   ├── Modal/
│   │   │   └── Toast/
│   │   ├── layout/             # 布局组件
│   │   │   ├── Header/
│   │   │   ├── Footer/
│   │   │   ├── BottomNav/      # 移动端底部导航
│   │   │   └── Layout.tsx
│   │   ├── snack/              # 零食相关组件
│   │   │   ├── SnackCard/
│   │   │   ├── SnackList/
│   │   │   ├── SnackDetail/
│   │   │   ├── AddSnackForm/
│   │   │   └── EditSnackForm/
│   │   ├── store/              # 商店相关组件
│   │   │   ├── StoreCard/
│   │   │   ├── StoreList/
│   │   │   └── AddStoreForm/
│   │   ├── review/             # 评价相关组件
│   │   │   ├── ReviewCard/
│   │   │   ├── ReviewList/
│   │   │   └── AddReviewForm/
│   │   ├── user/               # 用户相关组件
│   │   │   ├── UserProfile/
│   │   │   ├── UserCard/
│   │   │   └── EditProfileForm/
│   │   ├── feed/               # 信息流组件
│   │   │   ├── RecommendationFeed/
│   │   │   └── FeedItem/
│   │   ├── social/              # 社交功能组件
│   │   │   ├── FollowButton/
│   │   │   ├── FollowList/
│   │   │   └── MessageList/
│   │   └── gamification/       # 游戏化组件
│   │       ├── LevelBadge/
│   │       ├── Leaderboard/
│   │       └── AchievementBadge/
│   ├── pages/                  # 页面组件
│   │   ├── Home.tsx            # 首页（推荐信息流）
│   │   ├── Discover.tsx        # 发现页
│   │   ├── CreateSnack.tsx    # 创建零食页
│   │   ├── Messages.tsx        # 消息页
│   │   ├── Profile.tsx        # 个人中心
│   │   ├── SnackDetail.tsx    # 零食详情
│   │   ├── UserProfile.tsx    # 用户资料
│   │   ├── Login.tsx          # 登录页
│   │   └── Register.tsx       # 注册页
│   ├── hooks/                  # 自定义Hooks
│   │   ├── useAuth.ts
│   │   ├── useSnacks.ts
│   │   ├── useRecommendations.ts
│   │   ├── useInfiniteScroll.ts
│   │   └── useLocation.ts
│   ├── services/               # API服务层
│   │   ├── api.ts              # API客户端配置
│   │   ├── authService.ts
│   │   ├── snackService.ts
│   │   ├── storeService.ts
│   │   ├── reviewService.ts
│   │   ├── userService.ts
│   │   ├── recommendationService.ts
│   │   ├── messageService.ts
│   │   └── leaderboardService.ts
│   ├── context/                # Context API
│   │   ├── AuthContext.tsx
│   │   ├── ThemeContext.tsx
│   │   └── NotificationContext.tsx
│   ├── store/                  # 状态管理（如需要）
│   │   └── index.ts
│   ├── types/                  # TypeScript类型定义
│   │   ├── api.ts              # API响应类型
│   │   ├── snack.ts
│   │   ├── user.ts
│   │   ├── review.ts
│   │   └── common.ts
│   ├── utils/                  # 工具函数
│   │   ├── format.ts           # 格式化函数
│   │   ├── validation.ts      # 验证函数
│   │   ├── date.ts             # 日期处理
│   │   └── constants.ts        # 常量定义
│   ├── styles/                 # 全局样式
│   │   ├── _variables.scss     # SCSS变量
│   │   ├── _mixins.scss        # SCSS混入
│   │   ├── _reset.scss         # 重置样式
│   │   ├── _base.scss          # 基础样式
│   │   ├── _layout.scss        # 布局样式
│   │   └── main.scss           # 主样式文件
│   ├── config/                 # 配置文件
│   │   ├── environment.ts      # 环境配置
│   │   └── routes.ts           # 路由配置
│   ├── App.tsx                 # 根组件
│   ├── main.tsx                # 入口文件
│   └── vite-env.d.ts           # Vite类型定义
├── tests/                      # 测试文件
│   ├── unit/                   # 单元测试
│   ├── integration/            # 集成测试
│   └── e2e/                    # E2E测试
├── .eslintrc.cjs               # ESLint配置
├── .prettierrc                 # Prettier配置
├── index.html                  # HTML模板
├── package.json
├── tsconfig.json               # TypeScript配置
├── tsconfig.app.json
├── tsconfig.node.json
├── vite.config.ts              # Vite配置
└── vitest.config.ts            # Vitest测试配置
```

### 2.2 组件组织原则

**按功能模块组织**：组件按业务功能分组（snack, store, review, user等），每个功能模块包含相关的所有组件。

**组件目录结构**：
```
ComponentName/
├── ComponentName.tsx          # 主组件文件
├── ComponentName.module.scss  # 组件样式（CSS Modules）
├── ComponentName.test.tsx     # 组件测试
└── index.ts                   # 导出文件
```

### 2.3 路由结构

```typescript
// config/routes.ts
export const routes = {
  home: '/',
  discover: '/discover',
  create: '/create',
  messages: '/messages',
  profile: '/profile',
  snackDetail: (id: string) => `/snacks/${id}`,
  userProfile: (id: string) => `/users/${id}`,
  login: '/login',
  register: '/register',
};
```

### 2.4 样式组织

- **全局样式**：`styles/` 目录下的SCSS文件
- **组件样式**：使用CSS Modules（`.module.scss`）
- **移动端优先**：使用移动端断点，然后向上适配

```scss
// styles/_variables.scss
$mobile: 320px;
$mobile-large: 480px;
$tablet: 768px;
$desktop: 1024px;
$desktop-large: 1440px;
```

### 2.5 PWA相关文件

```
public/
├── manifest.json              # PWA清单文件
└── icons/
    ├── icon-192x192.png
    ├── icon-512x512.png
    └── apple-touch-icon.png

src/
└── service-worker.ts          # Service Worker（使用Workbox）
```

---

## 3. 后端文件架构

### 3.1 后端目录结构

```
src/backend/
├── SnackSpotAuckland.Api/     # 主API项目
│   ├── Controllers/           # 控制器
│   │   └── V1/                 # API版本1
│   │       ├── AuthController.cs
│   │       ├── UsersController.cs
│   │       ├── SnacksController.cs
│   │       ├── StoresController.cs
│   │       ├── CategoriesController.cs
│   │       ├── ReviewsController.cs
│   │       ├── RecommendationsController.cs
│   │       ├── MessagesController.cs
│   │       ├── LeaderboardsController.cs
│   │       └── BehaviorsController.cs
│   ├── Models/                # 数据模型
│   │   ├── Entities/          # 实体类（对应数据库表）
│   │   │   ├── User.cs
│   │   │   ├── Snack.cs
│   │   │   ├── Store.cs
│   │   │   ├── Category.cs
│   │   │   ├── Review.cs
│   │   │   ├── ReviewLike.cs
│   │   │   ├── Follow.cs
│   │   │   ├── Message.cs
│   │   │   ├── UserBehavior.cs
│   │   │   ├── UserProfile.cs
│   │   │   ├── SnackTag.cs
│   │   │   ├── Achievement.cs
│   │   │   └── UserAchievement.cs
│   │   └── Enums/             # 枚举类型
│   │       ├── BehaviorType.cs
│   │       └── LeaderboardType.cs
│   ├── DTOs/                  # 数据传输对象
│   │   ├── Requests/          # 请求DTO
│   │   │   ├── CreateSnackRequest.cs
│   │   │   ├── UpdateSnackRequest.cs
│   │   │   ├── CreateReviewRequest.cs
│   │   │   └── LoginRequest.cs
│   │   └── Responses/         # 响应DTO
│   │       ├── SnackResponse.cs
│   │       ├── UserResponse.cs
│   │       ├── RecommendationResponse.cs
│   │       └── ApiResponse.cs
│   ├── Services/              # 业务逻辑服务
│   │   ├── Interfaces/         # 服务接口
│   │   │   ├── IAuthService.cs
│   │   │   ├── ISnackService.cs
│   │   │   ├── IRecommendationService.cs
│   │   │   ├── IUserService.cs
│   │   │   └── IBehaviorTrackingService.cs
│   │   ├── AuthService.cs
│   │   ├── SnackService.cs
│   │   ├── RecommendationService.cs
│   │   ├── UserService.cs
│   │   └── BehaviorTrackingService.cs
│   ├── Data/                  # 数据访问层
│   │   ├── ApplicationDbContext.cs
│   │   ├── Repositories/       # 仓储模式（可选）
│   │   │   ├── IRepository.cs
│   │   │   └── Repository.cs
│   │   └── Seeders/            # 数据种子
│   │       └── DatabaseSeeder.cs
│   ├── Middleware/            # 中间件
│   │   ├── ErrorHandlingMiddleware.cs
│   │   ├── RequestLoggingMiddleware.cs
│   │   ├── RateLimitingMiddleware.cs
│   │   └── SecurityHeadersMiddleware.cs
│   ├── Filters/                # 过滤器
│   │   ├── ValidationFilter.cs
│   │   └── SwaggerOperationFilter.cs
│   ├── Validators/             # FluentValidation验证器
│   │   ├── CreateSnackRequestValidator.cs
│   │   ├── CreateReviewRequestValidator.cs
│   │   └── LoginRequestValidator.cs
│   ├── Mappings/               # AutoMapper配置
│   │   └── MappingProfile.cs
│   ├── Migrations/             # EF Core迁移
│   │   └── [迁移文件]
│   ├── Extensions/             # 扩展方法
│   │   ├── ServiceCollectionExtensions.cs
│   │   └── ApplicationBuilderExtensions.cs
│   ├── Configuration/          # 配置类
│   │   ├── JwtSettings.cs
│   │   └── DatabaseSettings.cs
│   ├── Program.cs               # 应用入口
│   ├── appsettings.json        # 配置文件
│   ├── appsettings.Development.json
│   ├── appsettings.Production.json
│   └── SnackSpotAuckland.Api.csproj
├── SnackSpotAuckland.Tests/    # 测试项目
│   ├── Controllers/
│   │   ├── SnacksControllerTests.cs
│   │   ├── UsersControllerTests.cs
│   │   └── AuthControllerTests.cs
│   ├── Services/
│   │   ├── RecommendationServiceTests.cs
│   │   └── AuthServiceTests.cs
│   ├── Helpers/
│   │   ├── TestDataFactory.cs
│   │   └── TestAuthHelper.cs
│   ├── TestFixtures/
│   │   └── WebApplicationFactoryFixture.cs
│   └── SnackSpotAuckland.Tests.csproj
└── SnackSpotAuckland.sln      # 解决方案文件
```

### 3.2 控制器组织

**按API版本分组**：所有控制器放在 `Controllers/V1/` 目录下，便于未来版本升级。

**命名规范**：
- 控制器名：`[Resource]Controller.cs`（如 `SnacksController.cs`）
- 路由：`/api/v1/[resource]`（如 `/api/v1/snacks`）

### 3.3 服务层组织

**接口和实现分离**：
- 接口定义在 `Services/Interfaces/` 目录
- 实现在 `Services/` 根目录

**服务职责**：
- 业务逻辑处理
- 数据验证
- 调用数据访问层
- 返回DTO对象

### 3.4 数据访问层

**DbContext**：`Data/ApplicationDbContext.cs` 包含所有实体配置。

**迁移文件**：`Migrations/` 目录，按时间戳命名。

---

## 4. 数据库相关

### 4.1 迁移文件组织

```
src/backend/SnackSpotAuckland.Api/
└── Migrations/
    ├── 20250127000000_InitialCreate.cs
    ├── 20250127000000_InitialCreate.Designer.cs
    └── ApplicationDbContextModelSnapshot.cs
```

**命名规范**：`YYYYMMDDHHMMSS_Description.cs`

### 4.2 种子数据

```
src/backend/SnackSpotAuckland.Api/
└── Data/
    └── Seeders/
        ├── DatabaseSeeder.cs
        ├── CategorySeeder.cs
        └── UserLevelSeeder.cs
```

### 4.3 数据库脚本

```
scripts/
└── database/
    ├── init.sql                # 初始化脚本
    ├── seed.sql                # 种子数据脚本
    └── backup.sh               # 备份脚本
```

---

## 5. 配置文件

### 5.1 前端配置

```
src/frontend/
├── vite.config.ts             # Vite配置
├── tsconfig.json              # TypeScript配置
├── .eslintrc.cjs              # ESLint配置
├── .prettierrc                # Prettier配置
└── package.json               # 依赖配置
```

### 5.2 后端配置

```
src/backend/SnackSpotAuckland.Api/
├── appsettings.json           # 基础配置
├── appsettings.Development.json
├── appsettings.Production.json
└── Program.cs                 # 依赖注入配置
```

### 5.3 环境变量

```
.env                           # 本地开发（不提交到Git）
.env.example                   # 环境变量示例（提交到Git）
.env.production               # 生产环境（不提交到Git）
```

---

## 6. 文档结构

```
docs/
├── ARCHITECTURE.md            # 架构设计文档（本文件）
├── CODING_STANDARDS.md        # 开发规范文档
├── DEPLOYMENT.md              # 部署文档
├── API.md                     # API文档
└── CONTRIBUTING.md            # 贡献指南
```

---

## 7. 部署相关

### 7.1 部署脚本

```
scripts/
├── deploy.sh                  # 部署脚本
├── setup-dev.sh               # 开发环境设置
├── setup-db.sh                # 数据库初始化
└── backup.sh                  # 备份脚本
```

### 7.2 Nginx配置

```
scripts/
└── nginx/
    ├── snackspot.conf         # Nginx站点配置
    └── ssl.conf                # SSL配置
```

### 7.3 系统服务配置

```
scripts/
└── systemd/
    └── snackspot-api.service  # systemd服务配置
```

---

## 8. 测试文件组织

### 8.1 前端测试

```
src/frontend/
└── tests/
    ├── unit/                  # 单元测试
    │   ├── components/
    │   ├── hooks/
    │   └── utils/
    ├── integration/            # 集成测试
    └── e2e/                   # E2E测试（Playwright/Cypress）
```

### 8.2 后端测试

```
src/backend/
└── SnackSpotAuckland.Tests/
    ├── Controllers/           # 控制器测试
    ├── Services/              # 服务测试
    ├── Middleware/            # 中间件测试
    └── Helpers/               # 测试辅助类
```

---

## 9. 文件命名规范总结

### 9.1 前端文件命名

- **组件文件**：`PascalCase.tsx`（如 `UserProfile.tsx`）
- **样式文件**：`ComponentName.module.scss`
- **工具文件**：`camelCase.ts`（如 `formatDate.ts`）
- **类型文件**：`camelCase.ts`（如 `user.ts`）
- **常量文件**：`constants.ts` 或 `UPPER_CASE.ts`

### 9.2 后端文件命名

- **类文件**：`PascalCase.cs`（如 `UserService.cs`）
- **接口文件**：`IPascalCase.cs`（如 `IUserService.cs`）
- **DTO文件**：`PascalCaseRequest.cs` 或 `PascalCaseResponse.cs`
- **枚举文件**：`PascalCase.cs`（如 `BehaviorType.cs`）

---

## 10. 目录结构可视化

```
snackwork/
│
├── .github/
│   └── workflows/          # CI/CD
│
├── docs/                    # 📚 文档
│   ├── ARCHITECTURE.md
│   ├── CODING_STANDARDS.md
│   └── DEPLOYMENT.md
│
├── plan/                    # 📋 产品规划
│   ├── product-spec-v2.0.md
│   └── recommendation-implementation-v4.md
│
├── scripts/                 # 🔧 脚本
│   ├── setup-dev.sh
│   ├── setup-db.sh
│   └── deploy.sh
│
└── src/                     # 💻 源代码
    │
    ├── frontend/            # ⚛️ React前端
    │   ├── public/
    │   ├── src/
    │   │   ├── components/  # 组件
    │   │   ├── pages/       # 页面
    │   │   ├── hooks/       # Hooks
    │   │   ├── services/    # API服务
    │   │   ├── types/       # 类型定义
    │   │   ├── utils/       # 工具函数
    │   │   └── styles/      # 样式
    │   └── tests/
    │
    └── backend/             # 🔷 .NET后端
        ├── SnackSpotAuckland.Api/
        │   ├── Controllers/ # 控制器
        │   ├── Models/      # 数据模型
        │   ├── Services/    # 业务服务
        │   ├── Data/        # 数据访问
        │   └── Middleware/ # 中间件
        └── SnackSpotAuckland.Tests/
```

---

## 11. 设计原则

### 11.1 模块化
- 按功能模块组织代码
- 每个模块职责单一
- 模块间低耦合

### 11.2 可维护性
- 清晰的目录结构
- 统一的命名规范
- 完善的文档

### 11.3 可扩展性
- 预留扩展空间
- 支持版本升级（API版本化）
- 易于添加新功能

### 11.4 移动端优先
- 前端组件优先考虑移动端
- 响应式设计
- PWA支持

---

**文档版本**: 1.0  
**创建日期**: 2025-01-27  
**最后更新**: 2025-01-27

