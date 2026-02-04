# Java UI Automation Test Extension

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
├── src/                    # VS Code 扩展源码
│   ├── panelProvider.ts    # 面板（进程列表、对象树、Object Info、测试步骤）
│   ├── agentLoader.ts     # 加载 Scanner / Highlight Agent（使用 JAVA_HOME/bin/java）
├── schemas/                # JSON Schema
├── java/
│   ├── agent-loader/       # Attach 并加载各类 Agent
│   ├── ui-scanner-agent/   # JVM Agent，扫描 AWT/Swing（输出 bounds、screenBounds、visible）
│   └── highlight-agent/    # JVM Agent，在屏幕坐标绘制红框闪烁 3 次
├── ProcessInfo/            # C# .NET Core，列举 Java 进程
└── package.json
```

## 数据与输出路径

- **扫描与脚本**：统一存放在**插件安装目录**下的 `scanedfiles/`（不依赖工作区）
  - 扫描结果、`objects.json`、`constants.json`、`script.json` / `script-*.json` 均在此目录
- **Java Agent 日志**：各 JAR 同目录下的 `javaagentLog/`
  - 例如：`java/agent-loader/target/javaagentLog/agent-loader.log`、`java/ui-scanner-agent/target/javaagentLog/ui-scanner-agent.log`

## 构建

### 1. 安装依赖

```bash
npm install
```

### 2. 编译扩展

```bash
npm run compile
```

### 3. 构建 Java 项目

```bash
cd java
mvn clean package
cd ..
```

### 4. 构建 ProcessInfo

```bash
cd ProcessInfo
dotnet publish -c Release
cd ..
```

### 5. 运行扩展

- 在 VS Code 中按 F5 启动 Extension Development Host
- 或打包：`vsce package`

## 使用流程

### 面板方式（推荐）

1. 启动目标 Java 应用
2. 打开底部面板「Java UI Automation」
3. 点击 **Java Applications** → 清空下拉框与对象列表，获取 Java 进程列表；进程会填入下拉框和左侧列表
4. 在左侧列表**双击**某一进程 → 对该进程 attach 并扫描 UI，对象树显示在左侧
5. 单击对象可查看 **Object Info**（含 Name、Type、Text、x,y,w,h、Visible 等）；**双击对象**可在目标窗口对应位置显示红框闪烁 3 次
6. 点击 **Generate Test Steps** 生成测试步骤（或使用命令面板 `Java UI: Generate Test Script`）
7. **Execute** 当前仅提示“仅生成脚本，不执行”

### 命令面板方式

1. 命令面板：`Java UI: Select Java Process and Scan` → 选择进程并扫描
2. 命令面板：`Java UI: Generate Test Script` → 输入填充数据，生成 `script-*.json` 和 `constants.json`

面板状态（进程列表、对象、步骤、日志）会随工作区持久化，切换 tab 后再切回会自动恢复。

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

## 常量文件

```json
{
  "constants": [
    { "id": "CONST_TEST_USER_ABC123", "value": "test@example.com", "category": "INPUT_DATA" }
  ]
}
```

## 环境要求

- Node.js 18+
- Java 17+（JDK，含 Attach API）；扩展通过 `JAVA_HOME/bin/java`（Windows 下 `java.exe`）调用
- .NET 8 SDK（用于 ProcessInfo）
- Maven 3.6+
- VS Code 1.85+
