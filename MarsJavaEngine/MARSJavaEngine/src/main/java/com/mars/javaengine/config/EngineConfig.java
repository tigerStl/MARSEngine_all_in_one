package com.mars.javaengine.config;

public class EngineConfig {
    private String swapDirectory;
    private String serverIp;
    private int serverPort;
    private String targetProcessName;
    private String targetProcessId;
    private boolean debugSingle;
    private int highlightLimit = 30;

    public EngineConfig() {
    }

    public EngineConfig(String swapDirectory, String serverIp, int serverPort,
                        String targetProcessName, String targetProcessId,
                        boolean debugSingle, int highlightLimit) {
        this.swapDirectory = swapDirectory;
        this.serverIp = serverIp;
        this.serverPort = serverPort;
        this.targetProcessName = targetProcessName;
        this.targetProcessId = targetProcessId;
        this.debugSingle = debugSingle;
        this.highlightLimit = highlightLimit > 0 ? highlightLimit : 30;
    }

    public String getSwapDirectory() {
        return swapDirectory;
    }

    public String getServerIp() {
        return serverIp;
    }

    public int getServerPort() {
        return serverPort;
    }

    public String getTargetProcessName() {
        return targetProcessName;
    }

    public String getTargetProcessId() {
        return targetProcessId;
    }

    public boolean isDebugSingle() {
        return debugSingle;
    }

    public int getHighlightLimit() {
        return highlightLimit;
    }
}
