# 调试 Record Agent（附加到目标进程）

Record Agent 运行在**被测 Java 进程**内。要调试 agent 代码（如 `FxRecordSupport`、`RecordAgent`），需要先让目标进程开启 JDWP 调试端口，再用 Cursor/VS Code 或 IntelliJ **Attach** 到该进程。

## 1. 使用端口 5005（默认）

默认调试端口为 **5005**。以下配置均使用该端口。

## 2. 开启调试端口（两种方式任选其一）

### 方式 A：在 agent 配置中打开（推荐，无需改被测应用启动方式）

在 agent JAR 同目录下放置或修改 `marsJavaAgent-config.json`，增加 **JdwpPort**（例如 5005）：

```json
{
  "IsHighlightObjectWhileReplay": true,
  "JdwpPort": 5005
}
```

开始录制并注入 agent 后，agent 会通过 Attach API 在当前进程内打开 JDWP 端口，无需被测应用启动时加 JVM 参数。  
**配置说明**：在 agent JAR 同目录或 classpath 资源中的 **`marsJavaAgent-config.json`** 里设置 `"JdwpPort": 5005` 即开启调试端口；设为 `0` 或不写该项则关闭。扩展将 JAR 复制到临时目录时，请将同一目录下的 `marsJavaAgent-config.json` 一并放置（或依赖 JAR 内 classpath 中的默认配置）。

### 方式 B：启动被测应用时加 JVM 参数

在启动被测 Java 应用时加上 JVM 参数，例如：

```bash
java -agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=*:5005 -jar YourApp.jar
```

- `suspend=n`：进程立即启动，不等待调试器连接（若希望“启动即暂停”可改为 `suspend=y`）。
- `address=*:5005`：监听 5005 端口，允许本机/远程连接；仅本机可用 `address=5005`。

若用 Maven/Gradle/IDE 运行被测应用，在运行配置的 **VM options** 中加入上述 `-agentlib:jdwp=...` 即可。

## 3. 在另一个 Cursor 实例中 Attach

1. 用**另一个 Cursor 窗口**打开本仓库（或只打开 `java/marsJavaAgent`）。
2. 确保已安装 **Extension Pack for Java**（含 Java Debugger）。
3. 在 `marsJavaAgent` 或 `FxRecordSupport` 中下断点（如 `handleJavaFxRecordEvent`）。
4. 在**主 Cursor 窗口**中：选择被测进程 → 开始录制（注入 agent）。
5. 在**第二个 Cursor 窗口**：按 **F5** 或 **Run and Debug** → 选择 **“Attach to Java (Record Agent Debug)”**。
6. 在被测应用里操作（如点击 Tab、按钮），断点会命中。

使用的配置文件：`.vscode/launch.json` 中的 **“Attach to Java (Record Agent Debug)”**（host=localhost, port=5005）。

## 4. 在 IntelliJ IDEA 中 Attach

1. 用 IntelliJ 打开本仓库（或 `java/marsJavaAgent`）。
2. 在 `FxRecordSupport.java` 等 agent 代码中下断点。
3. 被测应用已按上文用 `-agentlib:jdwp=...,address=*:5005` 启动，并在扩展里已开始录制（agent 已注入）。
4. 在 IntelliJ：**Run → Attach to Process**，或 **Run → Edit Configurations** 中选择 **“Attach Record Agent Debug”**，再点击 Debug。
5. 若使用 **Edit Configurations**：选择 **Remote JVM Debug**，Host=localhost，Port=5005，保存后运行该配置。

使用的配置文件：`.idea/runConfigurations/Attach_Record_Agent_Debug.xml`（port=5005）。

## 5. 使用其他调试端口

若 5005 被占用，可改用其他端口（如 5006）：

- **被测应用**：将 `address=*:5005` 改为 `address=*:5006`。
- **Cursor**：编辑 `.vscode/launch.json`，将 **“Attach to Java (Record Agent Debug)”** 的 `"port": 5005` 改为 `5006`。
- **IntelliJ**：编辑 `.idea/runConfigurations/Attach_Record_Agent_Debug.xml`，将 `PORT` 改为 `5006`。

## 6. 流程小结

| 步骤 | 操作 |
|------|------|
| 1 | 用 `-agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=*:5005` 启动被测应用 |
| 2 | 在扩展里选择该进程并开始录制（注入 agent） |
| 3 | 在 Cursor 或 IntelliJ 中 Attach 到 localhost:5005 |
| 4 | 在 agent 代码中下断点，在被测应用里操作以触发断点 |

调试的是**目标进程**；agent 的 class 在该进程中加载，故断点下在 `marsJavaAgent` 工程内，Attach 到的是被测应用的 JVM（且该 JVM 已开启 JDWP 端口）。

---

## 7. Agent 日志文件位置

Record Agent 的日志由 `AgentLogger` 写入**文件**，位置如下。

- **优先**：若 agent JAR 为 `file:` URL 且其父目录可写，则使用 **JAR 同目录下的 `logs`**，即：
  - **Windows**：`%TEMP%\logs\marsJavaagent-{yyyyMMdd-HHmm}.log`（扩展把 JAR 拷到 `%TEMP%` 时）
- **否则（推荐查看）**：使用 **`{java.io.tmpdir}/mars-javaagent-logs/`**：
  - **Windows**：`%TEMP%\mars-javaagent-logs\marsJavaagent-{yyyyMMdd-HHmm}.log`  
    例如：`C:\Users\<用户>\AppData\Local\Temp\mars-javaagent-logs\marsJavaagent-20260226-1430.log`
  - **macOS / Linux**：`$TMPDIR/mars-javaagent-logs/` 或 `/tmp/mars-javaagent-logs/` 下的同名文件。

Agent 初始化成功后会在**标准错误**中打印一行：`[marsJavaAgent] log file: <绝对路径>`，便于确认实际写入位置。

文件名格式：`marsJavaagent-{yyyyMMdd-HHmm}.log`；同一分钟多次初始化时会加后缀 `-01`、`-02` 等。

其他相关日志（若存在）：

- **agent-loader**（注入进程）：见 agent-loader 工程中的 `AgentLogUtil`，一般在 agent-loader JAR 所在目录的 `javaagentLog/agent-loader.log`。
- **record 数据**：录制步骤写入扩展传入的 `recordDir` 下的 `record.jsonl`，不是 Logger 输出的 log 文件。
