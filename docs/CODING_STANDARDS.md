
# SnackSpot Auckland v2.0

## 开发规范文档（v1.2 – Production Ready）

**适用范围**：frontend / backend 全代码仓库
**目标**：统一命名、分层、数据流、事件与安全策略，避免隐性架构退化

---

## 1. 命名规范（Naming Conventions）

### 1.1 前端命名规范

#### 1.1.1 文件与目录命名（强制）

* **组件文件**：`PascalCase.tsx`

  ```ts
  SnackCard.tsx
  RecommendationFeed.tsx
  ```

* **目录命名**：**camelCase（项目内统一，不得混用）**

  ```text
  components/features/snack
  components/features/feed
  ```

* **工具 / service / query 文件**：`camelCase.ts`

  ```ts
  snackService.ts
  feedQueries.ts
  queryKeys.ts
  ```

* **类型定义文件**：`camelCase.ts`

  ```ts
  snack.ts
  user.ts
  ```

* **样式文件**：

  * 组件样式：`ComponentName.module.scss`
  * 全局样式：`_variables.scss`

---

### 1.2 后端命名规范

* **类 / 接口 / 文件**：`PascalCase`
* **接口**：`I{PascalCase}`
* **私有字段**：`_camelCase`
* **异步方法**：必须以 `Async` 结尾

---

## 2. 前端代码组织原则（V3.0 对齐）

### 2.1 目录职责划分（强制）

```text
src/
├── components/
│   ├── common/        # 原子 UI
│   ├── layout/        # 布局组件
│   └── features/      # 业务 UI（无数据请求）
├── pages/             # 路由容器
├── queries/           # 服务端状态层（TanStack Query）
├── services/          # 纯 HTTP 客户端
├── hooks/             # UI hooks（无业务）
├── store/             # 全局 UI 状态（非业务）
```

---

### 2.2 严格边界（Strict Boundary）

#### pages/

**允许**

* useQuery / useInfiniteQuery
* 权限校验、路由参数
* 组装 feature 组件

**禁止**

* 复杂 UI（>50 行 JSX 必须拆）
* 直接写业务交互逻辑

#### components/features/

**允许**

* UI 渲染
* 用户交互
* 局部 UI state

**禁止**

* API 请求
* useQuery / useMutation

---

## 3. 数据层规范（Queries Layer）

### 3.1 Queries 职责（强制）

* 管理：

  * Query Keys
  * 缓存策略
  * 分页 / 无限滚动
  * 去重与失效

* 禁止：

  * 在 `hooks/` 或 `components/` 中直接调用 `useQuery`

---

### 3.2 Query Keys 规范（强制）

* Query Key 必须：

  * 可预测
  * 稳定
  * 禁止使用对象引用

```ts
['feed', 'reco', geoKey]
```

---

### 3.3 Query Keys 工厂化（推荐）

```ts
// queries/queryKeys.ts
export const queryKeys = {
  feed: {
    all: ['feed'] as const,
    reco: (geoKey: string) =>
      [...queryKeys.feed.all, 'reco', geoKey] as const,
  },
  user: {
    profile: (userId: string) =>
      ['user', 'profile', userId] as const,
  },
};
```

**禁止**：在业务代码中直接拼写 `'feed'`, `'reco'` 等 magic string。

---

## 4. 样式规范（移动端优先）

### 4.1 styles/mobile 职责（强制）

**只允许**

* 断点变量
* 触摸规范
* 安全区适配
* 极简工具类

**禁止**

* 任何具体组件类名

```scss
// styles/mobile/_touch.scss
@mixin minTarget {
  min-width: 44px;
  min-height: 44px;
}
```

组件适配必须写在自身 `*.module.scss` 中。

---

## 5. PWA 规范（安全强制）

### 5.1 Service Worker 策略

* 使用 `vite-plugin-pwa`
* `skipWaiting: true`
* `clientsClaim: true`
* 前端必须提示「新版本可用，点击刷新」

### 5.2 缓存黑白名单（强制）

**禁止缓存**

* `/api/auth/*`
* `/api/users/me`
* `/api/messages/*`

**允许缓存**

* `/api/categories`
* `/api/public/*`

---

## 6. 后端架构规范（V3.0）

### 6.1 分层结构（强制）

```text
Core/            # 领域（无依赖）
Application/     # 应用服务 + 事件处理
Infrastructure/  # DB / Cache / File
```

---

### 6.2 事件一致性与幂等（强制）

* 模式：In-Process（MediatR）
* 一致性：最终一致性
* 主流程成功即返回
* Handler：

  * 必须幂等
  * 必须 try-catch
  * 禁止影响主事务

#### ⚠️ 重要说明（必须阅读）

> 当前采用进程内事件。
> 若在数据库保存成功后、事件处理前进程宕机，该事件可能丢失。
> MVP 阶段可接受；涉及资金/强一致性数据时，必须升级为 Outbox 或持久化队列。

---

## 7. 数据库命名与 EF Core 映射（v1.2 修订）

### 7.1 数据库命名（强制）

* 表名：`snake_case`
* 列名：`snake_case`
* MySQL（Linux）大小写敏感，禁止 PascalCase

---

### 7.2 EF Core 映射策略（强制）

* **默认**：使用全局 snake_case 命名约定

  ```csharp
  options.UseMySql(...)
         .UseSnakeCaseNamingConvention();
  ```

* **仅在以下情况才允许 `[Table] / [Column]`**：

  1. 兼容旧表
  2. 特殊命名
  3. 视图 / 只读表

* **禁止**为所有属性手写 `[Column]`

---

## 8. 文件上传规范（安全强制）

### 8.1 处理流程

1. Controller 接收 `IFormFile`
2. 校验：

   * 扩展名白名单
   * Magic Number
3. ImageSharp 压缩 / 转 WebP
4. 新 GUID 文件名
5. **存储**

### 8.2 存储规则（v1.2 补漏）

* 路径：

  ```
  wwwroot/uploads/images/{yyyy}/{mm}/{guid}.webp
  ```
* **代码必须自动创建不存在的目录**

  ```csharp
  Directory.CreateDirectory(path);
  ```
* 数据库只存相对路径

---

## 9. 错误处理

* Controller 禁止 try-catch
* 统一使用 ErrorHandlingMiddleware
* 返回统一 error schema

---

## 10. Git 提交规范

遵循 **Conventional Commits**，与 v1.0 相同（略）。

---

## 11. 规范级别声明

* 本文档为 **强制执行规范**
* PR Review 必须以此为准
* 架构变更必须同步更新本文件

---

**文档版本**：v1.2
**状态**：Production Ready
**对齐架构**：Architecture V3.0
**维护原则**：宁可规则少，但必须可执行

