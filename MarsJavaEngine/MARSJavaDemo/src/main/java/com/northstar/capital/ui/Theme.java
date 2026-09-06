package com.northstar.capital.ui;

import javax.swing.BorderFactory;
import javax.swing.UIManager;
import javax.swing.border.Border;
import javax.swing.plaf.nimbus.NimbusLookAndFeel;
import java.awt.Color;
import java.awt.Font;
import java.awt.Insets;

public final class Theme {
    public static final Color HEADER_BG = new Color(0x1B2A4A);
    public static final Color HEADER_FG = new Color(0xE8EEF6);
    public static final Color MENU_BG = new Color(0x243044);
    public static final Color ACCENT = new Color(0xC9A227);
    public static final Color WORKSPACE_BG = new Color(0xD6DCE4);
    public static final Color PANEL_BG = Color.WHITE;
    public static final Color SECTION_BORDER = new Color(0x8A94A6);
    public static final Color TABLE_HEADER = new Color(0x2C3E5A);
    public static final Color TABLE_ALT = new Color(0xF0F3F7);
    public static final Color STATUS_BG = new Color(0x1B2A4A);
    public static final Color ERROR_BORDER = new Color(0xB42318);
    public static final Color SUCCESS = new Color(0x1B7A3D);
    public static final Color FAIL = new Color(0x9B1C1C);
    public static final Color TREE_BG = new Color(0xF7F8FA);
    public static final Color PROD_WARN = new Color(0x7A1D1D);
    public static final Font UI_FONT = new Font("Segoe UI", Font.PLAIN, 11);
    public static final Font UI_FONT_BOLD = new Font("Segoe UI", Font.BOLD, 11);
    public static final Font HEADER_FONT = new Font("Segoe UI", Font.BOLD, 13);
    public static final Font SMALL_FONT = new Font("Segoe UI", Font.PLAIN, 10);

    private Theme() {
    }

    public static void install() {
        try {
            UIManager.setLookAndFeel(new NimbusLookAndFeel());
        } catch (Exception ignored) {
            // fall back to platform default
        }
        UIManager.put("control", WORKSPACE_BG);
        UIManager.put("nimbusBase", new Color(0x2C3E5A));
        UIManager.put("nimbusBlueGrey", new Color(0x4A5A73));
        UIManager.put("text", new Color(0x1A2332));
        setFont("Label.font", UI_FONT);
        setFont("Button.font", UI_FONT);
        setFont("ToggleButton.font", UI_FONT);
        setFont("RadioButton.font", UI_FONT);
        setFont("CheckBox.font", UI_FONT);
        setFont("ComboBox.font", UI_FONT);
        setFont("TextField.font", UI_FONT);
        setFont("FormattedTextField.font", UI_FONT);
        setFont("TextArea.font", UI_FONT);
        setFont("Table.font", UI_FONT);
        setFont("TableHeader.font", UI_FONT_BOLD);
        setFont("Tree.font", UI_FONT);
        setFont("Menu.font", UI_FONT);
        setFont("MenuItem.font", UI_FONT);
        setFont("TabbedPane.font", UI_FONT);
        setFont("TitledBorder.font", UI_FONT_BOLD);
        setFont("ToolTip.font", SMALL_FONT);
        setFont("Spinner.font", UI_FONT);
        UIManager.put("Table.rowHeight", 18);
        UIManager.put("TextField.margin", new Insets(1, 3, 1, 3));
        UIManager.put("Button.contentMargins", new Insets(2, 8, 2, 8));
    }

    private static void setFont(String key, Font font) {
        UIManager.put(key, font);
    }

    public static Border sectionBorder(String title) {
        return BorderFactory.createCompoundBorder(
                BorderFactory.createTitledBorder(BorderFactory.createLineBorder(SECTION_BORDER), title),
                BorderFactory.createEmptyBorder(4, 6, 6, 6));
    }

    public static Border fieldBorder() {
        return BorderFactory.createCompoundBorder(
                BorderFactory.createLineBorder(new Color(0x9AA3B2)),
                BorderFactory.createEmptyBorder(1, 3, 1, 3));
    }

    public static Border invalidBorder() {
        return BorderFactory.createCompoundBorder(
                BorderFactory.createLineBorder(ERROR_BORDER, 2),
                BorderFactory.createEmptyBorder(1, 3, 1, 3));
    }
}
