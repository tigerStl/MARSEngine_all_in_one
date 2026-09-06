# MARS Java Engine

[中文](README.md) | English

This repository contains three Maven modules and a Swing demo application:

- `MARSJavaEngineAgent`: executable injector (`java -jar`) that attaches to a running JVM.
- `MARSJavaEngine`: injected engine that starts services, scans UI objects, and executes automation commands.
- `MARSJavaMcp`: Java MCP server implementing `doc/mars_java_automation_mcp_methods_spec.md`.
- `MarsJavaDemo`: Northstar Capital Markets Workstation, a fictional capital-markets desktop app for MARS UI automation.

## Build

Requires JDK 11+. If PowerShell says `mvn` is not recognized, Maven is not on PATH. On Windows use the repo wrapper:

```
.\mvnw.cmd -q -DskipTests package
```

If Maven is installed, from the repo root:

```
mvn -q -DskipTests package
```

## Modules

- `MARSJavaEngineAgent` produces `MARSJavaEngineAgent-1.0.0-shaded.jar`
- `MARSJavaEngine` produces `MARSJavaEngine-1.0.0-shaded.jar`
- `MARSJavaMcp` produces `MARSJavaMcp-1.0.0.jar`

## Run the Northstar demo

`MarsJavaDemo` is a standalone Maven project (Java 17+). It is not part of the parent reactor.

From `MarsJavaDemo`:

```
mvn clean package
mvn exec:java
```

Or after packaging:

```
java -jar target/northstar-financial-demo-1.0.0.jar
```

More detail: `MarsJavaDemo/README.md`

## Documentation

- `doc/README.md` — requirements, usage, and MCP install index
- `doc/requirements.md` — Java Engine / Agent requirements and protocol
- `doc/usage.md` — build, inject, WebSocket commands
- `doc/mcp-install.md` — install MARS MCP in Cursor / VS Code
- `MARSJavaEngineAgent/README.md`
- `MARSJavaEngineAgent/doc/USAGE.md`

## WebSocket Test Requests

Use the `SvcIp` and `PortNumber` from `swapDirectory/MarsJavaEngineSwap.json` to connect:

```
ws://{SvcIp}:{PortNumber}
```

Supported JSON payloads:

Scan all UI objects:
```
{
  "MessageSource": "TestClient",
  "MessageType": "GET_UIOBJECTS_ALL",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {}
}
```

Unload engine:
```
{
  "MessageSource": "TestClient",
  "MessageType": "UNLOAD_ENGINE",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {}
}
```

Get object by mouse position:
```
{
  "MessageSource": "TestClient",
  "MessageType": "GET_UIOBJECT_BY_MOUSE",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {}
}
```

Get object by x,y:
```
{
  "MessageSource": "TestClient",
  "MessageType": "GET_UIOBJECT_BY_XY",
  "Time": "2026-01-25T12:00:00Z",
  "MessageInfo": {
    "x": 100,
    "y": 200
  }
}
```
