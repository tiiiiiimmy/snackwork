
# SnackSpot Auckland v2.0 - 开发规范文档 (v1.1)

> 与《文件架构设计文档 V3.0》完全一致
> **更新重点**：统一目录命名（frontend 子目录 camelCase）、引入 `queries/`（TanStack Query 模式）、明确 `pages vs features` 边界、PWA 缓存白名单/黑名单、后端 Core/Application/Infrastructure 分层、事件最终一致性+幂等、文件上传安全闭环、DB snake_case 强制。

---

## 0. 项目命名死规（必须遵守）

### 0.1 根目录（Monorepo 顶层）

* 根目录一级子目录必须 **lowercase**（无连字符）

  * ✅ `backend/`, `frontend/`, `docs/`, `plan/`, `scripts/`
  * ❌ `front-end/`, `BackEnd/`

### 0.2 前端子目录命名（统一 camelCase）

* `frontend/src/` 下所有目录统一 **camelCase**

  * ✅ `components/features/`, `components/common/`, `styles/mobile/`（mobile 本身可保留作为固定名）
  * ✅ `components/snackCard/`（示例）
  * ❌ `snack-card/`, `SnackCard/`（目录）

> 注：文件命名仍按组件 PascalCase / 工具 camelCase 等规则执行。

### 0.3 后端子目录命名（PascalCase）

* `backend/SnackSpotAuckland.Api/` 下目录统一 **PascalCase**

  * ✅ `Controllers/`, `Core/`, `Application/`, `Infrastructure/`

---

## 1. 命名规范

### 1.1 前端命名规范（React + TypeScript）

#### 1.1.1 文件命名

* 组件文件：`PascalCase.tsx`

  * ✅ `SnackCard.tsx`, `RecommendationFeed.tsx`
* 样式文件：`ComponentName.module.scss`

  * ✅ `SnackCard.module.scss`
* 工具文件：`camelCase.ts`

  * ✅ `formatDate.ts`, `buildGeoKey.ts`
* Query 文件：`camelCase.ts`

  * ✅ `feedQueries.ts`, `userQueries.ts`
* 类型文件：`camelCase.ts`

  * ✅ `snack.ts`, `user.ts`
* 常量文件：统一 `constants.ts`（推荐），或 `UPPER_CASE.ts`（仅用于极少数全局常量）

  * ✅ `constants.ts`

#### 1.1.2 变量/函数/组件命名

* 变量/函数：`camelCase`
* React 组件：`PascalCase`
* 常量：`UPPER_SNAKE_CASE`

#### 1.1.3 类型/接口

* interface/type：`PascalCase`
* enum：`PascalCase`，成员 `UPPER_SNAKE_CASE`

#### 1.1.4 SCSS 命名

* CSS Modules 内 class 建议用 `camelCase`（因为通过 `styles.xxx` 引用更自然）

  * ✅ `.likeButton {}` → `styles.likeButton`
* 全局工具类（仅允许在 `styles/mobile/_utilities.scss`）：`kebab-case`

  * ✅ `.tap-target`, `.hide-scrollbar`

> 约束：组件样式必须在 `*.module.scss` 内；全局 styles 目录禁止出现业务组件 class（见 §4）。

---

### 1.2 后端命名规范（.NET 9 / C#）

* 类/文件：`PascalCase.cs`
* 接口：`I + PascalCase`
* 方法/属性：`PascalCase`
* 私有字段：`_camelCase`
* 异步方法必须 `Async` 结尾

---

## 2. 代码组织原则（与 V3.0 对齐）

### 2.1 前端组织原则（Strict）

#### 2.1.1 目录结构约束（摘要）

```
frontend/src/
  components/
    common/
    layout/
    features/
  pages/
  queries/
  services/
  store/
  hooks/
  styles/
  pwa/
```

#### 2.1.2 `pages/` vs `components/features/` 边界（强制）

**`pages/`（路由容器）**

* ✅ 允许：

  * 路由入口、AuthGuard
  * 调用 `queries/` 导出的 hooks（`useQuery/useInfiniteQuery`）
  * 组装 Feature 组件、传 props
