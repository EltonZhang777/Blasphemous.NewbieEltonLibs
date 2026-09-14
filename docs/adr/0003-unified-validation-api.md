---
status: accepted
---

# 统一静态验证 API 与迁移边界

将通用谓词验证集中到 `Blasphemous.NewbieEltonLibs.Extensions.System.ValidationUtils`。`Validate<T>(T obj, Func<T, bool> validate, bool logToModLog = true, bool throwError = false)` 返回单次谓词执行结果；失败时按开关写入 `ModLog` 和抛出通用 `ArgumentException`，错误文案沿用现有 `` `{obj}` of type `{typeof(T)}` isn't a valid argument``。`validate` 为空抛 `ArgumentNullException`，谓词自身的异常原样传播。`logToModLog: false, throwError: false` 覆盖静默布尔验证，因此不新增 `TryValidate`。

保留 `TraverseUtils.Validate(..., bool throwError = false)` 并标记 `[Obsolete]`，以 `logToModLog: true` 转发到新 API；保持旧签名、日志/抛异常语义和错误文案，同时修复谓词重复执行。`SetValueIfValidated` 迁移到新 API，并在存储参数校验中显式关闭日志；`AnimationStorage` 与 `SpriteStorage` 的参数失败改用通用 `ArgumentException`，但重复注册、替换缺失资源等注册状态错误保持原有异常。`AnimationInfo` 与 `AnimationImportInfo` 的错误聚合留给 S2。

`AutoModCommand` 的结构/重复命令校验、`ModCommandExtensions.ValidateParameterList` 的控制台输出、`CheatConsoleLogging` 的枚举范围校验以及上游 `ModCommand` 不机械迁移：它们分别具有 fail-fast、用户可见输出、专属异常或上游所有权语义。

## Considered options

- 将 `TryValidate` 作为第二个公开入口：拒绝，因为两个开关已经表达静默失败，额外入口不增加行为。
- 继续把通用验证放在 `TraverseUtils`：拒绝，因为它不依赖遍历，且会继续把通用 API 绑定到游戏遍历工具。
- 统一替换所有条件分支：拒绝，因为会丢失领域异常、注册状态和控制台输出语义。
