# MCP Demo 测试手册（Windows）

> 适用场景：你已启动 Demo Java 程序，准备验证 MARS 的 MCP + fallback 链路是否可用。

---

## 1. 前置条件

- VS Code 已打开工作区：`MarsPlugins/VSCode`
- 扩展已编译通过：`npm run compile`
- Java Agent 已编译通过：`cd java && mvn clean package`
- ProcessInfo 已编译通过：`cd ProcessInfo && dotnet build -c release`
- Demo Java 程序已启动且界面可见（非 headless）

---

## 2. 快速冒烟（5 分钟）

1. 打开面板

- 命令面板执行：`Java UI Automation: Show Panel`

2. 列进程（验证 `mars-list-processes` 等价链路）

- 点击 `Java Applications` 按钮
- 期望：下拉框出现 Java 进程列表

3. 选择进程（验证 `mars-select-process` 等价链路）

- 在下拉框选择你的 Demo PID
- 期望：日志出现应用选择信息

4. 扫描对象树（验证 `mars-get-object-tree` 等价链路）

- 双击进程或点击触发扫描
- 期望：左侧对象树出现节点，Object Info 可显示属性

5. 执行单步（验证 `mars-execute-step` 等价链路）

- Test Steps 表中点击某一行 Execute
- 期望：该步状态变 success 或 failed，并显示耗时/错误

---

## 3. 13 个工具验证矩阵（按功能映射）

> 当前阶段已有 MCP 命令路由实现；若你暂时不做“命令直调”，可按 UI 映射验证底层能力。

1. `mars-list-processes`

- 操作：点击 `Java Applications`
- 期望：返回进程列表

2. `mars-select-process`

- 操作：下拉框选择进程
- 期望：selectedPid 生效

3. `mars-start-record`

- 操作：触发开始录制（可带 pid）
- 期望：进入录制态（Record 按钮切换为 Stop 语义，进程下拉禁用）

4. `mars-stop-record`

- 操作：触发停止录制
- 期望：退出录制态，步骤数量更新

5. `mars-get-object-tree`

- 操作：扫描进程
- 期望：`roots` 非空（取决于目标应用）

6. `mars-highlight-object`

- 操作：对象树点击节点后执行高亮
- 期望：目标区域高亮（Windows）

7. `mars-get-steps`

- 操作：加载/编辑步骤后查看表格
- 期望：步骤列表与面板一致

8. `mars-update-step`

- 操作：编辑 Parent/Object/Para/Data/Expected 任一列
- 期望：步骤实时更新并可保存

9. `mars-execute-step`

- 操作：行内 Execute
- 期望：返回 success/failed + duration

10. `mars-run-replay`

- 操作：点击 Execute（全量回放）
- 期望：replayProgress 连续更新；失败时给 failedIndex

11. `mars-export-objects`

- 操作：对象树右键 Export
- 期望：导出 JSON 成功

12. `mars-export-diagnostics`

- 操作：点击 `Diag`
- 期望：导出诊断包（含 summary/log/steps）

13. `mars-get-last-errors`

- 操作：制造一个失败（如错误 parent），再查看日志
- 期望：最近错误可聚合（当前可通过面板日志/诊断包验证）

---

## 4. 推荐测试数据（最小）

准备 3 条步骤：

1. `Click`：可见按钮
2. `FillEdit`：可编辑输入框
3. `VerifyObjectValue`：期望值校验

建议再加 1 条故意失败步骤（错误 parent 或错误 object），用于验证：

- 错误码映射
- failedIndex
- 诊断日志可读性

---

## 5. 通过标准（Demo Gate）

- 能稳定完成：列进程 → 选择进程 → 开始录制 → 停止录制 → 扫描对象 → 执行单步 → 全量回放
- 至少 1 条成功步骤 + 1 条失败步骤可解释
- 导出对象与诊断均成功
- 无崩溃/卡死/静默失败

---

## 6. 常见问题排查

1. 看不到进程

- 先确认 Demo Java 进程仍在运行
- 检查 ProcessInfo 构建结果

2. 扫描为空

- 确认目标窗口可见
- 检查 agent 注入日志

3. 高亮失败

- 仅 Windows 支持
- 目标对象需有 `screenBounds`

4. 回放失败：Parent object not found

- 检查步骤里的 `parentIdentifier`
- 利用日志 compare 格式排查字段匹配

---

## 7. 下一步（建议）

测试完成后，把以下信息发给我，我会继续推进联调：

- 成功/失败步骤截图或日志片段
- 失败时的 errorCode / errorMessage
- 你希望优先优化的 2 个点（稳定性/速度/日志可读性）
