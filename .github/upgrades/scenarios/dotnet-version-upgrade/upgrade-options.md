# Upgrade Options — qr-transport

Assessment: 6 projects, all on modern .NET (net6.0/net6.0-windows), 1 medium-risk WPF project with significant API incidents.

## Strategy

### Upgrade Strategy
All projects are already modern .NET, but the solution includes a medium-complexity WPF application with many API incidents, so incremental buildability is safer.

| Value | Description |
|-------|-------------|
| **Top-Down** (selected) | Upgrade entry-point applications first, temporarily multi-target shared libraries, then consolidate. |
| All-at-Once | Upgrade all projects simultaneously in one atomic pass. |

## Compatibility

### Unsupported API Handling
Assessment reports API-level incompatibilities and behavioral changes (especially in WPF), so complex API handling policy is required.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Resolve all API changes in the same task, including complex replacements. |
| Defer Complex Changes | Stub complex API replacements first and resolve in follow-up subtasks. |

### Unsupported Packages
Assessment reports incompatible/deprecated packages, so package resolution policy is required.

| Value | Description |
|-------|-------------|
| **Resolve Inline** (selected) | Research and resolve incompatible packages directly in upgrade tasks. |
| Defer Resolution | Stub/condition out problematic packages first, then resolve in follow-up tasks. |
| Compatibility Mode | Keep framework references via compatibility assemblies/suppressions where applicable. |

### Windows Native APIs
The solution contains a WPF application and Windows-specific APIs/packages, requiring explicit Windows compatibility posture.

| Value | Description |
|-------|-------------|
| **Windows Compatibility Pack** (selected) | Add Windows compatibility support and keep Windows-only behavior during migration. |
| No Compatibility Pack | Force immediate replacement of Windows-specific APIs for cross-platform readiness. |
