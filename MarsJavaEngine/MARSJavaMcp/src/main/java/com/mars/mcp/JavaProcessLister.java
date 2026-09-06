package com.mars.mcp;

import com.sun.tools.attach.VirtualMachine;
import com.sun.tools.attach.VirtualMachineDescriptor;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public final class JavaProcessLister {
    private JavaProcessLister() {
    }

    public static List<Map<String, Object>> list(String attachedPid) {
        List<Map<String, Object>> applications = new ArrayList<>();
        String self = String.valueOf(ProcessHandle.current().pid());
        for (VirtualMachineDescriptor descriptor : VirtualMachine.list()) {
            if (self.equals(descriptor.id())) {
                continue;
            }
            String display = descriptor.displayName() == null ? "" : descriptor.displayName();
            if (display.contains("MARSJavaMcp") || display.contains("MARSJavaEngineAgent")) {
                continue;
            }
            Map<String, Object> item = new LinkedHashMap<>();
            item.put("applicationId", "app-" + descriptor.id());
            try {
                item.put("processId", Integer.parseInt(descriptor.id()));
            } catch (NumberFormatException ex) {
                item.put("processId", descriptor.id());
            }
            item.put("name", inferName(display));
            item.put("displayName", display);
            item.put("technology", inferTechnology(display));
            item.put("status", descriptor.id().equals(attachedPid) ? "CONNECTED" : "AVAILABLE");
            applications.add(item);
        }
        return applications;
    }

    public static String inferName(String displayName) {
        if (displayName == null || displayName.isBlank()) {
            return "Java Application";
        }
        if (displayName.contains("northstar") || displayName.contains("com.northstar.capital.Main")) {
            return "Northstar Capital Markets Workstation";
        }
        String[] parts = displayName.split("\\s+");
        return parts[0];
    }

    public static String inferTechnology(String displayName) {
        if (displayName != null && displayName.toLowerCase().contains("javafx")) {
            return "JavaFX";
        }
        return "Swing";
    }
}
