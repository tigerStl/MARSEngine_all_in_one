// ...existing code...
package com.mars.javaui.keyword;

import java.awt.Component;
import java.awt.Container;
import java.awt.Frame;
import java.awt.KeyboardFocusManager;
import java.awt.Window;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.logging.Logger;

import javax.swing.JComponent;
import javax.swing.SwingUtilities;

import com.mars.javaui.record.AgentLogger;

/**
 * Abstract base for all keyword-based steps. Standard TestStep: Keyword, top parent identifiers,
 * object identifiers, parameter, data. findObject is implemented here for replay.
 */
public abstract class MarsKeyword {

	private static final Logger LOG = Logger.getLogger(MarsKeyword.class.getName());

	public abstract String getKeyword();

	/** Build standard step: keyword, topParentIdentifier, objectIdentifier, parameter, data. */
	public Map<String, Object> buildScriptStep(Component comp, String parameter, String data, String assertValue) {
		return buildScriptStep(getKeyword(), comp, parameter, data, assertValue);
	}

	/** Static: build standard step given keyword name. */
	public static Map<String, Object> buildScriptStep(String keyword, Component comp, String parameter, String data, String assertValue) {
		Map<String, Object> step = new LinkedHashMap<>();
		step.put("keyword", keyword);
		if (parameter != null && !parameter.isEmpty()) step.put("parameter", parameter);
		if (data != null && !data.isEmpty()) step.put("data", data);
		if (assertValue != null && !assertValue.isEmpty()) step.put("assertValue", assertValue);
		step.put("parentIdentifier", buildParentIdentifier(comp));
		step.put("objectIdentifier", buildObjectIdentifier(comp));
		return step;
	}

	/** Top parent (window/dialog) identifier. */
	public static Map<String, Object> buildParentIdentifier(Component comp) {
		AgentLogger.begin(LOG, "buildParentIdentifier");
		try {
			if (comp == null) return new LinkedHashMap<>();
			Component root = resolveTopParent(comp);
			if (root != null) {
				AgentLogger.info(LOG, "resolveParent componentType=" + comp.getClass().getName()
						+ ", componentName=" + safe(getComponentJavaName(comp))
						+ " -> parentType=" + root.getClass().getName()
						+ ", parentName=" + safe(getComponentJavaName(root)));
			} else {
				AgentLogger.info(LOG, "resolveParent componentType=" + comp.getClass().getName()
						+ ", componentName=" + safe(getComponentJavaName(comp))
						+ " -> parentType=null,parentName=");
			}
			return buildObjectIdentifier(root);
		} finally {
			AgentLogger.end(LOG, "buildParentIdentifier");
		}
	}

	private static String safe(String s) {
		return s == null ? "" : s;
	}

	private static Component resolveTopParent(Component comp) {
		AgentLogger.begin(LOG, "resolveTopParent");
		try {
			if (comp == null) return null;

			// Prefer Dialog explicitly for modal scenarios.
			for (Component p = comp; p != null; p = p.getParent()) {
				if (p instanceof javax.swing.JFrame) {
					AgentLogger.info(LOG, "resolveTopParent branch=JFrame type=" + p.getClass().getName()
							+ ", text=" + safe(getComponentJavaName(p)));
					return p;
				}
				if (p instanceof javax.swing.JDialog) {
					AgentLogger.info(LOG, "resolveTopParent branch=JDialog type=" + p.getClass().getName()
							+ ", text=" + safe(getComponentJavaName(p)));
					return p;
				}
				if (p instanceof javax.swing.JWindow) {
					AgentLogger.info(LOG, "resolveTopParent branch=JWindow type=" + p.getClass().getName()
							+ ", text=" + safe(getComponentJavaName(p)));
					return p;
				}
				if (p instanceof java.awt.Frame) {
					AgentLogger.info(LOG, "resolveTopParent branch=Frame type=" + p.getClass().getName()
							+ ", text=" + safe(getComponentJavaName(p)));
					return p;
				}
				if (p instanceof java.awt.Dialog) {
					AgentLogger.info(LOG, "resolveTopParent branch=Dialog type=" + p.getClass().getName()
							+ ", text=" + safe(getComponentJavaName(p)));
					return p;
				}
				if (p instanceof Window) {
					AgentLogger.info(LOG, "resolveTopParent branch=WindowChain type=" + p.getClass().getName()
							+ ", text=" + safe(getComponentJavaName(p)));
					return p;
				}
			}

			// Fallback for cases where parent chain is unusual.
			Component windowAncestor = SwingUtilities.getWindowAncestor(comp);
			if (windowAncestor != null) {
				AgentLogger.info(LOG, "resolveTopParent branch=SwingWindowAncestor type=" + windowAncestor.getClass().getName()
						+ ", text=" + safe(getComponentJavaName(windowAncestor)));
				return windowAncestor;
			}

			// Detached component case (e.g. modal dialog button closes immediately on click).
			Window activeWindow = KeyboardFocusManager.getCurrentKeyboardFocusManager().getActiveWindow();
			if (activeWindow != null) {
				AgentLogger.info(LOG, "resolveTopParent branch=ActiveWindow type=" + activeWindow.getClass().getName()
						+ ", text=" + safe(getComponentJavaName(activeWindow)));
				return activeWindow;
			}
			Window focusedWindow = KeyboardFocusManager.getCurrentKeyboardFocusManager().getFocusedWindow();
			if (focusedWindow != null) {
				AgentLogger.info(LOG, "resolveTopParent branch=FocusedWindow type=" + focusedWindow.getClass().getName()
						+ ", text=" + safe(getComponentJavaName(focusedWindow)));
				return focusedWindow;
			}

			Component top = comp;
			while (top.getParent() != null) {
				top = top.getParent();
			}
			AgentLogger.info(LOG, "resolveTopParent top type=" + top.getClass().getName() + ", text=" + safe(getComponentJavaName(top)));
			return top;
		} finally {
			AgentLogger.end(LOG, "resolveTopParent");
		}
	}

