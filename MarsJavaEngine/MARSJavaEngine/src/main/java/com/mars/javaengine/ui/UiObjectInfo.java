package com.mars.javaengine.ui;

public class UiObjectInfo {
    private String className;
    private String name;
    private String text;
    private String javaTypePath;
    private String javaNamePath;
    private int x;
    private int y;
    private int width;
    private int height;

    public UiObjectInfo() {
    }

    public UiObjectInfo(String className, String name, String text, String javaTypePath,
                        String javaNamePath, int x, int y, int width, int height) {
        this.className = className;
        this.name = name;
        this.text = text;
        this.javaTypePath = javaTypePath;
        this.javaNamePath = javaNamePath;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public UiObjectInfo(String className, String name, int x, int y, int width, int height) {
        this(className, name, null, null, null, x, y, width, height);
    }

    public String getClassName() {
        return className;
    }

    public String getName() {
        return name;
    }

    public String getText() {
        return text;
    }

    public String getJavaTypePath() {
        return javaTypePath;
    }

    public String getJavaNamePath() {
        return javaNamePath;
    }

    public int getX() {
        return x;
    }

    public int getY() {
        return y;
    }

    public int getWidth() {
        return width;
    }

    public int getHeight() {
        return height;
    }
}
