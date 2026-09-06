package com.mars.javaengine.automation;

import java.awt.Component;
import java.awt.Rectangle;
import javax.swing.JMenuItem;
import javax.swing.JTabbedPane;
import javax.swing.JTable;
import javax.swing.JTree;
import javax.swing.tree.TreePath;

public class ControlHandle {
    public static final String KIND_COMPONENT = "COMPONENT";
    public static final String KIND_TREE_NODE = "TREE_NODE";
    public static final String KIND_TAB = "TAB";
    public static final String KIND_TABLE = "TABLE";
    public static final String KIND_MENU_ITEM = "MENU_ITEM";

    private final String id;
    private final String kind;
    private final Component component;
    private final String className;
    private final String name;
    private final String label;
    private final String type;
    private JTree tree;
    private TreePath treePath;
    private JTabbedPane tabbedPane;
    private int tabIndex = -1;
    private JTable table;
    private int tableRow = -1;
    private int tableColumn = -1;
    private JMenuItem menuItem;

    public ControlHandle(String id, String kind, Component component, String className,
                         String name, String label, String type) {
        this.id = id;
        this.kind = kind;
        this.component = component;
        this.className = className;
        this.name = name;
        this.label = label;
        this.type = type;
    }

    public String getId() {
        return id;
    }

    public String getKind() {
        return kind;
    }

    public Component getComponent() {
        return component;
    }

    public String getClassName() {
        return className;
    }

    public String getName() {
        return name;
    }

    public String getLabel() {
        return label;
    }

    public String getType() {
        return type;
    }

    public JTree getTree() {
        return tree;
    }

    public void setTree(JTree tree, TreePath treePath) {
        this.tree = tree;
        this.treePath = treePath;
    }

    public TreePath getTreePath() {
        return treePath;
    }

    public JTabbedPane getTabbedPane() {
        return tabbedPane;
    }

    public void setTabbedPane(JTabbedPane tabbedPane, int tabIndex) {
        this.tabbedPane = tabbedPane;
        this.tabIndex = tabIndex;
    }

    public int getTabIndex() {
        return tabIndex;
    }

    public JTable getTable() {
        return table;
    }

    public void setTable(JTable table, int row, int column) {
        this.table = table;
        this.tableRow = row;
        this.tableColumn = column;
    }

    public int getTableRow() {
        return tableRow;
    }

    public int getTableColumn() {
        return tableColumn;
    }

    public JMenuItem getMenuItem() {
        return menuItem;
    }

    public void setMenuItem(JMenuItem menuItem) {
        this.menuItem = menuItem;
    }

    public boolean isStale() {
        if (KIND_MENU_ITEM.equals(kind)) {
            return menuItem == null;
        }
        if (KIND_TREE_NODE.equals(kind)) {
            return tree == null || !tree.isShowing();
        }
        if (KIND_TAB.equals(kind)) {
            return tabbedPane == null || !tabbedPane.isShowing();
        }
        return component == null || !component.isDisplayable();
    }

    public Rectangle boundsOnScreen() {
        Component target = component;
        if (target == null && tree != null) {
            target = tree;
        }
        if (target == null && tabbedPane != null) {
            target = tabbedPane;
        }
        if (target == null || !target.isShowing()) {
            return null;
        }
        try {
            java.awt.Point p = target.getLocationOnScreen();
            return new Rectangle(p.x, p.y, target.getWidth(), target.getHeight());
        } catch (Exception ex) {
            return null;
        }
    }
}
