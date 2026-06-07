package com.liteqms.tv.display

import com.liteqms.tv.signalr.CallState
import com.liteqms.tv.signalr.ConnectionState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

data class DisplayUiState(
    val patientNumber: String = "—",
    val roomLabel: String = "—",
    val recentCalls: List<RecentCallUi> = emptyList(),
    val hasRecentCalls: Boolean = false,
    val connectionState: ConnectionState = ConnectionState.DISCONNECTED,
    val isRecall: Boolean = false,
    val showJustCalledBadge: Boolean = false,
    val animateNewCall: Boolean = false
)

data class RecentCallUi(
    val patientNumber: String,
    val roomNumber: String
)

class DisplayViewModel {

    private val _uiState = MutableStateFlow(DisplayUiState())
    val uiState: StateFlow<DisplayUiState> = _uiState.asStateFlow()

    fun onNewCall(state: CallState) {
        _uiState.value = _uiState.value.copy(
            patientNumber = state.patientNumber,
            roomLabel = state.roomNumber,
            recentCalls = state.recentCalls
                .take(4)
                .map { RecentCallUi(it.patientNumber, it.roomNumber) },
            hasRecentCalls = state.recentCalls.isNotEmpty(),
            isRecall = state.isRecall,
            showJustCalledBadge = true,
            animateNewCall = true
        )
    }

    fun onStateSync(state: CallState) {
        _uiState.value = _uiState.value.copy(
            patientNumber = state.patientNumber,
            roomLabel = state.roomNumber,
            recentCalls = state.recentCalls
                .take(4)
                .map { RecentCallUi(it.patientNumber, it.roomNumber) },
            hasRecentCalls = state.recentCalls.isNotEmpty(),
            isRecall = state.isRecall,
            showJustCalledBadge = false,
            animateNewCall = false
        )
    }

    fun onQueueReset() {
        _uiState.value = DisplayUiState(
            connectionState = _uiState.value.connectionState
        )
    }

    fun onConnectionStateChanged(state: ConnectionState) {
        _uiState.value = _uiState.value.copy(connectionState = state)
    }

    fun clearJustCalledBadge() {
        _uiState.value = _uiState.value.copy(showJustCalledBadge = false)
    }

    fun clearAnimation() {
        _uiState.value = _uiState.value.copy(animateNewCall = false)
    }
}
