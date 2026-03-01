# 提交总结

## Java Agent (marsJavaAgent) 与 JavaFX 录制

### SelectTab 步骤
- **data 字段**：由“选中索引”改为优先使用 **Tab 的 text/caption**（来自 Tab.getText() 或内嵌 Label），仅无文本时退回索引。
- **FxRecordSupport.dataForMouseClick**：通过 TabPane.getTabs().get(index) 取 Tab 再 getText() 作为 data。

### Agent 调试端口
- **JdwpPort 配置**：在 `marsJavaAgent-config.json` 中增加 `JdwpPort`（如 5005），agent 启动时通过 Attach API 动态加载 JDWP，无需被测应用启动时加 `-agentlib:jdwp`。
- **RecordAgent**：`enableJdwp(port)`、从配置读取 JdwpPort、agentmain 中若 JdwpPort>0 则调用 enableJdwp。
- **文档**：`doc/debug-record-agent.md` 增加“方式 A：在配置中打开调试端口”的说明。

### JavaFX 录制 (FxRecordSupport)
- **ObservableList 反射**：`getFromObservableList` / `indexOfInObservableList` 改为使用 **java.util.List.class** 取方法并 invoke，避免反射 `com.sun.javafx.collections.ObservableListWrapper` 导致 “module javafx.base does not export” 错误。
- **异常日志**：所有相关 catch 中增加 LOG（getFromObservableList、indexOfInObservableList、emitStep、SelectTab、attach/detach、invokeNoArg、fillJavaFxScreenBounds、asInt 等）。
- **Tab 键与步骤**：FX 事件处理改为在 **Platform.runLater** 中执行，避免在事件分发中阻塞导致 Tab 焦点切换失效；步骤仍由 stepSender 正常发出。
- **编译**：handleJavaFxRecordEvent 的 catch 中引用 keyword/stepTarget 导致作用域错误，已改为在 try 前声明并在 try 内赋值。

---

## JavaFXDemo

- **调试端口 5005**：`fix-jpackage-cfg.ps1` 在 [JavaOptions] 中增加 `-agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=*:5005`。
- **打包 exe**：使用 `--type app-image`（无需 WiX），`build-exe.ps1` 完成 mvn package、dependency 复制、jpackage、fix-jpackage-cfg。
- **fix-jpackage-cfg.ps1**：classpath 与 module-path 使用 **$APPDIR**，按 app 目录下实际 jar 动态生成 classpath；增加 **Run-console-debug.bat** 便于在控制台查看 Java 报错。
- **README.md**：说明构建 exe、5005 调试、以及“Child process exited with code 1”时运行 Run-console-debug.bat 与重新 build-exe.ps1。

---

## 配置与文档

- **marsJavaAgent-config.json**：增加 JdwpPort（默认 0），文档说明通过该文件配置调试端口与其它项。
