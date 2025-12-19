
**主要修订摘要：**

1. **命名死规**：顶层目录强制 `lowercase`，杜绝歧义。
2. **前端架构升级**：
* 新增 `src/queries/` 层（TanStack Query 模式），接管数据状态。
* 明确 `pages/` (路由容器) vs `features/` (业务逻辑) 的边界。
* 收紧 `styles/mobile` 职责，防止样式二义性。
* PWA 增加安全白名单机制。


3. **后端架构闭环**：
* 引入 `Application/EventHandlers`，与 `Services` 分离。
* 明确“同进程最终一致性”策略 + 幂等性要求。
* 细化缓存策略（Key 设计 + 经纬度 Bucket）。
* 完善文件上传的安全与 Nginx 托管策略。



---

# SnackSpot Auckland v2.0 - 文件架构设计文档 (V3.0)

## 1. 项目整体架构

### 1.1 根目录结构与命名

**核心规则**：根目录下的一级子目录必须使用 **全小写 (lowercase)**，无连字符。

```text
snackwork/
├── backend/                    # .NET 9.0 后端项目根目录
├── frontend/                   # React 19 前端项目根目录
├── docs/                       # 文档
├── plan/                       # 规划
├── scripts/                    # 运维与数据库脚本
├── .gitignore
└── README.md

```

### 1.2 子目录命名规范 (严格执行)

* **前端子目录**：`camelCase` (如 `components/snackCard`) 或 `kebab-case` (如 `assets/icon-sets`)，**项目内统一**。
* **后端子目录**：`PascalCase` (如 `Controllers`, `EventHandlers`)，符合 C# 惯例。

---

## 2. 前端文件架构 (`frontend/`)

### 2.1 目录结构

新增 `queries` 数据层，明确 `mobile` 样式用途。

```text
frontend/
├── src/
│   ├── assets/                 # 静态资源
│   ├── components/
│   │   ├── common/             # 原子组件 (Button, Input)
│   │   ├── layout/             # 布局组件
│   │   └── features/           # ⚠️ 业务组件 (只包含UI+局部交互)
│   │       ├── snack/
│   │       └── feed/
│   ├── pages/                  # ⚠️ 路由容器 (只做数据获取+组装)
│   ├── hooks/                  # 通用 UI Hooks (useScroll, useWindowSize)
│   ├── queries/                # 🆕 数据请求层 (React Query keys & fetchers)
│   │   ├── feedQueries.ts      # 包含缓存策略、无限加载逻辑
│   │   └── userQueries.ts
│   ├── services/               # 纯 HTTP 客户端 (Axios 实例, 拦截器)
│   ├── store/                  # 全局 UI 状态 (Zustand/Context, 只有非业务状态)
│   ├── styles/
│   │   ├── mobile/             # ⚠️ 移动端专用变量/Mixins (无组件样式)
│   │   │   ├── _breakpoints.scss
│   │   │   └── _touch.scss
│   │   └── main.scss
│   ├── pwa/                    # PWA 配置
│   │   └── reloadPrompt.tsx    # 版本更新提示组件
│   ├── utils/
│   ├── App.tsx
│   └── main.tsx
└── vite.config.ts              # 集成 vite-plugin-pwa

```
#### 2.1.2 样式目录职责 (`frontend/src/styles/`)

**核心原则**：全局样式目录 **只定义工具，不定义组件**。

```text
frontend/src/styles/
├── mobile/                     # 📱 移动端基础设施 (禁止包含具体组件样式!)
│   ├── _breakpoints.scss       # 断点定义 ($mobile: 320px)
│   ├── _touch.scss             # 触摸规范 ($touch-target-min: 44px)
│   ├── _safe-area.scss         # 刘海屏适配 (env(safe-area-inset-bottom))
│   └── _utilities.scss         # 极简工具类 (仅限 .tap-target, .no-scrollbar)
├── _variables.scss             # 全局颜色、字体变量
├── _mixins.scss                # 通用 Mixins
└── main.scss                   # 全局重置与字体引入

```

