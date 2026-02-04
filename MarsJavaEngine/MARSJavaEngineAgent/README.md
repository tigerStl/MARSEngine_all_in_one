# MARSJavaEngineAgent

Executable agent used to inject `MARSJavaEngine` into a running Java process.

## Parameters

```
<processName> <processId> <swapDirectory> <serverIp> <serverPort> [debug-single|unload]
```

## Quick start

```
mvn -DskipTests package
java -jar MARSJavaEngineAgent/target/MARSJavaEngineAgent-1.0.0.jar \
  "oracle.ide.osgi.boot.OracleIdeLauncher" 14744 "C:\temp\mars\javaengine" localhost 8080
```

More details in `doc/USAGE.md`.
