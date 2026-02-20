# VSCode Extension 产品发布准备文档（Release Readiness）

> 适用对象：`MARS Java UI Automation` Extension（含 Java Agent / HighlightOverlay / ProcessInfo）
> 
> 更新日期：2026-02-20

## 1. 发布目标

将当前 Extension 从“功能可用”提升为“可对外稳定发布”，覆盖功能完整性、易用性、稳定性、文档、兼容性与运维能力。

---

## 2. 发布分级要求

## P0（必须完成，发布门槛）

### 2.1 核心功能闭环验收

- 录制 → 可视化编辑 → 保存 → 重载 → 回放 全流程通过。
- 重点回归项全部通过：
  - `record&replay` 之后，点击 object tree item **不会**错误创建新的 visual 节点。
  - `SearchAndUpdate` 在回放时目标 cell 能进入编辑态并完成更新。
  - 在 Visual 中修改 `data` 后，Test Steps 表格与实际执行数据保持一致。
  - 当 `IsHighlightObjectWhileReplay=true` 时，回放执行 keyword 前先高亮目标对象。

### 2.2 配置体系定稿（Extension + Java Agent）

- `marsJavaAgent-config.json` 可随 JAR 一起分发并被 agent 读取。
- 明确配置优先级：
  1. 用户/部署侧外部配置
  2. 工作区配置
  3. JAR 内默认配置
- 配置字段建议使用布尔值而非字符串：

```json
{
  "IsHighlightObjectWhileReplay": true
}
```

### 2.3 稳定性与错误处理

- 关键异常场景有清晰错误提示与恢复策略：
  - Agent 未注入成功
  - 端口冲突 / WebSocket 连接失败
  - 对象定位失败
  - 回放超时
  - 目标窗口未聚焦
- 关键链路具备可追踪日志（extension + java agent + helper 进程）。

### 2.4 发布质量门禁

- 建立最低测试门槛：单元测试 + 集成测试 + 冒烟测试。
- CI 阶段自动执行：`lint` / `build` / `test` / `package`。
- 任一门禁失败禁止发布。

---

## P1（强烈建议，影响易用性与口碑）

### 3.1 易用性增强

- 表格编辑行为完善：`Enter` 提交，`Esc` 回滚。
- 回放过程可视化：当前 step、执行状态、失败原因。
- 一键导出诊断包（日志、版本、配置摘要）。

### 3.2 上手文档

- 10 分钟快速开始（安装、录制、回放、调试）。
- 配置项说明（默认值、作用、示例）。
- 已知限制/边界条件说明（例如特定控件类型、多显示器场景）。

### 3.3 兼容性矩阵

- VS Code 版本矩阵
- JDK 版本矩阵
- Windows 版本与 DPI/缩放矩阵
- 多显示器、不同分辨率下的高亮与定位稳定性

---

## P2（产品化与长期维护）

### 4.1 Marketplace 发布准备

- 完整 `README`、`CHANGELOG`、图标、演示 GIF/视频。
- 语义化版本（SemVer）与升级说明。

### 4.2 安全与合规

- 三方依赖许可证审计（TS/Java/.NET）。
- 隐私与遥测说明（采集什么、如何关闭）。

### 4.3 可运维性

- 关键指标监控：成功率、失败类型、耗时分布。
- 异常聚合与版本回溯机制。

---

## 5. 可发布定义（Definition of Release, DoR）

满足以下条件即可进入发布：

1. P0 全部完成并验收通过。
2. P1 至少完成“易用性关键项 + 文档 + 兼容性冒烟”。
3. 主干连续 3 天无 blocker。
4. 回放成功率达到目标阈值（建议：`>95%`）。

---

## 6. 发布前检查清单（可直接执行）

## 6.1 功能与回归

- [ ] 录制/编辑/保存/加载/回放全流程通过
- [ ] Object Tree 点击不会误增 visual 节点
- [ ] SearchAndUpdate 回放进入编辑态并更新成功
- [ ] Visual 修改 `data` 后，表格与执行一致
- [ ] Highlight 开关行为符合预期

## 6.2 构建与打包

- [ ] Extension 编译通过（`npm run compile`）
- [ ] Java 模块打包通过（`mvn clean package`）
- [ ] ProcessInfo 构建通过（`dotnet build -c Release`）
- [ ] 产物包含配置文件与必要可执行文件

## 6.3 质量与文档

- [ ] 关键路径测试通过并留存报告
- [ ] README/安装文档/FAQ 更新完成
- [ ] 已知限制与风险已明确

## 6.4 发布与回滚

- [ ] 发布版本号、变更说明、兼容范围已确认
- [ ] 回滚包与回滚步骤已验证
- [ ] 发布后监控与值班机制已安排

---

## 7. 建议的下一步执行顺序

1. 固化配置优先级与默认值（先定规范）。
2. 做一次完整回归并输出测试报告。
3. 完成文档补全（Quick Start + 配置说明 + FAQ）。
4. 完成 CI 门禁与发布流水线。
5. 小范围灰度（内部用户）后再正式发布。
