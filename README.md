# Flotsam Trainer

《Flotsam》单机修改器 · Unofficial single-player trainer for **Flotsam**

以 **BepInEx 5 + HarmonyX** 插件形式实现，游戏内按 `F1` 打开面板。
所有补丁点都经过 IL 反汇编核实，不是靠内存扫描猜出来的数值。

An in-game trainer built on **BepInEx 5 + HarmonyX**. Press `F1` in game to open the panel.
Every patch target was verified by disassembling `Assembly-CSharp.dll` — no blind memory scanning.

> 非官方项目，与 Pajama Llama Games 无关。仓库不包含任何游戏资源。
> Unofficial project, not affiliated with Pajama Llama Games. No game assets are included.

---

## 目录 · Table of Contents

- [功能 · Features](#功能--features)
- [打捞配额详解 · Salvage quota explained](#打捞配额详解--salvage-quota-explained)
- [环境要求 · Requirements](#环境要求--requirements)
- [安装 · Installation](#安装--installation)
- [使用 · Usage](#使用--usage)
- [从源码构建 · Building from source](#从源码构建--building-from-source)
- [实现原理 · How it works](#实现原理--how-it-works)
- [常见问题 · Troubleshooting](#常见问题--troubleshooting)
- [注意事项 · Disclaimer](#注意事项--disclaimer)
- [License](#license)
- [English](#english)

---

## 功能 · Features

| 模块 / Module | 说明 / Description |
|---|---|
| **游戏速度**<br>Game speed | 0.5x–10x 实时倍率，`F2` 减速 / `F3` 加速 / `F4` 开关<br>Live 0.5x–10x multiplier with hotkeys |
| **材料编辑**<br>Item editor | 搜索任意材料（中英文名均可），直接把仓库总数设为指定值<br>Search any item, set the total amount stored in your warehouses |
| **打捞加速**<br>Salvage speed | 放大打捞/搬运动画播放速度，1x–5x<br>Speeds up the pickup/dropoff animation used by salvage and hauling |
| **指定物品加速**<br>Per-item speed | 只给指定物品（默认「金属废料」）额外加速，1x–8x<br>Extra speed for one item type only (default: metal scrap) |
| **打捞配额**<br>Salvage quota | 四模式：智能 / 满载 / 均分 / 每人固定<br>Four dispatch modes — see below |
| **最近船优先**<br>Nearest boat | 给离目标更近的船长加优先级，减少舍近求远<br>Prefer captains whose boat is closer to the salvage target |
| **生存**<br>Survival | 不会饿死 / 渴死，不影响正常吃饭喝水<br>Drifters no longer die from hunger or thirst |
| **研究**<br>Research | 无限研究点、研究瞬间完成<br>Unlimited research points and instant research |

---

## 打捞配额详解 · Salvage quota explained

原版有个让人费解的行为：**打捞点物品很少时，无论你设了几个人，只会派一艘船去。**
原因是物品被同一个漂流者一次装满（`ProjectAssignment` 的容量判定按背包/货舱算）。

Vanilla has a quirk: when a salvage site holds few items, only **one** boat is dispatched
regardless of how many drifters you assigned — because the first drifter fills their pack
and nobody else gets any work.

但"船装满"和"多船并行"本质互斥：

- 船要装满 → 物品少时只能派一条船
- 要多船并行 → 每条船只能装一部分

所以做成了四种模式 / Hence four modes:

| 模式 / Mode | 规则 / Rule | 适用 / Use when |
|---|---|---|
| **智能** Smart *(默认 default)* | 物品 ≤ 单船容量 → 均分并行；物品 > 单船容量 → 按需要的船数满载<br>Items ≤ one boat → split evenly; more than one boat → load each boat to capacity | 通用 general |
| **满载** Full load | 不限制，装到容量为止（≈原版）<br>No cap, fill to capacity | 效率优先 efficiency |
| **均分** Even split | `ceil(总数 / 人数)`，剩余零头归最后一个到场的人<br>Even share, remainder goes to the last drifter on site | 强制多船 force multi-boat |
| **每人固定** Fixed | 固定件数（默认 6）<br>Fixed number of items per drifter | 手动微调 manual |

示例 / Examples：

- 20 件、单船容量 10、指派 3 人 → 智能模式需要 2 条船 → 每人 10 件，两条船装满一趟结束
- 10 件、单船容量 10、指派 5 人 → 智能模式不足一船 → 每人 2 件，五条船并行

---

## 环境要求 · Requirements

- Flotsam（Unity 6000.3.x / Mono 运行时；Epic 与 Steam 版本均可）
- Windows x64
- 构建需要 .NET SDK 8 / Building requires .NET SDK 8

## 安装 · Installation

```powershell
# 1. 构建 / Build（需要 .NET SDK 8）
powershell -ExecutionPolicy Bypass -File .\build.ps1 -GameManagedDir "D:\epic\Flotsam\Flotsam_Data\Managed"

# 2. 安装 / Install
powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1 -GameDir "D:\epic\Flotsam"
```

`install.ps1` 会做三件事 / install.ps1 does three things:

1. `vendor/bepinex` 不存在时，自动下载 BepInEx 5.4.23.6 win_x64（GitHub Releases，失败自动切镜像）
   Downloads BepInEx when missing, with mirror fallback
2. 复制 `winhttp.dll`、`doorstop_config.ini`、`.doorstop_version`、`BepInEx/` 到游戏根目录
   Copies the runtime files into the game root
3. 把 `dist/FlotsamTrainer.dll` 放进 `BepInEx/plugins/`
   Drops the plugin into `BepInEx/plugins/`

若游戏目录已有 BepInEx，会先整体备份成 `BepInEx_backup_时间戳`。
An existing `BepInEx/` folder is backed up first.

## 使用 · Usage

1. 启动游戏并进入存档 / Launch the game and load a save
2. 按 `F1` 开关面板 / Press `F1` to toggle the panel
3. 配置文件 / Config: `<游戏目录>\BepInEx\config\local.flotsam.trainer.cfg`

## 卸载 · Uninstall

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall.ps1 -GameDir "D:\epic\Flotsam"
```

只删除 `winhttp.dll`、`doorstop_config.ini`、`.doorstop_version`、`BepInEx/`，游戏本体不动。
Removes only the injected files; the game itself is never modified.

---

## 从源码构建 · Building from source

```powershell
dotnet build .\src\FlotsamTrainer\FlotsamTrainer.csproj -c Release `
    -p:GameManagedDir="D:\epic\Flotsam\Flotsam_Data\Managed"
```

编译需要引用游戏自带的托管程序集（`Assembly-CSharp.dll`、`UnityEngine.*.dll`）。
这些文件不在仓库中，需要你本机已安装游戏。

Building references the game's managed assemblies, which are **not** shipped in this repo —
you need the game installed locally.

改完代码后一键编译并覆盖安装 / Rebuild and redeploy:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy.ps1 -GameDir "D:\epic\Flotsam"
```

---

## 实现原理 · How it works

游戏是 Unity + **Mono** 运行时，`Assembly-CSharp.dll` 未混淆，可完整反编译，
因此所有功能都是方法级 Harmony 补丁，而不是内存扫描。

The game runs on Unity + **Mono** with an unobfuscated `Assembly-CSharp.dll`, so every feature
is a method-level Harmony patch rather than a memory scan.

| 功能 / Feature | 目标 / Target |
|---|---|
| 游戏速度 / Game speed | `GameSpeedManager.SetGameSpeedAndTimeScale` |
| 材料编辑 / Item editor | `Inventory.AddItem`、`Inventory.RemoveItem`、`Storage.ReturnStoredItemCount` |
| 打捞加速 / Salvage speed | `AnimatorHelper.SetFloat("Speed Multiplier")`、`Salvager.ReturnSalvageTime` |
| 打捞配额 / Salvage quota | `ProjectAssignment.AddItemToHaul` |
| 最近船 / Nearest boat | `Project.ReturnAgentPriority` |
| 不死 / No starvation | `PajamaLlama.Fltsm.DietVital.IsCauseOfDeath` |
| 研究 / Research | `CommunityResearch.get_CurrentProgress`、`ResearchPoints` |

详细的 IL 依据和踩坑记录见 [docs/design.md](docs/design.md)，包括：

- `Storage.ReturnItem` 名字像"取出物品"，实际只 `PeekItem`，不能用来删物品
- `Community.TryReturnClosestMooringpointWithAvailableBoat` 是一段 0 引用的死代码
- 打捞时长由动画事件驱动，不存在"每件固定秒数"这种配置

See [docs/design.md](docs/design.md) for the full reverse-engineering notes.

---

## 常见问题 · Troubleshooting

**按 F1 没反应 / Panel does not open**
查看 `<游戏目录>\BepInEx\LogOutput.log`，搜索 `[补丁成功]` 与 `[补丁失败]`。
若整个日志都没有 `Flotsam Trainer` 字样，说明 BepInEx 没注入成功，通常是被杀软拦了 `winhttp.dll`。

Check `BepInEx\LogOutput.log` for `[补丁成功]` / `[补丁失败]`. If the plugin never appears,
BepInEx injection failed — usually antivirus blocking `winhttp.dll`.

**材料搜索不到 / Item cannot be found**
本地化名可能没加载，换成英文关键字（如 `Metal`、`Scrap`）再试，面板上的「已匹配」会显示命中项。

**打捞配额好像没生效 / Quota seems ignored**
确认打捞点上指派的人数 > 1；另外「满载」模式就是原版行为，不会有变化。

**游戏更新后失效 / Broken after a game update**
方法签名可能变了，日志里会出现 `[补丁失败]`。用 `tools/AsmDump` 重新核对方法名即可，插件不会崩溃游戏
（每个补丁独立 try/catch）。

---

## 注意事项 · Disclaimer

- **仅限单机使用。** Flotsam 没有联机模式，本插件不接触任何在线服务。
  Single-player only. Flotsam has no multiplayer and this plugin never touches online services.
- 修改后的数值会随正常存档流程写入存档，卸载插件不会自动还原。
  Modified values persist in your save; uninstalling does not revert them.
- 修改前请自行备份 `%USERPROFILE%\AppData\LocalLow\Pajama Llama Games\Flotsam\Saves\`。
  Back up your saves before using.
- 杀毒软件可能对 `winhttp.dll`（UnityDoorstop 代理注入）报警，属于误报。
  Antivirus may flag `winhttp.dll` (UnityDoorstop proxy injection) — a false positive.
- 本项目只修改本机内存中的游戏状态，不修改游戏文件、不绕过任何 DRM。
  This project only alters in-memory game state; it does not modify game files or bypass DRM.

## License

MIT — 见 [LICENSE](LICENSE)。

---

# English

**Flotsam Trainer** is an in-game trainer for the single-player game *Flotsam*, implemented as a
BepInEx 5 + HarmonyX plugin. Press `F1` in game to open the panel.

### Features

| Module | Description |
|---|---|
| Game speed | Live 0.5x–10x multiplier (`F2` slower, `F3` faster, `F4` toggle) |
| Item editor | Search any item by localized or asset name and set the total stored amount |
| Salvage speed | Speeds up the pickup/dropoff animation used by salvage and hauling (1x–5x) |
| Per-item speed | Extra multiplier for a single item type, e.g. metal scrap (1x–8x) |
| Salvage quota | Smart / Full load / Even split / Fixed, see the table above |
| Nearest boat | Biases agent priority toward captains whose boat is closer to the target |
| Survival | Drifters never die of hunger or thirst |
| Research | Unlimited research points, instant research |

### Why the salvage quota exists

Vanilla assigns all items to the first drifter that arrives, until their pack (or the boat's hold)
is full. With a small salvage site you therefore see a single boat sent out no matter how many
drifters you assigned. Loading boats to capacity and running boats in parallel are mutually
exclusive goals, so the trainer exposes four modes instead of hard-coding one behaviour.

### Install

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -GameManagedDir "D:\epic\Flotsam\Flotsam_Data\Managed"
powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1 -GameDir "D:\epic\Flotsam"
```

The install script downloads BepInEx 5.4.23.6 if needed, backs up any existing `BepInEx/`, copies the
runtime files into the game root and drops `FlotsamTrainer.dll` into `BepInEx/plugins/`.

### Uninstall

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall.ps1 -GameDir "D:\epic\Flotsam"
```

### Build requirements

- .NET SDK 8
- A local install of Flotsam (the project references its managed assemblies; they are not shipped here)

### Notes

- Single-player only; the game has no multiplayer mode.
- Modified values are written into your save files by the game itself. Back up
  `%USERPROFILE%\AppData\LocalLow\Pajama Llama Games\Flotsam\Saves\` first.
- Antivirus software may flag `winhttp.dll`; that is UnityDoorstop's proxy injection, a false positive.
- After a game update the method signatures may change and patches will report failures in the log.
  The plugin fails per-patch and never takes the game down with it.

### License

MIT.
