package com.mars.javaui.unified;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.net.URL;
import java.net.URLConnection;
import java.net.URLStreamHandler;
import java.security.CodeSource;
import java.security.ProtectionDomain;
import java.security.cert.Certificate;
import java.util.HashMap;
import java.util.Map;
import java.util.jar.JarEntry;
import java.util.jar.JarInputStream;

/**
 * Loads classes/resources from an in-memory JAR blob.
 */
final class InMemoryJarClassLoader extends ClassLoader {
    private final Map<String, byte[]> classBytes = new HashMap<>();
    private final Map<String, byte[]> resourceBytes = new HashMap<>();
    private final ProtectionDomain protectionDomain;

    InMemoryJarClassLoader(byte[] jarData, ClassLoader parent) throws IOException {
        super(parent);
        this.protectionDomain = new ProtectionDomain(
                new CodeSource(null, (Certificate[]) null),
                null,
                this,
                null
        );
        parseJar(jarData);
    }

    @Override
    protected Class<?> findClass(String name) throws ClassNotFoundException {
        String path = name.replace('.', '/') + ".class";
        byte[] bytes = classBytes.get(path);
        if (bytes == null) throw new ClassNotFoundException(name);
        return defineClass(name, bytes, 0, bytes.length, protectionDomain);
    }

    @Override
    public URL getResource(String name) {
        if (!resourceBytes.containsKey(name)) {
            return super.getResource(name);
        }
        try {
            return new URL(null, "memory:/" + name, new URLStreamHandler() {
                @Override
                protected URLConnection openConnection(URL u) {
                    return new URLConnection(u) {
                        @Override
                        public void connect() { }

                        @Override
                        public InputStream getInputStream() {
                            byte[] data = resourceBytes.get(name);
                            return new ByteArrayInputStream(data != null ? data : new byte[0]);
                        }
                    };
                }
            });
        } catch (Exception e) {
            return null;
        }
    }

    @Override
    public InputStream getResourceAsStream(String name) {
        byte[] data = resourceBytes.get(name);
        if (data != null) return new ByteArrayInputStream(data);
        return super.getResourceAsStream(name);
    }

    private void parseJar(byte[] jarData) throws IOException {
        try (JarInputStream jis = new JarInputStream(new ByteArrayInputStream(jarData))) {
            JarEntry entry;
            while ((entry = jis.getNextJarEntry()) != null) {
                if (entry.isDirectory()) continue;
                byte[] bytes = jis.readAllBytes();
                String name = entry.getName();
                resourceBytes.put(name, bytes);
                if (name.endsWith(".class")) {
                    classBytes.put(name, bytes);
                }
            }
        }
    }
}
