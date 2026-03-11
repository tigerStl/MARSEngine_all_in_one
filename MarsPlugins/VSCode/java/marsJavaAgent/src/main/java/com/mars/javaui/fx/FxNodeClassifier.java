package com.mars.javaui.fx;

import java.io.InputStream;
import java.io.InputStreamReader;
import java.lang.reflect.Method;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;
import java.util.regex.Pattern;

import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

/**
 * Table-driven JavaFX node classifier: classify(node) -> NodeMeta,
 * foldAndLift(eventTarget) -> LiftResult(semanticTarget, semanticParent).
 * Rules are loaded from fx-node-classifier-rules.json (src/main/resources/fx-node-classifier-rules.json, packaged in JAR).
 */
public final class FxNodeClassifier {

    private static final Logger LOG = Logger.getLogger(FxNodeClassifier.class.getName());

    /** Exclude Skin / SkinBase (e.g. MenuButtonSkinBase) from being treated as semantic Labeled/Button. */
    private static final Pattern STRUCTURAL_SKIN_PATTERN = Pattern.compile(".*Skin(Base)?$", Pattern.CASE_INSENSITIVE);

    /** Classpath resource path: same as java/marsJavaAgent/src/main/resources/fx-node-classifier-rules.json in JAR. */
    private static final String RULES_RESOURCE = "fx-node-classifier-rules.json";

    private FxNodeClassifier() {}

    /** Rule: className contains pattern -> (category, boundary, semanticType). Optional textFrom: "className#methodName" for child-chain text resolution. */
    private static final class ClassRule {
        final String pattern;
        final String category;
        final String boundary;
        final String semanticType;
        /** Optional: e.g. "com.sun.javafx.scene.control.ContextMenuContent$MenuLabel#getText". Search semantic object's child chain for matching class, then invoke method. */
        final List<String> textFrom;

        ClassRule(String pattern, String category, String boundary, String semanticType) {
            this(pattern, category, boundary, semanticType, null);
        }

        ClassRule(String pattern, String category, String boundary, String semanticType, List<String> textFrom) {
            this.pattern = pattern;
            this.category = category;
            this.boundary = boundary;
            this.semanticType = semanticType;
            this.textFrom = textFrom != null ? Collections.unmodifiableList(new ArrayList<>(textFrom)) : Collections.emptyList();
        }
    }

    private static final List<ClassRule> RULES = buildRules();

    /**
     * Build rules from fx-node-classifier-rules.json on classpath (packaged from src/main/resources/fx-node-classifier-rules.json).
     * Falls back to built-in rules if the resource is missing or invalid.
     */
    private static List<ClassRule> buildRules() {
        try (InputStream in = FxNodeClassifier.class.getClassLoader().getResourceAsStream(RULES_RESOURCE)) {
            if (in != null) {
                List<ClassRule> fromJson = new ArrayList<>();
                JsonArray arr = JsonParser.parseReader(new InputStreamReader(in, StandardCharsets.UTF_8)).getAsJsonArray();
                for (int i = 0; i < arr.size(); i++) {
                    JsonObject o = arr.get(i).getAsJsonObject();
                    String pattern = o.has("pattern") ? o.get("pattern").getAsString() : null;
                    String category = o.has("category") ? o.get("category").getAsString() : FxNodeCategory.NON_BOUNDARY;
                    String boundary = o.has("boundary") ? o.get("boundary").getAsString() : FxNodeCategory.NON_BOUNDARY;
                    String semanticType = o.has("semanticType") ? o.get("semanticType").getAsString() : null;
                    List<String> textFrom = null;
                    if (o.has("textFrom") && o.get("textFrom").isJsonArray()) {
                        JsonArray tf = o.getAsJsonArray("textFrom");
                        textFrom = new ArrayList<>();
                        for (int j = 0; j < tf.size(); j++) {
                            String entry = tf.get(j).getAsString();
                            if (entry != null && !entry.isEmpty()) textFrom.add(entry);
                        }
                    }
                    if (pattern != null && !pattern.isEmpty())
                        fromJson.add(new ClassRule(pattern, category, boundary, semanticType != null ? semanticType : "UNKNOWN", textFrom));
                }
                if (!fromJson.isEmpty()) return Collections.unmodifiableList(fromJson);
            }
        } catch (Exception ignored) { }
        return buildRulesFallback();
    }

