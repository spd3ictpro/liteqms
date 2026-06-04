package com.liteqms.tv.discovery

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress

data class ServerInfo(
    val name: String,
    val ip: String,
    val port: Int
)

class DiscoveryService {

    private companion object {
        const val DISCOVER_PORT = 56789
        const val TIMEOUT_MS = 3000
        val MAGIC_REQUEST = "LITEQMS_DISCOVER".toByteArray(Charsets.UTF_8)
    }

    suspend fun discover(): List<ServerInfo> = withContext(Dispatchers.IO) {
        val servers = mutableListOf<ServerInfo>()
        try {
            val socket = DatagramSocket().apply {
                broadcast = true
                soTimeout = TIMEOUT_MS
            }

            val packet = DatagramPacket(
                MAGIC_REQUEST,
                MAGIC_REQUEST.size,
                InetAddress.getByName("255.255.255.255"),
                DISCOVER_PORT
            )
            socket.send(packet)

            val buffer = ByteArray(1024)
            val receivePacket = DatagramPacket(buffer, buffer.size)

            while (true) {
                try {
                    socket.receive(receivePacket)
                    val json = String(receivePacket.data, 0, receivePacket.length, Charsets.UTF_8)
                    val obj = JSONObject(json)
                    servers.add(
                        ServerInfo(
                            name = obj.getString("ServerName"),
                            ip = obj.getString("IP"),
                            port = obj.getInt("Port")
                        )
                    )
                } catch (_: java.net.SocketTimeoutException) {
                    break
                }
            }

            socket.close()
        } catch (_: Exception) {
        }

        servers.distinctBy { "${it.ip}:${it.port}" }
    }
}
