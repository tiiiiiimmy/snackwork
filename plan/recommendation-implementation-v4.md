# 推荐系统实现指南 - 方案4：简化混合推荐

## 概述

本文档提供方案4（简化混合推荐）的完整实现指南，包括数据库设计、后端实现、API设计和前端集成。

**推荐策略**：
- **用户偏好推荐**（60%权重）：基于用户历史行为
- **热门推荐**（40%权重）：保证内容质量

---

## 1. 数据库设计

### 1.1 用户行为记录表

```sql
CREATE TABLE UserBehaviors (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    SnackId CHAR(36) NOT NULL,
    BehaviorType VARCHAR(20) NOT NULL, -- 'view', 'review', 'like', 'favorite'
    Weight DECIMAL(3,2) DEFAULT 1.0,   -- 行为权重
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (SnackId) REFERENCES Snacks(Id) ON DELETE CASCADE,
    INDEX idx_user (UserId),
    INDEX idx_snack (SnackId),
    INDEX idx_user_behavior (UserId, BehaviorType),
    INDEX idx_user_snack (UserId, SnackId),
    INDEX idx_created (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

**行为类型和权重**：
- `view`: 浏览（权重 0.3）
- `review`: 评价（权重 1.0）
- `like`: 点赞评价（权重 0.5）
- `favorite`: 收藏（权重 1.5，未来扩展）

### 1.2 用户偏好画像表（缓存）

```sql
CREATE TABLE UserProfiles (
    UserId CHAR(36) PRIMARY KEY,
    PreferredCategories JSON,      -- {"categoryId": weight, ...}
    PreferredTags JSON,             -- {"tagName": weight, ...}
    PreferredStores JSON,           -- {"storeId": weight, ...}
    AverageRatingPreference DECIMAL(3,2), -- 偏好的平均评分
    TotalBehaviors INT DEFAULT 0,   -- 总行为数
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_updated (UpdatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### 1.3 推荐结果缓存表（可选，性能优化）

```sql
CREATE TABLE RecommendationCache (
    UserId CHAR(36) NOT NULL,
    SnackId CHAR(36) NOT NULL,
    Score DECIMAL(5,4) NOT NULL,
    Source VARCHAR(20) NOT NULL, -- 'content' or 'popular'
    CachedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (UserId, SnackId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (SnackId) REFERENCES Snacks(Id) ON DELETE CASCADE,
    INDEX idx_user_score (UserId, Score DESC),
    INDEX idx_cached (CachedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

---

## 2. 后端实现（.NET Core）

### 2.1 数据模型

```csharp
// Models/UserBehavior.cs
namespace SnackSpotAuckland.Api.Models;

public enum BehaviorType
{
    View = 0,
    Review = 1,
    Like = 2,
    Favorite = 3
}

public class UserBehavior
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid SnackId { get; set; }

    [Required]
    public BehaviorType BehaviorType { get; set; }

    [Column(TypeName = "decimal(3,2)")]
    public decimal Weight { get; set; } = 1.0m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(SnackId))]
    public virtual Snack Snack { get; set; } = null!;
}

// Models/UserProfile.cs
public class UserProfile
{
    [Key]
    public Guid UserId { get; set; }

    [Column(TypeName = "json")]
    public string PreferredCategories { get; set; } = "{}"; // JSON string

    [Column(TypeName = "json")]
    public string PreferredTags { get; set; } = "{}";

    [Column(TypeName = "json")]
    public string PreferredStores { get; set; } = "{}";

    [Column(TypeName = "decimal(3,2)")]
    public decimal AverageRatingPreference { get; set; } = 0m;

    public int TotalBehaviors { get; set; } = 0;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
```

### 2.2 推荐服务接口

```csharp
// Services/IRecommendationService.cs
namespace SnackSpotAuckland.Api.Services;

public interface IRecommendationService
{
    Task<List<RecommendationItem>> GetRecommendationsAsync(
        Guid userId, 
        int limit = 20, 
        int page = 1);
    
