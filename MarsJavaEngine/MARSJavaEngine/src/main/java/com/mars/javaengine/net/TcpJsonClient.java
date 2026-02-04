package com.mars.javaengine.net;

import java.io.OutputStreamWriter;
import java.io.PrintWriter;
import java.net.Socket;
import java.nio.charset.StandardCharsets;

public final class TcpJsonClient {
    private TcpJsonClient() {
    }

    public static void sendJson(String serverIp, int serverPort, String json) throws Exception {
        try (Socket socket = new Socket(serverIp, serverPort);
             PrintWriter writer = new PrintWriter(
                 new OutputStreamWriter(socket.getOutputStream(), StandardCharsets.UTF_8), true)) {
            writer.println(json);
        }
    }
}
