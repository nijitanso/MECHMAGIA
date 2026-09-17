# MechMagia

基于 Godot 4.7 + C#(.NET 8)的六边形格子战棋原型。项目实现了一套完整的兵棋核心循环：六边形地图、算子（部队棋子）堆叠、控制区（ZOC）、最短路径寻路、战斗结算表（CRT）以及交替进行的回合阶段。

## 特性

- 六边形网格地图：使用 `TileMapLayer` + 轴向坐标（Q/R）与偏移坐标（odd-r）互转
- 算子堆叠：多个单位可位于同一地格，按索引偏移绘制并支持栈内提到栈顶
- ZOC（控制区）：敌方控制区会截断移动，进入后无法继续前进
- 移动范围计算：Dijkstra 最短路径，地格进入成本来自瓦片自定义数据层
- 战斗系统：攻击力/防御力点数比 + D6 骰子查 CRT 表，得出撤退或歼灭结果
- 回合与阶段：玩家移动 → 敌方移动 → 玩家攻击 → 敌方攻击，循环推进
- 摄像机：滚轮缩放、中键拖拽平移、WASD/方向键与屏幕边缘平移
- 数据驱动：单位初始配置从 JSON 加载

## 技术栈

| 项 | 说明 |
| --- | --- |
| 引擎 | Godot 4.7（.NET / C# 版本，Forward Plus 渲染） |
| 语言 | C#，目标框架 `net8.0`（Android 平台为 `net9.0`） |
| 工程文件 | `MechMagia.csproj`、`MechMagia.sln` |
| 物理引擎 | Jolt Physics（3D 配置项，当前项目主要为 2D） |
| 主场景 | `res://scene/Main.tscn` |

## 环境要求

- Godot 4.7 的 .NET 版（普通版无法运行 C# 脚本）
- .NET SDK 8.0 或更高版本

## 运行方式

### 使用 Godot 编辑器

1. 用 Godot 4.7 .NET 版打开项目根目录（包含 `project.godot` 的目录）
2. 首次打开会触发 NuGet 还原与 C# 编译
3. 按 F5 或点击运行按钮，加载主场景 `scene/Main.tscn`

### 命令行构建

```bash
# 还原依赖并编译 C# 程序集
dotnet build MechMagia.sln
```

构建产物由 Godot 在运行时加载，命令行编译主要用于提前检查 C# 代码是否能通过。

## 操作说明

| 输入 | 行为 |
| --- | --- |
| 鼠标左键点击算子 | 选中单位，显示移动范围（绿色高亮）与敌方 ZOC |
| 鼠标左键点击地格 | 选中地格，取消当前单位选中 |
| Ctrl + 鼠标左键 | 多选单位（用于多单位联合进攻） |
| 鼠标右键 | 若悬停敌人且满足攻击条件则发起进攻，否则移动到目标地格 |
| 空格 / 回车 | 推进到下一阶段（`TurnManager.NextPhase`） |
| 鼠标滚轮 | 缩放地图（向上放大、向下缩小） |
| 鼠标中键拖拽 | 平移摄像机 |
| WASD / 方向键 | 平移摄像机 |
| 鼠标靠近屏幕边缘 | 边缘平移摄像机 |

## 目录结构

```text
.
├── data/                  # 单位配置数据（UnitSetup.json）
├── extensions/            # 通用扩展方法（列表 Shuffle 等）
├── idea/                  # 设计资料（CRT-V1.xlsx 战斗结算表设计稿）
├── resource/              # 贴图、字体等美术资源
├── scene/                 # 场景与全部 C# 脚本
│   ├── Main.tscn/.cs      # 主场景与总入口，负责实例化单位并广播状态
│   ├── Map.cs             # 地图核心：图结构、ZOC、寻路、堆叠管理
│   ├── Counter.tscn/.cs   # 算子（棋子）节点，承载单位数据与交互
│   ├── UnitStack.cs       # 算子堆叠的数据结构与高亮逻辑
│   ├── UnitData.cs        # 单位数据模型与 JSON 加载
│   ├── AxialCoor.cs       # 六边形轴向坐标工具
│   ├── AttackProcessor.cs # 战斗结算处理器（CRT）
│   ├── TurnManager.cs     # 回合与阶段管理
│   ├── MouseManager.cs    # 鼠标悬停/选中状态管理
│   ├── MapInteraction*.cs # 地格高亮、移动范围、ZOC 等显示层
│   └── Camera2d.cs        # 摄像机平移与缩放
├── project.godot          # Godot 工程配置
└── MechMagia.csproj       # .NET 工程配置
```

## 核心机制

### 坐标与地图

地图是六边形网格，使用 `TileMapLayer`。脚本内部用 `AxialCoor`（Q/R 轴向坐标）做邻居与距离计算，渲染和瓦片查询用 `Vector2I` 偏移坐标（odd-r），两者通过 `OffsetToAxial` / `AxialToOffset` 互转。地格的移动力进入成本存储在瓦片自定义数据层 `MpEntryCost`。

