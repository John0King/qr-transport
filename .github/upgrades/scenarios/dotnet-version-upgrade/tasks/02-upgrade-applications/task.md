# 02-upgrade-applications: 升级应用入口项目并同步依赖项目到 .NET 10

先升级应用入口项目（ASP.NET Core Web 与 WPF）到 .NET 10，再同步其依赖类库与测试项目目标框架，确保应用可在新框架上构建运行。该任务覆盖所有项目的 TFM 变更、必要的 NuGet 升级与不兼容项修复（含 assessment 标记的包问题）。

针对风险较高的 WPF 项目，按“Fix Inline”策略直接修复 API 不兼容与行为变化点；对 Windows 能力保持“Windows Compatibility Pack”策略，确保升级后仍维持 Windows 运行语义。对不兼容/弃用包采用“Resolve Inline”策略在本任务内完成替换或版本调整，不留存桩实现。

**Done when**: 所有项目目标框架升级到 .NET 10（含 windows 变体）；包兼容问题完成内联修复；解决方案构建通过且关键测试通过。

---

