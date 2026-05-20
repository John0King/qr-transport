# .NET Version Upgrade Progress

## Overview

将 `qr-transport` 解决方案从 .NET 6 升级到 .NET 10，采用 Top-Down 策略优先保障应用入口升级与可构建性。执行中按已确认选项处理 API 与包兼容问题，并保持 Windows 兼容能力。
**Progress**: 0/3 tasks complete <progress value="0" max="100"></progress> 0%
**Progress**: 0/3 tasks complete <progress value="0" max="100"></progress> 0%

## Tasks
- 🔄 01-prerequisites: 升级前置检查与环境对齐 ([Content](tasks/01-prerequisites/task.md))
- 🔲 01-prerequisites: 升级前置检查与环境对齐
- 🔲 02-upgrade-applications: 升级应用入口项目并同步依赖项目到 .NET 10
- 🔲 03-final-validation: 全量验证与收尾清理
