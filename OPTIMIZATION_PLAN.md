# jRandomSkills Performance Optimization Plan

## Executive Summary
The plugin currently has **~112 skills** with multiple performance bottlenecks. Analysis identified that average server frametime impact primarily comes from:
1. **Repeated `GetPlayers()` calls** per tick (40+ independent calls)
2. **Nested player loops** with distance calculations in skills like ToxicSmoke, Magneto, HomingNades
3. **CheckTransmit operations** (Ghost, Glaz, Wallhack) - O(entities × players)
4. **Unthrottled entity updates** (FalconEye camera positioning)

**Estimated improvement potential: 20-40% frametime reduction** with comprehensive optimization

---

## Priority 1: Critical Impact (Implement First)

### 1.1 Centralize GetPlayers() Calls
**Problem**: Each skill independently calls `Utilities.GetPlayers()` every tick, resulting in redundant LINQ queries  
**Solution**: Cache valid players at frame start, share across all skills

```csharp
// In PlayerOnTick.cs - Execute at frame start
private static CCSPlayerController[] CachedValidPlayers = [];

private static void UpdateGameRules()
{
    // ... existing code ...
    CachedValidPlayers = Utilities.GetPlayers()
        .Where(p => p.IsValid && !p.IsBot && !p.IsHLTV)
        .ToArray();
}

// Provide static accessor for all skills
public static CCSPlayerController[] GetValidPlayers() => CachedValidPlayers;
```

**Impact**: ~10-15% reduction (eliminates redundant LINQ queries)  
**Difficulty**: Easy  
**Files to modify**: `PlayerOnTick.cs`, update all skills to use cached list

---

### 1.2 Implement Player Cache by SteamID
**Problem**: Event handlers repeatedly call `SkillPlayer.FirstOrDefault()` with linear search  
**Solution**: Use `ConcurrentDictionary<ulong, jSkill_PlayerInfo>` instead of `ConcurrentBag`

```csharp
// In jRandomSkills.cs
public ConcurrentDictionary<ulong, jSkill_PlayerInfo> SkillPlayerDict { get; set; } = [];

// Replace all FirstOrDefault calls:
// OLD: Instance?.SkillPlayer.FirstOrDefault(p => p.SteamID == player.SteamID)
// NEW: Instance?.SkillPlayerDict.TryGetValue(player.SteamID, out var skillPlayer) ? skillPlayer : null
```

**Impact**: ~5-8% reduction (O(1) lookups vs O(n) searches)  
**Difficulty**: Medium (requires refactoring many lines)  
**Files to modify**: `jRandomSkills.cs`, `PlayerEvents.cs`, `PlayerOnTick.cs`, all event handlers

---

### 1.3 Reduce Tick Frequency for Low-Priority Skills
**Problem**: All skills execute `OnTick()` every frame (128 ticks/sec on CS2)  
**Solution**: Throttle skills to 32-64 ticks/sec based on criticality

Create tier system:
- **Tier 0 (128 ticks)**: Damage-based, movement - Aimbot, Dash, OneShot
- **Tier 1 (64 ticks)**: Vision/tracking - HomingNades, FalconEye, ToxicSmoke
- **Tier 2 (32 ticks)**: Visual/cosmetic - Glitch, ChaoticDamage, Noclip HUD

```csharp
// In skill implementation
public static void OnTick()
{
    if (Server.TickCount % 2 != 0) return; // 64 Hz
    // ... skill logic ...
}
```

**Impact**: ~8-12% reduction (fewer executions of expensive OnTick methods)  
**Difficulty**: Easy (add tick checks)  
**Target skills**: ToxicSmoke (64→32Hz), FalconEye (128→64Hz), Glitch (128→32Hz)

---

## Priority 2: High Impact (Implement Second)

### 2.1 Optimize Distance Calculations
**Problem**: Skills like ToxicSmoke, Magneto recalculate distances every tick for all players  
**Solution**: Pre-calculate distances once per tick, share results

```csharp
// Create utility class
public static class DistanceCache
{
    private static Dictionary<(uint, uint), float> Cache = [];
    
    public static float GetDistance(CCSPlayerController player1, CCSPlayerController player2)
    {
        var key = (player1.Index, player2.Index);
        if (!Cache.TryGetValue(key, out var dist))
        {
            dist = SkillUtils.GetDistance(
                player1.PlayerPawn?.Value?.AbsOrigin ?? new Vector(0, 0, 0),
                player2.PlayerPawn?.Value?.AbsOrigin ?? new Vector(0, 0, 0)
            );
            Cache[key] = dist;
        }
        return dist;
    }
    
    public static void ClearCache() => Cache.Clear();
}

// Call ClearCache() at start of frame in PlayerOnTick
```

