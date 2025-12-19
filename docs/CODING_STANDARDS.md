# SnackSpot Auckland v2.0 - 开发规范文档

## 1. 命名规范

### 1.1 前端命名规范

#### 1.1.1 文件和目录命名

- **组件文件**：`PascalCase.tsx`
  ```typescript
  // ✅ 正确
  UserProfile.tsx
  SnackCard.tsx
  AddSnackForm.tsx
  
  // ❌ 错误
  userProfile.tsx
  snack-card.tsx
  ```

- **目录名**：`camelCase` 或 `kebab-case`
  ```typescript
  // ✅ 正确
  src/components/userProfile/
  src/components/snack-card/
  src/hooks/
  ```

- **工具文件**：`camelCase.ts`
  ```typescript
  // ✅ 正确
  formatDate.ts
  validateEmail.ts
  apiClient.ts
  ```

- **类型定义文件**：`camelCase.ts`
  ```typescript
  // ✅ 正确
  user.ts
  snack.ts
  api.ts
  ```

- **常量文件**：`constants.ts` 或 `UPPER_CASE.ts`
  ```typescript
  // ✅ 正确
  constants.ts
  API_ENDPOINTS.ts
  ```

#### 1.1.2 变量和函数命名

- **变量**：`camelCase`
  ```typescript
  // ✅ 正确
  const userName = 'John';
  const snackList = [];
  const isLoading = true;
  
  // ❌ 错误
  const user_name = 'John';
  const SnackList = [];
  ```

- **函数**：`camelCase`
  ```typescript
  // ✅ 正确
  function getUserProfile() {}
  const handleSubmit = () => {};
  const fetchSnacks = async () => {};
  
  // ❌ 错误
  function GetUserProfile() {}
  const handle_submit = () => {};
  ```

- **组件**：`PascalCase`
  ```typescript
  // ✅ 正确
  function UserProfile() {}
  const SnackCard = () => {};
  
  // ❌ 错误
  function userProfile() {}
  const snackCard = () => {};
  ```

- **常量**：`UPPER_SNAKE_CASE`
  ```typescript
  // ✅ 正确
  const API_BASE_URL = 'https://api.example.com';
  const MAX_FILE_SIZE = 5 * 1024 * 1024;
  
  // ❌ 错误
  const apiBaseUrl = 'https://api.example.com';
  const maxFileSize = 5 * 1024 * 1024;
  ```

- **私有变量/函数**：`_camelCase`（可选，TypeScript中不常用）
  ```typescript
  // ✅ 正确（如果使用）
  const _internalHelper = () => {};
  ```

#### 1.1.3 类型和接口命名

- **接口**：`PascalCase`，以 `I` 开头（可选）
  ```typescript
  // ✅ 正确
  interface User {
    id: string;
    name: string;
  }
  
  interface ISnackResponse {
    id: string;
    name: string;
  }
  ```

- **类型别名**：`PascalCase`
  ```typescript
  // ✅ 正确
  type UserId = string;
  type SnackList = Snack[];
  ```

- **枚举**：`PascalCase`，枚举值 `UPPER_SNAKE_CASE`
  ```typescript
  // ✅ 正确
  enum UserRole {
    ADMIN = 'ADMIN',
    USER = 'USER',
  }
  ```

#### 1.1.4 CSS/SCSS命名

- **类名**：`kebab-case`
  ```scss
  // ✅ 正确
  .user-profile {}
  .snack-card {}
  .btn-primary {}
  
  // ❌ 错误
  .userProfile {}
  .snackCard {}
  .btnPrimary {}
  ```

- **CSS变量**：`kebab-case`，以 `--` 开头
  ```scss
  // ✅ 正确
  :root {
    --primary-color: #007bff;
    --font-size-base: 16px;
  }
  ```

### 1.2 后端命名规范

#### 1.2.1 文件和类命名

- **类文件**：`PascalCase.cs`
  ```csharp
  // ✅ 正确
  UserService.cs
  SnackController.cs
  ApplicationDbContext.cs
  ```

- **类名**：`PascalCase`
  ```csharp
  // ✅ 正确
  public class UserService { }
  public class SnackController : ControllerBase { }
  ```

- **接口**：`IPascalCase.cs`
  ```csharp
  // ✅ 正确
  public interface IUserService { }
  public interface IRecommendationService { }
  ```

#### 1.2.2 方法和属性命名

