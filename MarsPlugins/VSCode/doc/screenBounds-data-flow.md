# screenBounds（绝对坐标）数据流与缺失对比

## 1. Agent：如何得到屏幕坐标并写入传输对象

### 1.1 Swing/AWT 控件

**文件**: `java/marsJavaAgent/src/main/java/com/mars/javaui/protocol/AgentProtocol.java`  
**方法**: `scanComponent(Component c, String parentId)`（约 560–621 行）

```java
try {
    Point loc = c.getLocationOnScreen();
    Dimension dim = c.getSize();
    if (loc != null && dim != null) {
        Map<String, Integer> screenBounds = new LinkedHashMap<>();
        screenBounds.put("x", loc.x);
        screenBounds.put("y", loc.y);
        screenBounds.put("width", dim.width);
        screenBounds.put("height", dim.height);
        node.put("screenBounds", screenBounds);
    }
} catch (Exception ignored) { }
```

- 屏幕坐标来源: `Component.getLocationOnScreen()` + `Component.getSize()`。
- 写入位置: 每个节点的 `Map<String, Object> node`，键 `"screenBounds"`。

---

**文件**: `java/marsJavaAgent/src/main/java/com/mars/javaui/keyword/MarsKeyword.java`  
**方法**: `buildObjectIdentifier(Component comp)`（约 188–199 行）

- 同样用 `comp.getLocationOnScreen()` + `comp.getBounds()` 写入 `id.put("screenBounds", ...)`（用于录制/回放时的 objectKey，不是扫描树）。

### 1.2 JavaFX 控件

**文件**: `AgentProtocol.java`  
**方法**: `fillJavaFxWindowBounds`（约 729–742 行）— 仅用于 **Window 根节点**

```java
Integer x = asInteger(invokeNoArg(fxWindow, "getX"));
Integer y = asInteger(invokeNoArg(fxWindow, "getY"));
Integer width = asInteger(invokeNoArg(fxWindow, "getWidth"));
Integer height = asInteger(invokeNoArg(fxWindow, "getHeight"));
// ...
node.put("screenBounds", new LinkedHashMap<>(bounds));
```

- 屏幕坐标: Window 的 getX/Y/Width/Height（已是屏幕坐标）。
- 写入: 根节点 `node.put("screenBounds", ...)`。

**方法**: `fillJavaFxNodeBounds(Object fxNode, Map<String, Object> node)`（约 744–790 行）— 所有 **子 Node**

- 相对坐标: `fxNode.getLayoutBounds()` → getMinX, getMinY, getWidth, getHeight → `node.put("bounds", ...)`。
- 绝对坐标: 调用 `fxNode.localToScreen(layoutBounds)` 得到屏幕 Bounds，再 getMinX/getMinY/getWidth/getHeight → `node.put("screenBounds", sb)`。
- 可能缺失点: `getMethod("localToScreen", layoutBounds.getClass())` 若用实现类（如 BoundingBox）可能找不到方法（Node 声明为 `localToScreen(Bounds)`），需用接口 `javafx.geometry.Bounds` 查找。

---

## 2. Agent 将对象树传给 Extension 的代码

**文件**: `java/marsJavaAgent/src/main/java/com/mars/javaui/record/RecordAgent.java`

**2.1 构建树并作为 RPC result**

- 约 737–742 行:
  - 收到 `agent.getObjectTree` 后，在 EDT 上执行:  
    `treeHolder[0] = AgentProtocol.buildObjectTree(h);`  
  - `result = treeHolder[0]`，即 `Map<String, Object>`，内含 `"roots"` → `List<Map<String, Object>>`，每个节点即上面带 `screenBounds` 的 node。

**2.2 序列化并发送**

- 约 812–815 行:
```java
Map<String, Object> resp = new LinkedHashMap<>();
resp.put("id", id);
if (result != null) resp.put("result", result);
conn.send(toJson(resp));
```

- `toJson`（约 3720–3749 行）: 递归序列化 Map/List/Number/Boolean，不会丢弃 `screenBounds`。  
- 结论: Agent 端若在 node 上写了 `screenBounds`，就会出现在发往 Extension 的 JSON 里。
- **Agent 将对象树传输到 Extension 的精确代码**:  
  - 构建树: `RecordAgent.java` 737–742 行 `result = AgentProtocol.buildObjectTree(h)`。  
  - 设置并发送: `RecordAgent.java` 812–815 行 `resp.put("result", result); conn.send(toJson(resp));`。

---

## 3. Extension 接收并写入文件

**文件**: `src/agentLoader.ts`

- 约 194–210 行: WebSocket 收到 message 后解析 `msg.result`，取出 `roots`，再写文件:
```ts
const tree = msg.result;
const roots = tree && typeof tree === 'object' && 'roots' in tree ? (tree as { roots: unknown }).roots : [];
const scanOutput = JSON.stringify({ roots });
fs.writeFileSync(outputPath, scanOutput, 'utf-8');
```

- 这里只是透传 `roots`，不会删除 `screenBounds`。  
- 结论: 若 agent 发送的 `result.roots[].screenBounds` 存在，文件中就会有。

---

## 4. Extension 读取文件并转为 UI 对象

**文件**: `src/panelProvider.ts`  
- 约 619–623 行: 读取 scan 结果并转换:
```ts
const raw = fs.readFileSync(result.outputPath, 'utf-8');
const scan: ScanOutput = JSON.parse(raw);
const objects = convertScanToUIObjects(scan);
const objectTree = convertScanToUIObjectTree(scan);
```

**文件**: `src/objectConverter.ts`  
- `ScannedNode` 含 `screenBounds?: { x, y, width, height }`。
- `toIdentifier(node)`（约 76–91 行）:
```ts
if (node.screenBounds) identifier.screenBounds = node.screenBounds;
```
- `convertScanToUIObjectTree` 用同一 `toIdentifier` 构建每个节点的 `identifier`，故会保留 `screenBounds`。

- 结论: 只要 scan JSON 里有 `screenBounds`，extension 侧会一直带到 `UIObject.identifier.screenBounds`。

---

## 5. 对比与可能缺失点

| 环节 | 是否有 screenBounds | 说明 |
|------|---------------------|------|
| Agent Swing scanComponent | 有 | getLocationOnScreen + getSize → node.put("screenBounds", ...) |
| Agent JavaFX Window | 有 | fillJavaFxWindowBounds 直接写 screenBounds |
| Agent JavaFX Node | 可能无 | fillJavaFxNodeBounds 用 localToScreen；getMethod(..., layoutBounds.getClass()) 可能因声明为 Bounds 接口而找不到方法，导致未写入 |
| Agent → Extension 发送 | 透传 | toJson(result) 含 roots，不丢字段 |
| Extension 写文件 | 透传 | JSON.stringify({ roots }) |
| Extension 读文件 + objectConverter | 保留 | toIdentifier 复制 node.screenBounds → identifier.screenBounds |
| Panel highlight | 依赖 identifier.screenBounds | _handleHighlight 无 screenBounds 则直接 return，不画框 |

**结论**: 最可能缺失在 **Agent 端 JavaFX 子节点** 的 `fillJavaFxNodeBounds` 中：`localToScreen` 通过 `layoutBounds.getClass()` 查找方法可能失败，应改为用接口 `javafx.geometry.Bounds` 查找 `localToScreen`，确保所有 JavaFX 节点都写入 `screenBounds`。
