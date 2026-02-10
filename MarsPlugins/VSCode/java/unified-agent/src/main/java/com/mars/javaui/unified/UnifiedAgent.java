package com.mars.javaui.unified;

import java.lang.instrument.Instrumentation;

import com.mars.javaui.record.RecordAgent;
import com.mars.javaui.scanner.UIScannerAgent;

/**
 * Single entry point for both UI scan and record/replay agents.
 * Agent args: path to JSON file → scan; "recordDir|pid" → record.
 */
public class UnifiedAgent {

    public static void agentmain(String agentArgs, Instrumentation inst) {
        if (agentArgs != null && agentArgs.contains("|")) {
            RecordAgent.agentmain(agentArgs, inst);
        } else {
            UIScannerAgent.agentmain(agentArgs, inst);
        }
    }

    public static void premain(String agentArgs, Instrumentation inst) {
        agentmain(agentArgs, inst);
    }
}
