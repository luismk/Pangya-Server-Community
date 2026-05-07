# PangyaSharp — Private Pangya Server (JP) ⛳

> A full-featured private server implementation for Pangya JP, written in C# / .NET.

🔙 [Back to main README](README.md)

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Servers](#servers)
- [PangyaAPI Libraries](#pangyaapi-libraries)
- [Prerequisites](#prerequisites)
- [ODBC System DSN Setup](#odbc-system-dsn-setup)
- [Configuration](#configuration)
- [Building & Running](#building--running)
- [Port Reference](#port-reference)
- [Security & Anti-Bot](#security--anti-bot)
- [Database Layer](#database-layer)
- [Game Modes](#game-modes)
- [GameServer Managers](#gameserver-managers)
- [Project Structure](#project-structure)
- [Community](#community)

---

## Overview

PangyaSharp replicates the multi-server infrastructure of Pangya JP, including:

- **Authentication** — session key validation between servers
- **Login** — client entry point, account verification, server list
- **Game** — core gameplay logic: rooms, modes, items, events, quests
- **Ranking** — player/character/guild leaderboards
- **Messenger** — friend list, online status, messaging

The project consists of **5 independent TCP servers** plus a shared **PangyaAPI** layer. All servers connect to a single **SQL Server (MSSQL)** database via **ODBC**.

---

## Architecture

```
[Pangya JP Client]
       │
  [LoginServer :10103]     ──► Authenticates users, provides server list
       │
  [AuthServer :7777]       ──► Validates session keys between servers (internal)
       │
  [GameServer :20201]      ──► Core game logic: rooms, modes, inventory, events
       │
  [RankingServer :4774]    ──► Player/character/guild rankings
       │
  [MessengerServer :30303] ──► Friends, online status, messaging
       │
  [SQL Server / MSSQL]     ──► Single shared database (ODBC via DSN "pangya")
```

### Login Flow

1. Client connects to **LoginServer** (port 10103).
2. LoginServer validates credentials in the database and generates a session key.
3. LoginServer returns the list of available GameServers to the client.
4. Client connects directly to the chosen **GameServer**.
5. GameServer validates the session key with **AuthServer**.
6. Player enters channels and game rooms.

---

## Servers

### AuthServer

Internal server — validates session keys between other servers. Clients never connect directly to it.

| Parameter | Default | Description |
|-----------|---------|-------------|
| Port | 7777 | TCP listen port |
| GUID | 8888 | Unique server identifier |
| Version | AS.Release.2.0 | Server version |
| MaxUser | 100 | Max simultaneous connections |
| TTL | 60000 ms | Disconnect timeout |

---

### LoginServer

Client entry point. Handles authentication, IP blocking, account creation control, and server listing.

| Parameter | Default | Description |
|-----------|---------|-------------|
| Port | 10103 | TCP listen port |
| MaxUser | 2001 | Max simultaneous users |
| CREATEUSER | 0 | Allow new account creation (1=ON) |
| ACCESSFLAG | 0 | 0=everyone, 1=GM/whitelisted IPs only |
| MANUTENTION | 0 | 1=server under maintenance |
| TTL | 60000 ms | Disconnect timeout |

---

### GameServer

The main game server. Manages channels, rooms, game modes, inventory, missions, events, guilds, and all gameplay logic.

| Parameter | Default | Description |
|-----------|---------|-------------|
| Port | 20201 | TCP listen port |
| MaxUser | 2001 | Max simultaneous users |
| EXPRATE | 300 | EXP multiplier (300 = 3×) |
| PANGRATE | 100 | Pang multiplier (100 = 1×) |
| CLUBMASTERYRATE | 200 | Club mastery multiplier |
| GP_EVENT | 1 | Grand Prix Event (0=OFF, 1=ON) |
| GOLDEN_TIME_EVENT | 1 | Item raffle event (0=OFF, 1=ON) |
| GZ_EVENT | 0 | Grand Zodiac Event (0=OFF, 1=ON) |
| LOGIN_REWARD | 0 | Daily login reward (0=OFF, 1=ON) |
| ANGEL_EVENT | 1 | Reduces quit counter per completed game |
| GAMEGUARDAUTH | 0 | GameGuard authentication (0=OFF) |

**Channels** are configured in `server.ini` under `[CHANNEL1]`, `[CHANNEL2]`, etc.

---

### RankingServer

Manages player, character, and guild ranking registries.

| Parameter | Default | Description |
|-----------|---------|-------------|
| Port | 4774 | TCP listen port |
| GUID | 4774 | Unique identifier |
| Version | RS.Release.2.0 | Server version |

---

### MessengerServer

Handles the friend list, online/offline status, and player messaging.

| Parameter | Default | Description |
|-----------|---------|-------------|
| Port | 30303 | TCP listen port |
| GUID | 30303 | Unique identifier |
| TTL | 0 | TTL disabled for messenger |

---

## PangyaAPI Libraries

### PangyaAPI.Network

Networking layer shared by all servers.

- `Server` — abstract TCP server base class
- `Session` / `SessionManager` / `Client` — connection management
- `Cipher` + `CryptoOracle` — Pangya packet encryption/decryption (XOR with public/private key tables)
- `MiniLzo` — LZO packet compression
- `Packet` / `PacketBuffer` — binary packet read/write
- `ConfigDDos` / `IpDdosFilter` — DDoS rate limiting
- `unit` / `thread` — async processing units

**Encrypted packet format:** `[salt(1)] [len_low(1)] [len_high(1)] [pad(1)] [public_key(1)] [encrypted_data...]`

---

### PangyaAPI.SQL

Database abstraction layer.

- `Pangya_DB` — abstract base for all DB command classes
- `DbFactory` — creates the correct DB driver (`mssql`, `mysql`, or `postgresql`)
- `mssql` — SQL Server driver via ODBC
- `mysql` — MySQL/MariaDB driver
- `ctx_db` — connection context (host, port, user, password)
- `Response` / `Result_Set` — query result types

**Supported engines:**

| Engine | INI Key | Status |
|--------|---------|--------|
| SQL Server | `MSSQL` or `SQLSERVER` | ✅ Active |
| MySQL / MariaDB | `MYSQL` | ✅ Active |
| PostgreSQL | `POSTGRESQL` | 🔧 In development |

---

### PangyaAPI.Utilities

General utilities shared by all servers.

| Component | File | Responsibility |
|-----------|------|----------------|
| IniHandle | IniLib.cs | `.ini` config file parser |
| message_pool | Log/message_pool.cs | Async log message pool |
| PangyaBinaryReader/Writer | Models/ | Binary packet I/O |
| PangyaSyncTimer | PangyaSyncTimer.cs | Synchronized event timer |
| Singleton | Singleton.cs | Generic singleton pattern |
| exception | exception.cs | Custom exception class |
| UtilTime | UtilTime.cs | Date/time formatting helpers |

---

### PangyaAPI.IFF.JP

Reader for Pangya JP's proprietary IFF binary data format. Contains models for all game items:

`Character`, `ClubSet`, `Ball`, `Caddie`, `Mascot`, `Card`, `Course`, `Item`, `Part`, `Furniture`, `Achievement`, `GrandPrixData`, and many more.

---

### PangyaAPI.Discord *(optional)*

Discord webhook integration for server event notifications.

---

## Prerequisites

- **OS:** Windows 10/11 or Windows Server 2016+
- **Runtime:** .NET Framework 4.8 or .NET 6+
- **Database:** Microsoft SQL Server 2016+
- **ODBC Driver:** ODBC Driver 17 for SQL Server (recommended)
- **IDE:** Visual Studio 2022 (recommended) or MSBuild
- **NuGet:** `System.Data.Odbc` 10.0.3 (included in `packages/`)

---

## ODBC System DSN Setup

All servers connect to SQL Server through a **System DSN** named `pangya`. This name must match the `DBIP` value in every `server.ini`.

### Step-by-step

**1. Open ODBC Data Source Administrator**

Press `Win + R`, type `odbcad32.exe`, press Enter.

> ⚠️ Use the correct bitness: `C:\Windows\System32\odbcad32.exe` for 64-bit, `C:\Windows\SysWOW64\odbcad32.exe` for 32-bit.

**2. Create a new System DSN**

- Go to the **System DSN** tab → click **Add**
- Select **ODBC Driver 17 for SQL Server** → click **Finish**

**3. Configure the DSN**

| Field | Value | Note |
|-------|-------|------|
| Name | `pangya` | Must match `DBIP` in `server.ini` |
| Description | Pangya Database | Optional |
| Server | `(local)` or `127.0.0.1\SQLEXPRESS` | Your SQL Server address |

**4. Authentication**

- Select **SQL Server Authentication**
- Login ID: `pangya` | Password: `pangya`
- Check: *Connect to SQL Server to obtain default settings*

**5. Default Database**

- Change default database to: `pangya`

**6. Finish & Test**

- Click **Test Data Source** — expected: `TESTS COMPLETED SUCCESSFULLY!`

### server.ini snippet

```ini
[NORMAL_DB]
DBENGINE  =  MSSQL
DBIP      =  pangya
DBNAME    =  pangya
DBUSER    =  pangya
DBPASS    =  pangya
DBPORT    =  1433
DBLOG     =  1
```

> 📥 Download ODBC Driver 17: https://docs.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server

---

## Configuration

### Common server.ini keys

| Section | Key | Description |
|---------|-----|-------------|
| `[SERVERINFO]` | `PORT` | TCP listen port |
| `[SERVERINFO]` | `IP` | Bind address (`127.0.0.1` for local) |
| `[SERVERINFO]` | `GUID` | Unique server ID |
| `[SERVERINFO]` | `MAXUSER` | Maximum concurrent users |
| `[OPTION]` | `TTL` | Client timeout in ms (0 = disabled) |
| `[OPTION]` | `ANTIBOTTTL` | Anti-bot timeout in ms |
| `[OPTION]` | `SAME_ID_LOGIN` | Allow duplicate logins — **testing only** |
| `[LOG]` | `DIR` | Log output directory |
| `[AUTHSERVER]` | `IP` / `PORT` | AuthServer address |

### Anti-DDoS (config/socket_config.ini)

```ini
[IPRULES]
enable_ip_rules         = 1
limit_connection_per_ip = 2
order                   = deny,allow
allow                   = 127.0.0.1
allow                   = all
deny                    = 192.168.0.1/24

[DDOS]
ddos_interval           = 3000
ddos_count              = 5
ddos_autoreset          = 3000
```

---

## Building & Running

```bash
# 1. Open PangyaSharp.sln in Visual Studio 2022
# 2. Restore NuGet packages:  Build > Restore NuGet Packages
# 3. Select:                  Release | Any CPU
# 4. Build:                   Ctrl+Shift+B
```

**Recommended startup order:**

```
1. AuthServer
2. LoginServer
3. GameServer
4. RankingServer
5. MessengerServer
```

> Make sure the ODBC DSN is configured before starting any server.

---

## Port Reference

| Server | TCP Port | GUID | Role |
|--------|----------|------|------|
| AuthServer | 7777 | 8888 | Internal key validation |
| LoginServer | 10103 | 10103 | Client entry point |
| GameServer | 20201 | 20201 | Core game logic |
| RankingServer | 4774 | 4774 | Leaderboards |
| MessengerServer | 30303 | 30303 | Friends & chat |
| SQL Server | 1433 | — | Database |

---

## Security & Anti-Bot

| Mechanism | Description |
|-----------|-------------|
| TTL | Disconnects unresponsive clients after timeout |
| ANTIBOTTTL | Detects bots that skip the validation packet |
| IP Rules | Allow/deny by IP and CIDR with per-IP connection limits |
| DDoS Filter | Rate limiting per IP within a configurable time window |
| MAC Ban | MAC address banning via database |
| IP Ban | IP banning via database |
| GameGuard Auth | Optional GameGuard authentication |
| CapabilityFlags | Per-player permission flags stored in the database |

---

## Database Layer

All database interactions follow the **Repository Command Pattern**:

```csharp
// 1. Each command is a class inheriting Pangya_DB
// 2. Implement prepareConsulta() — builds and executes the stored procedure
// 3. Implement lineResult()      — processes each returned row
// 4. Call from game logic:

var cmd = new CmdPlayerInfo(uid);
cmd.exec();
// result available via cmd properties
```

---

## Game Modes

| Mode | File | Description |
|------|------|-------------|
| Match / Stroke | `GameModes/Match.cs` | Standard scoring / stroke play |
| Practice | `GameModes/Practice.cs` | Solo practice |
| Grand Prix | `GameModes/GrandPrix.cs` | GP tournament |
| Grand Zodiac | `GameModes/GrandZodiac.cs` | Grand Zodiac event |
| Guild Battle | `GameModes/GuildBattle.cs` | Guild vs. guild |
| Pang Battle | `GameModes/PangBattle.cs` | Pang currency battle |
| Versus | `GameModes/Versus.cs` | 1v1 versus mode |
| Tourney | `GameModes/Tourney.cs` | Official tournament |
| Approach | `GameModes/Approach.cs` | Approach mission |
| Chip-In Practice | `GameModes/ChipInPractice.cs` | Chip-in practice |
| Special Shuffle Course | `GameModes/SpecialShuffleCourse.cs` | Random course shuffle |

---

## GameServer Managers

| Manager | Responsibility |
|---------|----------------|
| PlayerManager | Connected players list |
| RoomManager | Active game rooms |
| CharacterManager | Player characters |
| CaddieManager | Equipped caddies |
| ItemManager | Item inventory |
| CardManager | Player cards |
| GuildRoomManager | Guild rooms |
| MailBoxManager | Mailbox system |
| DailyQuestManager | Daily quests |
| AchievementManager | Achievements |
| BroadcastManager | Broadcast messages |
| PersonalShopManager | Personal shop |
| WarehouseManager | Item warehouse |

---

## Project Structure

```
PangyaSharp/
├── AuthServer/
├── LoginServer/
├── GameServer/
│   ├── Game/
│   │   ├── GameModes/       # All game mode implementations
│   │   ├── Manager/         # In-memory state managers
│   │   ├── System/          # Feature systems (drops, events, shops…)
│   │   └── Base/            # Abstract game base classes
│   ├── Repository/          # DB command classes (300+ files)
│   ├── Models/              # Game enums and data types
│   └── PangyaEnums/         # Packet and flag enumerations
├── RankingServer/
├── MessengerServer/
├── PangyaAPI/
│   ├── PangyaAPI.Network/   # TCP server base + crypto + DDoS
│   ├── PangyaAPI.SQL/       # Database abstraction (ODBC)
│   ├── PangyaAPI.Utilities/ # Shared utilities + logging
│   ├── PangyaAPI.IFF.JP/    # IFF binary format reader
│   └── PangyaAPI.Discord/   # Optional Discord webhook
└── packages/                # NuGet (System.Data.Odbc)
```

---

## Community

*   **Discord:** [Retreev Community](https://discord.gg/HwDTssf)
*   **Server:** [Pangya Fun Community](https://discord.gg/DEwj7DnBHb)
*   **YouTube:** [@devluismk](https://www.youtube.com/@devluismk)

---

🔙 [Back to main README](README.md)

*Dedicated to the Pangya Community Project.*
