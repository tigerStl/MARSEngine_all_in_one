# MCP 规范（项目落地版）与设计依据

> 说明：本文件不是逐字复制官方全文，而是基于 Model Context Protocol（MCP）核心思想，给出可直接用于 MARS 项目的实施规范与“为什么这样做”。

---

## 1. MCP 是什么（在本项目里的定义）

MCP（Model Context Protocol）用于把“模型能力”与“工具能力”解耦：
- 模型负责理解用户意图
- MCP Server/Tool 负责执行真实动作（列进程、扫对象、执行步骤、回放等）

在 MARS 中，MCP 层是 Chat 与现有 Extension/Agent 之间的标准接口。

---

## 2. 规范总则（必须遵守）

### 2.1 接口稳定优先
- tool 名称、输入字段、输出字段一旦冻结，不做破坏式变更。
- 新需求通过“新增可选字段”扩展，而不是改名/改语义。

**为什么**：避免模型 prompt、客户端解析器、日志系统同时崩。

### 2.2 强 schema + 强校验
- 每个 tool 必须有明确 JSON schema（request/response）。
- 服务端必须校验并返回标准错误码。

**为什么**：模型输出有不确定性，schema 是最后一道防线。

### 2.3 幂等优先、可观测优先
- 查询类操作应幂等（list/get）。
- 修改类操作必须可追踪（requestId、toolName、duration、errorCode）。

**为什么**：便于重试、回放、排障和审计。

### 2.4 最小权限原则
- 高风险动作（批量回放、写文件、执行）需要显式工具调用，不允许“隐式副作用”。

**为什么**：减少误操作和安全风险。

### 2.5 跨平台约束
- 协议字段不包含平台专有语义（如硬编码 Windows 路径）。
- 平台差异通过错误码/能力探测暴露。

**为什么**：同一 MCP 协议同时服务 Windows/Linux。

---

## 3. 协议形态（建议）

### 3.1 通用响应包络

```json
{
  "ok": true,
  "requestId": "uuid",
  "errorCode": null,
  "errorMessage": null,
  "data": {}
}
```

### 3.2 错误响应

```json
{
  "ok": false,
  "requestId": "uuid",
  "errorCode": "MARS_E_OBJECT_NOT_FOUND",
  "errorMessage": "Object not found under parent",
  "data": null
}
```

### 3.3 字段规范
- 时间：epoch ms（number）
- 布尔：严格 boolean
- 下标：`integer >= 0`
- 路径：绝对路径，使用运行平台本机格式

**为什么**：统一解析规则，减少前后端转换歧义。

---

## 4. Tool 分类规范

### 4.1 Query Tools（只读）
例如：`listProcesses/getObjectTree/getSteps/getLastErrors`
- 不应改变系统状态
- 支持高频调用

### 4.2 Command Tools（有副作用）
例如：`selectProcess/updateStep/executeStep/runReplay/export*`
- 必须返回执行状态
- 失败需给明确 errorCode

**为什么**：清晰区分“读”与“写”，便于权限与重试策略。

---

## 5. 参数与返回设计规范

### 5.1 输入参数
- 必填最小化：只要求执行所需字段。
- 可选字段用于增强（如 `strictParent`, `rootWindowHint`）。
- 不接受“松散字符串拼接参数”（避免解析歧义）。

### 5.2 输出参数
- 必须返回最小可用信息（status、failedIndex、filePath 等）。
- 不返回超大冗余数据（必要时分页/限制条数）。

**为什么**：降低 token 消耗和网络成本，提升模型决策效率。

---

## 6. 错误码规范

建议错误码前缀统一：`MARS_E_*`

最少包含：
- `MARS_E_INVALID_ARGUMENT`
- `MARS_E_NO_PROCESS_SELECTED`
- `MARS_E_AGENT_ATTACH_FAILED`
- `MARS_E_PARENT_NOT_FOUND`
- `MARS_E_OBJECT_NOT_FOUND`
- `MARS_E_STEP_INDEX_INVALID`
- `MARS_E_REPLAY_FAILED`
- `MARS_E_PLATFORM_NOT_SUPPORTED`
- `MARS_E_HEADLESS_ENVIRONMENT`

**为什么**：让模型可以“按错误码分支处理”，而不是解析自然语言报错。

---

## 7. 日志与可观测规范

每次 tool 调用记录：
- `requestId`
- `toolName`
- `inputSchemaValid`
- `durationMs`
- `resultStatus`
- `errorCode`
- `fallbackUsed`

推荐附加：
- `selectedPid`
- `platform`
- `agentConnected`

**为什么**：快速定位是“模型意图错、参数错、还是执行层错”。

---

## 8. 与本项目现状的映射

- 已有 Java Agent RPC 与 panel 消息机制可复用。
- MCP 只需新增“标准 tool 入口”，底层仍调用既有服务。
- 本地 intent fallback 仅做兜底，不替代 MCP 主路径。

**为什么**：投入最小、收益最大，避免重写大量成熟逻辑。

---

## 9. 为什么要“先 MCP，再编码”

1) 先定协议，避免返工
- 代码易改，协议难改（上下游依赖多）。

2) 先定错误模型，降低调试成本
- 统一错误码后，chat 编排和前端提示都可复用。

3) 先定跨平台边界，避免 Windows 偏置
- Linux 问题大多源于路径/进程/图形环境假设不一致。

---

## 10. 对你当前决策的建议

在你已确认“11 tools 全上 + Windows 先行”的前提下：
- 立即按本规范冻结 `v0.1 schema`
- 进入 Phase 1 只打通 4 条主链路（`listProcesses/selectProcess/getObjectTree/executeStep`）
- 同步埋点可观测字段，确保 Demo 阶段就可定位问题

---

## 11. 术语补充

- **MCP Server**：暴露 tools 的服务侧实现
- **Tool**：可被模型调用的标准动作接口
- **Schema**：入参与出参的数据结构定义
- **Fallback**：MCP 不可用时，本地规则兜底路径

