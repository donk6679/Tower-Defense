# 守卫核心 Guard the Core

> 一个使用 Unity 制作的 2D 塔防 Demo：建造炮塔、抵御 8 波敌人、守住你的核心。

## 项目简介

《守卫核心》是一款俯视角 2D 塔防小游戏。敌人会沿着固定路线一波波冲向核心，玩家需要利用金币在道路周围建造不同类型的炮塔，通过输出、减速与升级搭配阻止敌人前进。守住全部 8 波即获胜，核心生命归零则失败。

项目从零搭建，包含地图生成、波次调度、炮塔建造与升级、敌人行走动画、战斗特效、完整 UI 流程与音频系统，适合作为 Unity 入门练习与个人作品集项目。

## 玩法特色

- **三种炮塔**：机炮塔（持续输出）、冰霜塔（减速控制）、狙击塔（远程高伤）
- **三种敌人**：小兵、疾行者、重装兵，拥有不同的血量、速度与金币奖励
- **炮塔升级**：最高 2 级，升级后提升伤害、射程或减速效果
- **自由拆除**：拆除炮塔按累计投入返还部分金币
- **自动衔接的波次**：上一波最后一个敌人生成后进入倒计时，玩家可以提前开波
- **完整的局内 UI**：金币、生命、波次使用图标与位图数字显示
- **完整流程**：主菜单 → 战斗 → 胜利 / 失败结算 → 重玩 / 返回主菜单

## 操作说明

1. 点击地图上的**空地**，弹出炮塔选择菜单，点击图标即可建造
2. 点击**已建造的炮塔**，可以升级或拆除，同时显示该炮塔当前的攻击范围
3. 消灭敌人获得金币，金币用于建造与升级
4. 敌人到达核心会扣除生命值
5. 上一波最后一个敌人出现后，屏幕下方出现"下一波"按钮与倒计时进度条：
   - 点击按钮可以提前开始下一波
   - 不点击则进度条走完后自动开始下一波

## 游戏规则

| 项目 | 说明 |
| --- | --- |
| 胜利条件 | 抵御全部 8 波敌人，核心生命大于 0 |
| 失败条件 | 核心生命归零 |
| 初始金币 | 200 |
| 初始生命 | 10 |
| 波次数量 | 8 |
| 炮塔升级 | 最高 2 级 |

## 技术栈

- **引擎**：Unity 2022.3.62f3c1（2D）
- **语言**：C#
- **UI**：Unity UGUI
- **音频**：AudioSource + 自建 AudioManager
- **编辑器工具**：自写 Editor 工具生成地图、UI、Prefab 与图标导入配置

## 快速开始

1. 使用 Unity Hub 添加本项目目录，选择 Unity 2022.3 LTS 打开
2. 打开 `Assets/Scenes/MainMenu.unity`
3. 按 Play，点击"开始游戏"进入战斗

如果只想调试关卡，也可以直接打开 `Assets/Scenes/Main.unity` 运行。

## 打包

1. `File → Build Settings`
2. 平台选择 `Windows, Mac, Linux` → `Windows` → `Intel 64-bit`
3. 场景列表顺序：

```
1. Assets/Scenes/MainMenu.unity
2. Assets/Scenes/Main.unity
```

4. 点击 `Build`，输出目录请选择项目 `Assets` 之外的文件夹
5. 发布时需要把整个输出目录一起打包（exe、`*_Data`、`MonoBleedingEdge`、`UnityPlayer.dll` 等缺一不可）

## 项目结构

```
Assets/
├─ Scenes/                 # MainMenu / Main
├─ Scripts/
│  ├─ Audio/               # AudioManager（音效与 BGM）
│  ├─ Enemies/             # Enemy、WaveManager、WaveSettings、死亡碎片
│  ├─ Effects/             # 弹道拖尾、命中与死亡特效
│  ├─ Game/                # GameManager（金币、生命、胜负）
│  ├─ Map/                 # PathManager、BuildSlot、地块分类
│  ├─ Towers/              # BuildManager、TowerBase、三种炮塔、子弹
│  └─ UI/                  # UIManager、炮塔菜单、HUD 数字、主菜单
├─ Editor/                 # 生成与配置工具（不参与打包）
├─ Prefabs/                # 炮塔、敌人、子弹 Prefab
├─ Resources/
│  ├─ TowerIcons/          # 炮塔图标
│  ├─ Monsters/            # 怪物行走帧
│  ├─ HudIcons/            # HUD 图标与 0-9 数字
│  └─ Audio/               # 音效与 BGM（命名规范见下）
└─ Sprites/                # 地图与 UI 图片
```

## 音效

音频系统会从 `Resources/Audio/SFX/` 与 `Resources/Audio/BGM/` 按名字读取音频文件，缺少的文件会自动跳过，不影响游戏运行。

需要的音效文件名：

| 文件名 | 触发时机 |
| --- | --- |
| `click` | UI 按钮点击 |
| `build` | 建造炮塔 |
| `upgrade` | 升级炮塔 |
| `demolish` | 拆除炮塔 |
| `shoot_gun` / `shoot_frost` / `shoot_sniper` | 三种炮塔开火 |
| `hit` | 子弹命中敌人 |
| `enemy_death` | 敌人被击杀 |
| `life_lost` | 敌人到达核心 |
| `wave_start` | 新一波开始 |
| `early_wave` | 玩家提前开波 |
| `victory` / `defeat` | 胜利 / 失败 |
| `bgm_menu` / `bgm_battle` | 主菜单 / 战斗背景音乐 |

## 素材与授权

- 地图地砖：Kenney 塔防素材（CC0）
- 主菜单与胜利 / 失败结算图：AI 生成图片（豆包），使用或发布前请确认相应授权条款
- 炮塔、怪物、HUD 图标：项目自制 / 基于现有素材修改
- 音频文件：仓库中暂未附带，请自行添加并遵守对应素材的授权协议

## 后续计划

- [ ] 补充音效与背景音乐素材
- [ ] 增加第二张地图与更多敌人类型
- [ ] 增加 Boss 波次与特殊技能
- [ ] 加入设置界面（音量、分辨率）
- [ ] 数值平衡与难度曲线调整

## 作者

- 作者：donk6679
- GitHub: (https://github.com/donk6679)

如果这个项目对你有帮助，欢迎点一个 Star。
