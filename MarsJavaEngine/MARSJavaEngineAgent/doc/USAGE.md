# MARSJavaEngineAgent Usage

## Build

From repo root:

```
mvn -DskipTests package
```

Artifacts:

- `MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar`
- `MARSJavaEngine/target/MARSJavaEngine-1.0.0.jar`

## Run (inject)

```
java -jar MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar \
  "<processName>" <processId> "<swapDirectory>" <serverIp> <serverPort>
```

Example:

```
java -jar MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar \
  "oracle.ide.osgi.boot.OracleIdeLauncher" 14744 "C:\temp\mars\javaengine" localhost 8080
```

## Debug single mode

`debug-single` searches a Java process by `processName` and injects.

```
java -jar MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar \
  "<processName>" 0 "<swapDirectory>" <serverIp> <serverPort> debug-single
```

After injection, the agent sends command:

```
{ "MessageSource": "MARSJavaEngineAgent", "MessageType": "GET_UIOBJECTS_ALL" }
```

## Unload (stop services)

`unload` sends a command to stop the engine services (HTTP/WebSocket/keepalive).

```
java -jar MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar \
  "<processName>" 0 "<swapDirectory>" <serverIp> <serverPort> unload
```

## Output files

- `MarsJavaEngineSwap.json` contains service IP and ports.
- `MarsJavaEngineUiObjects.json` contains scan metadata and UI items.
- Logs go to `swapDirectory/log/`.
