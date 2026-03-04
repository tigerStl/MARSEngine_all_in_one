package com.mars.javafxdemo;

import javafx.application.Application;
import javafx.application.Platform;
import javafx.beans.property.SimpleStringProperty;
import javafx.geometry.Insets;
import javafx.scene.Scene;
import javafx.scene.control.*;
import javafx.scene.control.cell.TextFieldTableCell;
import javafx.scene.layout.*;
import javafx.stage.Stage;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

/**
 * LoanIQ-style JavaFX demo for UI automation testing.
 * Includes: multi-level menu, popup menu, tree, table, tabs, checkbox, radiobutton.
 */
public class LoanIQStyleDemo extends Application {

    private TreeView<String> dealTree;
    private TableView<DealRow> dealTable;
    private final List<DealRow> dealData = new ArrayList<>();

    @Override
    public void start(Stage primaryStage) {
        primaryStage.setTitle("LoanIQ Demo - Deal Management");
        primaryStage.setWidth(1100);
        primaryStage.setHeight(700);

        BorderPane root = new BorderPane();
        root.setTop(buildMenuBar());
        root.setLeft(buildDealTree());
        root.setCenter(buildCenterContent());
        root.setBottom(buildStatusBar());

        Scene scene = new Scene(root);
        primaryStage.setScene(scene);
        primaryStage.setOnCloseRequest(e -> Platform.exit());
        primaryStage.show();
    }

    private MenuBar buildMenuBar() {
        MenuBar menuBar = new MenuBar();

        // File
        Menu fileMenu = new Menu("_File");
        MenuItem newDeal = new MenuItem("_New Deal");
        newDeal.setOnAction(e -> onNewDeal());
        MenuItem open = new MenuItem("_Open...");
        open.setOnAction(e -> onOpen());
        MenuItem save = new MenuItem("_Save");
        save.setOnAction(e -> onSave());
        SeparatorMenuItem sep1 = new SeparatorMenuItem();
        MenuItem exit = new MenuItem("E_xit");
        exit.setOnAction(e -> Platform.exit());
        fileMenu.getItems().addAll(newDeal, open, save, sep1, exit);

        // Deal (multi-level)
        Menu dealMenu = new Menu("_Deal");
        MenuItem newFacility = new MenuItem("_New Facility");
        newFacility.setOnAction(e -> onNewFacility());
        Menu amendMenu = new Menu("_Amend");
        MenuItem amendFacility = new MenuItem("Amend _Facility");
        amendFacility.setOnAction(e -> onAmendFacility());
        MenuItem amendLoan = new MenuItem("Amend _Loan");
        amendLoan.setOnAction(e -> onAmendLoan());
        amendMenu.getItems().addAll(amendFacility, amendLoan);
        MenuItem cancelDeal = new MenuItem("_Cancel Deal");
        cancelDeal.setOnAction(e -> onCancelDeal());
        dealMenu.getItems().addAll(newFacility, amendMenu, new SeparatorMenuItem(), cancelDeal);

        // View
        Menu viewMenu = new Menu("_View");
        MenuItem refresh = new MenuItem("_Refresh");
        refresh.setOnAction(e -> onRefresh());
        Menu filtersMenu = new Menu("_Filters");
        CheckMenuItem showClosed = new CheckMenuItem("Show _Closed Deals");
        showClosed.setSelected(false);
        CheckMenuItem showSyndicated = new CheckMenuItem("Show _Syndicated Only");
        showSyndicated.setSelected(false);
        filtersMenu.getItems().addAll(showClosed, showSyndicated);
        viewMenu.getItems().addAll(refresh, filtersMenu);

        // Help
        Menu helpMenu = new Menu("_Help");
        MenuItem about = new MenuItem("_About");
        about.setOnAction(e -> onAbout());
        helpMenu.getItems().add(about);

        menuBar.getMenus().addAll(fileMenu, dealMenu, viewMenu, helpMenu);
        return menuBar;
    }

