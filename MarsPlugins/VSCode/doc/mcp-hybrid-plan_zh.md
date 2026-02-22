# MARS Chat 集成方案（MCP + 轻量本地 Intent Fallback）

## 1. 目标与范围

本文档用于确认：将当前 MARS（VS Code Extension + Java Agent + ProcessInfo）接入聊天交互能力时，采用 **MCP 标准工具层** + **轻量本地 Intent Fallback** 的混合架构。

目标：
- 先可用：尽快完成可演示 Demo。
- 可扩展：后续可接不同模型/客户端，不被单一 prompt 绑定。
- 可运维：具备日志、权限、失败回退、跨平台（Windows/Linux）能力。

不在本阶段目标：
- 一次性做完所有关键字语义理解。
- 重写现有 replay/record 主逻辑。

---

## 2. 架构总览

### 2.1 分层

1) **Chat Orchestrator（扩展侧）**
- 负责会话上下文、工具调用编排、回退策略。
- 优先走 MCP tools；失败时走本地 intent fallback。

2) **MCP Tool Layer（扩展侧）**
- 对外暴露稳定工具接口（schema 固定）。
- 工具内部调用现有 extension/service/agent RPC。

3) **Domain Service（现有业务层）**
- 复用当前实现：对象树获取、步骤编辑、单步执行、回放、导出等。

4) **Java Agent / ProcessInfo（执行层）**
- 维持现有协议，避免大改。

### 2.2 调用优先级

- 默认：`Chat -> MCP tool`
- MCP 不可用/调用失败/参数不合法：`Chat -> local intent fallback`
- fallback 仅覆盖高频动词（执行、查看对象、编辑步骤、导出、诊断）。

---

## 3. MCP 工具清单（首批）

> 说明：以下是首批可落地最小集，先保留“高价值 + 低风险”。

### 3.1 会话与目标应用

1. `mars.listProcesses`
- 入参：无
- 出参：`[{ pid, displayName, mainClass, osHint }]`
- 用途：列出可附加 Java 进程

2. `mars.selectProcess`
- 入参：`{ pid }`
- 出参：`{ ok, selectedPid }`
- 用途：绑定当前会话目标进程

### 3.2 对象与定位

3. `mars.getObjectTree`
- 入参：`{ rootWindowHint?, refresh? }`
- 出参：`{ roots:[...] }`
- 用途：抓取对象树

4. `mars.highlightObject`
- 入参：`{ objectKey, parentKey? }`
- 出参：`{ ok, message? }`
- 用途：高亮定位对象

### 3.3 步骤与回放

5. `mars.getSteps`
- 入参：无
- 出参：`{ steps:[...] }`
- 用途：读取当前步骤

6. `mars.updateStep`
- 入参：`{ index, patch }`
- patch 允许：`keyword/parentIdentifier/objectIdentifier/parameter/data/assertValue/skipped`
- 出参：`{ ok, step }`
- 用途：编辑单步（支持 parent/object）

7. `mars.executeStep`
- 入参：`{ index }`
- 出参：`{ ok, status, durationMs, error? }`
- 用途：执行单步

8. `mars.runReplay`
- 入参：`{ fromIndex?, toIndex?, strictParent? }`
- 出参：`{ ok, failedIndex?, error? }`
- 用途：批量回放

### 3.4 数据导入导出与诊断

9. `mars.exportObjects`
- 入参：`{ format: "json", includeParents?: boolean }`
- 出参：`{ ok, filePath }`

10. `mars.exportDiagnostics`
- 入参：`{ includeLogs?: boolean }`
- 出参：`{ ok, filePath }`

11. `mars.getLastErrors`
- 入参：`{ limit?: number }`
- 出参：`{ items:[{ts, scope, message}] }`

---

## 4. 轻量本地 Intent Fallback 清单

仅做“兜底 + 高频”语义，避免复杂 NLU：

- `list process / 选择进程`
- `scan object / 刷新对象`
- `执行第N步 / run step N`
- `回放全部 / run replay`
- `修改步骤字段（parameter/data/assertValue）`
- `导出对象 / 导出诊断`

策略：
- 规则优先（关键短语 + 正则）
- 不做多轮复杂槽位推理
- 解析失败直接提示用户改用结构化命令

---

## 5. Linux 可行性与兼容策略（重点）

### 5.1 总体可行性

