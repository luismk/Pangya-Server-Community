using System;

namespace Pangya_GameServer.PangyaEnums
{
    [Flags]
    public enum CapabilityFlags : uint
    {
        PLAYER = 0,                 // 0x01 - Inteligência Artificial Modo
        COMPUTER = 1 << 0,                 // 0x01 - Inteligência Artificial Modo
        GALLERY = 1 << 1,                  // 0x02 - Unknown
        GAME_MASTER = 1 << 2,              // 0x04 - GM(Game Master)
        OBSERVER = (1 << 3) | (1 << 2) | (1 << 1), // 0x0E - OBSERVER
        GM_EDIT_SITE = 1 << 3,             // 0x08 - Pode editar a parte adm do site
        BLOCK_GIVE_ITEM_GM = 1 << 4,       // 0x10 - Bloqueia o GM de enviar itens(MC)
        GOD = 1 << 5,                      // 0x20 - Unknown(somente s4)
        MOD_SYSTEM_EVENT = 1 << 6,         // 0x40 - Moderador de sistema
        GM_NORMAL = 1 << 7,                // 0x80 - GM player normal(PCBANG)
        BLOCK_GIFT_SHOP = 1 << 8,          // 0x100 - Bloqueia envio de presente no shop
        LOGIN_TEST_SERVER = 1 << 9,        // 0x200 - Login em servidores de teste
        MANTLE = 1 << 10,                  // 0x400 - Entra em servidores escondidos
        DEVELOPER = 1 << 11,               // 0x800 - Unknown
        MATCH_PLAYER = 1 << 12,            // 0x1000 - Unknown (Faltando no original)
        UNKNOWN_5 = 1 << 13,               // 0x2000 - Unknown (Faltando no original)
        PREMIUM_USER = 1 << 14,            // 0x4000 - Usuário premium
        TITLE_GM = 1 << 15                 // 0x8000 - Título de GM
    }
}