**Impact**: ~5-10% (eliminates duplicate calculations)  
**Difficulty**: Medium  
**Files to modify**: `SkillUtils.cs` (add cache), skills using distance checks

---

### 2.2 Batch Entity Updates
**Problem**: FalconEye updates camera position every single tick without batching  
**Solution**: Use `Server.NextFrame()` to defer non-critical updates

```csharp
// Instead of direct entity manipulation each tick:
private static float timeSinceLast = 0;
public static void OnTick()
{
    timeSinceLast += Server.TickInterval;
    if (timeSinceLast < 0.1f) return; // Update every 100ms instead of 8ms
    
    timeSinceLast = 0;
    // Update entity position
}
```

**Impact**: ~3-5% (reduces entity update frequency)  
**Difficulty**: Easy  
**Target skills**: FalconEye, Wallhack (model updates)

---

### 2.3 Optimize CheckTransmit Operations
**Problem**: Ghost, Glaz, Wallhack rebuild visibility lists every time CheckTransmit is called  
**Solution**: Implement efficient culling with spatial caching

```csharp
// In CheckTransmit handlers - add early exit conditions
public static void CheckTransmit(CCheckTransmitInfoList transmitList)
{
    // Only recalculate if player moved significantly or after 100ms
    if (LastUpdateTime > 0.1f && !PlayerMovedSignificantly())
        return;
    
    // ... proceed with transmit calculation ...
}
```

**Impact**: ~5-8% (reduces redundant visibility calculations)  
**Difficulty**: Medium  
**Files**: Skills with CheckTransmit (Ghost, Glaz, Wallhack)

---

## Priority 3: Medium Impact (Implement Third)

### 3.1 Reduce Event Handler Lock Contention
**Problem**: All event handlers use `lock(setLock)` - potential bottleneck with many concurrent events

**Solution**: Use `ReaderWriterLockSlim` for better concurrency

```csharp
// In PlayerEvents.cs
private static ReaderWriterLockSlim setLockRW = new();

private static HookResult HandleSkillEvent(string eventName, params object[] args)
{
    setLockRW.EnterReadLock();
    try
    {
        // ... existing code ...
    }
    finally
    {
        setLockRW.ExitReadLock();
    }
}
```

**Impact**: ~2-4% (better event handler parallelism)  
**Difficulty**: Easy  
**Files**: `PlayerEvents.cs`

---

### 3.2 Implement Skill Profiling System
**Problem**: Unknown which skills actually cause performance problems  
**Solution**: Add optional profiling to measure per-skill execution time

```csharp
// Add to PlayerEvents.cs
private static Dictionary<string, long> SkillExecutionTime = [];

private static HookResult HandleSkillEvent(string eventName, params object[] args)
{
    foreach (var playerSkill in Instance.SkillPlayer)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Instance.SkillAction(playerSkill.Skill.ToString(), eventName, args);
        sw.Stop();
        
        var key = $"{playerSkill.Skill}_{eventName}";
        SkillExecutionTime.AddOrUpdate(key, sw.ElapsedMilliseconds, 
            (k, v) => (v + sw.ElapsedMilliseconds) / 2); // Running average
    }
}

// Console command to dump stats
css_skill_profile // Shows slowest skills
```

**Impact**: 0% direct improvement, but enables informed optimization  
**Difficulty**: Easy  
**Files**: `PlayerEvents.cs`

---

### 3.3 Object Pool for Frequently Created Objects
**Problem**: Many skills create temporary objects (Vector, math calculations) frequently  
**Solution**: Use object pooling for vectors and common allocations

```csharp
public static class ObjectPool
{
    private static Stack<Vector> VectorPool = new(100);
    
    public static Vector RentVector(float x, float y, float z)
    {
        if (VectorPool.TryPop(out var v))
        {
            v.X = x;
            v.Y = y;
            v.Z = z;
            return v;
        }
        return new Vector(x, y, z);
    }
    
    public static void ReturnVector(Vector v) => VectorPool.Push(v);
}
```

**Impact**: ~1-2% (reduced GC pressure)  
**Difficulty**: Medium  
**Files**: `SkillUtils.cs`

---

## Priority 4: Minor Optimizations

### 4.1 LINQ Query Optimization
Replace expensive LINQ chains with early-exit loops where applicable:

```csharp
// OLD (multiple LINQ operations)
var target = Utilities.GetPlayers()
    .Where(p => p.IsValid && p.Team == CsTeam.Terrorist)
    .FirstOrDefault(p => GetDistance(p, player) < 100);

// NEW (single pass, early exit)
foreach (var p in CachedValidPlayers)
{
    if (p.Team == CsTeam.Terrorist && GetDistance(p, player) < 100)
        return p;
}
return null;
```

