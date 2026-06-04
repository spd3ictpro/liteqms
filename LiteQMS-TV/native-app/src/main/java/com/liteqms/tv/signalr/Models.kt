package com.liteqms.tv.signalr

data class CallState(
    val roomNumber: String,
    val patientNumber: String,
    val timestamp: String,
    val recentCalls: List<RecentCall>,
    val callCount: Int,
    val isRecall: Boolean
)

data class RecentCall(
    val id: Int,
    val roomNumber: String,
    val patientNumber: String,
    val timestamp: String,
    val isCNA: Boolean
)
