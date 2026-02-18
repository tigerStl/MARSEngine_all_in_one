package com.mars.javaui.unified;

import java.lang.instrument.Instrumentation;
import com.mars.javaui.record.RecordAgent;

/**
 * Unified Agent entry point combining UI Scanner and Record/Replay functionality.
 * Single agent entry point that handles all functionality via WebSocket communication.
 */
public class UnifiedAgent {

    /**
     * Agent entry point for attach (agentmain).
     * Delegates to RecordAgent which provides WebSocket server for all functionality.
     */
    public static void agentmain(String agentArgs, Instrumentation inst) {
        RecordAgent.agentmain(agentArgs, inst);
    }

    /**
     * Agent entry point for -javaagent (premain).
     * Delegates to RecordAgent which provides WebSocket server for all functionality.
     */
    public static void premain(String agentArgs, Instrumentation inst) {
        RecordAgent.agentmain(agentArgs, inst);
    }
}