    private static List<ClassRule> buildRulesFallback() {
        List<ClassRule> r = new ArrayList<>();
        r.add(new ClassRule("TabPaneSkin", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.COMPOSITE_BOUNDARY, "TABPANE"));
        r.add(new ClassRule("TabHeaderSkin", FxNodeCategory.SEMANTIC_PART, FxNodeCategory.ACTION_BOUNDARY, "TAB"));
        List<String> menuTextFrom = java.util.Arrays.asList(
            "com.sun.javafx.scene.control.ContextMenuContent$MenuLabel#getText",
            "com.sun.javafx.scene.control.LabeledText#getText");
        r.add(new ClassRule("ContextMenuContent$MenuLabel", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "MENUITEM", menuTextFrom));
        r.add(new ClassRule("ContextMenuContent$MenuItemContainer", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "MENUITEM", menuTextFrom));
        r.add(new ClassRule("MenuBarButton", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "MENUITEM", menuTextFrom));
        r.add(new ClassRule("MenuItemContainer", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "MENUITEM", menuTextFrom));
        r.add(new ClassRule("javafx.scene.control.skin.", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "SKIN_OR_VFLOW"));
        r.add(new ClassRule("com.sun.javafx.", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "SKIN_OR_VFLOW"));
        r.add(new ClassRule("Skin", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "SKIN_OR_VFLOW"));
        r.add(new ClassRule("VirtualFlow", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "SKIN_OR_VFLOW"));
        r.add(new ClassRule("ScrollBar", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("VirtualScrollBar", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("Pane", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("Region", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("HBox", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("VBox", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("StackPane", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("AnchorPane", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("BorderPane", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("GridPane", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("FlowPane", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("TilePane", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("Group", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("ScrollPane", FxNodeCategory.STRUCTURAL_CONTAINER, FxNodeCategory.NON_BOUNDARY, "LAYOUT_CONTAINER"));
        r.add(new ClassRule("TreeCell", FxNodeCategory.SEMANTIC_PART, FxNodeCategory.ACTION_BOUNDARY, "TREECELL"));
        r.add(new ClassRule("TableCell", FxNodeCategory.SEMANTIC_PART, FxNodeCategory.ACTION_BOUNDARY, "TABLECELL"));
        r.add(new ClassRule("ListCell", FxNodeCategory.SEMANTIC_PART, FxNodeCategory.ACTION_BOUNDARY, "LISTCELL"));
        r.add(new ClassRule("TableColumnHeader", FxNodeCategory.SEMANTIC_PART, FxNodeCategory.ACTION_BOUNDARY, "COLUMN_HEADER"));
        r.add(new ClassRule("TableRow", FxNodeCategory.SEMANTIC_PART, FxNodeCategory.NON_BOUNDARY, "TABLEROW"));
        r.add(new ClassRule("MenuItem", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "MENUITEM", menuTextFrom));
        r.add(new ClassRule("CheckBox", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "CHECKBOX"));
        r.add(new ClassRule("RadioButton", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "RADIOBUTTON"));
        r.add(new ClassRule("Button", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "BUTTON"));
        r.add(new ClassRule("ToggleButton", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "BUTTON"));
        r.add(new ClassRule("Hyperlink", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "BUTTON"));
        r.add(new ClassRule("TextField", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "TEXT_INPUT"));
        r.add(new ClassRule("TextArea", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "TEXT_INPUT"));
        r.add(new ClassRule("PasswordField", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "TEXT_INPUT"));
        r.add(new ClassRule("ComboBox", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "INPUT_CONTROL"));
        r.add(new ClassRule("ChoiceBox", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "INPUT_CONTROL"));
        r.add(new ClassRule("DatePicker", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "INPUT_CONTROL"));
        r.add(new ClassRule("Slider", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "INPUT_CONTROL"));
        r.add(new ClassRule("ColorPicker", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "INPUT_CONTROL"));
        r.add(new ClassRule("TreeView", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.COMPOSITE_BOUNDARY, "TREEVIEW"));
        r.add(new ClassRule("TableView", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.COMPOSITE_BOUNDARY, "TABLEVIEW"));
        r.add(new ClassRule("ListView", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.COMPOSITE_BOUNDARY, "LISTVIEW"));
        r.add(new ClassRule("TabPane", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.COMPOSITE_BOUNDARY, "TABPANE"));
        r.add(new ClassRule("Tab", FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "TAB"));
        r.add(new ClassRule("Label", FxNodeCategory.DECORATION, FxNodeCategory.NON_BOUNDARY, "DECORATION"));
        r.add(new ClassRule("ImageView", FxNodeCategory.DECORATION, FxNodeCategory.NON_BOUNDARY, "DECORATION"));
        r.add(new ClassRule("Separator", FxNodeCategory.DECORATION, FxNodeCategory.NON_BOUNDARY, "DECORATION"));
        r.add(new ClassRule("ProgressBar", FxNodeCategory.DECORATION, FxNodeCategory.NON_BOUNDARY, "DECORATION"));
        r.add(new ClassRule("ProgressIndicator", FxNodeCategory.DECORATION, FxNodeCategory.NON_BOUNDARY, "DECORATION"));
        return Collections.unmodifiableList(r);
    }

    public static FxNodeCategory.NodeMeta classify(Object node) {
        if (node == null) {
            return new FxNodeCategory.NodeMeta(null, FxNodeCategory.UNKNOWN, FxNodeCategory.NON_BOUNDARY, null);
        }
        String className = node.getClass().getName();
        for (ClassRule rule : RULES) {
            if (className.contains(rule.pattern)) {
                return new FxNodeCategory.NodeMeta(node, rule.category, rule.boundary, rule.semanticType);
            }
        }
        if (isInteractable(node)) {
            return new FxNodeCategory.NodeMeta(node, FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "CUSTOM_INTERACTIVE");
        }
        return new FxNodeCategory.NodeMeta(node, FxNodeCategory.UNKNOWN, FxNodeCategory.NON_BOUNDARY, "UNKNOWN");
    }

    /**
     * Returns textFrom hints for the rule that matched this node: list of "className#methodName".
     * Used to get display text by searching the semantic object's child chain for a node whose class
     * matches, then invoking the method (e.g. getText).
     */
    public static List<String> getTextFromHints(Object node) {
        if (node == null) return Collections.emptyList();
        String className = node.getClass().getName();
        for (ClassRule rule : RULES) {
            if (className.contains(rule.pattern) && rule.textFrom != null && !rule.textFrom.isEmpty()) {
                return rule.textFrom;
            }
        }
        return Collections.emptyList();
    }

    /** Fold: structural container / skin / virtualflow never as step target. */
    public static boolean shouldFold(FxNodeCategory.NodeMeta meta) {
        if (meta == null) return true;
        return FxNodeCategory.STRUCTURAL_CONTAINER.equals(meta.category)
                || "SKIN_OR_VFLOW".equals(meta.semanticType);
    }

    /** True if meta is a part of a composite semantic (TableCell, TreeCell, ListCell, Tab). 组合语义的一部分. */
    public static boolean isCompositeSemanticPart(FxNodeCategory.NodeMeta meta) {
        if (meta == null) return false;
        return FxNodeCategory.SEMANTIC_PART.equals(meta.category)
                && FxNodeCategory.ACTION_BOUNDARY.equals(meta.boundary);
    }

    /** True if meta is a simple semantic control (TextField, Button, CheckBox, etc.). 简单语义，需向上查找组合语义. */
    public static boolean isSimpleSemanticControl(FxNodeCategory.NodeMeta meta) {
        if (meta == null) return false;
        return FxNodeCategory.SEMANTIC_CONTROL.equals(meta.category)
                && FxNodeCategory.ACTION_BOUNDARY.equals(meta.boundary);
    }

    /**
     * True if this simple semantic is a <b>terminal</b>: we stop lifting immediately and use it as the step target.
     * Terminal semantics (e.g. MenuBarButton, MenuItem, MenuItemContainer) never sit under a composite we prefer—
     * their parent is typically MenuBar (STRUCTURAL_CONTAINER). So we do not continue walking ancestors.
     * Contrast: other simple semantics (Button, TextField) are kept as candidate but we keep looking up for
     * SEMANTIC_PART (e.g. Tab, TreeCell) or COMPOSITE_BOUNDARY context.
     * 可停止的语义：遇此类即停，不再向上查找组合语义。
     */
    public static boolean isTerminalSemanticControl(FxNodeCategory.NodeMeta meta) {
        if (meta == null) return false;
        return "MENUITEM".equals(meta.semanticType);
    }

    public static Object parentOf(Object node) {
        if (node == null) return null;
        try {
            Method m = node.getClass().getMethod("getParent");
            return m.invoke(node);
        } catch (Exception e) {
            return null;
        }
    }

    private static boolean isInteractable(Object node) {
        if (node == null) return false;
        try {
            Method focus = node.getClass().getMethod("isFocusTraversable");
            if (Boolean.TRUE.equals(focus.invoke(node))) return true;
        } catch (Exception ignored) {
        }
        try {
            Method mt = node.getClass().getMethod("isMouseTransparent");
            Object v = mt.invoke(node);
            if (Boolean.FALSE.equals(v)) return true;
        } catch (Exception ignored) {
        }
        return false;
    }

    /** Normalize: Skin→getSkinnable (Tab etc.); inside TabHeaderSkin→TabHeaderSkin; text/graphic inside Labeled/Button→that control. */
    public static Object normalizeToMeaningfulNode(Object node) {
        if (node == null) return null;
        String name = node.getClass().getName();
        // Spec: Skin → skinnable (model) if possible
        if (isSkin(node)) {
            Object skinnable = getSkinnable(node);
            if (skinnable != null) return skinnable;
        }
        // Any node inside TabHeaderSkin (e.g. LabeledText, Label) -> tab header for SelectTab
        Object tabHeader = nearestAncestorWithClassNameContaining(node, "TabHeaderSkin");
        if (tabHeader != null) return tabHeader;
        if (isTextNodeInsideLabeled(node, name)) {
            Object a = nearestAncestorMatching(node, FxNodeClassifier::isLabeledControl);
            if (a != null) {
                LOG.log(Level.FINE, "normalizeToMeaningfulNode: text node normalized to labeled control, node={0}, ancestor={1}",
                        new Object[]{ name, a.getClass().getName() });
                return a;
            }
        }
        if (isGraphicInsideButton(node, name)) {
            Object a = nearestAncestorMatching(node, FxNodeClassifier::isButton);
            if (a != null) {
                LOG.log(Level.FINE, "normalizeToMeaningfulNode: graphic node normalized to button control, node={0}, ancestor={1}",
                        new Object[]{ name, a.getClass().getName() });
                return a;
            }
        }
        return node;
    }

    private static boolean isSkin(Object node) {
        if (node == null) return false;
        String cn = node.getClass().getName();
        return cn.contains("javafx.scene.control.Skin") || cn.contains(".Skin");
    }

    private static Object getSkinnable(Object skin) {
        if (skin == null) return null;
        try {
            Method m = skin.getClass().getMethod("getSkinnable");
            return m.invoke(skin);
        } catch (Exception e) {
            return null;
        }
    }

    private static Object nearestAncestorWithClassNameContaining(Object node, String fragment) {
        Object cur = parentOf(node);
        while (cur != null) {
            if (cur.getClass().getName().contains(fragment)) return cur;
            cur = parentOf(cur);
        }
        return null;
    }

    private static boolean isTextNodeInsideLabeled(Object node, String className) {
        return (className.contains("Text") && !className.contains("TextField") && !className.contains("TextArea") && !className.contains("PasswordField"))
                || className.contains("Glyph") || (className.contains("Labeled") && className.contains("Text"));
    }

    private static boolean isGraphicInsideButton(Object node, String className) {
        return className.contains("ImageView") || (className.contains("Shape") && !className.contains("Button"));
    }

    private static Object nearestAncestorMatching(Object node, java.util.function.Predicate<Object> predicate) {
        Object cur = parentOf(node);
        while (cur != null) {
            if (predicate.test(cur)) return cur;
            cur = parentOf(cur);
        }
        return null;
    }

    private static boolean isLabeledControl(Object node) {
        if (node == null) return false;
        String n = node.getClass().getName();
        if (STRUCTURAL_SKIN_PATTERN.matcher(n).matches() || n.contains("SkinBase")) {
            LOG.log(Level.FINE, "FxNodeClassifier: node excluded from labeled control (structural/skin), className={0}", n);
            return false;
        }
        return n.contains("Labeled") || n.contains("Button") || n.contains("CheckBox") || n.contains("RadioButton")
                || n.contains("Label") || n.contains("MenuItem") || n.contains("TitledPane") || n.contains("Accordion");
    }

    private static boolean isButton(Object node) {
        if (node == null) return false;
        String n = node.getClass().getName();
        if (STRUCTURAL_SKIN_PATTERN.matcher(n).matches() || n.contains("SkinBase")) {
            LOG.log(Level.FINE, "FxNodeClassifier: node excluded from button control (structural/skin), className={0}", n);
            return false;
        }
        return n.contains("Button") || n.contains("Hyperlink");
    }

    /** Walk up until composite boundary (TreeView/TableView/ListView/TabPane); type-compatible with part. */
    public static Object findOwningCompositeControl(Object partNode) {
        Object cur = parentOf(partNode);
        while (cur != null) {
            FxNodeCategory.NodeMeta meta = classify(cur);
            if (shouldFold(meta)) {
                cur = parentOf(cur);
                continue;
            }
            if (FxNodeCategory.SEMANTIC_CONTROL.equals(meta.category) && FxNodeCategory.COMPOSITE_BOUNDARY.equals(meta.boundary)) {
                if (isOwnerTypeCompatible(partNode, cur)) return cur;
            }
            cur = parentOf(cur);
        }
        return null;
    }

    private static boolean isOwnerTypeCompatible(Object partNode, Object composite) {
        String partType = partNode != null ? partNode.getClass().getName() : "";
        String compType = composite != null ? composite.getClass().getName() : "";
        if (partType.contains("TreeCell") && compType.contains("TreeView")) return true;
        if (partType.contains("TableCell") && compType.contains("TableView")) return true;
        if (partType.contains("ListCell") && compType.contains("ListView")) return true;
        if (partType.contains("ColumnHeader") && compType.contains("TableView")) return true;
        if (partType.contains("TabHeaderSkin") && (compType.contains("TabPane") || compType.contains("TabPaneSkin"))) return true;
        return false;
    }

    public static Object findNearestSemanticControlAncestor(Object node) {
        Object cur = parentOf(node);
        while (cur != null) {
            FxNodeCategory.NodeMeta meta = classify(cur);
            if (shouldFold(meta)) {
                cur = parentOf(cur);
                continue;
            }
            if (FxNodeCategory.SEMANTIC_CONTROL.equals(meta.category)) return cur;
            cur = parentOf(cur);
        }
        return null;
    }

    public static Object findNearestSemanticBoundaryAncestor(Object node) {
        Object cur = parentOf(node);
        while (cur != null) {
            FxNodeCategory.NodeMeta meta = classify(cur);
            if (shouldFold(meta)) {
                cur = parentOf(cur);
                continue;
            }
            if (FxNodeCategory.SEMANTIC_CONTROL.equals(meta.category)) return cur;
            if (FxNodeCategory.SEMANTIC_PART.equals(meta.category)) {
                cur = parentOf(cur);
                continue;
            }
            cur = parentOf(cur);
        }
        return null;
    }

    /** Semantic parent for step: if target is part -> owning composite; else nearest semantic boundary above. */
    public static Object findSemanticParentBoundary(Object node) {
        if (node == null) return null;
        FxNodeCategory.NodeMeta partMeta = classify(node);
        if (FxNodeCategory.SEMANTIC_PART.equals(partMeta.category)) {
            Object owner = findOwningCompositeControl(node);
            if (owner != null) return owner;
            return findNearestSemanticControlAncestor(node);
        }
        return findNearestSemanticBoundaryAncestor(node);
    }

    /**
     * Fold structural nodes and lift to semantic target + semantic parent.
     * Returns (semanticTarget, semanticParent, foldedChain).
     * Uses default max ancestor hops (3).
     */
    public static FxNodeCategory.LiftResult foldAndLift(Object eventTargetNode) {
        return foldAndLift(eventTargetNode, 5);
    }

    /**
     * Fold structural nodes and lift to semantic target within maxAncestorHops.
     * Rule: 组合语义 (Table/Tree/List/TabPane 及其 part) vs 简单语义 (TextField, Button, CheckBox 等).
     * - <b>Composite-semantic part</b> (TableCell, TreeCell, ListCell, Tab): use as target immediately.
     * - <b>Simple-semantic control</b>: always look upward (within maxAncestorHops) for a composite-semantic
     *   part; if found, that part is the target (e.g. SearchAndUpdate); if not found, use the control
     *   itself as target (e.g. FillEdit, ClickButton).
     *
     * @param maxAncestorHops max parent steps from normalized node (default 5); 0 = use 5
     */
    public static FxNodeCategory.LiftResult foldAndLift(Object eventTargetNode, int maxAncestorHops) {
        int maxHops = maxAncestorHops <= 0 ? 5 : maxAncestorHops;
        Object n0 = normalizeToMeaningfulNode(eventTargetNode);
        List<Object> folded = new ArrayList<>();
        FxNodeCategory.NodeMeta targetMeta = null;
        FxNodeCategory.NodeMeta candidateSimpleSemantic = null;
        Object cur = n0;
        int hopCount = 0;

        while (cur != null && hopCount <= maxHops) {
            FxNodeCategory.NodeMeta meta = classify(cur);

            if (shouldFold(meta)) {
                folded.add(cur);
                cur = parentOf(cur);
                hopCount++;
                continue;
            }

            if (isCompositeSemanticPart(meta)) {
                targetMeta = meta;
                break;
            }

            if (isSimpleSemanticControl(meta)) {
                if (isTerminalSemanticControl(meta)) {
                    targetMeta = meta;
                    break;
                }
                if (candidateSimpleSemantic == null) candidateSimpleSemantic = meta;
                cur = parentOf(cur);
                hopCount++;
                continue;
            }

            if (FxNodeCategory.SEMANTIC_CONTROL.equals(meta.category) && FxNodeCategory.COMPOSITE_BOUNDARY.equals(meta.boundary)) {
                if (targetMeta == null) targetMeta = meta;
                cur = parentOf(cur);
                hopCount++;
                continue;
            }

            if (FxNodeCategory.DECORATION.equals(meta.category)) {
                if (isInteractable(cur)) {
                    if (candidateSimpleSemantic == null) {
                        candidateSimpleSemantic = new FxNodeCategory.NodeMeta(cur, FxNodeCategory.SEMANTIC_CONTROL, FxNodeCategory.ACTION_BOUNDARY, "DECORATION_AS_CONTROL");
                    }
                }
                cur = parentOf(cur);
                hopCount++;
                continue;
            }

            cur = parentOf(cur);
            hopCount++;
        }

        if (targetMeta == null && candidateSimpleSemantic != null) {
            targetMeta = candidateSimpleSemantic;
        }
        if (targetMeta == null) {
            return new FxNodeCategory.LiftResult(null, null, folded);
        }

        Object parent = findSemanticParentBoundary(targetMeta.node);
        return new FxNodeCategory.LiftResult(targetMeta.node, parent, folded);
    }
}