    private VBox buildDealTree() {
        TreeItem<String> rootItem = new TreeItem<>("Deals");
        rootItem.setExpanded(true);

        TreeItem<String> deal1 = new TreeItem<>("DEAL-001 - Acme Corp");
        deal1.setExpanded(true);
        TreeItem<String> fac1 = new TreeItem<>("Facility A - Revolver");
        fac1.setExpanded(true);
        fac1.getChildren().add(new TreeItem<>("Loan 1"));
        fac1.getChildren().add(new TreeItem<>("Loan 2"));
        deal1.getChildren().add(fac1);
        TreeItem<String> fac2 = new TreeItem<>("Facility B - Term");
        fac2.getChildren().add(new TreeItem<>("Loan 1"));
        deal1.getChildren().add(fac2);
        rootItem.getChildren().add(deal1);

        TreeItem<String> deal2 = new TreeItem<>("DEAL-002 - Global Inc");
        deal2.setExpanded(true);
        deal2.getChildren().add(new TreeItem<>("Facility A - Bridge"));
        rootItem.getChildren().add(deal2);

        dealTree = new TreeView<>(rootItem);
        dealTree.setPrefWidth(280);
        dealTree.setShowRoot(true);
        dealTree.setId("dealTree");

        ContextMenu treeContext = new ContextMenu();
        MenuItem newFac = new MenuItem("New Facility");
        newFac.setOnAction(e -> onNewFacility());
        MenuItem newLoan = new MenuItem("New Loan");
        newLoan.setOnAction(e -> onNewLoan());
        MenuItem editItem = new MenuItem("Edit");
        editItem.setOnAction(e -> onEditTreeItem());
        MenuItem deleteItem = new MenuItem("Delete");
        deleteItem.setOnAction(e -> onDeleteTreeItem());
        treeContext.getItems().addAll(newFac, newLoan, new SeparatorMenuItem(), editItem, deleteItem);
        dealTree.setContextMenu(treeContext);

        VBox left = new VBox(8);
        left.setPadding(new Insets(8));
        Label lbl = new Label("Deal Structure");
        left.getChildren().addAll(lbl, dealTree);
        VBox.setVgrow(dealTree, Priority.ALWAYS);
        return left;
    }

    private TabPane buildCenterContent() {
        TabPane tabs = new TabPane();
        tabs.setId("mainTabPane");

        Tab overviewTab = new Tab("Overview");
        overviewTab.setContent(buildOverviewTab());
        overviewTab.setId("tabOverview");

        Tab detailsTab = new Tab("Details");
        detailsTab.setContent(buildDetailsTab());
        detailsTab.setId("tabDetails");

        Tab documentsTab = new Tab("Documents");
        documentsTab.setContent(buildDocumentsTab());
        documentsTab.setId("tabDocuments");

        tabs.getTabs().addAll(overviewTab, detailsTab, documentsTab);
        return tabs;
    }