可行，但需要把“与 Windows 强绑定”的路径和注入细节抽象化。

### 5.2 必做兼容项

1) **路径与文件系统**
- 禁止硬编码 `C:\...`
- 全部使用 `path.join / File.separator / os.tmpdir`
- 日志目录统一由配置或运行时计算

2) **进程发现/附加**
- ProcessInfo 若为 .NET 方案，Linux 需要：
  - 方案A：补 Linux 构建与发布
  - 方案B：改为 Java/JPS 或 cross-platform Node 进程探测

3) **Agent 注入机制**
- 校验 Linux JVM attach 可用（`tools.jar`/JDK 版本/权限）
- 增加 attach 失败分类（权限、JDK 缺失、目标 JVM 不兼容）

4) **UI 自动化环境**
- Linux 需有图形会话（X11/Wayland）
- Headless 环境明确返回不可执行而非静默失败

5) **键鼠行为差异**
- Robot 在 Linux 的焦点与输入时序可能不同
- 将关键延迟参数外置可配置（而非硬编码）

### 5.3 Linux 验收基线

- 能发现并选择 Java 进程
- 能 attach agent 并完成 handshake
- 能获取 object tree
- 能执行单步 Click / FillEdit
- 能 run replay 并回传失败位置

---

## 6. Demo 可行性（建议先做）

### Demo 范围（MVP）

- 用户在 chat 输入：
  1. “列出进程”
  2. “选择 pid=xxxx”
  3. “扫描对象树”
  4. “执行第3步”
  5. “把第3步 expected 改成 XXX”
  6. “回放全部并告诉我失败在哪一步”

### Demo 成功标准

- 全流程无需离开 chat 完成
- 出错可解释（非空洞失败）
- 至少 1 个 fallback intent 命中成功

---

## 7. 逐步实施计划

### Phase 0（文档与接口冻结）
- 确认 tool 名称、入参 schema、错误码约定
- 冻结首批 8~11 个工具范围

#### Phase 0 冻结稿（v0.1）

##### A. 全局约定

1) 通用响应包络（所有 tool）

```json
{
  "ok": true,
  "requestId": "uuid",
  "errorCode": null,
  "errorMessage": null,
  "data": {}
}
```

2) 通用规则
- `requestId`：可选，若未传由服务端生成并回传。
- `ok=false` 时必须返回 `errorCode` 与 `errorMessage`。
- 时间统一使用 epoch ms（`number`）。
- 路径统一返回绝对路径；Windows/Linux 均使用本机原生分隔符。

3) 标识符对象（Identifier）

```json
{
  "javaType": "javax.swing.JButton",
  "name": "okButton",
  "text": "OK",
  "caption": "OK",
  "index": 0,
  "javaNamePath": ["frame0", "dialog1", "okButton"],
  "javaTypePath": ["javax.swing.JFrame", "javax.swing.JDialog", "javax.swing.JButton"]
}
```

##### B. 11 个工具冻结 Schema

1. `mars.listProcesses`
- request
```json
{}
```
- response.data
```json
{
  "items": [
    {
      "pid": 12345,
      "displayName": "LoanIQ Demo",
      "mainClass": "com.demo.Main",
      "osHint": "windows"
    }
  ]
}
```

2. `mars.selectProcess`
- request
```json
{ "pid": 12345 }
```
- response.data
```json
{ "selectedPid": 12345 }
```

3. `mars.getObjectTree`
- request
```json
{ "rootWindowHint": "LoanIQ", "refresh": true }
```
- response.data
```json
{ "roots": [] }
```

4. `mars.highlightObject`
- request
```json
{ "objectKey": {}, "parentKey": {} }
```
- response.data
```json
{ "message": "highlight sent" }
```

5. `mars.getSteps`
- request
```json
{}
```
- response.data
```json
{ "steps": [] }
```

6. `mars.updateStep`
- request
```json
{
  "index": 2,
  "patch": {
    "keyword": "VerifyObjectValue",
    "parentIdentifier": {},
    "objectIdentifier": {},
    "parameter": "",
    "data": "",
    "assertValue": "expected",
    "skipped": false
  }
}
```
- response.data
```json
{ "step": {} }
```

7. `mars.executeStep`
- request
```json
{ "index": 2 }
```
- response.data
```json
{
  "status": "success",
  "durationMs": 132,
  "error": null
}
```

