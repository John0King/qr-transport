# 03-final-validation: 全量验证与收尾清理

执行全量恢复、全量构建与测试验证，确认升级后的解决方案在目标框架下稳定。该任务记录仍需后续优化但不阻塞上线的项（如性能调优、可选代码现代化），并整理升级结果用于交付。

同时复核升级选项是否落地一致：Top-Down 执行路径、Unsupported API Fix Inline、Unsupported Packages Resolve Inline、Windows Compatibility Pack 均应在结果中有对应证据。

**Done when**: 解决方案全量构建成功；测试结果满足发布标准；升级结果与遗留事项已记录完成。

