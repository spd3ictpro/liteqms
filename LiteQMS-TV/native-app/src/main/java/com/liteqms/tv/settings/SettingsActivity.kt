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
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.liteqms.tv.discovery.DiscoveryActivity
import kotlinx.coroutines.launch
import org.json.JSONArray
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL

class SettingsActivity : AppCompatActivity() {

    companion object {
        private const val PREFS_NAME = "liteqms_native"
        private const val KEY_SERVER_URL = "server_url"
        private const val KEY_SAVED_SERVERS = "saved_servers"
    }

    private lateinit var urlInput: EditText
    private lateinit var statusText: TextView
    private lateinit var savedServersContainer: LinearLayout

    private val discoveryLauncher = registerForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { result ->
        if (result.resultCode == RESULT_OK) {
            val data = result.data ?: return@registerForActivityResult
            val url = data.getStringExtra("server_url") ?: return@registerForActivityResult
            val name = data.getStringExtra("server_name") ?: url
            urlInput.setText(url)
            addServerToList(name, url)
        }
    }

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

        TextView(this).apply {
            text = "Saved Servers"
            textSize = 18f
            setTextColor(Color.parseColor("#0d9488"))
            setPadding(0, 0, 0, 8)
            root.addView(this)
        }

        savedServersContainer = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
        }
        root.addView(savedServersContainer)
        renderSavedServers()

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
                discoveryLauncher.launch(
                    Intent(this@SettingsActivity, DiscoveryActivity::class.java)
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
            text = "Connect"
            setOnClickListener { saveAndConnect() }
            root.addView(this)
        }

        setContentView(root)
        loadSavedUrl()
    }

    private fun loadSavedUrl() {
        val saved = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .getString(KEY_SERVER_URL, null)
        if (saved != null) urlInput.setText(saved)
    }

    private fun loadSavedServers(): MutableList<Pair<String, String>> {
        val json = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .getString(KEY_SAVED_SERVERS, null) ?: return mutableListOf()
        return try {
            val arr = JSONArray(json)
            val list = mutableListOf<Pair<String, String>>()
            for (i in 0 until arr.length()) {
                val obj = arr.getJSONObject(i)
                list.add(obj.getString("name") to obj.getString("url"))
            }
            list
        } catch (_: Exception) {
            mutableListOf()
        }
    }

    private fun saveServerList(servers: List<Pair<String, String>>) {
        val arr = JSONArray()
        for ((name, url) in servers) {
            arr.put(JSONObject().apply {
                put("name", name)
                put("url", url)
            })
        }
        getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .edit()
            .putString(KEY_SAVED_SERVERS, arr.toString())
            .apply()
    }

    private fun addServerToList(name: String, url: String) {
        val servers = loadSavedServers().toMutableList()
        servers.removeAll { it.second == url }
        servers.add(0, name to url)
        saveServerList(servers)
        renderSavedServers()
    }

    private fun renderSavedServers() {
        savedServersContainer.removeAllViews()
        val servers = loadSavedServers()

        if (servers.isEmpty()) {
            savedServersContainer.addView(TextView(this).apply {
                text = "No saved servers"
                textSize = 14f
                setTextColor(Color.GRAY)
                setPadding(0, 0, 0, 8)
            })
            return
        }

        for ((name, url) in servers) {
            val row = LinearLayout(this).apply {
                orientation = LinearLayout.HORIZONTAL
                setPadding(0, 4, 0, 4)
                setBackgroundColor(Color.parseColor("#1a1a1a"))
            }

            val label = TextView(this).apply {
                text = "$name  ($url)"
                textSize = 14f
                setTextColor(Color.WHITE)
                setPadding(12, 8, 12, 8)
                layoutParams = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WRAP_CONTENT, 1f)
                setOnClickListener {
                    urlInput.setText(url)
                    saveAndConnect()
                }
            }
            row.addView(label)

            val deleteBtn = Button(this).apply {
                text = "×"
                textSize = 18f
                setTextColor(Color.RED)
                setPadding(12, 8, 12, 8)
                setOnClickListener {
                    val updated = loadSavedServers().toMutableList()
                    updated.removeAll { it.second == url }
                    saveServerList(updated)
                    renderSavedServers()
                }
            }
            row.addView(deleteBtn)

            savedServersContainer.addView(row)
        }
    }

    private fun testConnection() {
        var url = urlInput.text.toString().trim().trimEnd('/')
        if (url.isEmpty()) {
            statusText.text = "Enter a URL first"
            statusText.setTextColor(Color.RED)
            return
        }
        if (!url.startsWith("http://") && !url.startsWith("https://")) {
            url = "http://$url"
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

    private fun saveAndConnect() {
        var url = urlInput.text.toString().trim().trimEnd('/')
        if (url.isEmpty()) {
            statusText.text = "Enter a server URL"
            statusText.setTextColor(Color.RED)
            return
        }
        if (!url.startsWith("http://") && !url.startsWith("https://")) {
            url = "http://$url"
        }

        getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .edit()
            .putString(KEY_SERVER_URL, url)
            .apply()

        addServerToList(url, url)

        setResult(RESULT_OK)
        finish()
    }
}
