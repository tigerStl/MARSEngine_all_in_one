package com.mars.javaengine.automation;

import java.awt.Component;
import java.util.ArrayList;
import java.util.IdentityHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

public class ControlRegistry {
    private final AtomicInteger sequence = new AtomicInteger(1);
    private final Map<Component, String> componentIds = new IdentityHashMap<>();
    private final Map<String, ControlHandle> handles = new ConcurrentHashMap<>();

    public synchronized ControlHandle registerComponent(Component component, String className, String name,
                                                        String label, String type) {
        String id = componentIds.get(component);
        if (id == null) {
            id = "obj-" + sequence.getAndIncrement();
            componentIds.put(component, id);
        }
        ControlHandle handle = new ControlHandle(id, ControlHandle.KIND_COMPONENT, component, className, name, label, type);
        handles.put(id, handle);
        return handle;
    }

    public synchronized ControlHandle registerSynthetic(String kind, Component owner, String className,
                                                        String name, String label, String type) {
        String id = "obj-" + sequence.getAndIncrement();
        ControlHandle handle = new ControlHandle(id, kind, owner, className, name, label, type);
        handles.put(id, handle);
        return handle;
    }

    public ControlHandle get(String id) {
        if (id == null) {
            return null;
        }
        return handles.get(id);
    }

    public List<ControlHandle> all() {
        return new ArrayList<>(handles.values());
    }

    public synchronized void retain(List<ControlHandle> current) {
        handles.clear();
        componentIds.clear();
        for (ControlHandle handle : current) {
            handles.put(handle.getId(), handle);
            if (handle.getComponent() != null && ControlHandle.KIND_COMPONENT.equals(handle.getKind())) {
                componentIds.put(handle.getComponent(), handle.getId());
            }
        }
    }
}
