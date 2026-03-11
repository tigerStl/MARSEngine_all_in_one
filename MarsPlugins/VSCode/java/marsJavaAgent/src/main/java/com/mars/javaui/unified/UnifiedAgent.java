package com.mars.javaui.unified;

import java.lang.instrument.Instrumentation;
import java.lang.reflect.Method;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Bootstrap agent entry point. Loads encrypted marsJavaResource.bin in memory
 * and delegates to com.mars.javaui.record.RecordAgent from decrypted payload.
 */
public class UnifiedAgent {
    private static final String RESOURCE_FILE = "marsJavaResource.bin";
    private static final String DELEGATE_CLASS = "com.mars.javaui.record.RecordAgent";
    private static final String DELEGATE_METHOD = "agentmain";

    /**
     * Agent entry point for attach (agentmain).
     */
    public static void agentmain(String agentArgs, Instrumentation inst) {
        delegate(agentArgs, inst);
    }

    /**
     * Agent entry point for -javaagent (premain).
     */
    public static void premain(String agentArgs, Instrumentation inst) {
        delegate(agentArgs, inst);
    }

    private static void delegate(String agentArgs, Instrumentation inst) {
        try {
            byte[] payload = loadEncryptedPayload();
            if (payload != null) {
                InMemoryJarClassLoader loader = new InMemoryJarClassLoader(payload, UnifiedAgent.class.getClassLoader());
                Class<?> delegate = Class.forName(DELEGATE_CLASS, true, loader);
                Method method = delegate.getMethod(DELEGATE_METHOD, String.class, Instrumentation.class);
                method.invoke(null, agentArgs, inst);
                return;
            }
            // Dev fallback: allow direct classpath delegate when bin is unavailable.
            Class<?> fallback = Class.forName(DELEGATE_CLASS);
            Method method = fallback.getMethod(DELEGATE_METHOD, String.class, Instrumentation.class);
            method.invoke(null, agentArgs, inst);
        } catch (Throwable e) {
            // Print cause so "Agent failed to initialize" shows the real reason when attach fails.
            System.err.println("[UnifiedAgent] bootstrap failed: " + e.getMessage());
            Throwable c = e.getCause();
            if (c != null) {
                System.err.println("[UnifiedAgent] cause: " + c.getClass().getName() + ": " + c.getMessage());
                if (c.getCause() != null) {
                    System.err.println("[UnifiedAgent] cause.cause: " + c.getCause().getClass().getName() + ": " + c.getCause().getMessage());
                }
                c.printStackTrace(System.err);
            } else {
                e.printStackTrace(System.err);
            }
            throw new RuntimeException("Failed to bootstrap encrypted agent payload", e);
        }
    }

    private static byte[] loadEncryptedPayload() throws Exception {
        Path jarDir = resolveAgentJarDirectory();
        Path bin = jarDir.resolve(RESOURCE_FILE);
        if (!Files.exists(bin)) return null;
        byte[] encrypted = Files.readAllBytes(bin);
        return AgentResourceCrypto.decrypt(encrypted);
    }

    private static Path resolveAgentJarDirectory() throws Exception {
        Path path = Paths.get(UnifiedAgent.class.getProtectionDomain().getCodeSource().getLocation().toURI());
        Path file = Files.isDirectory(path) ? path : path.getParent();
        return file != null ? file : Paths.get(".");
    }
}
