# 实现笔记

记录每个功能是怎么在游戏程序集里定位到的，以及踩过的坑。游戏版本：Unity 6000.3.26f1，
`Assembly-CSharp.dll` 未混淆，Mono 运行时，可直接反编译。

---

## 1. 游戏速度

`GameSpeed` 是枚举（`Paused / Zero / One / Two / Three / Four / Eight`），数值不是倍率本身。

`GameSpeedManager.SetGameSpeedAndTimeScale(GameSpeed)` 的实际逻辑：

```
Time.timeScale = GameplaySettings.GetGameSpeedSettings().GameplayTimeScale
_waterTimeScale = ... .WaterTimeScale
Time.fixedDeltaTime = 0.033f
```

所以补丁是在它之后读一次 `Time.timeScale` 作为"基准值"，再按倍率放大。
基准值单独保存，避免多次触发时被反复相乘。

兜底：补丁被内联时，直接从 `GameplaySettings.GameSpeedSettings.GameplayTimeScale` 反射读基准。

---

## 2. 材料编辑

注册表：`GameSettings.Instance.ItemSettings.ItemProperties`（Unity 数组，全量物品定义）。

| 操作 | 接口 |
|---|---|
| 统计 | `Storage.ReturnStoredItemCount(ItemProperties)` |
| 增加 | `new Item(props)` → `Inventory.AddItem(item, SubInventoryType.Storage)`；满了退回 `SubInventory.AddItem(item, true)` |
| 删除 | `Inventory.RemoveItem(props, SubInventoryType)` |

### 踩过的坑

`Storage.ReturnItem(ItemProperties, SubInventoryType)` 名字像"取出物品"，但它的实现是：

```
_storageInventory.ReturnItemFromProperties(props)
  → TryReturnSlot(...)
  → IInventorySlot.PeekItem()      // 只读！
```

**它只 Peek 不删除。** 用它做删除，界面数量会归零但物品还在仓库里。
真正的删除路径是 `Inventory.RemoveItem` → `SubInventory.TakeItem`。

---

## 3. 打捞加速

单件打捞没有固定秒数，是动画事件驱动的：

```
SalvageTaskBase.SalvageItem
  → ItemToHaul.IncrementStateCoroutine(rigEventType, attribute)
    → TransferItemCoroutine:
          AnimatorHelper.SetFloat(agent, "Speed Multiplier", 属性修正)
          循环等 rig 事件，直到 _drifterEventAmount >= animationCycles
          结束时 SetFloat(agent, "Speed Multiplier", 1)
    → 状态推进
```

所以加速 = 放大 `"Speed Multiplier"`。因为动画播放变快，事件更早触发，整段搬运同步变快。

`Salvager.ReturnSalvageTime(Item)` 是打捞站建筑的时长（按 `SalvageableCategory.SalvageTime` 查表），
单独用 Postfix 缩短。

### 指定物品加速

`TransferItemCoroutine` 是私有方法，Prefix 可以拿到 `ItemToHaul` 和 `Agent`，
在那里记录"这一趟搬的是哪个物品"；随后 `SetFloat` 命中关键字时再乘一次倍率。
只在本趟开始的那次调用生效（结束时那次重置为 1 不参与），避免影响后续动作。

---

## 4. 打捞配额（船数与装载）

原版判定：

```
ProjectAssignment.ReturnCanHaulItem(item)
    = ReturnRemainingItemToHaulCount() < ReturnItemHaulingCapacity()

ProjectAssignment.ReturnItemHaulingCapacity()
    = 有船 ? 船的 Inventory.ReturnStorageCapacity()
           : 漂流者 Inventory.ReturnStorageCapacity()
```

`Marker.PopulateItemsToHaul` / `LandmarkActionSalvage.PopulateItemsToHaul` 的循环是
"只要还装得下就继续塞给同一个人"，所以物品数 ≤ 容量时，第一个人全包，其余人没活干，
表现就是"只派一艘船"。

补丁在 `AddItemToHaul` 上做 Prefix：达到当前模式的配额就返回 `false`
（这正是原版塞满时的返回值，调用方循环会自然停止），物品流向其他人。

### 目标冲突

- 船要装满 ⇒ 物品少时只能派一条船
- 要多船并行 ⇒ 每条船只装一部分

两者不可兼得，所以做成模式：

| 模式 | 公式 |
|---|---|
| 智能 | `total <= capacity` ? `ceil(total/agents)` : `ceil(total / min(agents, ceil(total/capacity)))` |
| 满载 | 不限制（原版） |
| 均分 | `ceil(total/agents)`，剩余零头归最后一个到场的人 |
| 每人固定 | 固定 N 件 |

`total` 取自 `Marker.SalvageableItemsInRadius.Count`。

---

## 5. 最近的船

排查结论：`Community.TryReturnClosestMooringpointWithAvailableBoat(Vector3, BoatType, out MooringPointBase)`
里确实写的是"遍历停泊点，选离目标最近的、有可用船的那个"，**但它在整个程序集里 0 处引用，是死代码**。

实际决定派谁去的是 `Project.ReturnAgentPriority(Agent)`，返回值越大越优先；船跟随船长，
所以"派哪条船"等价于"选了哪个船长"。

补丁：Postfix 该方法，对打捞类项目给"船长船只离目标更近"的人加
`2000 / (1 + 距离)` 的分值。量级只够影响同项目内部排序。

---

## 6. 生存与研究

- `PajamaLlama.Fltsm.DietVital.IsCauseOfDeath()` → `Amount < Limit`，Postfix 强制 `false` 即可不饿死渴死
- `Community.PlayerCommunity.Research.ResearchPoints` 是 public 字段，每帧写值
- `CommunityResearch.get_CurrentProgress` 返回 `CurrentResearch.Progress`，Postfix 改成 `CurrentCost` 即瞬间完成

---

## 7. 存档格式（未在本项目中使用，供参考）

`Saves/*.fs` 是双层 gzip + JSON：

```
文件 = gzip(JSON{ "_info": ..., "_data": [gzip 字节数组] })
_data 解压后 = JSON（Newtonsoft，带 "__type" 类型标注）
```

理论上可以写离线存档编辑器，本项目选择运行时修改，避免破坏文件结构。

---

## 工具

`tools/AsmDump` 是本项目自用的分析工具（Mono.Cecil）：

```powershell
# 导出类型与成员
dotnet run --project tools/AsmDump -- <Assembly-CSharp.dll> out.txt "关键字正则"

# 反汇编某个方法
dotnet run --project tools/AsmDump -- il <Assembly-CSharp.dll> <类型全名> <方法名>

# 查找某个成员的调用方
dotnet run --project tools/AsmDump -- find <Assembly-CSharp.dll> <成员名> [类型名片段]
```
