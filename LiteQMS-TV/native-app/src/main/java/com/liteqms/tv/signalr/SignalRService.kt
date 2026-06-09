package com.liteqms.tv.signalr

import android.util.Log
import com.microsoft.signalr.Action
import com.microsoft.signalr.Action1
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow

enum class ConnectionState {
    DISCONNECTED,
    RECONNECTING,
    CONNECTED
}

class SignalRService(
    private val serverUrl: String,
    private val onNewCall: (CallState) -> Unit,
    private val onStateSync: (CallState) -> Unit,
    private val onQueueReset: () -> Unit,
    private val onCnaUpdated: (Int, Boolean) -> Unit
) {
    private var connection: HubConnection? = null
    private var job: Job? = null
    private val scope = CoroutineScope(Dispatchers.Default + SupervisorJob())

    private val _connectionState = MutableStateFlow(ConnectionState.DISCONNECTED)
    val connectionState: StateFlow<ConnectionState> = _connectionState

    fun start() {
        if (connection != null) return

        connection = HubConnectionBuilder.create("$serverUrl/queueHub").build()

        connection!!.on("NewCall", Action1<Any> { raw ->
            @Suppress("UNCHECKED_CAST")
            val map = raw as? Map<*, *>
            map?.let { parseCallState(it) }?.let { onNewCall(it) }
        }, Any::class.java)

        connection!!.on("QueueReset", Action { onQueueReset() })

        connection!!.on("ReceiveCurrentState", Action1<Any> { raw ->
            @Suppress("UNCHECKED_CAST")
            val map = raw as? Map<*, *>
            map?.let { parseCallState(it) }?.let { onStateSync(it) }
        }, Any::class.java)

        connection!!.on("CNAUpdated", Action1<Any> { raw ->
            if (raw is List<*>) {
                val id = (raw.getOrNull(0) as? Number)?.toInt() ?: return@Action1
                val isCNA = raw.getOrNull(1) as? Boolean ?: return@Action1
                onCnaUpdated(id, isCNA)
            }
        }, Any::class.java)

        connection!!.onClosed {
            Log.w(TAG, "Connection closed")
            _connectionState.value = ConnectionState.DISCONNECTED
            reconnect()
        }

        connect()
    }

    private fun connect() {
        job = scope.launch {
            while (isActive) {
                _connectionState.value = ConnectionState.RECONNECTING
                try {
                    connection?.start()?.blockingAwait()
                    _connectionState.value = ConnectionState.CONNECTED
                    connection?.send("RequestCurrentState")
                    Log.i(TAG, "SignalR connected and state requested")
                    return@launch
                } catch (e: Exception) {
                    Log.e(TAG, "SignalR connect failed", e)
                    _connectionState.value = ConnectionState.DISCONNECTED
                    delay(5000)
                }
            }
        }
    }

    private fun reconnect() {
        job = scope.launch {
            var attempt = 0
            val delays = listOf(1000L, 5000L, 30000L)
            while (isActive) {
                _connectionState.value = ConnectionState.RECONNECTING
                delay(delays.getOrElse(attempt) { 30000L })
                try {
                    connection?.start()?.blockingAwait()
                    _connectionState.value = ConnectionState.CONNECTED
                    connection?.send("RequestCurrentState")
                    Log.i(TAG, "SignalR reconnected")
                    return@launch
                } catch (e: Exception) {
                    Log.w(TAG, "SignalR reconnect attempt ${attempt + 1} failed", e)
                    attempt++
                    _connectionState.value = ConnectionState.DISCONNECTED
                }
            }
        }
    }

    fun stop() {
        job?.cancel()
        try {
            connection?.stop()
        } catch (_: Exception) {
        }
        connection = null
        _connectionState.value = ConnectionState.DISCONNECTED
    }

    @Suppress("UNCHECKED_CAST")
    private fun parseCallState(map: Map<*, *>): CallState {
        val recentRaw = map["recentCalls"] as? List<Map<String, Any>> ?: emptyList()
        return CallState(
            roomNumber = map["roomNumber"] as? String ?: "",
            arrowDirection = map["arrowDirection"] as? String ?: "",
            patientNumber = map["patientNumber"] as? String ?: "",
            timestamp = map["timestamp"] as? String ?: "",
            recentCalls = recentRaw.map { parseRecentCall(it) },
            callCount = (map["callCount"] as? Number)?.toInt() ?: 0,
            isRecall = map["isRecall"] as? Boolean ?: false
        )
    }

    private fun parseRecentCall(map: Map<String, Any>): RecentCall {
        return RecentCall(
            id = (map["id"] as? Number)?.toInt() ?: 0,
            roomNumber = map["roomNumber"] as? String ?: "",
            patientNumber = map["patientNumber"] as? String ?: "",
            timestamp = map["timestamp"] as? String ?: "",
            isCNA = map["isCNA"] as? Boolean ?: false
        )
    }

    fun onDestroy() {
        stop()
        scope.cancel()
    }

    companion object {
        private const val TAG = "SignalRService"
    }
}