- **方法**：`PascalCase`
  ```csharp
  // ✅ 正确
  public async Task<User> GetUserAsync(Guid id) { }
  public void CreateSnack(CreateSnackRequest request) { }
  
  // ❌ 错误
  public async Task<User> getUserAsync(Guid id) { }
  public void create_snack(CreateSnackRequest request) { }
  ```

- **属性**：`PascalCase`
  ```csharp
  // ✅ 正确
  public string Name { get; set; }
  public int TotalReviews { get; set; }
  ```

- **私有字段**：`_camelCase`
  ```csharp
  // ✅ 正确
  private readonly ILogger _logger;
  private readonly ApplicationDbContext _context;
  ```

- **参数和局部变量**：`camelCase`
  ```csharp
  // ✅ 正确
  public async Task<User> GetUserAsync(Guid userId) { }
  var snackList = await _context.Snacks.ToListAsync();
  
  // ❌ 错误
  public async Task<User> GetUserAsync(Guid UserId) { }
  var SnackList = await _context.Snacks.ToListAsync();
  ```

#### 1.2.3 DTO命名

- **请求DTO**：`PascalCaseRequest.cs`
  ```csharp
  // ✅ 正确
  CreateSnackRequest.cs
  UpdateUserRequest.cs
  LoginRequest.cs
  ```

- **响应DTO**：`PascalCaseResponse.cs`
  ```csharp
  // ✅ 正确
  SnackResponse.cs
  UserResponse.cs
  ApiResponse.cs
  ```

#### 1.2.4 数据库命名

- **表名**：`PascalCase`（复数形式）
  ```sql
  -- ✅ 正确
  CREATE TABLE Users (...)
  CREATE TABLE Snacks (...)
  CREATE TABLE Reviews (...)
  ```

- **列名**：`PascalCase`
  ```sql
  -- ✅ 正确
  Id, UserName, Email, CreatedAt
  ```

---

## 2. 代码组织原则

### 2.1 前端代码组织

#### 2.1.1 组件组织

**按功能模块组织**：
```
components/
├── snack/          # 零食相关组件
├── user/           # 用户相关组件
├── review/         # 评价相关组件
└── common/         # 通用组件
```

**组件文件结构**：
```typescript
// ComponentName.tsx
import React from 'react';
import styles from './ComponentName.module.scss';

interface ComponentNameProps {
  // props定义
}

export const ComponentName: React.FC<ComponentNameProps> = ({ ... }) => {
  // 组件逻辑
  return (
    <div className={styles.container}>
      {/* JSX */}
    </div>
  );
};
```

#### 2.1.2 Hooks组织

**自定义Hooks**：
```typescript
// hooks/useSnacks.ts
import { useState, useEffect } from 'react';
import { snackService } from '../services/snackService';

export const useSnacks = () => {
  const [snacks, setSnacks] = useState<Snack[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // 逻辑
  }, []);

  return { snacks, loading };
};
```

#### 2.1.3 服务层组织

**API服务**：
```typescript
// services/snackService.ts
import { apiClient } from './api';

export const snackService = {
  async getSnacks(): Promise<Snack[]> {
    const response = await apiClient.get<Snack[]>('/snacks');
    return response.data;
  },

  async getSnackById(id: string): Promise<Snack> {
    const response = await apiClient.get<Snack>(`/snacks/${id}`);
    return response.data;
  },
};
```

### 2.2 后端代码组织

#### 2.2.1 控制器组织

**控制器结构**：
```csharp
// Controllers/V1/SnacksController.cs
namespace SnackSpotAuckland.Api.Controllers.V1;

[ApiController]
[Route("api/v1/snacks")]
[Authorize]
public class SnacksController : ControllerBase
{
    private readonly ISnackService _snackService;
    private readonly ILogger<SnacksController> _logger;

    public SnacksController(
        ISnackService snackService,
        ILogger<SnacksController> logger)
    {
        _snackService = snackService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSnacks([FromQuery] int page = 1)
    {
        // 实现
    }
}
```

#### 2.2.2 服务层组织

**服务接口和实现**：
```csharp
// Services/Interfaces/ISnackService.cs
public interface ISnackService
{
    Task<SnackResponse> GetSnackByIdAsync(Guid id);
    Task<SnackResponse> CreateSnackAsync(CreateSnackRequest request);
}

// Services/SnackService.cs
public class SnackService : ISnackService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public SnackService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // 实现
}
```

