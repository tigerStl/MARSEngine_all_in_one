# MARS Java Engine

This repository contains two Maven modules:

- `MARSJavaEngineAgent`: executable injector (`java -jar`) that attaches to a running JVM.
- `MARSJavaEngine`: injected engine that starts services and scans UI objects.

## Build

From the repo root:

```
mvn -q -DskipTests package
```

## Modules

- `MARSJavaEngineAgent` produces `MARSJavaEngineAgent-1.0.0-shaded.jar`
- `MARSJavaEngine` produces `MARSJavaEngine-1.0.0-shaded.jar`

## Documentation

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
