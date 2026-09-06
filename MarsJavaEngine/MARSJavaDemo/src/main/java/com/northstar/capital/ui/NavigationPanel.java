package com.northstar.capital.ui;

import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTree;
import javax.swing.border.EmptyBorder;
import javax.swing.tree.DefaultMutableTreeNode;
import javax.swing.tree.DefaultTreeModel;
import javax.swing.tree.TreePath;
import javax.swing.tree.TreeSelectionModel;
import java.awt.BorderLayout;
import java.awt.Dimension;
import java.util.function.Consumer;

public class NavigationPanel extends JPanel {
    public static final String NAV_DASHBOARD = "Dashboard";
    public static final String NAV_BOND_ENTRY = "Bond Entry";
    public static final String NAV_REPO = "Repo";
    public static final String NAV_SECURITY_SEARCH = "Security Search";
    public static final String NAV_FX_SPOT = "FX Spot";
    public static final String NAV_FX_FORWARD = "FX Forward";
    public static final String NAV_FX_SWAP = "FX Swap";
    public static final String NAV_IRS = "Interest Rate Swap";
    public static final String NAV_FRA = "FRA";
    public static final String NAV_BLOTTER = "Trade Blotter";
    public static final String NAV_AMENDMENTS = "Amendments";
    public static final String NAV_CANCELLATIONS = "Cancellations";
    public static final String NAV_POSITIONS = "Positions";
    public static final String NAV_MARKET_RISK = "Market Risk";
    public static final String NAV_LIMITS = "Limits";
    public static final String NAV_SETTLEMENTS = "Settlements";
    public static final String NAV_CONFIRMATIONS = "Confirmations";
    public static final String NAV_EXCEPTIONS = "Exceptions";
    public static final String NAV_COUNTERPARTIES = "Counterparties";
    public static final String NAV_SECURITIES = "Securities";
    public static final String NAV_BOOKS = "Books";
    public static final String NAV_SSI = "Settlement Instructions";

    private final JTree tree;

    public NavigationPanel(Consumer<String> onSelect) {
        setName("navigationPanel");
        setLayout(new BorderLayout());
        setBackground(Theme.TREE_BG);
        setPreferredSize(new Dimension(220, 100));
        setBorder(new EmptyBorder(4, 4, 4, 4));

        DefaultMutableTreeNode root = new DefaultMutableTreeNode("Northstar CM");
        root.add(leaf(NAV_DASHBOARD));

        DefaultMutableTreeNode trading = node("Trading");
        DefaultMutableTreeNode fi = node("Fixed Income");
        fi.add(leaf(NAV_BOND_ENTRY));
        fi.add(leaf(NAV_REPO));
        fi.add(leaf(NAV_SECURITY_SEARCH));
        DefaultMutableTreeNode fx = node("Foreign Exchange");
        fx.add(leaf(NAV_FX_SPOT));
        fx.add(leaf(NAV_FX_FORWARD));
        fx.add(leaf(NAV_FX_SWAP));
        DefaultMutableTreeNode rates = node("Rates");
        rates.add(leaf(NAV_IRS));
        rates.add(leaf(NAV_FRA));
        trading.add(fi);
        trading.add(fx);
        trading.add(rates);
        root.add(trading);

        DefaultMutableTreeNode tm = node("Trade Management");
        tm.add(leaf(NAV_BLOTTER));
        tm.add(leaf(NAV_AMENDMENTS));
        tm.add(leaf(NAV_CANCELLATIONS));
        root.add(tm);

        DefaultMutableTreeNode risk = node("Risk");
        risk.add(leaf(NAV_POSITIONS));
        risk.add(leaf(NAV_MARKET_RISK));
        risk.add(leaf(NAV_LIMITS));
        root.add(risk);

        DefaultMutableTreeNode ops = node("Operations");
        ops.add(leaf(NAV_SETTLEMENTS));
        ops.add(leaf(NAV_CONFIRMATIONS));
        ops.add(leaf(NAV_EXCEPTIONS));
        root.add(ops);

        DefaultMutableTreeNode ref = node("Reference Data");
        ref.add(leaf(NAV_COUNTERPARTIES));
        ref.add(leaf(NAV_SECURITIES));
        ref.add(leaf(NAV_BOOKS));
        ref.add(leaf(NAV_SSI));
        root.add(ref);

        tree = new JTree(new DefaultTreeModel(root));
        tree.setName("navigationTree");
        tree.setFont(Theme.UI_FONT);
        tree.setBackground(Theme.TREE_BG);
        tree.setRootVisible(false);
        tree.setShowsRootHandles(true);
        tree.getSelectionModel().setSelectionMode(TreeSelectionModel.SINGLE_TREE_SELECTION);
        expandAll();
        tree.addTreeSelectionListener(e -> {
            DefaultMutableTreeNode node = (DefaultMutableTreeNode) tree.getLastSelectedPathComponent();
            if (node != null && node.isLeaf() && node.getUserObject() instanceof NavNode nav) {
                onSelect.accept(nav.key());
            }
        });

        JScrollPane scroll = new JScrollPane(tree);
        scroll.setName("navigationScroll");
        scroll.setBorder(null);
        add(scroll, BorderLayout.CENTER);
    }

    public void select(String key) {
        DefaultMutableTreeNode root = (DefaultMutableTreeNode) tree.getModel().getRoot();
        DefaultMutableTreeNode match = find(root, key);
        if (match != null) {
            TreePath path = new TreePath(match.getPath());
            tree.setSelectionPath(path);
            tree.scrollPathToVisible(path);
        }
    }

    private DefaultMutableTreeNode find(DefaultMutableTreeNode node, String key) {
        if (node.getUserObject() instanceof NavNode nav && key.equals(nav.key())) {
            return node;
        }
        for (int i = 0; i < node.getChildCount(); i++) {
            DefaultMutableTreeNode found = find((DefaultMutableTreeNode) node.getChildAt(i), key);
            if (found != null) {
                return found;
            }
        }
        return null;
    }

    private void expandAll() {
        for (int i = 0; i < tree.getRowCount(); i++) {
            tree.expandRow(i);
        }
    }

    private static DefaultMutableTreeNode node(String label) {
        return new DefaultMutableTreeNode(new NavNode(label, label));
    }

    private static DefaultMutableTreeNode leaf(String key) {
        return new DefaultMutableTreeNode(new NavNode(key, key));
    }

    public record NavNode(String key, String label) {
        @Override
        public String toString() {
            return label;
        }
    }
}