#### 2.2.3 DTO组织

**请求和响应分离**：
```csharp
// DTOs/Requests/CreateSnackRequest.cs
public class CreateSnackRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public Guid StoreId { get; set; }
}

// DTOs/Responses/SnackResponse.cs
public class SnackResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
```

---

## 3. 导入/引用规范

### 3.1 前端导入顺序

```typescript
// 1. React和相关库
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

// 2. 第三方库
import axios from 'axios';
import { format } from 'date-fns';

// 3. 内部服务/工具
import { snackService } from '../services/snackService';
import { formatDate } from '../utils/date';

// 4. 类型定义
import type { Snack, User } from '../types';

// 5. 样式
import styles from './ComponentName.module.scss';
```

### 3.2 后端using顺序

```csharp
// 1. System命名空间
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 2. Microsoft命名空间
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// 3. 第三方库
using FluentValidation;
using AutoMapper;

// 4. 项目内部命名空间
using SnackSpotAuckland.Api.Models;
using SnackSpotAuckland.Api.Services;
using SnackSpotAuckland.Api.DTOs;
```

---

## 4. 注释规范

### 4.1 前端注释

#### 4.1.1 组件注释

```typescript
/**
 * 零食卡片组件
 * 
 * @component
 * @param {Snack} snack - 零食数据
 * @param {Function} onLike - 点赞回调函数
 * @param {boolean} showActions - 是否显示操作按钮
 * 
 * @example
 * <SnackCard 
 *   snack={snack} 
 *   onLike={handleLike}
 *   showActions={true}
 * />
 */
export const SnackCard: React.FC<SnackCardProps> = ({ 
  snack, 
  onLike, 
  showActions 
}) => {
  // 组件实现
};
```

#### 4.1.2 函数注释

```typescript
/**
 * 格式化日期为友好显示格式
 * 
 * @param {Date|string} date - 日期对象或日期字符串
 * @param {string} format - 格式化模板，默认为 'yyyy-MM-dd'
 * @returns {string} 格式化后的日期字符串
 * 
 * @example
 * formatDate(new Date(), 'yyyy-MM-dd') // '2025-01-27'
 */
export const formatDate = (date: Date | string, format = 'yyyy-MM-dd'): string => {
  // 实现
};
```

### 4.2 后端注释

#### 4.2.1 类注释

```csharp
/// <summary>
/// 零食服务，处理零食相关的业务逻辑
/// </summary>
public class SnackService : ISnackService
{
    // 实现
}
```

#### 4.2.2 方法注释

```csharp
/// <summary>
/// 根据ID获取零食详情
/// </summary>
/// <param name="id">零食ID</param>
/// <returns>零食响应对象</returns>
/// <exception cref="NotFoundException">当零食不存在时抛出</exception>
public async Task<SnackResponse> GetSnackByIdAsync(Guid id)
{
    // 实现
}
```

#### 4.2.3 复杂逻辑注释

```csharp
// 计算用户偏好分数
// 1. 分类匹配度（40%权重）
// 2. 标签匹配度（30%权重）
// 3. 商店匹配度（20%权重）
// 4. 评分匹配度（10%权重）
private decimal CalculateUserPreferenceScore(Snack snack, Guid userId)
{
    // 实现
}
```

---

## 5. Git提交规范

### 5.1 提交消息格式

使用 **Conventional Commits** 规范：

```
<type>(<scope>): <subject>

<body>

<footer>
```

### 5.2 提交类型

- `feat`: 新功能
- `fix`: 修复bug
- `docs`: 文档更新
- `style`: 代码格式（不影响功能）
- `refactor`: 重构
- `perf`: 性能优化
- `test`: 测试相关
- `chore`: 构建/工具/依赖更新

### 5.3 提交示例

```bash
# 新功能
feat(frontend): add snack recommendation feed

# 修复bug
fix(backend): fix user authentication token refresh

# 文档更新
docs: update API documentation

# 重构
refactor(backend): extract recommendation service

# 性能优化
perf(frontend): optimize image lazy loading

# 测试
test(backend): add recommendation service tests

# 构建/工具
chore: update dependencies
```

---

## 6. 代码质量规范

### 6.1 TypeScript规范

#### 6.1.1 类型定义

```typescript
// ✅ 正确：使用明确的类型
interface User {
  id: string;
  name: string;
  email: string;
}

function getUser(id: string): Promise<User> {
  // 实现
}

// ❌ 错误：使用any
function getUser(id: any): any {
  // 实现
}
```

