# Pangya Fun - Legacy Version (v1.0) ⛳

Servidor baseado no código de Acrisio (SuperSS Dev) — reconstruído e adaptado em C#.

Este repositório contém o código-fonte da **Versão 1.0** do projeto **Pangya Fun**. Este código representa o alicerce de anos de trabalho em engenharia reversa e desenvolvimento C#, focado na emulação fiel da experiência do servidor japonês.

Toda esta versão foi **DEDICADA AO PROJETO COMMUNITY**, visando fortalecer a comunidade de preservação do Pangya.
> ⚠️ **Este projeto é fornecido como base de estudo. Você é livre para modificar, adaptar ou utilizar como quiser.**

> **⚠️ AVISO:** Esta versão foi oficialmente descontinuada e movida para este repositório como um arquivo histórico. O projeto atual agora utiliza uma arquitetura moderna (.NET 10, MAUI, Docker).

---

 ### 📌 Visão Geral

Este projeto simula os principais componentes de um servidor PangYa:

- **LoginServer** – Autenticação de jogadores.
- **MessengerServer** – Sistema de mensagens e amigos, guild.
- **GameServer** – Lobby, salas e partidas.
- **RankServer** – Rank dos jogadores, melhores 12 jogadores no map e etc...
- **AuthServer** – Sicronia entre os servidores, dados, envio.

É compatível com o cliente japonês **ProjectG JP versão 972.00 ou superior**.

---
### ✅ Status do Projeto

| Componente       | Progresso |
|------------------|-----------|
| GameServer       | 100%      |
| MessengerServer  | 100%      |
| LoginServer      | 100%      |
| RankServer       | 100%      |
| AuthServer       | 100%      |

---
  
## 📺 Acompanhe o Desenvolvimento
Todo o progresso, tutoriais e novidades sobre o projeto você encontra no meu canal:
👉 [YouTube - @devluismk](https://www.youtube.com/@devluismk)

---

## 🛠 Arquitetura e Componentes (V1.0)

O servidor foi construído sobre uma base modular dividida em APIs especializadas:

| API                        | Função principal                                                                      |
|----------------------------|---------------------------------------------------------------------------------------|
| **PangyaAPI.Network**      | Gerencia conexões TCP, sessões, buffers, envio/recebimento e tratamento de pacotes.   |
| **PangyaAPI.SQL**          | Interface de acesso ao banco de dados (SQL Server), comandos e respostas assíncronas. |
| **PangyaAPI.IFF.JP**       | Manipula os arquivos IFF do cliente japonês (itens, personagens, cursos etc.).        |
| **PangyaAPI.Utilities**    | Ferramentas auxiliares: Log, enums, config `.ini`, criptografia, estrutura de erros.  |

---

### 🧩 Requisitos

Você vai precisar de alguns programas e ferramentas:

- [Visual Studio](https://visualstudio.microsoft.com/pt-br/) – para compilar o projeto.
- [SQL Server](https://www.microsoft.com/pt-br/sql-server/sql-server-downloads) – para gerenciar o banco de dados.
- Cliente do **Pangya JP** – compatível com versão **972.00 ou superior** (ProjectG JP).

---

## 🧩 O Desafio do GUID (Para fazer funcionar)

Para que as salas funcionem corretamente, existe um pequeno trecho de código que está propositalmente **comentado**. 

Esse código é o responsável por fazer o jogo "entender" qual **GUID** gerado pelo servidor será utilizado pelo cliente (jogador). Sem ele, a conexão entre o player e a sala não se completa.

**Seu único desafio é:** Explorar o código, encontrar esse GUID e remover o comentário. Se você estuda o protocolo do Pangya, saberá exatamente onde ele está.

---

## 🗄 Conectividade e Banco de Dados

A persistência de dados utiliza **MSSQL** através de pontes de conexão robustas:

*   **Driver:** Utiliza `odbc32` ou `odbc64` (configurado via System DSN no Windows).
*   **Configuração Padrão (server.ini / config):**

```ini
DBENGINE    = MSSQL
DBIP        = pangya
DBNAME      = pangya
DBUSER      = pangya
DBPASS      = pangya
DBPORT      = 1433
DBLOG       = 0
```
### 🖼️ Capturas de Tela

   [![Test Stress](https://img.youtube.com/vi/bshhw92QnSQ/0.jpg)](https://www.youtube.com/watch?v=bshhw92QnSQ)
   [![Test Stress 2](https://img.youtube.com/vi/VhF3byU_azc/0.jpg)](https://www.youtube.com/watch?v=VhF3byU_azc) 
---

### 📜 Licença

Este projeto não possui uma licença formal. Use por sua conta e risco.  
**Não recomendado para uso comercial sem entendimento profundo do código.**

----

🤝 Agradecimentos e Créditos
Este projeto não seria possível sem a colaboração e o apoio da comunidade.

Aos Jogadores: Um agradecimento especial a todos os jogadores que estiveram e estão me ajudando no PangYa Fun, especialmente aqueles que dedicaram tempo reportando bugs e problemas no código! O feedback de vocês é o que move o projeto.

Luiz (@devluismk - Lead Developer & System Architect)

🔮 O Futuro: Versão 2.0+
A nova fase do projeto (v2.0) traz:

Modern .NET: Alta performance e portabilidade.

Linux/Docker: Deploy escalável em ambientes Ubuntu. 

Preservando a história do Pangya, um pacote de cada vez.
