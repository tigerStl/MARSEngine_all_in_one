package com.mars.javaui.unified;

import javax.crypto.Cipher;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.util.Arrays;

/**
 * AES-GCM crypto helper for marsJavaResource.bin.
 */
final class AgentResourceCrypto {
    private static final byte[] MAGIC = "MARSBIN1".getBytes(StandardCharsets.US_ASCII);
    private static final int GCM_TAG_BITS = 128;
    private static final int IV_LEN = 12;
    private static final String ALGO = "AES/GCM/NoPadding";
    private static final String KEY_SEED = "MARS::UnifiedAgent::ResourceKey::v1";

    private AgentResourceCrypto() { }

    static byte[] encrypt(byte[] plain) throws Exception {
        SecretKey key = deriveKey();
        byte[] iv = new byte[IV_LEN];
        new SecureRandom().nextBytes(iv);
        Cipher cipher = Cipher.getInstance(ALGO);
        cipher.init(Cipher.ENCRYPT_MODE, key, new GCMParameterSpec(GCM_TAG_BITS, iv));
        byte[] encrypted = cipher.doFinal(plain);
        ByteBuffer out = ByteBuffer.allocate(MAGIC.length + 1 + iv.length + 4 + encrypted.length);
        out.put(MAGIC);
        out.put((byte) iv.length);
        out.put(iv);
        out.putInt(encrypted.length);
        out.put(encrypted);
        return out.array();
    }

    static byte[] decrypt(byte[] payload) throws Exception {
        ByteBuffer in = ByteBuffer.wrap(payload);
        byte[] magic = new byte[MAGIC.length];
        in.get(magic);
        if (!Arrays.equals(magic, MAGIC)) {
            throw new IllegalArgumentException("Invalid marsJavaResource.bin magic header");
        }
        int ivLen = in.get() & 0xFF;
        if (ivLen <= 0 || ivLen > 32) {
            throw new IllegalArgumentException("Invalid IV length in marsJavaResource.bin");
        }
        byte[] iv = new byte[ivLen];
        in.get(iv);
        int dataLen = in.getInt();
        if (dataLen <= 0 || dataLen > in.remaining()) {
            throw new IllegalArgumentException("Invalid encrypted payload length");
        }
        byte[] encrypted = new byte[dataLen];
        in.get(encrypted);
        Cipher cipher = Cipher.getInstance(ALGO);
        cipher.init(Cipher.DECRYPT_MODE, deriveKey(), new GCMParameterSpec(GCM_TAG_BITS, iv));
        return cipher.doFinal(encrypted);
    }

    private static SecretKey deriveKey() throws Exception {
        MessageDigest sha256 = MessageDigest.getInstance("SHA-256");
        byte[] digest = sha256.digest(KEY_SEED.getBytes(StandardCharsets.UTF_8));
        return new SecretKeySpec(Arrays.copyOf(digest, 16), "AES");
    }
}