    private BorderPane buildOverviewTab() {
        dealData.clear();
        dealData.add(new DealRow("DEAL-001", "Acme Corp", "50,000,000", "USD", "Active"));
        dealData.add(new DealRow("DEAL-002", "Global Inc", "25,000,000", "EUR", "Active"));
        dealData.add(new DealRow("DEAL-003", "Beta Ltd", "10,000,000", "GBP", "Closed"));

        TableColumn<DealRow, String> colDealId = new TableColumn<>("Deal ID");
        colDealId.setCellValueFactory(c -> c.getValue().dealIdProperty());
        colDealId.setCellFactory(TextFieldTableCell.forTableColumn());
        colDealId.setOnEditCommit(e -> e.getRowValue().dealIdProperty().set(e.getNewValue()));
        colDealId.setPrefWidth(100);
        TableColumn<DealRow, String> colBorrower = new TableColumn<>("Borrower");
        colBorrower.setCellValueFactory(c -> c.getValue().borrowerProperty());
        colBorrower.setCellFactory(TextFieldTableCell.forTableColumn());
        colBorrower.setOnEditCommit(e -> e.getRowValue().borrowerProperty().set(e.getNewValue()));
        colBorrower.setPrefWidth(150);
        TableColumn<DealRow, String> colAmount = new TableColumn<>("Amount");
        colAmount.setCellValueFactory(c -> c.getValue().amountProperty());
        colAmount.setCellFactory(TextFieldTableCell.forTableColumn());
        colAmount.setOnEditCommit(e -> e.getRowValue().amountProperty().set(e.getNewValue()));
        colAmount.setPrefWidth(120);
        TableColumn<DealRow, String> colCurrency = new TableColumn<>("Currency");
        colCurrency.setCellValueFactory(c -> c.getValue().currencyProperty());
        colCurrency.setCellFactory(TextFieldTableCell.forTableColumn());
        colCurrency.setOnEditCommit(e -> e.getRowValue().currencyProperty().set(e.getNewValue()));
        colCurrency.setPrefWidth(80);
        TableColumn<DealRow, String> colStatus = new TableColumn<>("Status");
        colStatus.setCellValueFactory(c -> c.getValue().statusProperty());
        colStatus.setCellFactory(TextFieldTableCell.forTableColumn());
        colStatus.setOnEditCommit(e -> e.getRowValue().statusProperty().set(e.getNewValue()));
        colStatus.setPrefWidth(80);

        dealTable = new TableView<>();
        dealTable.setId("dealTable");
        dealTable.getColumns().addAll(colDealId, colBorrower, colAmount, colCurrency, colStatus);
        dealTable.getItems().addAll(dealData);
        dealTable.setEditable(true);
        dealTable.setColumnResizePolicy(TableView.CONSTRAINED_RESIZE_POLICY);

        ContextMenu tableContext = new ContextMenu();
        MenuItem addRow = new MenuItem("Add Deal");
        addRow.setOnAction(e -> onAddDealRow());
        MenuItem editRow = new MenuItem("Edit");
        editRow.setOnAction(e -> onEditTableRow());
        MenuItem removeRow = new MenuItem("Remove");
        removeRow.setOnAction(e -> onRemoveTableRow());
        MenuItem copyRow = new MenuItem("Copy");
        copyRow.setOnAction(e -> onCopyRow());
        tableContext.getItems().addAll(addRow, new SeparatorMenuItem(), editRow, removeRow, copyRow);
        dealTable.setContextMenu(tableContext);

        BorderPane pane = new BorderPane(dealTable);
        pane.setPadding(new Insets(8));
        return pane;
    }

    private GridPane buildDetailsTab() {
        GridPane grid = new GridPane();
        grid.setHgap(12);
        grid.setVgap(10);
        grid.setPadding(new Insets(16));

        int row = 0;
        grid.add(new Label("Deal ID:"), 0, row);
        TextField dealIdField = new TextField("DEAL-001");
        dealIdField.setId("fieldDealId");
        grid.add(dealIdField, 1, row++);
        grid.add(new Label("Borrower:"), 0, row);
        TextField borrowerField = new TextField("Acme Corp");
        borrowerField.setId("fieldBorrower");
        grid.add(borrowerField, 1, row++);
        grid.add(new Label("Product Type:"), 0, row);
        ToggleGroup productGroup = new ToggleGroup();
        RadioButton rbSyndicated = new RadioButton("Syndicated");
        rbSyndicated.setToggleGroup(productGroup);
        rbSyndicated.setSelected(true);
        rbSyndicated.setId("radioSyndicated");
        RadioButton rbBilateral = new RadioButton("Bilateral");
        rbBilateral.setToggleGroup(productGroup);
        rbBilateral.setId("radioBilateral");
        HBox productBox = new HBox(16, rbSyndicated, rbBilateral);
        grid.add(productBox, 1, row++);
        grid.add(new Label("Options:"), 0, row);
        CheckBox cbRevolver = new CheckBox("Revolver");
        cbRevolver.setId("checkRevolver");
        CheckBox cbTerm = new CheckBox("Term Loan");
        cbTerm.setId("checkTerm");
        cbTerm.setSelected(true);
        CheckBox cbBridge = new CheckBox("Bridge");
        cbBridge.setId("checkBridge");
        HBox optionsBox = new HBox(16, cbRevolver, cbTerm, cbBridge);
        grid.add(optionsBox, 1, row++);
        grid.add(new Label("Include closed:"), 0, row);
        CheckBox cbIncludeClosed = new CheckBox("Include closed deals in list");
        cbIncludeClosed.setId("checkIncludeClosed");
        grid.add(cbIncludeClosed, 1, row++);
        return grid;
    }

