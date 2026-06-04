package com.liteqms.tv.discovery

import android.content.Intent
import android.graphics.Color
import android.os.Bundle
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.launch

class DiscoveryActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(48, 48, 48, 48)
            setBackgroundColor(Color.BLACK)
        }

        TextView(this).apply {
            text = "Discover Servers"
            textSize = 24f
            setTextColor(Color.WHITE)
            setPadding(0, 0, 0, 24)
            root.addView(this)
        }

        val statusText = TextView(this).apply {
            text = "Scanning..."
            textSize = 16f
            setTextColor(Color.GRAY)
            setPadding(0, 0, 0, 16)
            root.addView(this)
        }

        val resultsList = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            root.addView(this)
        }

        Button(this).apply {
            text = "Cancel"
            setOnClickListener { finish() }
            root.addView(this)
        }

        setContentView(root)

        lifecycleScope.launch {
            val servers = DiscoveryService().discover()
            if (servers.isEmpty()) {
                statusText.text = "No servers found"
            } else {
                statusText.text = "${servers.size} server(s) found"
                servers.forEach { server ->
                    val url = "http://${server.ip}:${server.port}"
                    Button(this@DiscoveryActivity).apply {
                        text = "${server.name} ($url)"
                        setOnClickListener {
                            val result = Intent().apply {
                                putExtra("server_url", url)
                                putExtra("server_name", server.name)
                            }
                            setResult(RESULT_OK, result)
                            finish()
                        }
                        resultsList.addView(this)
                    }
                }
            }
        }
    }
}
