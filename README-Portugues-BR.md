# PangyaSharp — Servidor Privado Pangya (JP) ⛳

> Implementação completa de servidor privado para o Pangya JP, escrita em C# / .NET.

🔙 [Voltar ao README principal](README.md)

---

## Índice

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Servidores](#servidores)
- [Bibliotecas PangyaAPI](#bibliotecas-pangyaapi)
- [Pré-requisitos](#pré-requisitos)
- [Configuração do ODBC System DSN](#configuração-do-odbc-system-dsn)
- [Configuração](#configuração)
- [Compilação e Execução](#compilação-e-execução)
- [Referência de Portas](#referência-de-portas)
- [Segurança e Anti-Bot](#segurança-e-anti-bot)
- [Camada de Banco de Dados](#camada-de-banco-de-dados)
- [Modos de Jogo](#modos-de-jogo)
- [Managers do GameServer](#managers-do-gameserver)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Comunidade](#comunidade)

---

## Visão Geral

O PangyaSharp replica a infraestrutura multi-servidor do Pangya JP, incluindo:

- **Autenticação** — validação de chaves de sessão entre servidores
- **Login** — ponto de entrada dos clientes, verificação de conta, lista de servidores
- **Jogo** — lógica principal: salas, modos, itens, eventos, missões
- **Ranking** — rankings de jogadores, personagens e guildas
- **Messenger** — lista de amigos, status online, mensagens

O projeto é composto por **5 servidores TCP independentes** mais uma camada compartilhada **PangyaAPI**. Todos os servidores se conectam a um único banco de dados **SQL Server (MSSQL)** via **ODBC**.

---

## Arquitetura

```
[Cliente Pangya JP]
       │
  [LoginServer :10103]     ──► Autentica usuários e fornece lista de servidores
       │
  [AuthServer :7777]       ──► Valida chaves de sessão entre servidores (interno)
       │
  [GameServer :20201]      ──► Lógica principal do jogo: salas, modos, inventário
       │
  [RankingServer :4774]    ──► Rankings de jogadores, personagens e guildas
       │
  [MessengerServer :30303] ──► Amigos, status online, mensagens
       │
  [SQL Server / MSSQL]     ──► Banco de dados único (ODBC via DSN "pangya")
```

### Fluxo de Login

1. O cliente conecta ao **LoginServer** (porta 10103).
2. O LoginServer valida as credenciais no banco e gera uma chave de sessão.
3. O LoginServer retorna a lista de GameServers disponíveis ao cliente.
4. O cliente conecta diretamente ao **GameServer** escolhido.
5. O GameServer valida a chave de sessão com o **AuthServer**.
6. O jogador entra nos canais e salas de jogo.

---

## Servidores

### AuthServer

Servidor interno — valida chaves de sessão entre os demais servidores. O cliente de jogo nunca se conecta diretamente a ele.

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| Porta | 7777 | Porta TCP de escuta |
| GUID | 8888 | Identificador único do servidor |
| Versão | AS.Release.2.0 | Versão do servidor |
| MaxUser | 100 | Máximo de conexões simultâneas |
| TTL | 60000 ms | Timeout de desconexão |

---

### LoginServer

Ponto de entrada dos clientes. Gerencia autenticação, bloqueio de IPs, criação de contas e listagem de servidores.

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| Porta | 10103 | Porta TCP de escuta |
| MaxUser | 2001 | Máximo de usuários simultâneos |
| CREATEUSER | 0 | Permitir criação de novas contas (1=SIM) |
| ACCESSFLAG | 0 | 0=todos, 1=somente GM e IPs permitidos |
| MANUTENTION | 0 | 1=servidor em manutenção |
| TTL | 60000 ms | Timeout de desconexão |

---

### GameServer

O servidor principal do jogo. Gerencia canais, salas, modos de jogo, inventário, missões, eventos, guildas e toda a lógica de gameplay.

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| Porta | 20201 | Porta TCP de escuta |
| MaxUser | 2001 | Máximo de usuários simultâneos |
| EXPRATE | 300 | Multiplicador de EXP (300 = 3x) |
| PANGRATE | 100 | Multiplicador de Pang (100 = 1x) |
| CLUBMASTERYRATE | 200 | Multiplicador de Mastery de Taco |
| GP_EVENT | 1 | Grand Prix Event (0=OFF, 1=ON) |
| GOLDEN_TIME_EVENT | 1 | Sorteio de itens (0=OFF, 1=ON) |
| GZ_EVENT | 0 | Grand Zodiac Event (0=OFF, 1=ON) |
| LOGIN_REWARD | 0 | Recompensa de login diário (0=OFF, 1=ON) |
| ANGEL_EVENT | 1 | Reduz quit a cada partida completada |
| GAMEGUARDAUTH | 0 | Autenticação GameGuard (0=OFF) |

**Canais** são configurados no `server.ini` nas seções `[CHANNEL1]`, `[CHANNEL2]`, etc.

---

### RankingServer

Gerencia os rankings de jogadores, personagens e guildas.

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| Porta | 4774 | Porta TCP de escuta |
| GUID | 4774 | Identificador único |
| Versão | RS.Release.2.0 | Versão do servidor |

---

### MessengerServer

Gerencia a lista de amigos, status online/offline e mensagens entre jogadores.

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| Porta | 30303 | Porta TCP de escuta |
| GUID | 30303 | Identificador único |
| TTL | 0 | TTL desativado para o messenger |

---

## Bibliotecas PangyaAPI

### PangyaAPI.Network

Camada de rede compartilhada por todos os servidores.

- `Server` — classe base TCP abstrata
- `Session` / `SessionManager` / `Client` — gerenciamento de conexões
- `Cipher` + `CryptoOracle` — criptografia/descriptografia de pacotes (XOR com tabelas de chave)
- `MiniLzo` — compressão LZO de pacotes
- `Packet` / `PacketBuffer` — leitura/escrita de pacotes binários
- `ConfigDDos` / `IpDdosFilter` — rate limiting anti-DDoS
- `unit` / `thread` — processamento assíncrono

**Formato de pacote cifrado:** `[salt(1)] [len_low(1)] [len_high(1)] [pad(1)] [public_key(1)] [dados_cifrados...]`

---

### PangyaAPI.SQL

Camada de abstração de banco de dados.

- `Pangya_DB` — classe base para todos os comandos de banco
- `DbFactory` — cria o driver correto (`mssql`, `mysql`, ou `postgresql`)
- `mssql` — driver SQL Server via ODBC
- `mysql` — driver MySQL/MariaDB
- `ctx_db` — contexto de conexão (host, porta, usuário, senha)
- `Response` / `Result_Set` — tipos de resultado de query

**Engines suportadas:**

| Engine | Chave no .ini | Status |
|--------|---------------|--------|
| SQL Server | `MSSQL` ou `SQLSERVER` | ✅ Ativo |
| MySQL / MariaDB | `MYSQL` | ✅ Ativo |
| PostgreSQL | `POSTGRESQL` | 🔧 Em desenvolvimento |

---

### PangyaAPI.Utilities

Utilitários gerais compartilhados por todos os servidores.

| Componente | Arquivo | Responsabilidade |
|------------|---------|-----------------|
| IniHandle | IniLib.cs | Leitura de arquivos `.ini` |
| message_pool | Log/message_pool.cs | Pool assíncrono de logs |
| PangyaBinaryReader/Writer | Models/ | Leitura/escrita binária de pacotes |
| PangyaSyncTimer | PangyaSyncTimer.cs | Timer sincronizado para eventos |
| Singleton | Singleton.cs | Padrão Singleton genérico |
| exception | exception.cs | Exceção customizada do PangyaSharp |
| UtilTime | UtilTime.cs | Formatação de datas e horas |

---

### PangyaAPI.IFF.JP

Biblioteca de leitura do formato binário proprietário IFF do Pangya JP. Contém modelos para todos os itens do jogo:

`Character`, `ClubSet`, `Ball`, `Caddie`, `Mascot`, `Card`, `Course`, `Item`, `Part`, `Furniture`, `Achievement`, `GrandPrixData`, entre outros.

---

### PangyaAPI.Discord *(opcional)*

Módulo de integração com Discord via webhook para notificações de eventos do servidor.

---

## Pré-requisitos

- **SO:** Windows 10/11 ou Windows Server 2016+
- **Runtime:** .NET Framework 4.8 ou .NET 6+
- **Banco de Dados:** Microsoft SQL Server 2016+
- **Driver ODBC:** ODBC Driver 17 for SQL Server (recomendado)
- **IDE:** Visual Studio 2022 (recomendado) ou MSBuild
- **NuGet:** `System.Data.Odbc` 10.0.3 (incluído em `packages/`)

---

## Configuração do ODBC System DSN

Todos os servidores se conectam ao SQL Server através de um **System DSN** chamado `pangya`. Esse nome deve coincidir com o valor de `DBIP` em todos os `server.ini`.

### Passo a passo

**1. Abrir o Administrador de Fontes de Dados ODBC**

Pressione `Win + R`, digite `odbcad32.exe` e pressione Enter.

> ⚠️ Use a versão correta: `C:\Windows\System32\odbcad32.exe` para 64 bits, `C:\Windows\SysWOW64\odbcad32.exe` para 32 bits.

**2. Criar novo DSN de Sistema**

- Vá para a aba **DSN de Sistema** → clique em **Adicionar**
- Selecione **ODBC Driver 17 for SQL Server** → clique em **Concluir**

**3. Configurar o DSN**

| Campo | Valor | Observação |
|-------|-------|-----------|
| Nome | `pangya` | Deve coincidir com o `DBIP` no `server.ini` |
| Descrição | Pangya Database | Opcional |
| Servidor | `(local)` ou `127.0.0.1\SQLEXPRESS` | Endereço do SQL Server |

**4. Autenticação**

- Selecione **Autenticação do SQL Server**
- ID de Logon: `pangya` | Senha: `pangya`
- Marque: *Conectar ao SQL Server para obter configurações padrão*

**5. Banco de Dados Padrão**

- Alterar banco de dados padrão para: `pangya`

**6. Finalizar e Testar**

- Clique em **Testar Fonte de Dados** — resultado esperado: `TESTS COMPLETED SUCCESSFULLY!`

### Trecho do server.ini

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

> 📥 Download do ODBC Driver 17: https://docs.microsoft.com/pt-br/sql/connect/odbc/download-odbc-driver-for-sql-server

---

## Configuração

### Chaves comuns do server.ini

| Seção | Chave | Descrição |
|-------|-------|-----------|
| `[SERVERINFO]` | `PORT` | Porta TCP de escuta |
| `[SERVERINFO]` | `IP` | IP de bind (`127.0.0.1` para local) |
| `[SERVERINFO]` | `GUID` | Identificador único do servidor |
| `[SERVERINFO]` | `MAXUSER` | Máximo de usuários simultâneos |
| `[OPTION]` | `TTL` | Timeout em ms (0=desativado) |
| `[OPTION]` | `ANTIBOTTTL` | Timeout anti-bot em ms |
| `[OPTION]` | `SAME_ID_LOGIN` | Permite mesmo ID logado 2x — **somente testes** |
| `[LOG]` | `DIR` | Diretório de logs |
| `[AUTHSERVER]` | `IP` / `PORT` | Endereço do AuthServer |

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

## Compilação e Execução

```bash
# 1. Abra PangyaSharp.sln no Visual Studio 2022
# 2. Restaure os pacotes NuGet:  Build > Restore NuGet Packages
# 3. Selecione:                  Release | Any CPU
# 4. Compile:                    Ctrl+Shift+B
```

**Ordem recomendada de inicialização:**

```
1. AuthServer
2. LoginServer
3. GameServer
4. RankingServer
5. MessengerServer
```

> Certifique-se de que o DSN ODBC está configurado antes de iniciar qualquer servidor.

---

## Referência de Portas

| Servidor | Porta TCP | GUID | Função |
|----------|-----------|------|--------|
| AuthServer | 7777 | 8888 | Validação interna de chaves |
| LoginServer | 10103 | 10103 | Entrada dos clientes |
| GameServer | 20201 | 20201 | Lógica principal do jogo |
| RankingServer | 4774 | 4774 | Rankings |
| MessengerServer | 30303 | 30303 | Amigos e chat |
| SQL Server | 1433 | — | Banco de dados |

---

## Segurança e Anti-Bot

| Mecanismo | Descrição |
|-----------|-----------|
| TTL | Desconecta clientes sem resposta dentro do tempo configurado |
| ANTIBOTTTL | Detecta bots que pulam o pacote de validação |
| IP Rules | Regras allow/deny por IP e CIDR com limite de conexões |
| DDoS Filter | Rate limiting por IP com janela de tempo configurável |
| MAC Ban | Banimento por MAC via banco de dados |
| IP Ban | Banimento por IP via banco de dados |
| GameGuard Auth | Suporte opcional à autenticação GameGuard |
| CapabilityFlags | Flags de permissão por jogador armazenadas no banco |

---

## Camada de Banco de Dados

Todas as interações com o banco seguem o **Padrão de Repositório por Comando**:

```csharp
// 1. Cada comando é uma classe que herda Pangya_DB
// 2. Implementa prepareConsulta() — monta e executa a stored procedure
// 3. Implementa lineResult()      — processa cada linha retornada
// 4. Chamada a partir da lógica de jogo:

var cmd = new CmdPlayerInfo(uid);
cmd.exec();
// resultado disponível via propriedades do cmd
```

---

## Modos de Jogo

| Modo | Arquivo | Descrição |
|------|---------|-----------|
| Match / Stroke | `GameModes/Match.cs` | Partida padrão / por tacadas |
| Practice | `GameModes/Practice.cs` | Prática individual |
| Grand Prix | `GameModes/GrandPrix.cs` | Torneio GP |
| Grand Zodiac | `GameModes/GrandZodiac.cs` | Evento Grand Zodiac |
| Guild Battle | `GameModes/GuildBattle.cs` | Batalha entre guildas |
| Pang Battle | `GameModes/PangBattle.cs` | Batalha por Pang |
| Versus | `GameModes/Versus.cs` | Modo 1v1 |
| Tourney | `GameModes/Tourney.cs` | Torneio oficial |
| Approach | `GameModes/Approach.cs` | Missão de aproximação |
| Chip-In Practice | `GameModes/ChipInPractice.cs` | Prática chip-in |
| Special Shuffle Course | `GameModes/SpecialShuffleCourse.cs` | Curso especial aleatório |

---

## Managers do GameServer

| Manager | Responsabilidade |
|---------|-----------------|
| PlayerManager | Lista de jogadores conectados |
| RoomManager | Salas de jogo ativas |
| CharacterManager | Personagens dos jogadores |
| CaddieManager | Caddies equipados |
| ItemManager | Inventário de itens |
| CardManager | Cartas dos jogadores |
| GuildRoomManager | Salas de guilda |
| MailBoxManager | Caixa de correio |
| DailyQuestManager | Missões diárias |
| AchievementManager | Conquistas |
| BroadcastManager | Mensagens broadcast |
| PersonalShopManager | Loja pessoal |
| WarehouseManager | Armazém de itens |

---

## Estrutura do Projeto

```
PangyaSharp/
├── AuthServer/
├── LoginServer/
├── GameServer/
│   ├── Game/
│   │   ├── GameModes/       # Implementações de modos de jogo
│   │   ├── Manager/         # Managers de estado em memória
│   │   ├── System/          # Sistemas (drops, eventos, lojas...)
│   │   └── Base/            # Classes base abstratas
│   ├── Repository/          # Comandos de banco (300+ arquivos)
│   ├── Models/              # Enums e tipos de dados
│   └── PangyaEnums/         # Enumerações de pacotes e flags
├── RankingServer/
├── MessengerServer/
├── PangyaAPI/
│   ├── PangyaAPI.Network/   # Base TCP + criptografia + DDoS
│   ├── PangyaAPI.SQL/       # Abstração de banco (ODBC)
│   ├── PangyaAPI.Utilities/ # Utilitários compartilhados + log
│   ├── PangyaAPI.IFF.JP/    # Leitor do formato IFF binário
│   └── PangyaAPI.Discord/   # Webhook Discord (opcional)
└── packages/                # NuGet (System.Data.Odbc)
```

---

## Comunidade

*   **Discord:** [Retreev Community](https://discord.gg/HwDTssf)
*   **Server:** [Pangya Fun Community](https://discord.gg/DEwj7DnBHb)
*   **YouTube:** [@devluismk](https://www.youtube.com/@devluismk)

---

🔙 [Voltar ao README principal](README.md)

*Dedicado à Comunidade do Projeto Pangya.*