    Task UpdateUserProfileAsync(Guid userId);
    
    Task RecordBehaviorAsync(
        Guid userId, 
        Guid snackId, 
        BehaviorType behaviorType);
}

public class RecommendationItem
{
    public Snack Snack { get; set; } = null!;
    public decimal Score { get; set; }
    public string Source { get; set; } = string.Empty; // "content" or "popular"
    public string RecommendationReason { get; set; } = string.Empty;
}
```

### 2.3 推荐服务实现

```csharp
// Services/RecommendationService.cs
namespace SnackSpotAuckland.Api.Services;

public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RecommendationService> _logger;

    // 行为权重配置
    private static readonly Dictionary<BehaviorType, decimal> BehaviorWeights = new()
    {
        { BehaviorType.View, 0.3m },
        { BehaviorType.Review, 1.0m },
        { BehaviorType.Like, 0.5m },
        { BehaviorType.Favorite, 1.5m }
    };

    public RecommendationService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<RecommendationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<RecommendationItem>> GetRecommendationsAsync(
        Guid userId, 
        int limit = 20, 
        int page = 1)
    {
        // 检查缓存（15分钟）
        var cacheKey = $"recommendations:{userId}:{page}:{limit}";
        if (_cache.TryGetValue(cacheKey, out List<RecommendationItem>? cached))
        {
            return cached ?? new List<RecommendationItem>();
        }

        var recommendations = new List<RecommendationItem>();
        var userViewedSnacks = await GetUserViewedSnackIdsAsync(userId);
        var skip = (page - 1) * limit;

        // 1. 用户偏好推荐（60%）
        var userPreferred = await GetUserPreferredSnacksAsync(userId, limit * 2);
        var contentBased = userPreferred
            .Where(s => !userViewedSnacks.Contains(s.Id))
            .Select(s => new RecommendationItem
            {
                Snack = s,
                Score = CalculateUserPreferenceScore(s, userId) * 0.6m,
                Source = "content",
                RecommendationReason = GetRecommendationReason(s, userId)
            });

        recommendations.AddRange(contentBased);

        // 2. 热门推荐（40%）
        var popular = await GetPopularSnacksAsync(limit * 2);
        var popularBased = popular
            .Where(s => !userViewedSnacks.Contains(s.Id))
            .Select(s => new RecommendationItem
            {
                Snack = s,
                Score = CalculatePopularityScore(s) * 0.4m,
                Source = "popular",
                RecommendationReason = "热门推荐"
            });

        recommendations.AddRange(popularBased);

        // 合并、去重、排序
        var finalRecommendations = recommendations
            .GroupBy(r => r.Snack.Id)
            .Select(g => new RecommendationItem
            {
                Snack = g.First().Snack,
                Score = g.Sum(r => r.Score),
                Source = g.First().Source, // 优先显示content
                RecommendationReason = g.First().RecommendationReason
            })
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.Snack.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .ToList();

        // 缓存15分钟
        _cache.Set(cacheKey, finalRecommendations, TimeSpan.FromMinutes(15));

        return finalRecommendations;
    }

    // 获取用户已浏览的零食ID
    private async Task<HashSet<Guid>> GetUserViewedSnackIdsAsync(Guid userId)
    {
        return await _context.UserBehaviors
            .Where(b => b.UserId == userId)
            .Select(b => b.SnackId)
            .Distinct()
            .ToHashSetAsync();
    }

