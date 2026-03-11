package com.mars.javaui.fx;

import java.lang.reflect.Method;

/**
 * Shared reflection helpers for JavaFX support classes.
 * Provides a generic invokeNoArg that can call public or declared
 * no-arg methods up the class hierarchy, and common value helpers.
 */
abstract class FxReflectionSupport {

    /**
     * Invoke a no-arg method by name on the given target.
     * <p>
     * Resolution rules:
     * <ol>
     *   <li>Try public method (including superclasses/interfaces) via getMethod.</li>
     *   <li>If not found, walk the class hierarchy and try getDeclaredMethod
     *       on each class; setAccessible(true) so protected/package/private
     *       methods can be called (subject to module access rules).</li>
     * </ol>
     */
    protected static Object invokeNoArg(Object target, String methodName) {
        if (target == null || methodName == null) return null;
        Class<?> c = target.getClass();
        try {
            // 1) Try public method (including superclasses/interfaces)
            Method m = c.getMethod(methodName);
            return m.invoke(target);
        } catch (NoSuchMethodException e) {
            // 2) Walk class hierarchy and try declared methods of any visibility
            while (c != null) {
                try {
                    Method dm = c.getDeclaredMethod(methodName);
                    dm.setAccessible(true);
                    return dm.invoke(target);
                } catch (NoSuchMethodException ignored) {
                    c = c.getSuperclass();
                } catch (Exception ex) {
                    return null;
                }
            }
            return null;
        } catch (Exception e) {
            return null;
        }
    }

    /** Trimmed string value: null if v is null or only whitespace. */
    protected static String asString(Object v) {
        if (v == null) return null;
        String s = String.valueOf(v).trim();
        return s.isEmpty() ? null : s;
    }

    /** Integer value: supports Number or parsable string; null on failure. */
    protected static Integer asInt(Object v) {
        if (v instanceof Number) return ((Number) v).intValue();
        if (v == null) return null;
        try {
            return (int) Math.round(Double.parseDouble(String.valueOf(v)));
        } catch (NumberFormatException e) {
            return null;
        }
    }
}

