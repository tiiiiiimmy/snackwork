# 信息流推荐系统实现方案

## 概述

将信息流从"基于关注关系"改为"相关推荐"，提供个性化内容推荐。

---

## 方案对比

### 方案1：基于内容的推荐（Content-Based）⭐ 推荐用于初期

**核心思想**：根据用户历史行为（浏览、评价、点赞）分析用户偏好，推荐相似特征的零食。

**推荐逻辑**：
1. 分析用户喜欢的零食特征（分类、标签、商店、评分）
2. 计算其他零食与用户偏好的相似度
3. 按相似度排序推荐

**实现步骤**：

#### 数据库设计
```sql
-- 用户行为记录表
CREATE TABLE UserBehaviors (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    SnackId CHAR(36) NOT NULL,
    BehaviorType VARCHAR(20) NOT NULL, -- 'view', 'review', 'like', 'favorite'
    Weight DECIMAL(3,2) DEFAULT 1.0,   -- 行为权重
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (SnackId) REFERENCES Snacks(Id),
    INDEX idx_user (UserId),
    INDEX idx_snack (SnackId),
    INDEX idx_user_behavior (UserId, BehaviorType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 用户偏好画像表（缓存）
CREATE TABLE UserProfiles (
    UserId CHAR(36) PRIMARY KEY,
    PreferredCategories JSON,      -- 偏好的分类及权重
    PreferredTags JSON,             -- 偏好的标签及权重
    PreferredStores JSON,           -- 偏好的商店及权重
    AverageRatingPreference DECIMAL(3,2), -- 偏好的平均评分
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### 推荐算法实现（伪代码）
```csharp
// 1. 计算用户偏好画像
UserProfile CalculateUserProfile(string userId)
{
    var behaviors = GetUserBehaviors(userId);
    var profile = new UserProfile();
    
    foreach (var behavior in behaviors)
    {
        var snack = GetSnack(behavior.SnackId);
        var weight = GetBehaviorWeight(behavior.BehaviorType);
        
        // 分类偏好
        profile.Categories[snack.CategoryId] += weight;
        
        // 标签偏好
        foreach (var tag in snack.Tags)
            profile.Tags[tag] += weight;
        
        // 商店偏好
        profile.Stores[snack.StoreId] += weight;
        
        // 评分偏好
        profile.AverageRating += snack.AverageRating * weight;
    }
    
    // 归一化
    NormalizeProfile(profile);
    return profile;
}

// 2. 计算零食推荐分数
decimal CalculateRecommendationScore(Snack snack, UserProfile profile)
{
    decimal score = 0;
    
    // 分类匹配度（权重40%）
    if (profile.Categories.ContainsKey(snack.CategoryId))
        score += profile.Categories[snack.CategoryId] * 0.4m;
    
    // 标签匹配度（权重30%）
    var tagMatch = snack.Tags
        .Where(t => profile.Tags.ContainsKey(t))
        .Sum(t => profile.Tags[t]);
    score += tagMatch * 0.3m;
    
    // 商店匹配度（权重20%）
    if (profile.Stores.ContainsKey(snack.StoreId))
        score += profile.Stores[snack.StoreId] * 0.2m;
    
    // 评分匹配度（权重10%）
    var ratingDiff = Math.Abs(snack.AverageRating - profile.AverageRating);
    score += (1 - ratingDiff / 5) * 0.1m;
    
    return score;
}