    // 获取用户偏好零食
    private async Task<List<Snack>> GetUserPreferredSnacksAsync(Guid userId, int limit)
    {
        var profile = await GetOrCalculateUserProfileAsync(userId);
        
        if (profile.TotalBehaviors == 0)
        {
            // 新用户，返回空列表（将只显示热门推荐）
            return new List<Snack>();
        }

        var categories = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
            profile.PreferredCategories) ?? new Dictionary<string, decimal>();
        var tags = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
            profile.PreferredTags) ?? new Dictionary<string, decimal>();
        var stores = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
            profile.PreferredStores) ?? new Dictionary<string, decimal>();

        // 构建查询
        var query = _context.Snacks
            .Where(s => !s.IsDeleted)
            .Include(s => s.Category)
            .Include(s => s.Store)
            .Include(s => s.Reviews)
            .AsQueryable();

        // 如果用户有偏好，按偏好筛选
        if (categories.Any() || tags.Any() || stores.Any())
        {
            var categoryIds = categories.Keys.Select(Guid.Parse).ToList();
            var storeIds = stores.Keys.Select(Guid.Parse).ToList();

            query = query.Where(s =>
                (categoryIds.Any() && categoryIds.Contains(s.CategoryId)) ||
                (storeIds.Any() && storeIds.Contains(s.StoreId)) ||
                (tags.Any() && s.Reviews.Any(r => 
                    r.Comment != null && tags.Keys.Any(t => r.Comment.Contains(t))))
            );
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    // 计算用户偏好分数
    private decimal CalculateUserPreferenceScore(Snack snack, Guid userId)
    {
        var profile = _context.UserProfiles
            .FirstOrDefault(p => p.UserId == userId);

        if (profile == null || profile.TotalBehaviors == 0)
            return 0;

        decimal score = 0;
        var categories = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
            profile.PreferredCategories) ?? new Dictionary<string, decimal>();
        var tags = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
            profile.PreferredTags) ?? new Dictionary<string, decimal>();
        var stores = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
            profile.PreferredStores) ?? new Dictionary<string, decimal>();

        // 分类匹配（40%）
        if (categories.ContainsKey(snack.CategoryId.ToString()))
        {
            score += categories[snack.CategoryId.ToString()] * 0.4m;
        }

        // 标签匹配（30%）
        var snackTags = _context.SnackTags
            .Where(t => t.SnackId == snack.Id)
            .Select(t => t.TagName)
            .ToList();
        
        var tagMatch = snackTags
            .Where(t => tags.ContainsKey(t))
            .Sum(t => tags[t]);
        score += tagMatch * 0.3m;

        // 商店匹配（20%）
        if (stores.ContainsKey(snack.StoreId.ToString()))
        {
            score += stores[snack.StoreId.ToString()] * 0.2m;
        }

        // 评分匹配（10%）
        var ratingDiff = Math.Abs(snack.AverageRating - profile.AverageRatingPreference);
        score += (1 - ratingDiff / 5.0m) * 0.1m;

        return Math.Min(score, 1.0m); // 归一化到0-1
    }

    // 获取热门零食
    private async Task<List<Snack>> GetPopularSnacksAsync(int limit)
    {
        return await _context.Snacks
            .Where(s => !s.IsDeleted)
            .Include(s => s.Category)
            .Include(s => s.Store)
            .Include(s => s.Reviews)
            .OrderByDescending(s => s.AverageRating)
            .ThenByDescending(s => s.TotalReviews)
            .ThenByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    // 计算热门度分数
    private decimal CalculatePopularityScore(Snack snack)
    {
        // 评分分数（50%）
        var ratingScore = snack.AverageRating / 5.0m;

        // 评价数分数（30%）
        var reviewScore = Math.Min(snack.TotalReviews / 100.0m, 1.0m);

        // 时间衰减（20%）
        var timeDecay = CalculateTimeDecay(snack.CreatedAt);

        return ratingScore * 0.5m + reviewScore * 0.3m + timeDecay * 0.2m;
    }

    // 时间衰减
    private decimal CalculateTimeDecay(DateTime createdAt)
    {
        var daysSince = (DateTime.UtcNow - createdAt).TotalDays;
        // 30天内为1，之后指数衰减
        if (daysSince <= 30) return 1.0m;
        return (decimal)Math.Exp(-(daysSince - 30) / 30.0);
    }

    // 获取推荐理由
    private string GetRecommendationReason(Snack snack, Guid userId)
    {
        var profile = _context.UserProfiles
            .FirstOrDefault(p => p.UserId == userId);

        if (profile == null) return "为您推荐";

        var categories = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
            profile.PreferredCategories) ?? new Dictionary<string, decimal>();
        var stores = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
            profile.PreferredStores) ?? new Dictionary<string, decimal>();

        if (categories.ContainsKey(snack.CategoryId.ToString()))
            return $"基于您喜欢的分类：{snack.Category.Name}";
        
        if (stores.ContainsKey(snack.StoreId.ToString()))
            return $"基于您喜欢的商店：{snack.Store.Name}";

        return "为您推荐";
    }

    // 获取或计算用户画像
    public async Task<UserProfile> GetOrCalculateUserProfileAsync(Guid userId)
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        // 如果画像不存在或超过1小时未更新，重新计算
        if (profile == null || 
            (DateTime.UtcNow - profile.UpdatedAt).TotalHours > 1)
        {
            profile = await CalculateUserProfileAsync(userId);
        }

        return profile;
    }

    // 计算用户画像
    private async Task<UserProfile> CalculateUserProfileAsync(Guid userId)
    {
        var behaviors = await _context.UserBehaviors
            .Where(b => b.UserId == userId)
            .Include(b => b.Snack)
                .ThenInclude(s => s.Category)
            .Include(b => b.Snack)
                .ThenInclude(s => s.Store)
            .ToListAsync();

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            _context.UserProfiles.Add(profile);
        }

        var categories = new Dictionary<string, decimal>();
        var tags = new Dictionary<string, decimal>();
        var stores = new Dictionary<string, decimal>();
        decimal totalRating = 0;
        int ratingCount = 0;

        foreach (var behavior in behaviors)
        {
            var snack = behavior.Snack;
            var weight = BehaviorWeights.GetValueOrDefault(behavior.BehaviorType, 1.0m) * behavior.Weight;

            // 分类偏好
            var categoryKey = snack.CategoryId.ToString();
            if (!categories.ContainsKey(categoryKey))
                categories[categoryKey] = 0;
            categories[categoryKey] += weight;

            // 商店偏好
            var storeKey = snack.StoreId.ToString();
            if (!stores.ContainsKey(storeKey))
                stores[storeKey] = 0;
            stores[storeKey] += weight;

            // 标签偏好（从评价中提取）
            if (behavior.BehaviorType == BehaviorType.Review)
            {
                // 可以从评价内容中提取标签，这里简化处理
                var snackTags = await _context.SnackTags
                    .Where(t => t.SnackId == snack.Id)
                    .Select(t => t.TagName)
                    .ToListAsync();
                
                foreach (var tag in snackTags)
                {
                    if (!tags.ContainsKey(tag))
                        tags[tag] = 0;
                    tags[tag] += weight;
                }
            }

            // 评分偏好
            if (snack.AverageRating > 0)
            {
                totalRating += snack.AverageRating * weight;
                ratingCount++;
            }
        }

        // 归一化
        var maxCategoryWeight = categories.Values.Any() ? categories.Values.Max() : 1;
        var maxTagWeight = tags.Values.Any() ? tags.Values.Max() : 1;
        var maxStoreWeight = stores.Values.Any() ? stores.Values.Max() : 1;

        foreach (var key in categories.Keys.ToList())
            categories[key] = categories[key] / maxCategoryWeight;

        foreach (var key in tags.Keys.ToList())
            tags[key] = tags[key] / maxTagWeight;

        foreach (var key in stores.Keys.ToList())
            stores[key] = stores[key] / maxStoreWeight;

        // 更新画像
        profile.PreferredCategories = JsonSerializer.Serialize(categories);
        profile.PreferredTags = JsonSerializer.Serialize(tags);
        profile.PreferredStores = JsonSerializer.Serialize(stores);
        profile.AverageRatingPreference = ratingCount > 0 ? totalRating / ratingCount : 0;
        profile.TotalBehaviors = behaviors.Count;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return profile;
    }

    // 记录用户行为
    public async Task RecordBehaviorAsync(
        Guid userId, 
        Guid snackId, 
        BehaviorType behaviorType)
    {
        // 检查是否已存在相同行为（避免重复记录）
        var existing = await _context.UserBehaviors
            .FirstOrDefaultAsync(b => 
                b.UserId == userId && 
                b.SnackId == snackId && 
                b.BehaviorType == behaviorType);

        if (existing != null)
        {
            existing.Weight += 0.1m; // 增加权重
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            var behavior = new UserBehavior
            {
                UserId = userId,
                SnackId = snackId,
                BehaviorType = behaviorType,
                Weight = BehaviorWeights.GetValueOrDefault(behaviorType, 1.0m)
            };
            _context.UserBehaviors.Add(behavior);
        }

        await _context.SaveChangesAsync();

        // 异步更新用户画像（不阻塞）
        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateUserProfileAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user profile for {UserId}", userId);
            }
        });

        // 清除推荐缓存
        _cache.Remove($"recommendations:{userId}:*");
    }

    // 更新用户画像
    public async Task UpdateUserProfileAsync(Guid userId)
    {
        await CalculateUserProfileAsync(userId);
    }
}
```

### 2.4 API控制器

```csharp
// Controllers/V1/RecommendationsController.cs
namespace SnackSpotAuckland.Api.Controllers.V1;