8. `mars.runReplay`
- request
```json
{ "fromIndex": 0, "toIndex": 10, "strictParent": true }
```
- response.data
```json
{
  "status": "done",
  "failedIndex": null,
  "error": null
}
```

9. `mars.exportObjects`
- request
```json
{ "format": "json", "includeParents": true }
```
- response.data
```json
{ "filePath": "..." }
```

10. `mars.exportDiagnostics`
- request
```json
{ "includeLogs": true }
```
- response.data
```json
{ "filePath": "..." }
```

11. `mars.getLastErrors`
- request
```json
{ "limit": 20 }
```
- response.data
```json
{
  "items": [
    {
      "ts": 1730000000000,
      "scope": "replay",
      "message": "Parent object not found"
    }
  ]
}
```

##### C. 入参校验规则（冻结）

- `pid`：`integer > 0`
- `index/fromIndex/toIndex/limit`：`integer >= 0`
- `toIndex >= fromIndex`
- `format`：首版仅允许 `json`
- `strictParent`：默认 `true`
- `patch`：至少包含 1 个已知字段
- `objectKey/parentKey`：必须为 object，空对象视为无效标识

##### D. 错误码映射（首版）

- 参数校验失败：`MARS_E_INVALID_ARGUMENT`
- 未选择进程：`MARS_E_NO_PROCESS_SELECTED`
- Agent 未连接/附加失败：`MARS_E_AGENT_ATTACH_FAILED`
- 对象未找到：`MARS_E_OBJECT_NOT_FOUND`
- 父对象未找到：`MARS_E_PARENT_NOT_FOUND`
- 步骤下标非法：`MARS_E_STEP_INDEX_INVALID`
- 回放失败：`MARS_E_REPLAY_FAILED`
- 平台不支持：`MARS_E_PLATFORM_NOT_SUPPORTED`
- 图形环境不可用：`MARS_E_HEADLESS_ENVIRONMENT`

##### E. Phase 0 完成标准

- 11 个 tool 名称与 request/response 字段冻结。
- 错误码与校验规则冻结。
- 文档冻结后，编码阶段仅新增字段，不做破坏式改名。

### Phase 1（MCP 最小链路）
- 打通 `listProcesses / selectProcess / getObjectTree / executeStep`
- 先不做复杂意图

### Phase 2（fallback intent）
- 上线高频兜底规则
- 加入日志：`tool_used` / `fallback_used`

### Phase 3（Linux 验证）
- 在 Linux 测试机跑验收基线
- 输出差异与修复清单

### Phase 4（扩展）
- 增加 runReplay、updateStep（parent/object）强化、diagnostics
- 引入权限策略（高风险动作确认）

---

## 8. 日志与可观测建议

统一记录（扩展侧）：
- `requestId`
- `toolName`
- `inputSchemaValid`
- `toolDurationMs`
- `fallbackUsed`
- `resultStatus`
- `errorCode/errorMessage`

统一记录（agent侧）：
- 收包、参数、匹配摘要、失败字段（已具备基础）

---

## 9. 风险与应对

1) 模型输出不稳定
- 通过 MCP schema + 参数校验收敛

2) fallback 规则膨胀
- 严格限制范围，只覆盖高频动词

3) Linux 兼容拖期
- 与 Windows 并行推进，Phase 3 独立验收

4) replay 失败定位成本高
- 保留并强化字段级 compare 日志（已在推进）

---

## 10. 待你确认（下一步讨论）

### 10.1 已确认

1. 首批工具：按上文 **11 个全上**。
2. Demo 目标场景：先在 **Windows** 环境验证。

### 10.2 待确认

1. fallback 是否只做“关键词+正则规则”？
2. Linux 进程发现路线：ProcessInfo 跨平台化 vs Java/JPS？

---

## 附录 A：建议错误码（草案）

- `MARS_E_NO_PROCESS_SELECTED`
- `MARS_E_AGENT_ATTACH_FAILED`
- `MARS_E_OBJECT_NOT_FOUND`
- `MARS_E_PARENT_NOT_FOUND`
- `MARS_E_STEP_INDEX_INVALID`
- `MARS_E_REPLAY_FAILED`
- `MARS_E_PLATFORM_NOT_SUPPORTED`
- `MARS_E_HEADLESS_ENVIRONMENT`