// 3. 获取推荐列表
List<Snack> GetRecommendations(string userId, int limit = 20)
{
    var profile = GetOrCalculateUserProfile(userId);
    var viewedSnacks = GetUserViewedSnackIds(userId);
    
    var allSnacks = GetAllActiveSnacks()
        .Where(s => !viewedSnacks.Contains(s.Id))
        .Select(s => new {
            Snack = s,
            Score = CalculateRecommendationScore(s, profile)
        })
        .OrderByDescending(x => x.Score)
        .Take(limit)
        .Select(x => x.Snack)
        .ToList();
    
    return allSnacks;
}
```

**优点**：
- ✅ 实现简单，不需要大量用户数据
- ✅ 推荐结果可解释（基于用户历史行为）
- ✅ 适合冷启动（新用户可基于热门内容推荐）
- ✅ 性能好，可以预计算用户画像

**缺点**：
- ❌ 推荐可能过于相似，缺乏多样性
- ❌ 难以发现用户潜在兴趣
- ❌ 需要定期更新用户画像

**适用场景**：初期用户数据较少时

---

### 方案2：协同过滤推荐（Collaborative Filtering）

**核心思想**：找到与当前用户行为相似的其他用户，推荐他们喜欢的零食。

**推荐逻辑**：
1. 计算用户之间的相似度（基于共同行为）
2. 找到最相似的K个用户
3. 推荐这些用户喜欢但当前用户未接触的零食

**实现步骤**：

#### 数据库设计
```sql
-- 用户相似度缓存表
CREATE TABLE UserSimilarities (
    UserId1 CHAR(36) NOT NULL,
    UserId2 CHAR(36) NOT NULL,
    SimilarityScore DECIMAL(5,4) NOT NULL, -- 0-1之间的相似度
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (UserId1, UserId2),
    FOREIGN KEY (UserId1) REFERENCES Users(Id),
    FOREIGN KEY (UserId2) REFERENCES Users(Id),
    INDEX idx_user1 (UserId1),
    INDEX idx_user2 (UserId2)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

#### 推荐算法实现（伪代码）
```csharp
// 1. 计算用户相似度（余弦相似度）
decimal CalculateUserSimilarity(string userId1, string userId2)
{
    var user1Behaviors = GetUserBehaviorVector(userId1); // 零食ID -> 行为权重
    var user2Behaviors = GetUserBehaviorVector(userId2);
    
    var commonSnacks = user1Behaviors.Keys
        .Intersect(user2Behaviors.Keys)
        .ToList();
    
    if (commonSnacks.Count == 0) return 0;
    
    // 余弦相似度
    var dotProduct = commonSnacks.Sum(s => 
        user1Behaviors[s] * user2Behaviors[s]);
    
    var magnitude1 = Math.Sqrt(user1Behaviors.Values.Sum(v => v * v));
    var magnitude2 = Math.Sqrt(user2Behaviors.Values.Sum(v => v * v));
    
    return dotProduct / (magnitude1 * magnitude2);
}

// 2. 获取相似用户
List<string> GetSimilarUsers(string userId, int k = 50)
{
    // 从缓存获取或计算
    var similarities = GetOrCalculateUserSimilarities(userId);
    
    return similarities
        .OrderByDescending(s => s.SimilarityScore)
        .Take(k)
        .Select(s => s.OtherUserId)
        .ToList();
}

// 3. 基于协同过滤推荐
List<Snack> GetCollaborativeRecommendations(string userId, int limit = 20)
{
    var similarUsers = GetSimilarUsers(userId);
    var userViewedSnacks = GetUserViewedSnackIds(userId);
    
    // 统计相似用户喜欢的零食
    var snackScores = new Dictionary<string, decimal>();
    
    foreach (var similarUserId in similarUsers)
    {
        var similarity = GetUserSimilarity(userId, similarUserId);
        var likedSnacks = GetUserLikedSnacks(similarUserId);
        
        foreach (var snackId in likedSnacks)
        {
            if (!userViewedSnacks.Contains(snackId))
            {
                if (!snackScores.ContainsKey(snackId))
                    snackScores[snackId] = 0;
                snackScores[snackId] += similarity;
            }
        }
    }
    
    return snackScores
        .OrderByDescending(x => x.Value)
        .Take(limit)
        .Select(x => GetSnack(x.Key))
        .ToList();
}
```

**优点**：
- ✅ 能发现用户潜在兴趣
- ✅ 推荐多样性好
- ✅ 适合用户数据充足时

**缺点**：
- ❌ 需要大量用户数据才能有效
- ❌ 冷启动问题（新用户/新零食）
- ❌ 计算复杂度高，需要定期更新相似度矩阵

**适用场景**：用户数据充足时（>1000活跃用户）

---

### 方案3：混合推荐系统 ⭐⭐⭐ 最佳方案

**核心思想**：结合多种推荐策略，根据用户状态动态调整权重。

**推荐策略组合**：
1. **基于内容的推荐**（40%权重）- 个性化
2. **热门推荐**（30%权重）- 保证内容质量
3. **位置推荐**（20%权重）- 本地化
4. **协同过滤**（10%权重）- 发现新兴趣

**实现步骤**：

#### 推荐算法实现（伪代码）
```csharp
List<Snack> GetHybridRecommendations(string userId, decimal? lat = null, decimal? lng = null, int limit = 20)
{
    var recommendations = new List<RecommendationItem>();
    var userViewedSnacks = GetUserViewedSnackIds(userId);
    
    // 1. 基于内容的推荐（40%）
    var contentBased = GetContentBasedRecommendations(userId, limit * 2)
        .Select(s => new RecommendationItem {
            Snack = s,
            Score = CalculateContentScore(s, userId) * 0.4m,
            Source = "content"
        });
    recommendations.AddRange(contentBased);
    
    // 2. 热门推荐（30%）
    var popular = GetPopularSnacks(limit * 2)
        .Where(s => !userViewedSnacks.Contains(s.Id))
        .Select(s => new RecommendationItem {
            Snack = s,
            Score = CalculatePopularityScore(s) * 0.3m,
            Source = "popular"
        });
    recommendations.AddRange(popular);
    
    // 3. 位置推荐（20%）
    if (lat.HasValue && lng.HasValue)
    {
        var nearby = GetNearbySnacks(lat.Value, lng.Value, limit * 2)
            .Where(s => !userViewedSnacks.Contains(s.Id))
            .Select(s => new RecommendationItem {
                Snack = s,
                Score = CalculateLocationScore(s, lat.Value, lng.Value) * 0.2m,
                Source = "location"
            });
        recommendations.AddRange(nearby);
    }
    
    // 4. 协同过滤（10%）
    if (HasEnoughUserData())
    {
        var collaborative = GetCollaborativeRecommendations(userId, limit)
            .Select(s => new RecommendationItem {
                Snack = s,
                Score = CalculateCollaborativeScore(s, userId) * 0.1m,
                Source = "collaborative"
            });
        recommendations.AddRange(collaborative);
    }
    
    // 合并并去重，按总分排序
    return recommendations
        .GroupBy(r => r.Snack.Id)
        .Select(g => new RecommendationItem {
            Snack = g.First().Snack,
            Score = g.Sum(r => r.Score),
            Source = string.Join(",", g.Select(r => r.Source).Distinct())
        })
        .OrderByDescending(r => r.Score)
        .ThenByDescending(r => r.Snack.CreatedAt) // 时间作为次要排序
        .Take(limit)
        .Select(r => r.Snack)
        .ToList();
}

// 热门度计算
decimal CalculatePopularityScore(Snack snack)
{
    // 综合评分、评价数、点赞数、时间衰减
    var ratingScore = snack.AverageRating / 5.0m;
    var reviewScore = Math.Min(snack.TotalReviews / 100.0m, 1.0m);
    var timeDecay = CalculateTimeDecay(snack.CreatedAt);
    
    return (ratingScore * 0.5m + reviewScore * 0.3m + timeDecay * 0.2m);
}

// 位置分数计算
decimal CalculateLocationScore(Snack snack, decimal userLat, decimal userLng)
{
    var distance = CalculateDistance(
        snack.Store.Latitude, snack.Store.Longitude,
        userLat, userLng);
    
    // 距离越近分数越高，5km内满分，20km外为0
    if (distance > 20) return 0;
    return Math.Max(0, 1 - distance / 20.0m);
}

// 时间衰减
decimal CalculateTimeDecay(DateTime createdAt)
{
    var daysSince = (DateTime.UtcNow - createdAt).TotalDays;
    // 30天内为1，之后指数衰减
    if (daysSince <= 30) return 1.0m;
    return (decimal)Math.Exp(-(daysSince - 30) / 30.0);
}
```

**优点**：
- ✅ 推荐质量高，兼顾多样性和相关性
- ✅ 适合不同阶段的用户（新用户、活跃用户）
- ✅ 可以动态调整权重
- ✅ 推荐结果更丰富

**缺点**：
- ❌ 实现复杂度较高
- ❌ 需要维护多个推荐策略

**适用场景**：推荐用于生产环境

---

### 方案4：简化混合推荐（适合快速实现）

**核心思想**：简化版混合推荐，只结合2-3种策略。

**推荐策略**：
1. **用户偏好推荐**（60%）- 基于用户历史行为
2. **热门推荐**（40%）- 保证内容质量

**实现步骤**：

```csharp
List<Snack> GetSimpleRecommendations(string userId, int limit = 20)
{
    var userViewedSnacks = GetUserViewedSnackIds(userId);
    
    // 1. 用户偏好推荐（60%）
    var userPreferred = GetUserPreferredSnacks(userId, limit * 2)
        .Where(s => !userViewedSnacks.Contains(s.Id))
        .Select(s => new {
            Snack = s,
            Score = CalculateUserPreferenceScore(s, userId) * 0.6m
        });
    
    // 2. 热门推荐（40%）
    var popular = GetPopularSnacks(limit * 2)
        .Where(s => !userViewedSnacks.Contains(s.Id))
        .Select(s => new {
            Snack = s,
            Score = CalculatePopularityScore(s) * 0.4m
        });
    
    // 合并、去重、排序
    return userPreferred
        .Concat(popular)
        .GroupBy(x => x.Snack.Id)
        .Select(g => new {
            Snack = g.First().Snack,
            Score = g.Sum(x => x.Score)
        })
        .OrderByDescending(x => x.Score)
        .Take(limit)
        .Select(x => x.Snack)
        .ToList();
}
```

**优点**：
- ✅ 实现简单快速
- ✅ 性能好
- ✅ 适合MVP阶段

**缺点**：
- ❌ 推荐多样性可能不足

**适用场景**：快速上线，后续优化

---

## 推荐方案选择

### 阶段1：MVP阶段（0-1000用户）
**推荐方案**：方案4（简化混合推荐）
- 快速实现
- 保证基本推荐质量
- 用户偏好 + 热门内容

### 阶段2：成长期（1000-10000用户）
**推荐方案**：方案3（完整混合推荐）
- 引入位置推荐
- 优化推荐算法
- 增加推荐多样性

### 阶段3：成熟期（10000+用户）
**推荐方案**：方案3 + 协同过滤
- 引入协同过滤
- 机器学习优化（可选）
- A/B测试不同策略

---

## 技术实现要点

### 1. 性能优化

**缓存策略**：
- 用户画像缓存：1小时更新一次
- 推荐结果缓存：15分钟更新一次
- 热门内容缓存：30分钟更新一次

**数据库优化**：
- 用户行为表按时间分区
- 推荐结果预计算并存储
- 使用Redis缓存热门推荐

### 2. 冷启动处理

**新用户**：
- 推荐热门内容
- 推荐高评分内容
- 推荐附近内容（如果有位置）

**新零食**：
- 给予初始曝光机会
- 结合分类/标签推荐

### 3. 推荐结果去重

- 排除用户已浏览的零食
- 排除用户已评价的零食
- 排除用户已点赞的零食

### 4. 实时性考虑

- 用户新行为实时更新权重
- 推荐结果定期刷新
- 支持手动刷新推荐

---

## API设计

### 推荐信息流API

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

---

## 实施建议

1. **第一阶段**：实现方案4（简化混合推荐）
2. **第二阶段**：收集用户行为数据，分析推荐效果
3. **第三阶段**：根据数据优化，升级到方案3
4. **持续优化**：A/B测试不同推荐策略，优化权重

---

## 数据收集

为了优化推荐效果，需要收集：
- 用户浏览记录
- 用户评价记录
- 用户点赞记录
- 用户搜索记录
- 推荐内容的点击率
- 推荐内容的转化率（评价、点赞）

---

**文档版本**: 1.0  
**创建日期**: 2025-01-27