[ApiController]
[Route("api/v1/feed")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        if (page < 1) page = 1;
        if (limit < 1 || limit > 100) limit = 20;

        var recommendations = await _recommendationService
            .GetRecommendationsAsync(userId, limit, page);

        var response = new
        {
            success = true,
            data = recommendations.Select(r => new
            {
                snack = new
                {
                    id = r.Snack.Id,
                    name = r.Snack.Name,
                    description = r.Snack.Description,
                    category = new
                    {
                        id = r.Snack.Category.Id,
                        name = r.Snack.Category.Name
                    },
                    store = new
                    {
                        id = r.Snack.Store.Id,
                        name = r.Snack.Store.Name
                    },
                    averageRating = r.Snack.AverageRating,
                    totalReviews = r.Snack.TotalReviews,
                    createdAt = r.Snack.CreatedAt
                },
                recommendationReason = r.RecommendationReason,
                score = r.Score,
                source = r.Source
            }),
            pagination = new
            {
                page,
                limit,
                total = recommendations.Count,
                totalPages = (int)Math.Ceiling(recommendations.Count / (double)limit)
            }
        };

        return Ok(response);
    }
}
```

### 2.5 行为记录中间件/服务

```csharp
// Services/BehaviorTrackingService.cs
public interface IBehaviorTrackingService
{
    Task TrackViewAsync(Guid userId, Guid snackId);
    Task TrackReviewAsync(Guid userId, Guid snackId);
    Task TrackLikeAsync(Guid userId, Guid snackId);
}

