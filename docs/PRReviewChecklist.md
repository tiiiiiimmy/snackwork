
# ✅ PR Review Checklist

**SnackSpot Auckland v2.0**

> Reviewer：请只勾选“你确认无问题的项”。
> 若某一大类不适用（如无前端/无后端），请标注 N/A。

---

## 0️⃣ 基础要求（所有 PR 必须满足）

* [ ] PR 标题符合 **Conventional Commits**

  * `feat(frontend): ...`
  * `fix(backend): ...`
* [ ] PR 只做一件事（无明显 scope creep）
* [ ] 无无关文件改动（如格式化、未使用代码）
* [ ] 所有 CI 检查通过

---

## 1️⃣ 前端架构与目录（Frontend Architecture）

### 1.1 目录与职责边界

* [ ] 新增/修改文件放在**正确目录**

  * UI → `components/features/`
  * 路由容器 → `pages/`
  * 数据请求 → `queries/`
  * HTTP → `services/`
* [ ] **`components/features/` 中没有直接调用 API**
* [ ] **`pages/` 中没有复杂 UI（>50 行 JSX）**

🚫 **高危反例（必须打回）**

* 在 feature 组件里 `useQuery`
* 在 hooks 里写分页/缓存逻辑
* 在 pages 里堆 UI + 业务逻辑

---

## 2️⃣ Queries / 数据层（TanStack Query）

### 2.1 数据获取方式

* [ ] 所有 `useQuery / useInfiniteQuery / useMutation` 均位于 `queries/`
* [ ] 没有使用 `useEffect + service` 拉取服务端数据
* [ ] 分页 / 无限滚动使用 `useInfiniteQuery`

---

### 2.2 Query Keys（强制）

* [ ] Query Key **稳定、可预测**
* [ ] 未使用对象引用作为 key
* [ ] 使用 `queryKeys` 工厂（如适用）
* [ ] invalidateQueries 使用的是工厂方法，而非硬编码字符串

🚫 **高危反例**

```ts
useQuery(['feed', { lat, lng }]) // ❌
invalidateQueries(['feed', 'reco']) // ❌ magic string
```

---

## 3️⃣ Services 层（HTTP 客户端）

* [ ] `services/` 中只包含 HTTP 调用
* [ ] 未在 service 中引入缓存、分页状态、业务规则
* [ ] Axios 拦截器未包含 UI / Toast / Router 逻辑

---

## 4️⃣ 样式与移动端（Styles & Mobile）

### 4.1 组件样式

* [ ] 所有组件样式均在 `*.module.scss`
* [ ] 未在 `styles/mobile/` 中写任何组件类名
* [ ] 移动端适配通过 mixin / 变量完成

### 4.2 触摸与可用性

* [ ] 可点击元素满足 **最小 44×44px**
* [ ] 未引入重复 / 冗余工具类

---

## 5️⃣ PWA & 前端安全（如涉及）

* [ ] Service Worker 未缓存：

  * `/api/auth/*`
  * `/api/users/me`
  * `/api/messages/*`
* [ ] 新增缓存规则不会存储鉴权或隐私数据
* [ ] 有版本更新提示（如修改 SW）

---

## 6️⃣ 后端分层与依赖（Backend Architecture）

### 6.1 分层正确性

* [ ] `Core/` 无任何基础设施依赖
* [ ] `Application/` 负责编排业务
* [ ] `Infrastructure/` 实现 DB / Cache / File
* [ ] Controller 只做请求/响应映射

🚫 **高危反例**

* Controller 写业务逻辑
* Core 依赖 EF / Redis / File IO

---

## 7️⃣ 事件系统（Event Handlers）

* [ ] 事件定义在 `Core/Events`
* [ ] Handler 位于 `Application/EventHandlers`
* [ ] Handler 实现 **幂等性**
* [ ] Handler 有 try-catch 且不影响主流程

⚠️ Reviewer 必须确认：
开发者理解 **In-Process 事件可能丢失** 的风险，且该事件不涉及强一致性数据。

---

## 8️⃣ 数据库 & EF Core

### 8.1 命名规范

* [ ] 表名 / 列名使用 `snake_case`
* [ ] 未新增 PascalCase 表名

### 8.2 EF Core 映射

* [ ] 使用全局 `UseSnakeCaseNamingConvention()`
* [ ] 未为每个字段手写 `[Column]`
* [ ] `[Table] / [Column]` 仅用于特殊或旧表

---

## 9️⃣ 文件上传（如涉及）

* [ ] 校验文件扩展名 + Magic Number
* [ ] 未使用用户原始文件名
* [ ] 自动创建 `{yyyy}/{mm}` 目录
* [ ] 数据库仅存相对路径
* [ ] 图片大小 / 格式符合规范

---

## 🔟 错误处理

* [ ] Controller 未手写 try-catch（除非极特殊）
* [ ] 错误由统一 Middleware 处理
* [ ] 未返回随意结构的 error object

---

## 1️⃣1️⃣ 测试（如适用）

* [ ] 新业务逻辑有对应单元测试
* [ ] 后端 API 行为变更有集成测试
* [ ] 测试未依赖真实外部资源

---

## 1️⃣2️⃣ Reviewer 最终确认

* [ ] 本 PR **不会引入架构倒退**
* [ ] 本 PR **不会制造隐性状态泥潭**
* [ ] 本 PR **在 3 个月后仍可维护**

---

### 📌 Reviewer 提示语（推荐复制）

> ❗ 此 PR 存在架构/规范风险，请根据 Checklist 第 X 条修正后再 Review。
> 本项目严格执行 v1.2 规范，以防长期维护成本失控。

---
