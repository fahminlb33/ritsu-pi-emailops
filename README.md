# 📬 Ritsu-Pi EmailOps

**Secure homelab management via email.**  
A Postmark-powered EmailOps module for the [Ritsu-Pi](https://github.com/fahminlb33/ritsu-pi) homelab automation stack.

* ✉️ Send commands from your inbox  
* 🐳 Manage Docker containers  
* 🔐 Secure, auditable, and remote  
* 🧠 Built with ASP.NET Core (.NET 9), EFCore, and SQLite

## 🚀 What is Ritsu-Pi EmailOps?

Ritsu-Pi EmailOps lets you control your homelab Docker containers via email commands, securely processed through Postmark's Inbound Webhook. It’s built for Raspberry Pi and self-hosters who want a zero-UI, minimal-attack-surface, out-of-band control plane.

This project is a feature module of [Ritsu-Pi](https://github.com/yourusername/ritsu-pi), an open-source platform for managing self-hosted services with automation and observability in mind.

## ✨ Features

* 📩 Email-based command interface (via Postmark)
* 🐳 Docker management: start, stop, restart, status
* 💻 System monitoring: CPU/memory/disk usage from Prometheus
* 🔒 Allowlist-based email sender authentication
* 🧾 Audit log with command history in SQLite
* 📥 Auto-response email replies with execution results
* 🖥️ Designed for headless, always-on homelab setups

## 📨 Supported Email Commands

I used the Gemini API with Semantic Kernel to parse the incoming email, meaning you can use natural language to converse with RitsuPi EmailOps!

Example email:

```plain
To: ops@ritsupi.kodesiana.app
Subject: Ollama is acting up

Can you restart the ollama container? It does not respond to any HTTP request.

Thanks!
```

## 🧩 Tech Stack

| Layer | Tech |
|-------|------|
| Language | C# (.NET 9) |
| Framework | ASP.NET Core |
| Persistence | SQLite + EF Core |
| Email Inbound | Postmark Inbound Webhook |
| Email Outbound | Postmark API |
| Container Control | Docker CLI / Docker.DotNet |

## ⚙️ Configuration

Setup your app settings in the `appsettings.Development.json` file.

* `Gemini:ApiKey` get your key from [Google AI Studio](https://aistudio.google.com/app/apikey)
* `Postmark:ServerToken` get your server token from [Postmark API Tokens](https://account.postmarkapp.com)
* `Postmark:ReplyToPattern` your inbound email. The `{0}` will be replaced with a unique thread hash
* `Postmark:FromEmail` the email sender from Postmark. Make sure the email you use to send and receive email from Postmark is from the same domain (if you're in Postmark Test Mode)
* `Postmark:AuthorizedEmails` a list of authorized emails to perform container restart and stop operations
* `Prometheus:BaseAddress` Prometheus base address, if you have one with Node Exporter metrics configured

## 🛠️ Getting Started

Follow these steps:

```bash
# Clone this repo
git clone https://github.com/yourusername/ritsu-pi-emailops.git
cd ritsu-pi-emailops

# Run database migrations
dotnet ef database update

# Now edit the appsettings.Development.json
# and fill the missing API keys and other config

# Run the app
dotnet run

# Expose the webhook endpoint (e.g., using ngrok)
ngrok http http://localhost:8080
```

## 🛠 Built for the Postmark Hackathon

This feature was created for the Postmark Inbox Innovators Hackathon
and is now a production module in Ritsu-Pi.

## 🛡 License

Licensed under the Apache License 2.0
