/*
 * Decompiled with CFR 0.152.
 */
package com.demo.loaniq.util;

import java.awt.Component;
import java.awt.Container;
import java.awt.Rectangle;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.OpenOption;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.attribute.FileAttribute;
import java.util.ArrayList;
import java.util.List;

public final class UiTreeDumper {
    private UiTreeDumper() {
    }

    public static String dump(Component root) {
        StringBuilder sb = new StringBuilder();
        UiTreeDumper.dump(sb, root, 0, new ArrayList<String>());
        return sb.toString();
    }

    private static void dump(StringBuilder sb, Component c, int depth, List<String> parentPath) {
        if (c == null) {
            return;
        }
        String name = c.getName() != null && !c.getName().isEmpty() ? c.getName() : "(no name)";
        String cls = c.getClass().getSimpleName();
        ArrayList<String> path = new ArrayList<String>(parentPath);
        path.add(name);
        String pathStr = String.join((CharSequence)" / ", path);
        Rectangle b = c.getBounds();
        String line = String.format("%s%s name=%s class=%s bounds=[x=%d,y=%d,w=%d,h=%d] path=%s", "  ".repeat(depth), cls, name, cls, b.x, b.y, b.width, b.height, pathStr);
        sb.append(line).append("\n");
        if (c instanceof Container) {
            for (Component child : ((Container)c).getComponents()) {
                UiTreeDumper.dump(sb, child, depth + 1, path);
            }
        }
    }

    public static void dumpToConsoleAndFile(Component root) {
        String out = UiTreeDumper.dump(root);
        System.out.println(out);
        try {
            Path dir = Paths.get("build", "tmp");
            Files.createDirectories(dir, new FileAttribute[0]);
            Path file = dir.resolve("ui-tree.txt");
            Files.writeString(file, (CharSequence)out, new OpenOption[0]);
            System.out.println("Written to " + file.toAbsolutePath());
        }
        catch (IOException e) {
            System.err.println("Write ui-tree.txt failed: " + e.getMessage());
        }
    }
}