	/** Object identifier: javaName, javaType, index, optional javaNamePath, javaTypePath, javaTitle for window. */
	public static Map<String, Object> buildObjectIdentifier(Component comp) {
		Map<String, Object> id = new LinkedHashMap<>();
		if (comp == null) {
			id.put("javaName", "");
			id.put("javaType", "");
			id.put("index", 0);
			return id;
		}
		String javaName = getComponentJavaName(comp);
		id.put("javaName", javaName != null ? javaName : "");
		id.put("javaType", comp.getClass().getName());
		id.put("index", getComponentIndex(comp));
		List<String> namePath = buildJavaNamePath(comp);
		if (!namePath.isEmpty()) id.put("javaNamePath", namePath);
		List<String> typePath = buildJavaTypePath(comp);
		if (!typePath.isEmpty()) id.put("javaTypePath", typePath);
		if (comp instanceof java.awt.Window) {
			String title = getWindowTitle(comp);
			if (title != null && !title.isEmpty()) id.put("javaTitle", title);
		}
		return id;
	}

	public static String getComponentJavaName(Component comp) {
		if (comp == null) return "";
		String name = comp.getName();
		if (name != null && !name.isEmpty()) return name;
		String windowTitle = getWindowTitle(comp);
		if (windowTitle != null && !windowTitle.isEmpty()) return windowTitle;
		try {
			if (comp instanceof JComponent && ((JComponent) comp).getAccessibleContext() != null) {
				String acc = ((JComponent) comp).getAccessibleContext().getAccessibleName();
				if (acc != null && !acc.isEmpty()) return acc;
			}
		} catch (Exception ignored) { }
		return "";
	}

	public static int getComponentIndex(Component comp) {
		if (comp == null) return 0;
		Container parent = comp.getParent();
		if (parent == null) return 0;
		List<Component> matches = new ArrayList<>();
		String javaName = getComponentJavaName(comp);
		String javaType = comp.getClass().getName();
		for (Component c : parent.getComponents()) {
			if (c == null) continue;
			if (!javaType.equals(c.getClass().getName())) continue;
			if (Objects.equals(javaName, getComponentJavaName(c))) matches.add(c);
		}
		int idx = matches.indexOf(comp);
		return idx >= 0 ? idx : 0;
	}

	public static List<String> buildJavaNamePath(Component comp) {
		List<String> list = new ArrayList<>();
		for (Component p = comp; p != null; p = p.getParent()) {
			String n = getComponentJavaName(p);
			if (n != null && !n.isEmpty()) list.add(0, n);
		}
		return list;
	}

	public static List<String> buildJavaTypePath(Component comp) {
		List<String> list = new ArrayList<>();
		for (Component p = comp; p != null; p = p.getParent()) {
			list.add(0, p.getClass().getName());
		}
		return list;
	}

	private static String getWindowTitle(Component c) {
		if (c instanceof java.awt.Frame) return ((Frame) c).getTitle();
		if (c instanceof java.awt.Dialog) return ((java.awt.Dialog) c).getTitle();
		return null;
	}

	/**
	 * Find component by top parent identifier and object identifier.
	 * First finds a window matching parentKey, then finds component matching objectKey under it.
	 */
	public static Component findObject(Map<String, Object> parentKey, Map<String, Object> objectKey) {
		if (objectKey == null || objectKey.isEmpty()) return null;
		Window[] windows = Window.getWindows();
		if (windows != null) {
			for (Window w : windows) {
				if (parentKey != null && !parentKey.isEmpty() && !componentMatchesKey(w, parentKey)) continue;
				Component c = findInContainer(w, objectKey);
				if (c != null) return c;
			}
		}
		Frame[] frames = Frame.getFrames();
		if (frames != null) {
			for (Frame f : frames) {
				if (f == null) continue;
				if (parentKey != null && !parentKey.isEmpty() && !componentMatchesKey(f, parentKey)) continue;
				Component c = findInContainer(f, objectKey);
				if (c != null) return c;
			}
		}
		return null;
	}

	private static Component findInContainer(Component c, Map<String, Object> objectKey) {
		if (c == null) return null;
		if (componentMatchesKey(c, objectKey)) return c;
		if (c instanceof Container) {
			for (Component child : ((Container) c).getComponents()) {
				Component found = findInContainer(child, objectKey);
				if (found != null) return found;
			}
		}
		return null;
	}

	private static boolean componentMatchesKey(Component c, Map<String, Object> key) {
		if (c == null || key == null) return false;
		Object type = key.get("javaType");
		if (type != null && !c.getClass().getName().equals(String.valueOf(type))) return false;
		Object name = key.get("javaName");
		if (name != null) {
			String n = getComponentJavaName(c);
			if (!Objects.equals(n, String.valueOf(name))) return false;
		}
		Object idx = key.get("index");
		if (idx != null) {
			int i = idx instanceof Number ? ((Number) idx).intValue() : Integer.parseInt(String.valueOf(idx));
			if (getComponentIndex(c) != i) return false;
		}
		return true;
	}
}
