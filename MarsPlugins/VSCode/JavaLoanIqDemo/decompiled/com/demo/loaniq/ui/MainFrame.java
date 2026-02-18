/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.ui;

import com.demo.loaniq.data.SampleDataFactory;
import com.demo.loaniq.model.DemoState;
import com.demo.loaniq.ui.panels.DealNotebookPanel;
import com.demo.loaniq.ui.panels.FacilityNotebookPanel;
import com.demo.loaniq.ui.panels.LoanNotebookPanel;
import com.demo.loaniq.ui.panels.PaymentNotebookPanel;
import com.demo.loaniq.ui.widgets.StatusBar;
import com.demo.loaniq.util.UiTreeDumper;
import java.awt.BorderLayout;
import java.awt.Component;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import javax.swing.JFrame;
import javax.swing.JMenu;
import javax.swing.JMenuBar;
import javax.swing.JMenuItem;
import javax.swing.JPopupMenu;
import javax.swing.JScrollPane;
import javax.swing.JSplitPane;
import javax.swing.JTabbedPane;
import javax.swing.JTree;
import javax.swing.tree.DefaultMutableTreeNode;
import javax.swing.tree.DefaultTreeModel;
import javax.swing.tree.TreePath;

public class MainFrame
extends JFrame {
    private static final String KEY_DEAL = "deal";
    private static final String KEY_FACILITY = "facility";
    private static final String KEY_LOAN = "loan";
    private static final String KEY_PAYMENTS = "payments";
    private final DemoState state;
    private final JTree tree;
    private final JTabbedPane tabbedPane;
    private final DealNotebookPanel dealPanel;
    private final FacilityNotebookPanel facilityPanel;
    private final LoanNotebookPanel loanPanel;
    private final PaymentNotebookPanel paymentPanel;
    private final StatusBar statusBar;

    public MainFrame() {
        this.setTitle("LoanIQ Demo");
        this.setDefaultCloseOperation(3);
        this.setSize(1000, 700);
        this.setLocationRelativeTo(null);
        this.state = SampleDataFactory.createSampleState();
        this.setJMenuBar(this.buildMenuBar());
        JSplitPane split = new JSplitPane(1);
        split.setLeftComponent(this.buildTreePanel());
        this.tree = (JTree)((JScrollPane)split.getLeftComponent()).getViewport().getView();
        this.tabbedPane = new JTabbedPane();
        this.tabbedPane.setName("LIQ_MAIN_TABBED_PANE");
        this.dealPanel = new DealNotebookPanel(this.state, this::refreshStatusBar);
        this.dealPanel.setName("LIQ_DEAL_NOTEBOOK");
        this.facilityPanel = new FacilityNotebookPanel(this.state, this::refreshStatusBar);
        this.facilityPanel.setName("LIQ_FACILITY_NOTEBOOK");
        this.loanPanel = new LoanNotebookPanel(this.state, this::refreshStatusBar);
        this.loanPanel.setName("LIQ_LOAN_NOTEBOOK");
        this.paymentPanel = new PaymentNotebookPanel(this.state, this::refreshStatusBar);
        this.paymentPanel.setName("LIQ_PAYMENT_NOTEBOOK");
        this.tabbedPane.addTab("Deal Notebook", this.dealPanel);
        this.tabbedPane.addTab("Facility Notebook", this.facilityPanel);
        this.tabbedPane.addTab("Loan Notebook", this.loanPanel);
        this.tabbedPane.addTab("Payment Notebook", this.paymentPanel);
        split.setRightComponent(this.tabbedPane);
        split.setResizeWeight(0.2);
        this.statusBar = new StatusBar();
        this.refreshStatusBar();
        this.getContentPane().setLayout(new BorderLayout());
        this.getContentPane().add((Component)split, "Center");
        this.getContentPane().add((Component)this.statusBar, "South");
        this.loadTreeFromState();
        this.onTreeSelection();
    }

    private void refreshStatusBar() {
        this.statusBar.setEnv(this.state.getEnv());
        this.statusBar.setUser(this.state.getUser());
        this.statusBar.setMessage(this.state.getLastMessage());
    }

    private JMenuBar buildMenuBar() {
        JMenuBar bar = new JMenuBar();
        bar.setName("LIQ_MENU_BAR");
        JMenu file = new JMenu("File");
        file.setName("LIQ_MENU_FILE");
        bar.add(file);
        JMenu actions = new JMenu("Actions");
        actions.setName("LIQ_MENU_ACTIONS");
        bar.add(actions);
        JMenu options = new JMenu("Options");
        options.setName("LIQ_MENU_OPTIONS");
        bar.add(options);
        JMenu window = new JMenu("Window");
        window.setName("LIQ_MENU_WINDOW");
        bar.add(window);
        JMenu help = new JMenu("Help");
        help.setName("LIQ_MENU_HELP");
        JMenuItem dumpItem = new JMenuItem("Dump UI Tree");
        dumpItem.setName("LIQ_MENU_DUMP_UI_TREE");
        dumpItem.addActionListener(e -> {
            UiTreeDumper.dumpToConsoleAndFile(this);
            this.state.setLastMessage("UI tree dumped");
            this.refreshStatusBar();
        });
        help.add(dumpItem);
        bar.add(help);
        return bar;
    }

    private JScrollPane buildTreePanel() {
        DefaultMutableTreeNode root = new DefaultMutableTreeNode("Portfolio");
        DefaultMutableTreeNode deals = new DefaultMutableTreeNode("Deals");
        DefaultMutableTreeNode deal1 = new DefaultMutableTreeNode("US_Syndicated_Loan_001");
        deal1.setUserObject(new NodeUserObject(KEY_DEAL, "US_Syndicated_Loan_001"));
        DefaultMutableTreeNode fac = new DefaultMutableTreeNode("Facility_A");
        fac.setUserObject(new NodeUserObject(KEY_FACILITY, "FACILITY_A"));
        DefaultMutableTreeNode loan = new DefaultMutableTreeNode("Loan_T3750");
        loan.setUserObject(new NodeUserObject(KEY_LOAN, "T3750"));
        fac.add(loan);
        deal1.add(fac);
        DefaultMutableTreeNode payments = new DefaultMutableTreeNode("Payments");
        payments.setUserObject(new NodeUserObject(KEY_PAYMENTS, null));
        deal1.add(payments);
        deals.add(deal1);
        root.add(deals);
        root.add(new DefaultMutableTreeNode("Customers"));
        root.add(new DefaultMutableTreeNode("Work Queue"));
        JTree t = new JTree(new DefaultTreeModel(root));
        t.setName("LIQ_MAIN_TREE");
        t.expandRow(0);
        t.expandRow(1);
        t.expandRow(2);
        t.addTreeSelectionListener(e -> this.onTreeSelection());
        t.addMouseListener(new MouseAdapter(this){
            final /* synthetic */ MainFrame this$0;
            {
                this.this$0 = mainFrame;
            }

            @Override
            public void mousePressed(MouseEvent mouseEvent) {
                if (mouseEvent.isPopupTrigger()) {
                    this.showTreePopup(mouseEvent);
                }
            }

            @Override
            public void mouseReleased(MouseEvent mouseEvent) {
                if (mouseEvent.isPopupTrigger()) {
                    this.showTreePopup(mouseEvent);
                }
            }

            private void showTreePopup(MouseEvent mouseEvent) {
                TreePath treePath = jTree.getPathForLocation(mouseEvent.getX(), mouseEvent.getY());
                if (treePath == null) {
                    return;
                }
                jTree.setSelectionPath(treePath);
                JPopupMenu jPopupMenu = new JPopupMenu();
                JMenuItem jMenuItem = new JMenuItem("Open");
                jMenuItem.addActionListener(actionEvent -> this.this$0.onTreeSelection());
                JMenuItem jMenuItem2 = new JMenuItem("Approve");
                jMenuItem2.addActionListener(actionEvent -> this.this$0.doApproveCurrent());
                JMenuItem jMenuItem3 = new JMenuItem("Release");
                jMenuItem3.addActionListener(actionEvent -> this.this$0.doReleaseCurrent());
                jPopupMenu.add(jMenuItem);
                jPopupMenu.add(jMenuItem2);
                jPopupMenu.add(jMenuItem3);
                jPopupMenu.show(jTree, mouseEvent.getX(), mouseEvent.getY());
            }
        });
        JScrollPane sp = new JScrollPane(t);
        sp.setName("LIQ_TREE_SCROLL");
        return sp;
    }

    private void doApproveCurrent() {
        int idx = this.tabbedPane.getSelectedIndex();
        if (idx == 0) {
            this.state.getSelectedDeal().setStatus("Approved");
            this.dealPanel.loadFrom(this.state.getSelectedDeal());
        } else if (idx == 1) {
            this.state.getSelectedFacility().setStatus("Approved");
            this.facilityPanel.loadFrom(this.state.getSelectedFacility());
        }
        this.state.setLastMessage("Approved");
        this.refreshStatusBar();
    }

    private void doReleaseCurrent() {
        if (this.state.getSelectedLoan() != null) {
            this.state.getSelectedLoan().setStatus("Released");
            this.loanPanel.loadFrom(this.state.getSelectedLoan());
            this.state.setLastMessage("Released");
            this.refreshStatusBar();
        }
    }

    private void loadTreeFromState() {
    }

    private void onTreeSelection() {
        String key;
        TreePath path = this.tree.getSelectionPath();
        if (path == null) {
            return;
        }
        Object last = path.getLastPathComponent();
        if (!(last instanceof DefaultMutableTreeNode)) {
            return;
        }
        DefaultMutableTreeNode node = (DefaultMutableTreeNode)last;
        Object uo = node.getUserObject();
        String string = key = uo instanceof NodeUserObject ? ((NodeUserObject)uo).key : null;
        if (KEY_DEAL.equals(key)) {
            this.tabbedPane.setSelectedIndex(0);
            this.dealPanel.loadFrom(this.state.getSelectedDeal());
        } else if (KEY_FACILITY.equals(key)) {
            this.tabbedPane.setSelectedIndex(1);
            this.facilityPanel.loadFrom(this.state.getSelectedFacility());
        } else if (KEY_LOAN.equals(key)) {
            this.tabbedPane.setSelectedIndex(2);
            this.loanPanel.loadFrom(this.state.getSelectedLoan());
        } else if (KEY_PAYMENTS.equals(key)) {
            this.tabbedPane.setSelectedIndex(3);
            this.paymentPanel.loadFrom(this.state.getSelectedPayment());
        }
        this.refreshStatusBar();
    }

    private static class NodeUserObject {
        final String key;
        final String id;

        NodeUserObject(String string, String string2) {
            this.key = string;
            this.id = string2;
        }

        public String toString() {
            if (MainFrame.KEY_DEAL.equals(this.key)) {
                return "US_Syndicated_Loan_001";
            }
            if (MainFrame.KEY_FACILITY.equals(this.key)) {
                return "Facility_A";
            }
            if (MainFrame.KEY_LOAN.equals(this.key)) {
                return "Loan_T3750";
            }
            if (MainFrame.KEY_PAYMENTS.equals(this.key)) {
                return "Payments";
            }
            return this.id != null ? this.id : this.key;
        }
    }
}

