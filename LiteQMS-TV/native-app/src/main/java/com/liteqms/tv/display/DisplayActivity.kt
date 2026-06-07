package com.liteqms.tv.display

import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.graphics.Typeface
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.PowerManager
import android.view.Gravity
import android.view.KeyEvent
import android.view.View
import android.view.WindowManager
import android.view.animation.AlphaAnimation
import android.view.animation.Animation
import android.view.animation.ScaleAnimation
import android.graphics.drawable.GradientDrawable
import android.widget.*
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import androidx.lifecycle.lifecycleScope
import com.liteqms.tv.R
import com.liteqms.tv.audio.AudioPlayer
import com.liteqms.tv.signalr.ConnectionState
import com.liteqms.tv.signalr.SignalRService
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import androidx.core.widget.TextViewCompat
import android.util.TypedValue
import java.text.SimpleDateFormat
import java.util.*

class DisplayActivity : AppCompatActivity() {

    companion object {
        private const val PREFS_NAME = "liteqms_native"
        private const val KEY_SERVER_URL = "server_url"
    }

    private lateinit var viewModel: DisplayViewModel
    private var signalR: SignalRService? = null
    private lateinit var audioPlayer: AudioPlayer

    private val settingsLauncher = registerForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { result ->
        if (result.resultCode == RESULT_OK) {
            val url = getSavedUrl()
            if (url != null) connectSignalR(url)
        }
    }

    private lateinit var patientNumberText: TextView
    private lateinit var roomLabelText: TextView
    private lateinit var clockText: TextView
    private lateinit var dateText: TextView
    private lateinit var justCalledBadge: TextView
    private lateinit var recentContainer: LinearLayout
    private lateinit var syncDot: View
    private lateinit var syncLabel: TextView
    private lateinit var reconnectBanner: LinearLayout
    private lateinit var reconnectBannerText: TextView
    private lateinit var reconnectSettingsBtn: Button
    private lateinit var rootLayout: FrameLayout

    private lateinit var settingsGear: Button

    private var clockJob: Job? = null
    private var badgeJob: Job? = null
    private var animationJob: Job? = null
    private var gearHideJob: Job? = null

    private val mainHandler = Handler(Looper.getMainLooper())

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        window.addFlags(
            WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON or
            WindowManager.LayoutParams.FLAG_FULLSCREEN
        )
        hideSystemUi()

        viewModel = DisplayViewModel()

        rootLayout = FrameLayout(this).apply {
            setBackgroundColor(Color.parseColor("#ccfbf1"))
        }

        buildUi()
        setContentView(rootLayout)

        audioPlayer = AudioPlayer(this)

        val url = getSavedUrl()
        if (url == null) {
            settingsLauncher.launch(Intent(this, com.liteqms.tv.settings.SettingsActivity::class.java))
        } else {
            connectSignalR(url)
        }

