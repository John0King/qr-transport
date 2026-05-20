# .NET Version Upgrade Plan

## Overview

**Target**: 将 `qr-transport` 解决方案从 .NET 6 升级到 .NET 10（含 `net6.0-windows` 到 `net10.0-windows`）。
**Scope**: 6 个项目（Web、WPF、类库、测试），约 3.2k LOC；核心风险集中在 WPF API 兼容与少量包兼容问题。

## Tasks

### 01-prerequisites: 升级前置检查与环境对齐

确认本地与仓库构建环境可支持 .NET 10：验证 SDK 可用性、处理 `global.json`（若存在）并确保恢复/构建链路在升级前处于可执行状态。该任务同时确认升级边界（仅目标框架与依赖升级，不做额外架构重构），以避免后续执行阶段范围漂移。

此任务还将核验当前方案中的项目目标框架分布与启动配置，确保后续按 Top-Down 方式推进时，应用项目升级路径明确，且不会因工具链差异引入非代码问题。

**Done when**: .NET 10 SDK 可用且与仓库配置一致；升级边界与顺序已固定；当前方案可恢复并可进入项目升级任务。

---

### 02-upgrade-applications: 升级应用入口项目并同步依赖项目到 .NET 10

先升级应用入口项目（ASP.NET Core Web 与 WPF）到 .NET 10，再同步其依赖类库与测试项目目标框架，确保应用可在新框架上构建运行。该任务覆盖所有项目的 TFM 变更、必要的 NuGet 升级与不兼容项修复（含 assessment 标记的包问题）。

针对风险较高的 WPF 项目，按“Fix Inline”策略直接修复 API 不兼容与行为变化点；对 Windows 能力保持“Windows Compatibility Pack”策略，确保升级后仍维持 Windows 运行语义。对不兼容/弃用包采用“Resolve Inline”策略在本任务内完成替换或版本调整，不留存桩实现。

**Done when**: 所有项目目标框架升级到 .NET 10（含 windows 变体）；包兼容问题完成内联修复；解决方案构建通过且关键测试通过。

---

### 03-final-validation: 全量验证与收尾清理

执行全量恢复、全量构建与测试验证，确认升级后的解决方案在目标框架下稳定。该任务记录仍需后续优化但不阻塞上线的项（如性能调优、可选代码现代化），并整理升级结果用于交付。

同时复核升级选项是否落地一致：Top-Down 执行路径、Unsupported API Fix Inline、Unsupported Packages Resolve Inline、Windows Compatibility Pack 均应在结果中有对应证据。

**Done when**: 解决方案全量构建成功；测试结果满足发布标准；升级结果与遗留事项已记录完成。
