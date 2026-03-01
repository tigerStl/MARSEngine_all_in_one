package com.mars.javaui.unified;

import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

/**
 * Build-time packer: encrypts core JAR into marsJavaResource.bin.
 */
public class PackAgentResource {
    public static void main(String[] args) throws Exception {
        if (args.length < 2) {
            throw new IllegalArgumentException("Usage: PackAgentResource <core-jar> <output-bin>");
        }
        Path in = Paths.get(args[0]);
        Path out = Paths.get(args[1]);
        byte[] jar = Files.readAllBytes(in);
        byte[] payload = AgentResourceCrypto.encrypt(jar);
        Files.write(out, payload);
        System.out.println("Packed encrypted payload: " + out + " (" + payload.length + " bytes)");
    }
}
