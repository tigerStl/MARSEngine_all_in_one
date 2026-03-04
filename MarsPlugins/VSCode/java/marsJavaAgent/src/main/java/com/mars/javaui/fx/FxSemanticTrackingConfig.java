package com.mars.javaui.fx;

import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;

import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

/**
 * JavaFX semantic tracking config from marsJavaAgent-config.json (SemanticTracking / SemanticPolicy).
 * Used by fold/lift resolution: MaxAncestorHops, ButtonInTableCell, etc.
 * Spec: java/doc/javafx_record_semantic_spec.md
 */
public final class FxSemanticTrackingConfig {

    private static final int DEFAULT_MAX_ANCESTOR_HOPS = 5;
    private static final String ROW_ACTION = "ROW_ACTION";

    private final int maxAncestorHops;
    private final boolean preferPartOverControl;
    private final boolean fallbackToSelectionModel;
    private final String buttonInTableCell;

    private FxSemanticTrackingConfig(int maxAncestorHops, boolean preferPartOverControl,
                                     boolean fallbackToSelectionModel, String buttonInTableCell) {
        this.maxAncestorHops = maxAncestorHops <= 0 ? DEFAULT_MAX_ANCESTOR_HOPS : maxAncestorHops;
        this.preferPartOverControl = preferPartOverControl;
        this.fallbackToSelectionModel = fallbackToSelectionModel;
        this.buttonInTableCell = buttonInTableCell != null ? buttonInTableCell : ROW_ACTION;
    }

    public int getMaxAncestorHops() {
        return maxAncestorHops;
    }

    public boolean isPreferPartOverControl() {
        return preferPartOverControl;
    }

    public boolean isFallbackToSelectionModel() {
        return fallbackToSelectionModel;
    }

    public String getButtonInTableCell() {
        return buttonInTableCell;
    }

    /** Load from classpath resource marsJavaAgent-config.json, then apply optional file override. */
    public static FxSemanticTrackingConfig load() {
        return load(null);
    }

    /**
     * Load config. If configPath is non-null and points to a JSON file, merge SemanticTracking/SemanticPolicy from it.
     * Otherwise use only classpath marsJavaAgent-config.json.
     */
    public static FxSemanticTrackingConfig load(String configPath) {
        JsonObject root = null;
        try (InputStream in = FxSemanticTrackingConfig.class.getClassLoader().getResourceAsStream("marsJavaAgent-config.json")) {
            if (in != null) {
                root = JsonParser.parseReader(new InputStreamReader(in, StandardCharsets.UTF_8)).getAsJsonObject();
            }
        } catch (Exception ignored) {
        }
        if (configPath != null && !configPath.trim().isEmpty()) {
            try (InputStream in = new java.io.FileInputStream(configPath.trim())) {
                JsonObject fileRoot = JsonParser.parseReader(new InputStreamReader(in, StandardCharsets.UTF_8)).getAsJsonObject();
                if (fileRoot != null) root = fileRoot;
            } catch (Exception ignored) {
            }
        }
        int maxHops = DEFAULT_MAX_ANCESTOR_HOPS;
        boolean preferPart = true;
        boolean fallback = true;
        String buttonInCell = ROW_ACTION;
        if (root != null) {
            if (root.has("SemanticTracking")) {
                JsonObject st = root.getAsJsonObject("SemanticTracking");
                if (st.has("MaxAncestorHops")) maxHops = st.get("MaxAncestorHops").getAsInt();
                if (st.has("PreferPartOverControl")) preferPart = st.get("PreferPartOverControl").getAsBoolean();
                if (st.has("FallbackToSelectionModel")) fallback = st.get("FallbackToSelectionModel").getAsBoolean();
            }
            if (root.has("SemanticPolicy")) {
                JsonObject sp = root.getAsJsonObject("SemanticPolicy");
                if (sp.has("ButtonInTableCell")) buttonInCell = sp.get("ButtonInTableCell").getAsString();
            }
        }
        return new FxSemanticTrackingConfig(maxHops, preferPart, fallback, buttonInCell);
    }
}