public class BehaviorTrackingService : IBehaviorTrackingService
{
    private readonly IRecommendationService _recommendationService;

    public BehaviorTrackingService(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task TrackViewAsync(Guid userId, Guid snackId)
    {
        await _recommendationService.RecordBehaviorAsync(
            userId, snackId, BehaviorType.View);
    }

    public async Task TrackReviewAsync(Guid userId, Guid snackId)
    {
        await _recommendationService.RecordBehaviorAsync(
            userId, snackId, BehaviorType.Review);
    }

    public async Task TrackLikeAsync(Guid userId, Guid snackId)
    {
        await _recommendationService.RecordBehaviorAsync(
            userId, snackId, BehaviorType.Like);
    }
}
```

---

## 3. 前端集成

### 3.1 API服务

```typescript
// services/recommendationService.ts
import axios from 'axios';

export interface RecommendationItem {
  snack: {
    id: string;
    name: string;
    description: string;
    category: {
      id: string;
      name: string;
    };
    store: {
      id: string;
      name: string;
    };
    averageRating: number;
    totalReviews: number;
    createdAt: string;
  };
  recommendationReason: string;
  score: number;
  source: 'content' | 'popular';
}

export interface RecommendationsResponse {
  success: boolean;
  data: RecommendationItem[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export const recommendationService = {
  async getRecommendations(
    page: number = 1,
    limit: number = 20
  ): Promise<RecommendationsResponse> {
    const response = await axios.get<RecommendationsResponse>(
      `/api/v1/feed/recommendations`,
      {
        params: { page, limit }
      }
    );
    return response.data;
  },

  async trackView(snackId: string): Promise<void> {
    await axios.post(`/api/v1/behaviors/view`, { snackId });
  },

  async trackReview(snackId: string): Promise<void> {
    await axios.post(`/api/v1/behaviors/review`, { snackId });
  },

  async trackLike(snackId: string): Promise<void> {
    await axios.post(`/api/v1/behaviors/like`, { snackId });
  }
};
```

### 3.2 React组件示例

```typescript
// components/RecommendationFeed.tsx
import { useState, useEffect } from 'react';
import { recommendationService, RecommendationItem } from '../services/recommendationService';

export const RecommendationFeed: React.FC = () => {
  const [recommendations, setRecommendations] = useState<RecommendationItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);

  useEffect(() => {
    loadRecommendations();
  }, []);

  const loadRecommendations = async () => {
    try {
      setLoading(true);
      const response = await recommendationService.getRecommendations(page, 20);
      setRecommendations(prev => [...prev, ...response.data]);
      setHasMore(response.pagination.page < response.pagination.totalPages);
    } catch (error) {
      console.error('Failed to load recommendations:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadMore = () => {
    if (!loading && hasMore) {
      setPage(prev => prev + 1);
      loadRecommendations();
    }
  };

  const handleSnackClick = async (snackId: string) => {
    // 记录浏览行为
    await recommendationService.trackView(snackId);
  };

  return (
    <div className="recommendation-feed">
      {recommendations.map((item) => (
        <div
          key={item.snack.id}
          className="recommendation-item"
          onClick={() => handleSnackClick(item.snack.id)}
        >
          <div className="snack-info">
            <h3>{item.snack.name}</h3>
            <p className="recommendation-reason">{item.recommendationReason}</p>
            <p className="snack-description">{item.snack.description}</p>
            <div className="snack-meta">
              <span>分类: {item.snack.category.name}</span>
              <span>商店: {item.snack.store.name}</span>
              <span>评分: {item.snack.averageRating.toFixed(1)}</span>
            </div>
          </div>
        </div>
      ))}
      
      {loading && <div>加载中...</div>}
      
      {hasMore && !loading && (
        <button onClick={loadMore}>加载更多</button>
      )}
    </div>
  );
};
```

---

## 4. 性能优化

### 4.1 缓存策略

- **推荐结果缓存**：15分钟
- **用户画像缓存**：1小时
- **热门内容缓存**：30分钟

### 4.2 数据库优化

- 用户行为表按时间分区（按月）
- 定期清理旧的行为记录（保留最近90天）
- 用户画像异步更新，不阻塞推荐请求

### 4.3 查询优化

- 使用索引优化查询
- 限制查询结果数量
- 使用分页避免大量数据加载

---

## 5. 实施步骤

### Phase 1: 数据库和模型（1-2天）
1. 创建数据库表
2. 创建EF Core模型
3. 创建迁移

### Phase 2: 推荐服务（2-3天）
1. 实现用户画像计算
2. 实现推荐算法
3. 实现行为记录

### Phase 3: API和前端（2-3天）
1. 创建推荐API
2. 创建行为记录API
3. 前端集成

### Phase 4: 测试和优化（1-2天）
1. 单元测试
2. 性能测试
3. 优化和调整

---

## 6. 监控和指标

### 关键指标
- 推荐点击率（CTR）
- 推荐转化率（浏览->评价）
- 推荐响应时间
- 用户画像更新频率

### 日志记录
- 推荐请求日志
- 行为记录日志
- 用户画像更新日志

---

**文档版本**: 1.0  
**创建日期**: 2025-01-27  
**适用方案**: 方案4 - 简化混合推荐