* ❌ 禁止：

  * 直接调用 `services/` 发请求
  * 写复杂交互逻辑
  * JSX 过长（**超过 50 行 JSX**：必须拆分到 features；布局型页面可例外但不得写业务交互）

**`components/features/`（业务 UI + 局部交互）**

* ✅ 允许：

  * 展示数据、处理点击/滑动/展开收起
  * 局部 UI 状态（open/close、selected tab）
* ❌ 禁止：

  * 直接发 API 请求（必须通过 props/回调触发，由 page/queries 处理）
  * 在组件内部创建 React Query 实例

#### 2.1.3 `hooks/` 的定位（强制）

* `hooks/` 只放 **通用 UI hooks**（如 `useScroll`, `useWindowSize`）
* ❌ 禁止在 `hooks/` 中直接调用 `useQuery` / `useInfiniteQuery`

  * 所有数据请求必须在 `queries/` 层定义

---

### 2.2 数据层规范：`queries/`（TanStack Query 模式）

#### 2.2.1 总原则

* `services/`：**纯 HTTP**（axios 实例、拦截器、REST 方法封装）
* `queries/`：**服务端状态**（Query Keys、缓存策略、失效策略、分页/无限滚动）
* `pages/`：只消费 `queries/` 暴露的 hooks 并渲染

#### 2.2.2 Query Key 规范（强制）

* Key 必须由：`[domain, resource, ...params]` 组成
* **禁止**在 queryKey 中放不稳定对象（避免引用导致 cache miss）

  * ✅ 用字符串/数字/boolean
  * ✅ 用 bucket 字符串 `geoKey`
  * ❌ `queryKey: ['feed', { lat, lng }]`

推荐：

```ts
// utils/buildGeoKey.ts
export const buildGeoKey = (lat: number, lng: number) =>
  `${lat.toFixed(2)}:${lng.toFixed(2)}`; // ~1.1km bucket
```

#### 2.2.3 Feed 无限滚动规范（示例）

```ts
// queries/feedQueries.ts
import { useInfiniteQuery } from '@tanstack/react-query';
import { feedService } from '@/services/feedService';
import { buildGeoKey } from '@/utils/buildGeoKey';

export const useRecommendationFeedQuery = (lat: number, lng: number) => {
  const geoKey = buildGeoKey(lat, lng);

  return useInfiniteQuery({
    queryKey: ['feed', 'reco', geoKey],
    queryFn: ({ pageParam = 1 }) =>
      feedService.getRecommendations({ page: pageParam, lat, lng }),
    getNextPageParam: (lastPage) => lastPage.pagination?.nextPage ?? undefined,
    staleTime: 15 * 60 * 1000, // 15 min
  });
};
```

#### 2.2.4 失效策略（Invalidation）约束

* 当用户发生 `Like/Review/Follow` 行为后：

  * 必须 `invalidateQueries(['feed','reco', ...])`（至少该用户相关 feed）
* `Follow` 行为需同时考虑：

  * 发起方：feed 失效
  * 被关注方：profile/leaderboard（若缓存）失效

---

## 3. 导入/引用规范

### 3.1 前端导入顺序

1. React / Router
2. 第三方库
3. 内部模块（services/queries/utils/components）
4. types
5. styles

### 3.2 后端 using 顺序

1. System
2. Microsoft
3. 第三方库
4. 项目内部命名空间

---

## 4. 样式规范（与 V3.0 对齐，Strict）

### 4.1 全局 styles 目录职责

`frontend/src/styles/` 只允许：

* 变量（colors/fonts）
* mixins
* mobile 基础设施（breakpoints/touch/safe-area）
* **极少**工具类（utilities）

❌ 禁止：

* 在 `styles/mobile/` 下写任何具体组件 class（如 `.snackCard`, `.navBar`）

### 4.2 组件样式归属权

* 所有组件样式必须在组件自身 `*.module.scss` 中完成（包括移动端适配）
* 移动端适配通过 `@use '@/styles/mobile/touch'` 等 mixins/变量完成

---

## 5. PWA 规范（安全优先）

### 5.1 工具链

* 使用 `vite-plugin-pwa`
* 更新策略：`skipWaiting: true`, `clientsClaim: true`
* 必须提供更新提示 UI（如 `pwa/reloadPrompt.tsx`）

