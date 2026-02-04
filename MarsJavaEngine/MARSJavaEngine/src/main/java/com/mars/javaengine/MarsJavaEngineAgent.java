package com.mars.javaengine;

import com.mars.javaengine.config.EngineConfig;
import com.mars.javaengine.util.JsonUtil;
import java.lang.instrument.Instrumentation;

public class MarsJavaEngineAgent {
    public static void agentmain(String args, Instrumentation inst) {
        EngineConfig config = JsonUtil.fromJson(args, EngineConfig.class);
        EngineService service = new EngineService(config);
        service.start();
    }

    public static void premain(String args, Instrumentation inst) {
        agentmain(args, inst);
    }
}
