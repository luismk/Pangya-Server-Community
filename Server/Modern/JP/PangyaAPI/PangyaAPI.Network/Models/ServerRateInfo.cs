using System;
using System.Collections.Generic;
using System.Numerics;

namespace PangyaAPI.Network.Models;

public class ServerRateInfo
{
    /// <summary>
    /// Taxa ou multiplicador do sistema de Raspadinha (Scratchy).
    /// </summary>
    public short Scratchy { get; set; }

    /// <summary>
    /// Rate de itens raros do Papel Shop (exclusivos).
    /// </summary>
    public short PapelShopRareItem { get; set; }

    /// <summary>
    /// Rate de itens normais/cookie do Papel Shop.
    /// </summary>
    public short PapelShopCookieItem { get; set; }

    /// <summary>
    /// Taxa de aparição ou recompensas de Tesouros (Treasure).
    /// </summary>
    public short Treasure { get; set; }

    /// <summary>
    /// Multiplicador da moeda do jogo (Pang) ganha nas partidas.
    /// </summary>
    public short Pang { get; set; }

    /// <summary>
    /// Multiplicador de Experiência (EXP) ganha pelos jogadores.
    /// </summary>
    public short Experience { get; set; }

    /// <summary>
    /// Rate de ganho de Maestria de Taco (Club Mastery).
    /// </summary>
    public short ClubMastery { get; set; }

    /// <summary>
    /// Rate de ocorrência de Chuva (Rain) nas partidas.
    /// </summary>
    public short Rain { get; set; }

    /// <summary>
    /// Rate relacionada ao Memorial Shop.
    /// </summary>
    public short MemorialShop { get; set; }

    /// <summary>
    /// Configuração ou tempo de duração do evento Grand Zodiac.
    /// </summary>
    public short GrandZodiacEventTime { get; set; }

    /// <summary>
    /// Config ativo do Angel Event(quit rate).
    /// </summary>
    public short AngelEvent { get; set; }

    /// <summary>
    /// Máscara de bits ou identificador das configurações ativas do Grand Prix Event.
    /// </summary>
    public short GrandPrixEvent { get; set; }

    /// <summary>
    /// Configuração do evento Golden Time.
    /// </summary>
    public short GoldenTimeEvent { get; set; }

    /// <summary>
    /// Config do evento de Recompensa de Login (Login Reward).
    /// </summary>
    public short LoginRewardEvent { get; set; }

    /// <summary>
    /// Configuração ou estado do Bot de Eventos do GM (GM Event Bot).
    /// </summary>
    public short GMEventBot { get; set; }

    /// <summary>
    /// Configuração do sistema de Cálculo Inteligente (Smart Calculation).
    /// </summary>
    public short SmartCalculation { get; set; }

    /// <summary>
    /// Configuração do evento World Tour.
    /// </summary>
    public short WorldTourEvent { get; set; }

    /// <summary>
    /// Configuração do evento de buracos específicos (Hole Event).
    /// </summary>
    public short HoleEvent { get; set; }

    /// <summary>
    /// Configuração do sistema de Missões (Mission Event).
    /// </summary>
    public short MissionEvent { get; set; }

    /// <summary>
    /// Config do Point Shop Event.
    /// </summary>
    public short PointShopEvent { get; set; }

    /// <summary>
    /// Conta quantos bits estão ativados (1) no evento GrandPrix usando contagem nativa otimizada por hardware.
    /// </summary>
    public uint CountBitGrandPrixEvent() =>
        (uint)BitOperations.PopCount((ushort)GrandPrixEvent);

    /// <summary>
    /// Retorna uma lista com os índices (1 a 16) dos bits ativados no GrandPrix.
    /// </summary>
    public List<uint> GetValueBitGrandPrixEvent()
    {
        var values = new List<uint>();
        for (int i = 0; i < 16; i++)
        {
            if (((GrandPrixEvent >> i) & 1) == 1)
            {
                values.Add((uint)(i + 1));
            }
        }
        return values;
    }

    /// <summary>
    /// Verifica se um determinado bit (tipo) está ativo no GrandPrix.
    /// </summary>
    public bool CheckBitGrandPrixEvent(int type)
    {
        if (type <= 0 || type > 16) return false;
        return ((GrandPrixEvent >> (type - 1)) & 1) == 1;
    }

    public override string ToString() =>
        $"GRAND_ZODIAC_EVENT_TIME={GrandZodiacEventTime}, " +
        $"GOLDEN_TIME_EVENT={GoldenTimeEvent}, " +
        $"ANGEL_EVENT={AngelEvent}, " +
        $"GRAND_PRIX_EVENT={GrandPrixEvent}, " +
        $"LOGIN_REWARD_EVENT={LoginRewardEvent}, " +
        $"BOT_GM_EVENT={GMEventBot}, " +
        $"SMART_CALCULATOR_SYSTEM={SmartCalculation}, " +
        $"SCRATCHY={Scratchy}, " +
        $"PAPEL_SHOP_RARE_ITEM={PapelShopRareItem}, " +
        $"PAPEL_SHOP_COOKIE_ITEM={PapelShopCookieItem}, " +
        $"TREASURE={Treasure}, " +
        $"PANG={Pang}, " +
        $"EXP={Experience}, " +
        $"CLUB_MASTERY={ClubMastery}, " +
        $"CHUVA={Rain}, " +
        $"MEMORIAL_SHOP={MemorialShop}";
}