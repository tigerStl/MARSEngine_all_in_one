# Java UI 自动化测试插件

针对 Java Application 的自动化测试插件。采用 JVM Agent 模式扫描界面元素，生成测试脚本（JSON 格式）。当前版本**仅生成脚本，不执行**。

## 功能

1. **JVM Agent 扫描**：自动 attach 到运行中的 Java 进程，扫描 AWT/Swing UI 层级
2. **多进程选择**：若存在多个 Java 应用，弹出对话框供用户选择
3. **对象结构**：两层结构（parent + object），以 JSON 保存，每个对象具有唯一名称
4. **定位方式**：text、caption、name、namePath、javaType、objectTypePath；必要时使用 index（从上到下、从左到右）
5. **关键词**：Edit/TextArea → FillEdit；ComboBox → SelectDropDown
6. **常量**：所有字符串均引用 constants.json，禁止硬编码
7. **进程信息**：通过同目录下的 ProcessInfo（C# .NET Core）获取
8. **面板**：底部可停靠面板，含「Java Applications」按钮、进程下拉框、对象列表、Object Info（含 x,y,w,h、visible）、测试步骤表；支持状态持久化（切换 tab 后恢复）
9. **高亮**：双击对象列表中的对象，在目标进程窗口对应位置绘制红色框闪烁 3 次
10. **Java Agent 日志**：agent-loader 与 ui-scanner-agent 的日志写入各 JAR 同目录下的 `javaagentLog/`

## 项目结构

```
VSCode/
├── src/                      # VS Code 扩展
│   ├── extension.ts          # 主入口与命令
│   ├── panelProvider.ts      # 面板（进程列表、对象树、Object Info、测试步骤）
│   ├── agentLoader.ts        # 加载 Scanner / Highlight Agent（使用 JAVA_HOME/bin/java）
│   ├── processInfo.ts        # 调用 ProcessInfo 获取 Java 进程
│   ├── objectConverter.ts    # 将扫描结果转为 UIObject（含 bounds、screenBounds、visible）
│   └── scriptGenerator.ts    # 生成测试脚本
├── schemas/                  # JSON Schema
├── java/
│   ├── agent-loader/         # 使用 Attach API 加载各类 Agent
│   ├── ui-scanner-agent/     # JVM Agent，扫描 AWT/Swing（输出 bounds、screenBounds、visible）
│   └── highlight-agent/      # JVM Agent，在屏幕坐标绘制红框闪烁 3 次
├── ProcessInfo/              # C# .NET Core 工具，列举 Java 进程
└── package.json
```

## 数据与输出路径

- **扫描与脚本**：统一存放在**插件安装目录**下的 `scanedfiles/`（不依赖工作区）
- **Java Agent 日志**：各 JAR 同目录下的 `javaagentLog/`（如 `agent-loader/target/javaagentLog/agent-loader.log`）

## 需求与实现对照

| 需求 | 实现 |
|------|------|
| JVM Agent 扫描 | UIScannerAgent 通过 agentmain attach 到目标 JVM，扫描 Window/Frame 及子组件 |
| 多进程选择 | ProcessInfo 获取 Java 进程列表；面板中「Java Applications」按钮 + 下拉框/列表 |
| 两层对象结构 | parent + identifier，支持 text、caption、name、javaType、bounds、screenBounds、visible |
| Index 模式 | 按 bounds 从上到下、从左到右排序，多元素时使用 index |
| FillEdit / SelectDropDown | 根据 javaType 推断：JTextField/JTextArea → FillEdit，JComboBox → SelectDropDown |
| 常量 | 所有字符串写入 constants.json，脚本中只引用 id |
| 仅生成脚本 | 只生成 JSON 脚本，不执行；Execute 按钮仅提示 |
| ProcessInfo | 独立 C# 工程，使用 WMI（Windows）和 /proc（Linux）获取进程信息 |
| 面板状态持久化 | 进程列表、对象、步骤、日志保存到 workspaceState，切换 tab 后恢复 |
| 对象高亮 | 双击对象 → 加载 highlight-agent，在目标窗口屏幕坐标绘制红框闪烁 3 次 |

## 脚本格式示例

```json
{
  "steps": [
    {
      "keyword": "FillEdit",
      "parentIdentifier": { "javaType": "javax.swing.JFrame" },
      "objectIdentifier": { "javaType": "javax.swing.JTextField", "name": "username" },
      "data": "CONST_TEST_USER_ABC123",
      "assertValue": null
    }
  ]
}
```

## 常量文件示例

```json
{
  "constants": [
    { "id": "CONST_TEST_USER_ABC123", "value": "test@example.com", "category": "INPUT_DATA" }
  ]
}
```

## 构建与运行

1. **安装依赖并编译扩展**：`npm install`、`npm run compile`
2. **构建 Java**：`cd java && mvn package`（含 agent-loader、ui-scanner-agent、highlight-agent）
3. **构建 ProcessInfo**：`npm run build:processinfo` 或 `dotnet publish -c Release`
4. **运行**：按 F5 启动 Extension Development Host；打开底部面板「Java UI Automation」，点击 **Java Applications** 获取进程，双击进程扫描，双击对象高亮；或使用命令面板 `Java UI: Select Java Process and Scan` / `Java UI: Generate Test Script`

## 环境要求

- Node.js 18+
- Java 17+（JDK，含 Attach API）；扩展通过 `JAVA_HOME/bin/java`（Windows 下 `java.exe`）调用
- .NET 8 SDK（用于 ProcessInfo）
- Maven 3.6+
- VS Code 1.85+