### 2.2 关键架构约束

#### 2.2.1 组件 vs 页面 (`Strict Boundary`)

* **`pages/`**：
* **职责**：路由入口、权限检查 (AuthGuard)、调用 `useQuery` 获取数据、组装 Feature 组件。
* **禁止**：写复杂的 JSX 结构（超过 50 行 JSX 必须拆分）、写复杂的 UI 交互逻辑。


* **`components/features/`**：
* **职责**：展示数据、处理用户交互（点击、滑动）、管理局部 UI 状态（展开/收起）。
* **禁止**：直接发起 API 请求（应通过 Props 接收数据或回调函数）。



#### 2.2.2 数据层 (`queries/`)

* **职责**：替代 `Context` 处理服务端状态。管理缓存 Key、失效策略 (Invalidation)、无限滚动游标。
* **示例**：
```typescript
// queries/feedQueries.ts
export const useRecommendationFeed = (lat: number, lng: number) => {
  return useInfiniteQuery({
    queryKey: ['feed', 'reco', { lat: lat.toFixed(2), lng: lng.toFixed(2) }], // 经纬度 bucket
    queryFn: ...
  });
};

```



#### 2.2.3 PWA 安全与更新

* **插件**：使用 `vite-plugin-pwa`。
* **更新策略**：`skipWaiting: true`, `clientsClaim: true`。前端需实现 "New content available, click to reload" Toast 提示。
* **缓存黑名单**：
* ❌ **绝对禁止缓存**：`/api/auth/*`, `/api/users/me`, `/api/messages/*` (涉及个人隐私和鉴权)。
* ✅ **允许缓存**：`/api/categories`, `/api/public/snacks` (公共数据)。



---

## 3. 后端文件架构 (`backend/`)

### 3.1 目录结构

重构 `Services` 层，分离 `Application` 逻辑与 `EventHandlers`。

```text
backend/SnackSpotAuckland.Api/
├── Controllers/
├── Core/                       # 领域层 (无依赖)
│   ├── Entities/
│   ├── Events/                 # 纯 POCO 事件定义 (SnackCreatedEvent.cs)
│   └── Interfaces/
├── Application/                # 应用层 (编排业务)
│   ├── Services/               # 核心业务 (SnackService, AuthService)
│   └── EventHandlers/          # 🆕 事件处理器 (UserXpHandler.cs)
├── Infrastructure/             # 基础设施
│   ├── Data/
│   ├── Caching/                # 缓存实现
│   └── FileStorage/            # 文件服务 (压缩、校验)
├── wwwroot/
│   └── uploads/                # 物理文件存储
└── Program.cs

```

### 3.2 关键架构设计

#### 3.2.1 事件驱动一致性 (`Application/EventHandlers`)

* **模式**：In-Process (进程内) 发布订阅 (使用 MediatR 或类似机制)。
* **一致性承诺**：**最终一致性**。
* 主业务 (创建零食) 成功即返回 200 OK。
* 副作用 (加经验) 异步执行，不阻塞 API 响应。


* **幂等性要求**：Handler 必须处理重复事件。
* *示例*：`UserXpHandler` 在加经验前，先检查 `UserBehaviorLog` 表是否存在该 `SnackId` 的记录。


* **容错**：所有 Handler 必须包裹在 `try-catch` 中，失败记录 Error Log（MVP 阶段暂不引入持久化消息队列，依靠日志补救）。

#### 3.2.2 推荐系统缓存策略 (`Infrastructure/Caching`)

* **Cache Key 设计规范**：
* **Feed**: `feed:reco:{userId}:{page}:{latBucket}:{lngBucket}`
* *LatBucket*: 经纬度保留 2 位小数 (约 1.1km 误差)，避免用户轻微移动导致缓存未命中。


* **Profile**: `user:profile:{userId}`


