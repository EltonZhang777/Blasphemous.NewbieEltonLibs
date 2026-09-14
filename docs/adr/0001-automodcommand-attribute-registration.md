# ADR-0001: AutoModCommand 用 Attribute + 反射自动注册子命令

## Status

accepted

## Context

上游 `Blasphemous.CheatConsole.ModCommand`（不可修改）要求每个子类手写 `AddSubCommands()` 返回 `Dictionary<string, Action<string[]>>`，且 help 文档只能通过手动注册的 `help` 子命令逐行 `Write` 输出。现有 mod（如 Blasphemous.LocalizationPatcher）每个命令约 150-220 行，其中注册与 help 样板约占 1/3。本仓库作为多个 mod 的前置库，希望子命令注册与帮助文档声明式完成。

## Decision

新增 `AutoModCommand : ModCommand`，配合 `ModSubCommandAttribute`（`AttributeTargets.Method`，`AllowMultiple = true`）实现声明式注册：

- 子类继承 `AutoModCommand`，在子命令处理方法上标注 `[ModSubCommand(name, description, usage = null, params validLengths)]`
- `AutoModCommand` 覆盖 `AddSubCommands()`，通过 `GetType().GetMethods(Instance | Public | NonPublic)` 反射收集带 Attribute 的方法，用 `Delegate.CreateDelegate` 绑定为 `Action<string[]>`，与 `AddCustomSubCommands()` 手写结果合并
- `help` 由基类自动注册（列表第一、其余字母序；`usage` 缺省回退为子命令名），子类可用同名 Attribute 覆盖
- 非法声明（重复子命令名、非 `void(string[])` 签名、static 方法）fail fast 抛 `InvalidOperationException`
- `AllowUppercase` 默认 `true`（virtual），子命令名原样入字典；`validLengths` 指定时自动校验参数个数，错误消息复用现有 `ValidateParameterList` 文案

## Considered Options

- **纯手写 `AddSubCommands()`**（上游现状）：无需反射，但每个命令重复注册样板，help 易与实现脱节
- **纯反射、无手写扩展点**：API 最简，但既有手写子命令无法渐进迁移
- **选中方案：Attribute + 反射 + 手写追加扩展点**：新命令声明式编写，旧命令可逐步迁移

## Consequences

- 反射发生在首次 `ProcessCommand`（`availableCommands ??= AddSubCommands()` 缓存一次），热路径无反射开销
- 子命令名大小写敏感（`AllowUppercase = true` 时），Attribute 名需与用户输入一致
- 依赖 net35 可用 API：`GetMethods(BindingFlags)`、`Delegate.CreateDelegate`、非泛型 `GetCustomAttributes` 均可
- 声明非法时在开发期抛异常而非运行时静默失败，需在发布前验证所有命令