### 5.2 缓存白名单/黑名单（强制）

* ❌ 禁止缓存（任何涉及鉴权/隐私）：

  * `/api/auth/*`
  * `/api/users/me`
  * `/api/messages/*`
  * 以及任何需要 `Authorization` 或 `credentials: include` 的请求
* ✅ 允许缓存（公共数据）：

  * `/api/categories`
  * `/api/public/snacks`（或明确 public 前缀的接口）

> Service Worker 里必须显式忽略 `request.credentials === 'include'` 的请求。

---

## 6. 后端分层与事件规范（与 V3.0 对齐）

### 6.1 分层约束（强制）

* `Core/`：领域层（无外部依赖），包含 Entities/Interfaces/Events（POCO）
* `Application/`：应用层（业务编排），包含 Services 与 EventHandlers
* `Infrastructure/`：基础设施（Data/Caching/FileStorage），可依赖外部库/数据库/文件系统
* `Controllers/`：仅做协议层（HTTP），不写业务规则

### 6.2 事件一致性与幂等（强制）

* 模式：In-Process 发布订阅（MediatR 或同类）
* 一致性：最终一致性（主业务成功即返回 200，副作用异步执行）
* ❌ 禁止 fire-and-forget（例如 `Task.Run()` / 不 await 的后台任务）
* ✅ Handler 必须：

  * try-catch 捕获异常并记录日志
  * 幂等：通过 `user_behavior_log`（或同类表）检查/唯一键防重复

建议 DB 级唯一约束（示例）：

* `(user_id, target_id, behavior_type)` UNIQUE

---

## 7. 数据库命名规范（强制）

> Linux 下 MySQL 表名大小写敏感，禁止 PascalCase 表名。

* 表名：`snake_case`（单数/复数任选，但项目内必须统一；推荐复数：`users`, `snacks`）
* 列名：`snake_case`（如 `created_at`, `user_id`）
* EF Core 映射必须显式指定：

  * `[Table("user_behaviors")]`
  * `[Column("created_at")]`
* 禁止依赖默认命名约定

---

## 8. 文件上传规范（安全闭环）

### 8.1 后端处理流程（强制）

1. Controller 接收 `IFormFile`
2. 校验：

   * 扩展名白名单（`.jpg`, `.png`, `.webp`）
   * Magic Number 校验（文件头字节）
3. 处理：

   * ImageSharp resize/压缩（最大 1MB）
   * 优先转 WebP
4. 命名：

   * 生成 GUID 文件名（严禁使用用户原始文件名）
5. 存储：

   * `wwwroot/uploads/images/{yyyy}/{mm}/{guid}.webp`
6. DB：

   * 仅存相对路径：`/uploads/images/...`

### 8.2 Nginx 托管（推荐）

* `location /uploads/` 直接由 Nginx 静态服务
* 建议设置正确 MIME 与缓存头：

  * `Cache-Control: public, max-age=31536000, immutable`

---

## 9. 错误处理规范

### 9.1 前端

* UI 层只负责展示错误（toast/banner）
* 业务错误/状态由 queries 管理（React Query 的 `error`, `isFetching`, `isLoading`）

### 9.2 后端（强制统一）

* 默认不在 Controller 写 try-catch
* 统一由 `ErrorHandlingMiddleware` 输出标准错误响应格式（与 API 规范一致）
* 仅对“可预期业务错误”抛出自定义异常（如 NotFoundException），由中间件映射为 404

---

## 10. Git 提交规范（Conventional Commits）

保持 v1.0 的规范不变（可继续使用）。

---

## 11. 测试规范（与架构一致）

### 11.1 前端

* UI 组件：Testing Library
* Queries：可通过 MSW/mock service 测试 key/分页/失效逻辑（推荐）

### 11.2 后端

* Unit：Application Services / EventHandlers（mock Infra）
* Integration：WebApplicationFactory 全链路（Controller→App→Infra）

---

**文档版本**: 1.1
**创建日期**: 2025-01-27
**最后更新**: 2025-01-27
**对齐架构版本**: 文件架构设计文档 V3.0