* **失效策略 (TTL & Invalidation)**：
* Feed TTL: 15 分钟。
* **主动失效**：当用户发生 `Like`, `Review`, `Follow` 行为时，立即清除该用户的 `feed:reco:{userId}:*` 模式下的 Key。



#### 3.2.3 安全的文件上传 (`Infrastructure/FileStorage`)

* **处理流程**：
1. **接收**：Controller 接收 `IFormFile`。
2. **校验**：
* 扩展名白名单 (`.jpg`, `.png`, `.webp`)。
* **Magic Number 校验** (检查文件头字节)。


3. **处理**：使用 `ImageSharp` 调整大小/压缩 (最大 1MB) 并转为 WebP (如支持)。
4. **命名**：生成新 GUID 文件名，**严禁**使用用户原始文件名。
5. **存储**：写入 `wwwroot/uploads/images/{yyyy}/{mm}/{guid}.webp`。


* **Nginx 配置**：
* 生产环境 Nginx 直接 `location /uploads/` 指向物理目录，不经过 .NET 管道，提升性能。



---

## 4. 安全与部署补充

### 4.1 跨域与 Cookie

由于前端 (`snackspot.co.nz`) 与后端 (`api.snackspot.co.nz`) 域名不同：

* **CORS**: 后端必须配置 `WithOrigins(...)` 且 `AllowCredentials = true`。
* **Cookie**: Refresh Token Cookie 必须设置 `SameSite=None; Secure`。

### 4.2 数据库表名

* **强制规则**：MySQL 表名在 Linux 下大小写敏感。
* **规范**：数据库表名统一使用 **`snake_case`** (如 `user_behaviors`)。
* **ORM 映射**：EF Core 实体中使用 `[Table("user_behaviors")]` 显式指定，禁止依赖默认命名约定。

---


#### 4.3 移动端样式开发规范 (**Strict**)

**1. 组件样式归属权**

* ❌ **禁止**：在 `styles/mobile/` 下编写任何具体组件的类名（如 `.snack-card`, `.nav-bar`）。
* ✅ **正确**：所有组件样式（包括移动端适配）必须写在组件自己的 `*.module.scss` 中。

**2. 移动端适配方式**
使用 `styles/mobile/` 提供的变量和 Mixins 在组件内部进行适配。

**示例代码**：

```scss
// ❌ 错误做法：在 styles/mobile/_card.scss 中写
.snack-card {
  width: 100%; // 破坏了组件内聚性
}

// ✅ 正确做法：在 components/snack/SnackCard/SnackCard.module.scss 中引用
@use '@/styles/mobile/touch' as touch;
@use '@/styles/mobile/safe-area' as safe;

.container {
  // 默认即为移动端 (Mobile First)
  width: 100%;
  padding: 1rem;
  
  // 使用全局定义的触摸规范
  .likeButton {
    @include touch.min-target; // 确保至少 44x44px
  }

  // 使用安全区域 Mixin
  padding-bottom: safe.env(safe-area-inset-bottom);

  // 桌面端适配使用媒体查询覆盖
  @media (min-width: 768px) {
    width: 300px;
  }
}

```

**3. `styles/mobile/` 文件内容范例**

* **_touch.scss**:
```scss
@mixin min-target {
  min-width: 44px;
  min-height: 44px;
  cursor: pointer;
}

@mixin no-tap-highlight {
  -webkit-tap-highlight-color: transparent;
}

```


* **_utilities.scss** (克制使用):
```scss
// 仅允许通用的原子类
.tap-target {
  min-width: 44px;
  min-height: 44px;
}

.hide-scrollbar {
  scrollbar-width: none;
  &::-webkit-scrollbar { display: none; }
}

```



---

**文档版本**: 3.0 (Final Architecture)
**修订**: 解决了目录命名歧义，明确了 PWA/缓存安全策略，细化了后端事件一致性和推荐缓存 Key 设计。