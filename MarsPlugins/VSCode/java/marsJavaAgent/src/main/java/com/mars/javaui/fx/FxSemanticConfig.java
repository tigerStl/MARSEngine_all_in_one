package com.mars.javaui.fx;

import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Properties;

/**
 * JavaFX semantic configuration (lightweight, optional).
 *
 * This is intentionally isolated from AWT/Swing logic and only used by JavaFX helpers.
 *
 * Format: properties file. Defaults are used when file is missing.
 *
 * Example keys:
 * - role.contains.TreeView=Tree
 * - role.contains.TreeCell=TreeNode
 * - role.contains.TableView=Table
 * - role.contains.TableCell=TableCell
 * - name.sources=accessibleText,text,id,value,tooltip
 */
public final class FxSemanticConfig {
    public final Map<String, String> roleContainsRules;
    public final String[] nameSources;

    private FxSemanticConfig(Map<String, String> roleContainsRules, String[] nameSources) {
        this.roleContainsRules = roleContainsRules;
        this.nameSources = nameSources;
    }

    public static FxSemanticConfig loadDefaultOrFromFile(String configPath) {
        Properties props = new Properties();
        if (configPath != null && !configPath.trim().isEmpty()) {
            File f = new File(configPath.trim());
            if (f.exists() && f.isFile()) {
                try (InputStream in = new FileInputStream(f)) {
                    props.load(in);
                } catch (Exception ignored) {
                    // fall back to defaults
                }
            }
        }
        Map<String, String> roleRules = new LinkedHashMap<>();
        // Defaults (can be overridden by properties).
        roleRules.put("TreeView", "Tree");
        roleRules.put("TreeCell", "TreeNode");
        roleRules.put("TreeItem", "TreeItem");
        roleRules.put("TableView", "Table");
        roleRules.put("TableCell", "TableCell");
        roleRules.put("ListView", "List");
        roleRules.put("ListCell", "ListItem");
        roleRules.put("TabPane", "Tabs");
        roleRules.put("Tab", "Tab");
        roleRules.put("ComboBox", "Combo");
        roleRules.put("ChoiceBox", "Combo");
        roleRules.put("TextField", "TextInput");
        roleRules.put("TextArea", "TextInput");
        roleRules.put("CheckBox", "CheckBox");
        roleRules.put("RadioButton", "RadioButton");
        roleRules.put("Button", "Button");
        roleRules.put("MenuItem", "MenuItem");

        for (String k : props.stringPropertyNames()) {
            if (k == null) continue;
            if (k.startsWith("role.contains.")) {
                String contains = k.substring("role.contains.".length()).trim();
                String role = props.getProperty(k, "").trim();
                if (!contains.isEmpty() && !role.isEmpty()) {
                    roleRules.put(contains, role);
                }
            }
        }

        String sources = props.getProperty("name.sources", "accessibleText,text,id,value,tooltip").trim();
        String[] nameSources = splitCsv(sources);
        return new FxSemanticConfig(roleRules, nameSources);
    }

    public String roleForTypeName(String javaTypeName) {
        if (javaTypeName == null) return null;
        for (Map.Entry<String, String> e : roleContainsRules.entrySet()) {
            String contains = e.getKey();
            if (contains != null && !contains.isEmpty() && javaTypeName.contains(contains)) {
                return e.getValue();
            }
        }
        return null;
    }

    private static String[] splitCsv(String s) {
        if (s == null || s.trim().isEmpty()) return new String[0];
        String[] parts = s.split(",");
        for (int i = 0; i < parts.length; i++) parts[i] = parts[i].trim();
        return parts;
    }
}