#### 6.1.2 可选属性

```typescript
// ✅ 正确
interface Snack {
  id: string;
  name: string;
  description?: string;  // 可选属性
}

// ❌ 错误
interface Snack {
  id: string;
  name: string;
  description: string | undefined;
}
```

### 6.2 C#规范

#### 6.2.1 异步方法

```csharp
// ✅ 正确：异步方法以Async结尾
public async Task<User> GetUserAsync(Guid id)
{
    return await _context.Users.FindAsync(id);
}

// ❌ 错误
public async Task<User> GetUser(Guid id)
{
    return await _context.Users.FindAsync(id);
}
```

#### 6.2.2 空值处理

```csharp
// ✅ 正确：使用null检查
var user = await _context.Users.FindAsync(id);
if (user == null)
{
    throw new NotFoundException($"User with id {id} not found");
}

// ❌ 错误：直接使用可能为null的对象
var user = await _context.Users.FindAsync(id);
var name = user.Name;  // 可能NullReferenceException
```

---

## 7. 错误处理规范

### 7.1 前端错误处理

```typescript
// ✅ 正确：使用try-catch处理异步错误
try {
  const snacks = await snackService.getSnacks();
  setSnacks(snacks);
} catch (error) {
  if (error instanceof AxiosError) {
    console.error('API Error:', error.response?.data);
    showToast('加载失败，请重试');
  } else {
    console.error('Unexpected error:', error);
  }
}
```

### 7.2 后端错误处理

```csharp
// ✅ 正确：使用统一的错误处理
[HttpGet("{id}")]
public async Task<IActionResult> GetSnack(Guid id)
{
    try
    {
        var snack = await _snackService.GetSnackByIdAsync(id);
        if (snack == null)
        {
            return NotFound(new { error = "Snack not found" });
        }
        return Ok(snack);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting snack {SnackId}", id);
        return StatusCode(500, new { error = "Internal server error" });
    }
}
```

---

## 8. 测试规范

### 8.1 前端测试

```typescript
// ComponentName.test.tsx
import { render, screen } from '@testing-library/react';
import { SnackCard } from './SnackCard';

describe('SnackCard', () => {
  it('should render snack name', () => {
    const snack = { id: '1', name: 'Test Snack' };
    render(<SnackCard snack={snack} />);
    expect(screen.getByText('Test Snack')).toBeInTheDocument();
  });
});
```

### 8.2 后端测试

```csharp
// SnacksControllerTests.cs
public class SnacksControllerTests
{
    [Fact]
    public async Task GetSnack_ReturnsOk_WhenSnackExists()
    {
        // Arrange
        var snackId = Guid.NewGuid();
        
        // Act
        var result = await _controller.GetSnack(snackId);
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
```

---

## 9. 性能优化规范

### 9.1 前端性能

- **懒加载**：使用 `React.lazy()` 和 `Suspense`
- **图片优化**：使用懒加载和WebP格式
- **代码分割**：按路由分割代码
- **Memoization**：使用 `useMemo` 和 `useCallback`

### 9.2 后端性能

- **异步操作**：所有I/O操作使用async/await
- **数据库查询**：使用 `Include()` 避免N+1问题
- **缓存**：合理使用内存缓存
- **分页**：列表查询必须分页

---

## 10. 安全规范

### 10.1 前端安全

- **输入验证**：所有用户输入必须验证
- **XSS防护**：使用React的自动转义
- **敏感信息**：不在前端存储敏感信息
- **HTTPS**：生产环境必须使用HTTPS

### 10.2 后端安全

- **输入验证**：使用FluentValidation验证所有输入
- **SQL注入**：使用参数化查询（EF Core自动处理）
- **认证授权**：所有API端点必须认证
- **速率限制**：实现API速率限制

---

## 11. 移动端优化规范

### 11.1 响应式设计

- **移动端优先**：先设计移动端，再适配桌面端
- **触摸优化**：按钮最小44px × 44px
- **性能优化**：首屏加载 < 2秒

### 11.2 PWA规范

- **Service Worker**：实现离线缓存
- **Manifest**：配置PWA清单
- **图标**：提供多种尺寸的图标

---

**文档版本**: 1.0  
**创建日期**: 2025-01-27  
**最后更新**: 2025-01-27