### 移动与 ZOC

`Map.GetHexMpList` 在单位被选中时调用 Dijkstra 计算可达地格，把进入成本不超过 `MP` 的地格高亮为绿色。规则要点：

- 敌方单位所在地格不可进入，也不会被算法继续扩展
- 进入敌方控制区（ZOC）后无法继续移动，因此 ZOC 地格不会继续向外扩展
- 若单位本身处于敌方 ZOC 内，移动范围被限制为相邻地格
- 移动结束后 `MoveLeft -= 1`，移动力耗尽即不再显示移动范围

### 算子堆叠

同一地格上的单位由 `UnitStack` 管理，栈内单位按索引以固定偏移错位显示，只有栈顶单位的碰撞体启用，因此只有栈顶可被点击。栈内单位数量大于 1 时生成 `StackMask`（`Area2D`）用于整栈悬停，并支持半透明高亮除悬停单位外的成员。

### 战斗结算（CRT）

战斗流程位于 `AttackProcessor`：

1. 计算点数比 `ratio = 总AP / 总DP`
2. 比值小于 0.5 时不允许进攻
3. 比值向下取整作为 CRT 列索引，骰子点数作为行索引，查表得到战斗结果

结果枚举与含义：

| 结果 | 含义 |
| --- | --- |
| `DR` / `DR2` / `DR3` | 防守方撤退 1 / 2 / 3 格 |
| `DE` | 防守方被歼灭 |
| `AR` | 进攻方撤退 1 格 |
| `AR1` / `AR2` | 进攻方损失 1 / 2 点 AP 并撤退 |
| `AE` | 进攻方被歼灭 |

撤退路径由 `Dijkstra.GetRetreatPath` 自动生成：连续三步在邻居中挑选“离最近敌人最远”的地格，避开敌方控制区与敌方单位；若因阻挡无法撤退，则该单位被歼灭。CRT 表的数据来源于 `idea/CRT-V1.xlsx`。

### 回合与阶段

`TurnManager` 管理回合状态。按空格或回车调用 `NextPhase` 切换：

```text
玩家移动 → 敌方移动 → 玩家攻击 → 敌方攻击 → 回合数 +1 → 玩家移动 ...
```

切换阶段时通过信号通知所有算子刷新移动力（`RefreshMovement`）或攻击力（`RefreshAttack`）。

## 架构说明

项目采用单例（autoload）+ 信号驱动的结构，`project.godot` 中注册了三个自动加载节点：

| 单例 | 职责 |
| --- | --- |
| `MouseManager` | 维护悬停/选中状态、被选中单位序列、悬停堆叠 |
| `AttackProcessor` | 记录进攻双方、骰子与 CRT 结果，广播 `Attack` 信号 |
| `TurnManager` | 维护当前回合、阵营与阶段，广播阶段切换信号 |

数据流向大致为：`Main` 从 JSON 加载单位并实例化 `Counter` → `Counter` 通过 `MoveUnit`、`SelectUnit`、`RemoveCounter` 等信号上报状态 → `Main` 广播 `UnitsUpdate` → `Map` 重新计算 `CoorWithUnit`、ZOC 与堆叠。地图交互只通过 `MapInteraction*` 三层 `TileMapLayer` 叠加高亮瓦片，不改动逻辑数据。

## 单位数据格式

单位配置位于 `data/UnitSetup.json`，由 `UnitDataJson.Initialize` 在启动时反序列化为 `UnitInfo` 列表。

```json
{
  "UnitSetup": [
    { "ID": 1, "PosX": 2, "PosY": 6, "Team": 0, "AP": 2, "DP": 3, "MP": 2, "MoveLeft": 1 }
  ]
}
```

| 字段 | 说明 |
| --- | --- |
| `ID` | 单位标识，同时用于回合开始时摄像机的聚焦顺序 |
| `PosX` / `PosY` | 初始偏移坐标 |
| `Team` | `0` 友军、`1` 敌军、`2` 中立（对应 `TeamEnum`） |
| `AP` | 攻击力 |
| `DP` | 防御力 |
| `MP` | 移动力 |
| `MoveLeft` | 剩余移动次数 |

## 开发状态

这是一个处于开发中的原型，以下部分是已知的待改进点（代码中以 `TODO` 标注）：

- `Map` 中的鼠标检测逻辑集中在 `UpdateMouseTracking`，计划拆分为子方法
- `Counter` 的事件订阅分散在发布者侧，中长期考虑引入事件总线统一管理
- 部分早期实现（如 `MouseManager` 中的按键时长统计、`UpperLayer`）已不再使用，保留待清理

## 许可

本项目未附带显式的开源许可证文件，使用前请先与仓库所有者确认授权。
