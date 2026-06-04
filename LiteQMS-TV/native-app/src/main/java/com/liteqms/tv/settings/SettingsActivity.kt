package com.liteqms.tv.settings

import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.os.Bundle
import android.view.inputmethod.EditorInfo
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.liteqms.tv.discovery.DiscoveryActivity
import kotlinx.coroutines.launch
import java.net.HttpURLConnection
import java.net.URL

class SettingsActivity : AppCompatActivity() {

    companion object {
        private const val PREFS_NAME = "liteqms_native"
        private const val KEY_SERVER_URL = "server_url"
        private const val REQUEST_DISCOVERY = 200
    }

    private lateinit var urlInput: EditText
    private lateinit var statusText: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(48, 48, 48, 48)
            setBackgroundColor(Color.BLACK)
        }

        TextView(this).apply {
            text = "LiteQMS Settings"
            textSize = 24f
            setTextColor(Color.WHITE)
            setPadding(0, 0, 0, 24)
            root.addView(this)
        }

        urlInput = EditText(this).apply {
            hint = "Server URL (e.g. http://192.168.1.100:5000)"
            setHintTextColor(Color.GRAY)
            setTextColor(Color.WHITE)
            setSingleLine()
            imeOptions = EditorInfo.IME_ACTION_DONE
            root.addView(this)
        }

        statusText = TextView(this).apply {
            text = ""
            textSize = 14f
            setPadding(0, 8, 0, 8)
            root.addView(this)
        }

        Button(this).apply {
            text = "Auto-discover"
            setOnClickListener {
                startActivityForResult(
                    Intent(this@SettingsActivity, DiscoveryActivity::class.java),
                    REQUEST_DISCOVERY
                )
            }
            root.addView(this)
        }

        Button(this).apply {
            text = "Test Connection"
            setOnClickListener { testConnection() }
            root.addView(this)
        }

        Button(this).apply {
            text = "Save"
            setOnClickListener { saveUrl() }
            root.addView(this)
        }

        setContentView(root)
        loadSavedUrl()
    }

    @Deprecated("Deprecated in Java")
    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        super.onActivityResult(requestCode, resultCode, data)
        if (requestCode == REQUEST_DISCOVERY && resultCode == RESULT_OK && data != null) {
            val url = data.getStringExtra("server_url")
            if (url != null) urlInput.setText(url)
        }
    }

    private fun loadSavedUrl() {
        val saved = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .getString(KEY_SERVER_URL, null)
        if (saved != null) urlInput.setText(saved)
    }

    private fun testConnection() {
        val url = urlInput.text.toString().trim().trimEnd('/')
        if (url.isEmpty()) {
            statusText.text = "Enter a URL first"
            statusText.setTextColor(Color.RED)
            return
        }

        lifecycleScope.launch {
            try {
                val conn = URL("$url/Display").openConnection() as HttpURLConnection
                conn.connectTimeout = 5000
                conn.readTimeout = 5000
                val code = conn.responseCode
                statusText.apply {
                    text = if (code == 200) "Connection OK (HTTP $code)" else "Unexpected: HTTP $code"
                    setTextColor(if (code == 200) Color.GREEN else Color.YELLOW)
                }
            } catch (e: Exception) {
                statusText.apply {
                    text = "Failed: ${e.message}"
                    setTextColor(Color.RED)
                }
            }
        }
    }

    private fun saveUrl() {
        val url = urlInput.text.toString().trim().trimEnd('/')
        if (url.isEmpty()) {
            statusText.text = "Enter a server URL"
            statusText.setTextColor(Color.RED)
            return
        }
        if (!url.startsWith("http://") && !url.startsWith("https://")) {
            statusText.text = "URL must start with http:// or https://"
            statusText.setTextColor(Color.RED)
            return
        }

        getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .edit()
            .putString(KEY_SERVER_URL, url)
            .apply()

        setResult(RESULT_OK)
        finish()
    }
}
