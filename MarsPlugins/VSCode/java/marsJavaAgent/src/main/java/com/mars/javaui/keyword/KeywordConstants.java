package com.mars.javaui.keyword;

/**
 * Single source of truth for all recording/replay step keywords.
 * Keep in sync with frontend ScriptKeyword (types.ts SCRIPT_KEYWORDS).
 */
public final class KeywordConstants {

    private KeywordConstants() {}

    public static final String CLICK = "Click";
    public static final String CLICK_BUTTON = "ClickButton";
    public static final String DOUBLE_CLICK_BUTTON = "DoubleClickButton";
    public static final String DOUBLE_CLICK = "DoubleClick";
    public static final String CLICK_MENU_ICON = "ClickMenuIcon";
    public static final String FILL_EDIT = "FillEdit";
    public static final String SELECT_DROP_DOWN = "SelectDropDown";
    public static final String SELECT_DROP_LIST = "SelectDropList";
    public static final String SELECT_LIST_ITEM = "SelectListItem";
    public static final String SELECT_MENU_ITEM = "SelectMenuItem";
    public static final String SELECT_TREE_LIST = "SelectTreeList";
    public static final String SELECT_TAB = "SelectTab";
    public static final String SELECT_MENU_ICON = "SelectMenuIcon";
    public static final String SELECT_POPUP_MENU = "SelectPopupMenu";
    public static final String CLICK_AT = "ClickAT";
    public static final String SEARCH_AND_CLICK = "SearchAndClick";
    public static final String SEARCH_AND_UPDATE = "SearchAndUpdate";
    public static final String VERIFY_OBJECT_VALUE = "VerifyObjectValue";
    public static final String SET_RADIO_BOX = "SetRadioBox";
    public static final String SET_CHECK_BOX = "SetCheckBox";
    public static final String CHECK = "Check";
    public static final String UNCHECK = "Uncheck";
    public static final String EXPAND_TREE_NODE = "ExpandTreeNode";
    public static final String COLLAPSE_TREE_NODE = "CollapseTreeNode";
}
