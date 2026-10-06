using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models; 
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static PangyaAPI.Network.Models.CharacterInfo;
namespace PangyaAPI.Network.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]//861
    public class CharacterInfo
    {
        public CharacterInfo()
        {
            id = -1;
            Card_NPC = new uint[4];
            Card_Character = new uint[4];
            Card_Caddie = new uint[4];
            parts_id = new uint[24];
            parts_typeid = new uint[24];
            auxparts = new uint[5];
            UccIndexList = new byte[216];
            cut_in = new uint[4];
            pcl = new byte[5];
        }

        public CharacterInfo(uint typeID, byte _default_hair, byte _default_shirts)
        {
            id = -1;
            _typeid = _typeid;
            default_hair = default_hair;
            default_shirts = default_shirts;
            Card_NPC = new uint[4];
            Card_Character = new uint[4];
            Card_Caddie = new uint[4];
            parts_id = new uint[24];
            parts_typeid = new uint[24];
            auxparts = new uint[5];
            UccIndexList = new byte[216];
            cut_in = new uint[4];
            pcl = new byte[5];
        }

        public uint _typeid { get; set; }
        public int id { get; set; }
        public byte default_hair { get; set; }
        public byte default_shirts { get; set; }
        public byte gift_flag { get; set; }
        public byte purchase { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)] 
        public uint[] parts_typeid { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)] 
        public uint[] parts_id { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 216)] 
        public byte[] UccIndexList { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public uint[] auxparts { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] cut_in { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] pcl { get; set; }
        /// <summary>
        /// Mastery, que aumenta os slot do stats do character
        /// </summary>
        public uint mastery { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Card_Character { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Card_Caddie { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] Card_NPC { get; set; }

        public enum Stats : int
        {
            S_POWER,
            S_CONTROL,
            S_ACCURACY,
            S_SPIN,
            S_CURVE,
        }


        public byte AngelEquiped()
        {
            uint typeId = (_typeid & 0x000000FF);
            uint partNum;

            var angel = Global.gacha_angel_wings.FirstOrDefault(el => sIff.Instance.getItemCharIdentify(el) == typeId);
            if (angel != 0 && (partNum = sIff.Instance.getItemCharPartNumber(angel)) >= 0u && parts_typeid[partNum] == angel)
                return 1; // 3% icon rosa e drop chance A+ e Treasure point A+

            // Verifica se o item está na lista de Gacha Angel Wings
            var gachaAngel = Global.gacha_angel_wings.FirstOrDefault(el => sIff.Instance.getItemCharIdentify(el) == typeId);
            if (gachaAngel != 0 && (partNum = sIff.Instance.getItemCharPartNumber(gachaAngel)) >= 0u && parts_typeid[partNum] == gachaAngel)
                return 2; // Drop chance A+ e Treasure point A+

            return 0; // Nenhuma Angel Wings equipada                
        }

        public bool isEquipedPartSlotThirdCaddieCardSlot()
        {
            for (var i = 0; i < (parts_typeid.Length); ++i)
            {
                Part part;
                if (parts_id[i] != 0 && (part = sIff.Instance.findPart(parts_typeid[i])) != null)
                {
                    if (part._CardSlot.CaddieSlot != 0) // Tem um Part que Libera o terceiro Caddie Card Slot
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool isPartEquiped(uint _part_typeid, int _id)
        {

            if (_part_typeid == 0)
                return false;

            if (sIff.Instance.getItemCharIdentify(_part_typeid) != (_typeid & 0x000000FF))
                return false;

            var part_num = sIff.Instance.getItemCharPartNumber(_part_typeid);

            if (parts_typeid[part_num] != _part_typeid || parts_id[part_num] != _id)
                return false;

            return true;
        }
        public bool isPartEquiped(uint _part_typeid)
        {

            if (_part_typeid == 0)
                return false;

            if (sIff.Instance.getItemCharIdentify(_part_typeid) != (_typeid & 0x000000FF))
                return false;

            var part_num = sIff.Instance.getItemCharPartNumber(_part_typeid);

            if (parts_typeid[part_num] != _part_typeid)
                return false;

            return true;
        }


        public bool isAuxPartEquiped(uint _auxPart_typeid)
        {

            if (_auxPart_typeid == 0)
                return false;

            for (var i = 0; i < auxparts.Length; ++i)
            {
                if (auxparts[i] == _auxPart_typeid)
                {
                    return true;
                }
            }

            return false;
        }

        public void unequipPart(Part _part)
        { // Deseequipa o Part do character e coloca os Parts Default do Character no lugar

            if (_part == null)
            {

                Singleton<list_fifo_console_asyc<AppMessage>>.Instance.push(new AppMessage("[CharacterInfo::unequipPart][Error] IFF::Part* _part is invalid(null).", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }
            for (uint i = 0; i < (parts_typeid.Length); ++i)
            {

                if (_part.position_mask.getSlot((int)i))
                { // Coloca Def Parts

                    uint def_part = (uint)(((i | (uint)(_typeid << 5)) << 13) | 0x8000400);

                    var part_find = sIff.Instance.findPart(def_part);

                    parts_typeid[i] = (part_find != null && part_find.ID != 0) ? (uint)def_part : 0;
                    parts_id[i] = 0;
                }
            }
        }


        public void unequipPart(uint _typeid)
        {

            // Invalid Typeid
            if (_typeid == 0u)
                return;

            var part = sIff.Instance.findPart(_typeid);

            if (part != null && part.ID != 0)
            {
                unequipPart(part);
            }
            else
            {

                Singleton<list_fifo_console_asyc<AppMessage>>.Instance.push(new AppMessage("[CharacterInfo::unequipPart][Error][WARNIG] Part[TYPEID=" + Convert.ToString(_typeid) + "], mas ele nao existe no IFF_STRUCT do server, desequipa sem usar a funcao do character. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Não vai pegar todos os Slots que o Part ocupava para desequipar, desequipa o só onde tem o typeid   
                for (uint i = 0; i < (parts_typeid.Length); ++i)
                {

                    // Não vai pergar todos os 
                    if (parts_typeid[i] == _typeid)
                    { // Coloca Def Parts

                        uint def_part = (uint)(((i | (uint)(_typeid << 5)) << 13) | 0x8000400);

                        var part_find = sIff.Instance.findPart(def_part);

                        parts_typeid[i] = (part_find != null && part_find.ID != 0) ? (uint)def_part : 0;
                        parts_id[i] = 0;

                        break;
                    }
                }
            }

        }

        public void unequipAuxPart(uint _typeid)
        {

            // Invalid Typeid
            if (_typeid == 0u)
                return;

            for (var i = 0; i < (auxparts.Length); ++i)
            {

                if (auxparts[i] == _typeid)
                {

                    auxparts[i] = 0;

                    // Já desequipou sai
                    break;
                }
            }
        }


        public sbyte getSlotOfStatsFromsbyteEquipedPartItem(Stats __stat)
        {   // Get Slot of stats from Character equiped item

            sbyte value = 0;

            // Invalid Stats type, Unknown type Stats
            if (__stat > Stats.S_CURVE)
                return -1;

            for (var i = 0; i < 24; ++i)
            {
                Part part;
                if (parts_id[i] != 0 && (part = sIff.Instance.findPart(parts_typeid[i])) != null)
                    value += (sbyte)part.SlotStats.getSlot[(int)__stat];
            }

            return value;
        }

        public sbyte getSlotOfStatsFromCharEquipedPartItem(byte stat)
        {
            var __stat = (Stats)stat;
            int totalValue = 0; // Use int para evitar problemas de cast durante a soma

            if (__stat > Stats.S_CURVE)
                return -1;

            for (var i = 0; i < parts_typeid.Length; ++i)
            {
                // Verifica se o slot de equipamento não está vazio
                if (parts_id[i] != 0)
                {
                    var part = sIff.Instance.findPart(parts_typeid[i]);
                    if (part != null)
                    {
                        short slotAmount = (short)part.SlotStats.getSlot[(ushort)__stat];
                        totalValue += slotAmount;
                    
                    }
                }
            }

            return (sbyte)totalValue;
        }

        public sbyte getSlotOfStatsFromCharEquipedAuxPart(byte stat)
        {
            var __stat = (Stats)stat;
            sbyte value = 0;
            AuxPart aux_part = null;

            // Invalid Stats type, Unknown type Stats
            if (__stat > Stats.S_CURVE)
            {
                return -1;
            }

            for (var i = 0; i < auxparts.Length; ++i)
            {

                if (auxparts[i] != 0)
                {
                    aux_part = sIff.Instance.findAuxPart(auxparts[i]);
                    if (aux_part != null)
                        value += (sbyte)aux_part.slot[(int)__stat];
                }

            }

            return value;
        }

        public sbyte getSlotOfStatsFromSetEffectTable(byte stat)
        {
            var __stat = (Stats)stat;
            sbyte value = 0;
            int ret = 0;

            // Set Effect Table
            SetEffectTable iff_SET = null;

            // Ids que já foram
            List<uint> check_id = new List<uint>();

            // Invalid Stats type, Unknown type Stats
            if (__stat > Stats.S_CURVE)
            {
                return -1;
            }

            // Part Item                                                                                     
            for (var i = 0; i < (parts_typeid.Length); ++i)
            {

                if (parts_typeid[i] != 0)
                {

                    iff_SET = sIff.Instance.findFirstItemInSetEffectTable(parts_typeid[i]);

                    // O Item no Set Effect Table
                    if (iff_SET != null)
                    {
                        if (check_id.Count == 0 || !check_id.Contains(iff_SET.Index))
                        {

                            // add Login para o check
                            check_id.Add(iff_SET.Index);

                            // Verifica sem tem todos os itens da tabela de efeito equipados
                            ret = 1;
                            for (var j = 0; j < (iff_SET.item.ID.Length); ++j)
                            {

                                if (iff_SET.item.ID[j] != 0u)
                                {

                                    if (sIff.Instance.getItemGroupIdentify(iff_SET.item.ID[j]) == PangyaAPI.IFF.Flags.IFF_GROUP.PART)
                                    {

                                        if (!isPartEquiped(iff_SET.item.ID[j]))
                                        {
                                            // Não tem o outro item equipado
                                            ret = 0;

                                            break;
                                        }

                                    }
                                    else if (sIff.Instance.getItemGroupIdentify(iff_SET.item.ID[j]) == IFF_GROUP.AUX_PART)
                                    {

                                        if (!isAuxPartEquiped(iff_SET.item.ID[j]))
                                        {

                                            // Não tem o outro item equipado
                                            ret = 0;

                                            break;
                                        }

                                    }
                                }
                            }

                            // Não tem todos os itens equipados
                            if (ret == 0)
                            {
                                continue;
                            }

                            // Effect 6 ONE_ALL_STATS
                            foreach (var _el in iff_SET.effect.effect)
                            {

                                if (_el == (byte)AbilityEffect.ONE_IN_ALL_STATS)
                                    value++;
                            }

                            // Slot
                            value += (sbyte)iff_SET.Slot[(int)__stat];
                        }
                    }
                }
            }

            //AUX PART ITEM
            for (var i = 0; i < (auxparts.Length); ++i)
            {

                if (auxparts[i] != 0)
                {

                    iff_SET = sIff.Instance.findFirstItemInSetEffectTable(auxparts[i]);

                    // O Item no Set Effect Table
                    if (iff_SET != null)
                    {

                        if (check_id.Count == 0 || !check_id.Contains(iff_SET.Index))
                        {

                            // add Login para o check
                            check_id.Add(iff_SET.Index);

                            // Verifica sem tem todos os itens da tabela de efeito equipados
                            ret = 1;

                            for (var j = 0u; j < (iff_SET.item.ID.Length); ++j)
                            {

                                if (iff_SET.item.ID[j] != 0u)
                                {

                                    if (sIff.Instance.getItemGroupIdentify(iff_SET.item.ID[j]) == IFF_GROUP.PART)
                                    {

                                        if (!isPartEquiped(iff_SET.item.ID[j]))
                                        {

                                            // Não tem o outro item equipado
                                            ret = 0;

                                            break;
                                        }

                                    }
                                    else if (sIff.Instance.getItemGroupIdentify(iff_SET.item.ID[j]) == IFF_GROUP.AUX_PART)
                                    {

                                        if (!isAuxPartEquiped(iff_SET.item.ID[j]))
                                        {

                                            // Não tem o outro item equipado
                                            ret = 0;

                                            break;
                                        }

                                    }
                                }
                            }

                            // Não tem todos os itens equipados
                            if (ret == 0)
                            {
                                continue;
                            }

                            // Effect 6 ONE_ALL_STATS
                            foreach (var _el in iff_SET.effect.effect)
                            {

                                if (_el == (byte)AbilityEffect.ONE_IN_ALL_STATS)
                                    value++;
                            }

                            // Slot
                            value += (sbyte)iff_SET.Slot[(int)__stat];
                        }
                    }
                }
            }

            return value;
        }

        public int getSlotOfStatsFromCharEquiped(byte stat)
        {
            var _stat = (Stats)stat;

            int value = 0;
            Character character = null;

            if (_stat > Stats.S_CURVE)
            {
                Console.WriteLine($"[Character] Erro: Stat {stat} inválido ou fora do range.");
                return -1;
            }

            character = sIff.Instance.findCharacter(_typeid);

            if (character != null)
            {
                int charValue = character.PCL[stat];
                value += charValue;
            }
            else
            {
                Console.WriteLine($"[Character] Aviso: Carta ID {character} no slot {stat} não foi encontrada no IFF.");
            }
            return value;
        }

        public int getSlotOfStatsFromCharEquipedCard(byte stat)
        {
            var _stat = (Stats)stat;
             
            int value = 0;
            Card card = null;

            if (_stat > Stats.S_CURVE)
            {
                Console.WriteLine($"[CardLog] Erro: Stat {stat} inválido ou fora do range.");
                return -1;
            }

            for (uint i = 0; i < Card_Character.Length; ++i)
            {
                uint cardId = Card_Character[i];

                if (cardId != 0)
                {
                    card = sIff.Instance.findCard(cardId);

                    if (card != null)
                    {
                        int cardValue = card.c[stat];
                        value += cardValue; 
                    }
                    else
                    {
                        Console.WriteLine($"[CardLog] Aviso: Carta ID {cardId} no slot {i} não foi encontrada no IFF.");
                    }
                }
            } 
            return value;
        }

        public void initComboDef()
        {
            if (_typeid == 0)
                return;

            // Limpa
            Array.Clear(parts_typeid, 0, parts_typeid.Length); // array com 24 uints
            Array.Clear(parts_id, 0, parts_id.Length);         // array com 24 uints

            for (uint i = 0; i < parts_typeid.Length; ++i)
            {
                uint part_typeid = (((_typeid << 5 /*CharIdentify*/) | i) << 13 /*PartNum*/) | 0x8000400;
                var part_find = sIff.Instance.findPart(part_typeid);
                if (part_find != null && part_find.ID == part_typeid) // <-- aqui estava errado
                    parts_typeid[i] = part_typeid;
            }
        }


        /// <summary>
        /// size = 513 bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            using var p = new Packet();
            p.Write(_typeid);
            p.Write(id);
            p.Write(default_hair);
            p.Write(default_shirts);
            p.Write(gift_flag);
            p.Write(purchase);
            p.WriteUInt32(parts_typeid);
            p.WriteUInt32(parts_id);
            for (int i = 0; i < 216; i++)
                p.WriteByte(0);
            p.WriteUInt32(auxparts);
            p.WriteUInt32(cut_in);
            p.WriteBytes(pcl);
            p.WriteUInt32(mastery);
            p.WriteUInt32(Card_Character);
            p.WriteUInt32(Card_Caddie);
            p.WriteUInt32(Card_NPC);
            return p.GetBytes;
        }

        public CharacterInfo ToRead(Packet r)
        {
            _typeid = r.ReadUInt32();
            id = r.ReadInt32();
            default_hair = r.ReadByte();
            default_shirts = r.ReadByte();
            gift_flag = r.ReadByte();
            purchase = r.ReadByte();
            parts_typeid = r.ReadUInt32(24);
            parts_id = r.ReadUInt32(24);
            UccIndexList = r.ReadBytes(216);
            auxparts = r.ReadUInt32(5);
            cut_in = r.ReadUInt32(4);
            pcl = r.ReadBytes(5);
            mastery = r.ReadUInt32();
            Card_Character = r.ReadUInt32(4);
            Card_Caddie = r.ReadUInt32(4);
            Card_NPC = r.ReadUInt32(4);

            return this;
        }

    }
}