        observeViewModel()
        startClock()
    }

    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)
        if (hasFocus) hideSystemUi()
    }

    override fun onDestroy() {
        super.onDestroy()
        signalR?.onDestroy()
        audioPlayer.release()
        clockJob?.cancel()
        badgeJob?.cancel()
        animationJob?.cancel()
        gearHideJob?.cancel()
    }

    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        if (::settingsGear.isInitialized && settingsGear.visibility != View.VISIBLE) {
            settingsGear.visibility = View.VISIBLE
            settingsGear.requestFocus()
        }
        gearHideJob?.cancel()
        gearHideJob = lifecycleScope.launch {
            delay(3000)
            if (::settingsGear.isInitialized) settingsGear.visibility = View.GONE
        }
        return super.onKeyDown(keyCode, event)
    }

    private fun connectSignalR(url: String) {
        signalR?.onDestroy()
        signalR = SignalRService(
            serverUrl = url.trimEnd('/'),
            onNewCall = { state ->
                mainHandler.post {
                    viewModel.onNewCall(state)
                    audioPlayer.play()
                }
            },
            onStateSync = { state ->
                mainHandler.post {
                    viewModel.onStateSync(state)
                }
            },
            onQueueReset = { mainHandler.post { viewModel.onQueueReset() } },
            onCnaUpdated = { _, _ -> }
        )

        lifecycleScope.launch {
            signalR?.connectionState?.collectLatest { state ->
                viewModel.onConnectionStateChanged(state)
            }
        }

        signalR?.start()
    }

    private fun buildUi() {
        val displayContainer = FrameLayout(this).apply {
            layoutParams = FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
            )
            setPadding(48, 48, 48, 48)
        }

        val splitLayout = createSplitLayout()
        displayContainer.addView(splitLayout)

        reconnectBanner = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER
            setPadding(24, 12, 24, 12)
            visibility = View.GONE
        }

        reconnectBannerText = TextView(this).apply {
            textSize = 18f
            gravity = Gravity.CENTER
            layoutParams = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WRAP_CONTENT, 1f)
            reconnectBanner.addView(this)
        }

        reconnectSettingsBtn = Button(this).apply {
            text = "Settings"
            textSize = 16f
            setPadding(24, 8, 24, 8)
            isFocusable = true
            visibility = View.GONE
            setOnClickListener {
                signalR?.onDestroy()
                signalR = null
                settingsLauncher.launch(Intent(this@DisplayActivity, com.liteqms.tv.settings.SettingsActivity::class.java))
            }
            setOnFocusChangeListener { _, hasFocus ->
                background = if (hasFocus) {
                    GradientDrawable().apply {
                        setColor(Color.parseColor("#0d9488"))
                        cornerRadius = 8f
                    }
                } else null
            }
            reconnectBanner.addView(this)
        }

        displayContainer.addView(reconnectBanner)

        rootLayout.addView(displayContainer)

        justCalledBadge = TextView(this).apply {
            text = "Just Called"
            setBackgroundColor(Color.parseColor("#0d9488"))
            setTextColor(Color.WHITE)
            textSize = 20f
            setPadding(40, 12, 40, 12)
            visibility = View.GONE
        }
        rootLayout.addView(justCalledBadge, FrameLayout.LayoutParams(
            FrameLayout.LayoutParams.WRAP_CONTENT,
            FrameLayout.LayoutParams.WRAP_CONTENT
        ).apply {
            gravity = Gravity.BOTTOM or Gravity.CENTER_HORIZONTAL
            bottomMargin = 80
        })

        settingsGear = Button(this).apply {
            text = "⚙"
            textSize = 28f
            setPadding(16, 16, 16, 16)
            visibility = View.GONE
            setOnClickListener {
                signalR?.onDestroy()
                signalR = null
                settingsLauncher.launch(Intent(this@DisplayActivity, com.liteqms.tv.settings.SettingsActivity::class.java))
            }
            setOnFocusChangeListener { _, hasFocus ->
                background = if (hasFocus) {
                    GradientDrawable().apply {
                        setColor(Color.parseColor("#0d9488"))
                        cornerRadius = 8f
                    }
                } else null
            }
        }
        rootLayout.addView(settingsGear, FrameLayout.LayoutParams(
            FrameLayout.LayoutParams.WRAP_CONTENT,
            FrameLayout.LayoutParams.WRAP_CONTENT
        ).apply {
            gravity = Gravity.TOP or Gravity.END
            topMargin = 16
            rightMargin = 16
        })
    }

    private fun createSplitLayout(): LinearLayout {
        val split = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            layoutParams = FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
            )
        }

        val leftPanel = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutParams = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.MATCH_PARENT, 0.7f)
        }

        val headerBar = createHeaderBar()
        leftPanel.addView(headerBar)

        val mainDisplay = createMainDisplay()
        leftPanel.addView(mainDisplay, LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT, 0, 1f
        ).apply { setMargins(0, 24, 0, 0) })

        split.addView(leftPanel)

        val rightPanel = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutParams = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.MATCH_PARENT, 0.3f)
            setPadding(24, 0, 0, 0)
        }

        recentContainer = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.MATCH_PARENT
            )
        }
        rightPanel.addView(recentContainer)
        split.addView(rightPanel)

        return split
    }

    private fun createHeaderBar(): LinearLayout {
        val bar = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            setPadding(24, 16, 24, 16)
            setBackgroundColor(Color.WHITE)
        }

        val brand = TextView(this).apply {
            text = "LiteQMS"
            textSize = 28f
            setTypeface(null, Typeface.BOLD)
            setTextColor(Color.parseColor("#0d9488"))
        }
        bar.addView(brand)

        val syncSection = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER_VERTICAL
            setPadding(24, 0, 0, 0)
        }

        syncDot = View(this).apply {
            layoutParams = LinearLayout.LayoutParams(16, 16).apply { rightMargin = 8 }
            setBackgroundResource(android.R.drawable.presence_offline)
        }
        syncSection.addView(syncDot)

        syncLabel = TextView(this).apply {
            text = "Disconnected"
            textSize = 14f
            setTextColor(Color.parseColor("#94a3b8"))
        }
        syncSection.addView(syncLabel)
        bar.addView(syncSection)

        val spacer = View(this)
        bar.addView(spacer, LinearLayout.LayoutParams(0, 0, 1f))

        val clockSection = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            gravity = Gravity.RIGHT
        }

        clockText = TextView(this).apply {
            textSize = 40f
            setTextColor(Color.parseColor("#0f172a"))
            typeface = Typeface.DEFAULT
            gravity = Gravity.RIGHT
        }
        clockSection.addView(clockText)

        dateText = TextView(this).apply {
            textSize = 16f
            setTextColor(Color.parseColor("#94a3b8"))
            gravity = Gravity.RIGHT
        }
        clockSection.addView(dateText)
        bar.addView(clockSection)

        return bar
    }

    private fun createMainDisplay(): FrameLayout {
        val display = FrameLayout(this).apply {
            setBackgroundColor(Color.WHITE)
        }

        val content = FrameLayout(this)
        val params = FrameLayout.LayoutParams(
            FrameLayout.LayoutParams.WRAP_CONTENT,
            FrameLayout.LayoutParams.WRAP_CONTENT
        ).apply { gravity = Gravity.CENTER }
        content.layoutParams = params

        val inner = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            gravity = Gravity.CENTER
        }

        patientNumberText = TextView(this).apply {
            textSize = 180f
            setTypeface(null, Typeface.BOLD)
            setTextColor(Color.parseColor("#0f172a"))
            gravity = Gravity.CENTER
            inner.addView(this)
        }

        roomLabelText = TextView(this).apply {
            textSize = 115f
            setTypeface(null, Typeface.BOLD)
            setTextColor(Color.parseColor("#0d9488"))
            gravity = Gravity.CENTER
            TextViewCompat.setAutoSizeTextTypeUniformWithConfiguration(this, 24, 115, 2, TypedValue.COMPLEX_UNIT_SP)
            inner.addView(this)
        }

        content.addView(inner)
        display.addView(content)
        return display
    }

    private fun observeViewModel() {
        lifecycleScope.launch {
            viewModel.uiState.collectLatest { state ->
                patientNumberText.text = state.patientNumber
                roomLabelText.text = state.roomLabel

                when (state.connectionState) {
                    ConnectionState.CONNECTED -> {
                        syncDot.setBackgroundResource(android.R.drawable.presence_online)
                        syncLabel.text = "Connected"
                        reconnectBanner.visibility = View.GONE
                    }
                    ConnectionState.RECONNECTING -> {
                        syncDot.setBackgroundResource(android.R.drawable.presence_away)
                        syncLabel.text = "Reconnecting..."
                        reconnectBanner.setBackgroundColor(Color.parseColor("#fef9c3"))
                        reconnectBannerText.apply {
                            text = "Reconnecting..."
                            setTextColor(Color.parseColor("#854d0e"))
                        }
                        reconnectSettingsBtn.visibility = View.GONE
                        reconnectBanner.visibility = View.VISIBLE
                    }
                    ConnectionState.DISCONNECTED -> {
                        syncDot.setBackgroundResource(android.R.drawable.presence_offline)
                        syncLabel.text = "Disconnected"
                        reconnectBanner.setBackgroundColor(Color.parseColor("#fee2e2"))
                        reconnectBannerText.apply {
                            text = "Offline — Check your connection"
                            setTextColor(Color.parseColor("#991b1b"))
                        }
                        reconnectSettingsBtn.visibility = View.VISIBLE
                        reconnectBanner.visibility = View.VISIBLE
                    }
                }

                if (state.showJustCalledBadge) {
                    justCalledBadge.visibility = View.VISIBLE
                    badgeJob?.cancel()
                    badgeJob = lifecycleScope.launch {
                        delay(5000)
                        justCalledBadge.visibility = View.GONE
                        viewModel.clearJustCalledBadge()
                    }
                }

                if (state.animateNewCall && state.patientNumber != "—") {
                    animatePatientNumber()
                    viewModel.clearAnimation()
                }

                renderRecentCalls(state.recentCalls, state.hasRecentCalls)
            }
        }
    }

    private fun animatePatientNumber() {
        animationJob?.cancel()
        val scale = ScaleAnimation(
            0.85f, 1f, 0.85f, 1f,
            Animation.RELATIVE_TO_SELF, 0.5f,
            Animation.RELATIVE_TO_SELF, 0.5f
        ).apply {
            duration = 400
            fillAfter = true
        }
        val alpha = AlphaAnimation(0.5f, 1f).apply {
            duration = 400
            fillAfter = true
        }
        patientNumberText.startAnimation(scale)
        patientNumberText.startAnimation(alpha)
    }

    private fun renderRecentCalls(calls: List<RecentCallUi>, hasData: Boolean) {
        recentContainer.removeAllViews()

        if (hasData) {
            calls.forEachIndexed { index, call ->
                val item = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    gravity = Gravity.CENTER
                    setPadding(24, 16, 24, 16)
                    setBackgroundColor(Color.parseColor("#f0fdfa"))
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT, 0, 1f
                    ).apply { setMargins(0, 0, 0, 8) }
                }

                val number = TextView(this).apply {
                    textSize = 60f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(Color.parseColor("#0f172a"))
                    gravity = Gravity.CENTER
                    TextViewCompat.setAutoSizeTextTypeUniformWithConfiguration(this, 24, 60, 2, TypedValue.COMPLEX_UNIT_SP)
                }
                item.addView(number)

                val room = TextView(this).apply {
                    textSize = 32f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(Color.parseColor("#0d9488"))
                    gravity = Gravity.CENTER
                    TextViewCompat.setAutoSizeTextTypeUniformWithConfiguration(this, 16, 32, 2, TypedValue.COMPLEX_UNIT_SP)
                }
                item.addView(number)

                val room = TextView(this).apply {
                    text = call.roomNumber
                    textSize = 24f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(Color.parseColor("#0d9488"))
                    gravity = Gravity.CENTER
                }
                item.addView(room)

                recentContainer.addView(item)
            }
        } else {
            for (i in 0 until 4) {
                val empty = TextView(this).apply {
                    text = "—"
                    textSize = 32f
                    gravity = Gravity.CENTER
                    setTextColor(Color.parseColor("#94a3b8"))
                    setBackgroundColor(Color.parseColor("#80ffffff"))
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT, 0, 1f
                    ).apply { setMargins(0, 0, 0, 8) }
                }
                recentContainer.addView(empty)
            }
        }
    }

    private fun startClock() {
        clockJob = lifecycleScope.launch {
            val sdf = SimpleDateFormat("HH:mm:ss", Locale.getDefault())
            val dateSdf = SimpleDateFormat("EEEE, dd MMMM yyyy", Locale.ENGLISH)
            while (true) {
                val now = Date()
                clockText.text = sdf.format(now)
                dateText.text = dateSdf.format(now)
                delay(1000)
            }
        }
    }

    private fun getSavedUrl(): String? {
        return getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .getString(KEY_SERVER_URL, null)
    }

    private fun hideSystemUi() {
        WindowCompat.setDecorFitsSystemWindows(window, false)
        val controller = WindowInsetsControllerCompat(window, window.decorView)
        controller.hide(WindowInsetsCompat.Type.systemBars())
        controller.systemBarsBehavior =
            WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
            window.decorView.systemUiVisibility = (
                View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
                    or View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                    or View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                    or View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                    or View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                    or View.SYSTEM_UI_FLAG_FULLSCREEN
                )
        }

        supportActionBar?.hide()
    }
}