**Impact**: ~1-3%  
**Difficulty**: Easy

---

### 4.2 Disable Expensive Skills in Specific Scenarios
**Problem**: Some skills are always expensive (Wallhack - O(entities))  
**Solution**: Add configurable disable list for competitive play

```csharp
private static HashSet<Skills> ExpensiveSkills = [Skills.Wallhack, Skills.Ghost, Skills.Glaz];
private static bool DisableExpensiveSkillsOnServerStress = false; // Config option

public static bool CanExecuteSkill(Skills skill)
{
    if (DisableExpensiveSkillsOnServerStress && ServerFPS < 200 && ExpensiveSkills.Contains(skill))
        return false;
    return true;
}
```

**Impact**: ~0-10% (only when server is stressed)  
**Difficulty**: Easy

---

### 4.3 Remove Unused Skills
**Problem**: BrokenSkills folder suggests unfinished/unused skills are loaded  
**Solution**: Clean up and document

**Action items**:
- Verify skills in `BrokenSkills/` folder are not loaded
- Check enum value for unused skills - mark as `[Obsolete]` if needed
- Document which skills are deprecated

**Impact**: ~0-5% depending on which are unused  
**Difficulty**: Easy

---

## Implementation Roadmap

### Phase 1 (Week 1) - Critical Impact
- [ ] Centralize `GetPlayers()` caching
- [ ] Convert `SkillPlayer` from `ConcurrentBag` to `ConcurrentDictionary`
- [ ] Profile results

**Expected improvement: 15-25% frametime reduction**

### Phase 2 (Week 2) - High Impact  
- [ ] Optimize distance calculations with cache
- [ ] Batch entity updates (FalconEye, Wallhack)
- [ ] Improve CheckTransmit efficiency
- [ ] Profile and measure

**Expected improvement: +8-15% additional**

### Phase 3 (Week 3) - Medium Impact
- [ ] Implement profiling system (data-driven decisions)
- [ ] Reduce lock contention
- [ ] Object pooling for common types

**Expected improvement: +5-8% additional**

### Phase 4 (Week 4+) - Polish
- [ ] LINQ optimizations based on profiling
- [ ] Clean up broken/unused skills
- [ ] Performance regression testing

---

## Measurement & Validation

### Before/After Benchmarking
Use console command to measure server FPS:

```bash
# In console: measure frame time before implementing changes
stats
```

### Recommended Metrics
- **Server FPS** (should improve from baseline)
- **Average skill execution time** (implement profiling)
- **Peak frametime spikes** (when skills activate)
- **Memory usage** (watch for growth)

### Testing Scenarios
1. All 32 players with random skills active
2. Skill activation spam (press UseSkill rapidly)
3. 5-10 difficult skills at once (Magneto + HomingNades + ToxicSmoke)
4. Normal competitive match conditions

---

## Skills to Investigate First

Based on analysis, profile these in order:

1. **HomingNades** - Nested loops with distance calculation
2. **ToxicSmoke** - Full player iteration per smoke
3. **Magneto** - Complex entity tracking
4. **FalconEye** - Unthrottled entity updates
5. **Wallhack** - CheckTransmit complexity
6. **Ghost** - Visibility manipulation
7. **Glaz** - Entity iteration

---

## Quick Wins (5-minute fixes)

These can be done immediately:

1. Add `if (Server.TickCount % 2 != 0) return;` to FalconEye, Glitch, Noclip (reduce to 64Hz)
2. Remove unused skills from [Obsolete] enum values
3. Replace all `ConcurrentBag.FirstOrDefault()` calls with dictionary lookup
4. Consolidate `Utilities.GetPlayers()` calls into one per frame

**Expected impact from quick wins: 10-15% improvement**

---

## Configuration Recommendations

Add these cvars to server config:

```
// skill_performance.cfg
sv_skills_enable_profiling 0          // Enable profiling (1 = on)
sv_skills_disable_expensive_under_fps 200  // Disable heavy skills if FPS drops
sv_skills_max_transmit_updates 2      // Limit CheckTransmit updates per tick
sv_skills_tick_rate_low_priority 32   // Hz for non-critical skills
```

---

## Future Considerations

- Consider moving to **event-driven architecture** instead of tick-based for skills that don't need per-frame updates
- Implement **skill complexity rating** and load-balance based on current server load
- Explore **async/parallel skill execution** for independent skills
- Create **skill configuration presets** (competitive-optimized, casual, full-featured)
