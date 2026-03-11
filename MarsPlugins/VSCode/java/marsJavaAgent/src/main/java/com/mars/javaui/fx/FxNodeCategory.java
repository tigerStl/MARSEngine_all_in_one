package com.mars.javaui.fx;

/**
 * JavaFX node classification: Category, Boundary, NodeMeta, LiftResult.
 * Used by FxNodeClassifier for table-driven fold/lift and step target resolution.
 *
 * Semantic object kinds (语义对象类别):
 * - <b>Composite semantic</b> (组合语义): TableView, TreeView, ListView, TabPane and their parts
 *   (TableCell, TreeCell, ListCell, Tab). Step target is the part (cell/tab) when inside one.
 * - <b>Simple semantic</b> (简单语义): TextField, Button, CheckBox, ComboBox, etc. For these we
 *   always look upward (within MaxAncestorHops) for a composite-semantic context; only if none is
 *   found do we use the simple semantic object itself as the step target.
 * - <b>Terminal semantic</b> (可停止的语义): e.g. MenuBarButton, MenuItem. Same as simple semantic
 *   but we stop immediately—no need to look up, since they never sit under a composite we prefer.
 */
public final class FxNodeCategory {

    private FxNodeCategory() {}

    // --- Category (semantic kind) ---
    public static final String SEMANTIC_CONTROL = "SEMANTIC_CONTROL";
    public static final String SEMANTIC_PART = "SEMANTIC_PART";
    public static final String STRUCTURAL_CONTAINER = "STRUCTURAL_CONTAINER";
    public static final String DECORATION = "DECORATION";
    public static final String UNKNOWN = "UNKNOWN";

    // --- Boundary (step target role) ---
    public static final String ACTION_BOUNDARY = "ACTION_BOUNDARY";
    public static final String COMPOSITE_BOUNDARY = "COMPOSITE_BOUNDARY";
    public static final String NON_BOUNDARY = "NON_BOUNDARY";

    /** Marks composite-semantic context: part of Table/Tree/List/TabPane (TableCell, TreeCell, ListCell, Tab). */
    public static final String COMPOSITE_SEMANTIC = "COMPOSITE_SEMANTIC";
    /** Marks simple-semantic control: Button, TextField, CheckBox, etc.; must look up for composite context first. */
    public static final String SIMPLE_SEMANTIC = "SIMPLE_SEMANTIC";

    /** Result of classify(node). */
    public static final class NodeMeta {
        public final Object node;
        public final String category;
        public final String boundary;
        public final String semanticType;
        public final float confidence;

        public NodeMeta(Object node, String category, String boundary, String semanticType) {
            this(node, category, boundary, semanticType, 1f);
        }

        public NodeMeta(Object node, String category, String boundary, String semanticType, float confidence) {
            this.node = node;
            this.category = category;
            this.boundary = boundary;
            this.semanticType = semanticType != null ? semanticType : UNKNOWN;
            this.confidence = confidence;
        }
    }

    /** Result of foldAndLift(eventTarget): semanticTarget + semanticParent for step building. */
    public static final class LiftResult {
        public final Object semanticTarget;
        public final Object semanticParent;
        public final java.util.List<Object> chainFoldedNodes;

        public LiftResult(Object semanticTarget, Object semanticParent, java.util.List<Object> chainFoldedNodes) {
            this.semanticTarget = semanticTarget;
            this.semanticParent = semanticParent;
            this.chainFoldedNodes = chainFoldedNodes != null ? chainFoldedNodes : java.util.Collections.emptyList();
        }
    }
}
