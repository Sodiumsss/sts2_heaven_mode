# HeavenMode

[中文](./README.md) | [English](./README.en.md)

## 简介

**Heaven** 为游戏新增了 **10 个全新的难度等级**。  
每一个难度都会加入新的挑战机制，使玩家在尖塔中面对更加严苛和多变的考验。

要解锁第一个 Heaven 难度，玩家 **必须先通关官方难度 10**。

如果你希望直接解锁全部难度，也可以手动开启：

1. 安装 Mod 后 **先启动一次游戏**。
2. 打开 Mod 文件夹。
3. 找到配置文件，将 **`unlock` 字段改为 `true`**。

完成后，所有 Heaven 难度都会被解锁。

## 开发者说明

运行 `tools/build_release.ps1` 前，请确保脚本中的 `$godot` 变量指向一个有效的 Godot 可执行文件，例如 **MegaDot 4.5.1**（`https://megadot.megacrit.com/`），或替换为本机上任意可用的路径。

### 参考文档

`docs/ref/` 目录包含本 Mod 开发过程中整理的游戏内部类参考笔记，每个文件对应一个被 Hook 的游戏类，记录了：

- 相关方法签名与关键逻辑
- 各 Heaven 难度采用的 Harmony Patch 方案（Prefix / Postfix）及选择原因
- 实现时的注意事项与边界情况

在开发新难度或修改现有机制时，建议先查阅对应的参考文档，了解游戏原版行为后再决定 patch 位置。