    private VBox buildDocumentsTab() {
        ListView<String> docList = new ListView<>();
        docList.setId("docList");
        docList.getItems().addAll("Credit Agreement - v1.2.pdf", "Fee Letter.pdf", "Amendment No.1.pdf");
        VBox box = new VBox(8, new Label("Deal Documents"), docList);
        box.setPadding(new Insets(8));
        VBox.setVgrow(docList, Priority.ALWAYS);
        return box;
    }

    private HBox buildStatusBar() {
        Label status = new Label("Ready");
        status.setId("statusLabel");
        Region spacer = new Region();
        HBox.setHgrow(spacer, Priority.ALWAYS);
        Label user = new Label("User: Demo");
        HBox bar = new HBox(12, status, spacer, user);
        bar.setPadding(new Insets(6, 10, 6, 10));
        bar.setStyle("-fx-background-color: #e0e0e0;");
        return bar;
    }

    private void onNewDeal() {
        showInfo("New Deal", "Create new deal dialog would open here.");
    }

    private void onOpen() {
        showInfo("Open", "Open deal dialog would open here.");
    }

    private void onSave() {
        showInfo("Save", "Deal saved.");
    }

    private void onNewFacility() {
        showInfo("New Facility", "New facility under selected deal.");
    }

    private void onNewLoan() {
        showInfo("New Loan", "New loan under selected facility.");
    }

    private void onAmendFacility() {
        showInfo("Amend Facility", "Amend facility dialog.");
    }

    private void onAmendLoan() {
        showInfo("Amend Loan", "Amend loan dialog.");
    }

    private void onCancelDeal() {
        showInfo("Cancel Deal", "Cancel deal confirmation.");
    }

    private void onRefresh() {
        showInfo("Refresh", "Data refreshed.");
    }

    private void onAbout() {
        showInfo("About", "LoanIQ Style Demo v1.0\nFor JavaFX UI automation testing.");
    }

    private void onEditTreeItem() {
        TreeItem<String> sel = dealTree.getSelectionModel().getSelectedItem();
        if (sel != null) showInfo("Edit", "Edit: " + sel.getValue());
    }

    private void onDeleteTreeItem() {
        TreeItem<String> sel = dealTree.getSelectionModel().getSelectedItem();
        if (sel != null) showInfo("Delete", "Delete: " + sel.getValue());
    }

    private void onAddDealRow() {
        dealData.add(new DealRow("DEAL-NEW", "New Borrower", "0", "USD", "Draft"));
        dealTable.getItems().add(dealData.get(dealData.size() - 1));
    }

    private void onEditTableRow() {
        DealRow row = dealTable.getSelectionModel().getSelectedItem();
        if (row != null) showInfo("Edit Row", "Edit " + row.getDealId());
    }

    private void onRemoveTableRow() {
        DealRow row = dealTable.getSelectionModel().getSelectedItem();
        if (row != null) {
            dealTable.getItems().remove(row);
            dealData.remove(row);
        }
    }

    private void onCopyRow() {
        DealRow row = dealTable.getSelectionModel().getSelectedItem();
        if (row != null) showInfo("Copy", "Copy " + row.getDealId());
    }

    private void showInfo(String title, String message) {
        Alert alert = new Alert(Alert.AlertType.INFORMATION);
        alert.setTitle(title);
        alert.setHeaderText(null);
        alert.setContentText(message);
        alert.showAndWait();
    }

    public static void main(String[] args) {
        launch(args);
    }

    public static class DealRow {
        private final SimpleStringProperty dealId, borrower, amount, currency, status;

        public DealRow(String dealId, String borrower, String amount, String currency, String status) {
            this.dealId = new SimpleStringProperty(dealId);
            this.borrower = new SimpleStringProperty(borrower);
            this.amount = new SimpleStringProperty(amount);
            this.currency = new SimpleStringProperty(currency);
            this.status = new SimpleStringProperty(status);
        }

        public SimpleStringProperty dealIdProperty() { return dealId; }
        public SimpleStringProperty borrowerProperty() { return borrower; }
        public SimpleStringProperty amountProperty() { return amount; }
        public SimpleStringProperty currencyProperty() { return currency; }
        public SimpleStringProperty statusProperty() { return status; }
        public String getDealId() { return dealId.get(); }
        public String getBorrower() { return borrower.get(); }
        public String getAmount() { return amount.get(); }
        public String getCurrency() { return currency.get(); }
        public String getStatus() { return status.get(); }
    }
}
