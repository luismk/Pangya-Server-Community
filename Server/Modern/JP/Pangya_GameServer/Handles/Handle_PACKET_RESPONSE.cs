using Pangya_GameServer.Channels;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models.Game;
using Pangya_GameServer.Models;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.PacketFunc
{
    /// <summary>
    /// somente as respostas para o client
    /// </summary>
    public static class Handle_PACKET_RESPONSE
    {
        //////
       static int MAX_BUFFERPacket = 1000;
          
        public static Packet MakeGameRoomList(this List<GameRoomInfoModel> v_element, int option)
        {

            Packet p = new Packet();

            p.init_plain(0x47);
            p.WriteByte((byte)((option == 0) ? v_element.Count() : 1));              // count;
            p.WriteByte((byte)option);
            p.WriteInt16(-1);                 // Não sei bem, mas sempre peguei esse pacote com -1 aqui             
            for (var i = 0; i < v_element.Count(); ++i)
                p.WriteBytes(v_element[i].ToArray());

            return p;
        }

        public static List<Packet> MakePlayerLobby(this List<PlayerLobbyInfo> v_element, int option)
        {
            var responses = new List<Packet>();
            int elements = v_element.Count;
            int itensPorPacote = 20;

            // Divide a lista apenas se necessário
            var splitList = (elements * 200 < (1000 - 100))
                ? new List<List<PlayerLobbyInfo>> { v_element } // Envia tudo em um pacote
                : v_element.Select((item, index) => new { item, index })
                           .GroupBy(x => x.index / itensPorPacote)
                           .Select(g => g.Select(x => x.item).ToList())
                           .ToList();

            // Gera pacotes corretamente
            foreach (var lista in splitList)
            {
                var p = new Packet(0x046);
                p.WriteByte((byte)option);
                p.WriteByte((byte)lista.Count);

                foreach (var item in lista)
                {
                    if(item != null)
                        p.WriteBytes(item.ToArray());
                }

                responses.Add(p);
            }

            return responses;
        }

        public static Packet pacote11F(PlayerInfo pi, short tipo)
        {
            var p = new Packet();
            if (pi == null)
                throw new exception("Erro PlayerInfo *pi is null. packet_func::pacote11F()");

            p.init_plain(0x11F);

            p.WriteInt16(tipo);

            p.WriteBytes(pi.Tutorial.ToArray());
            return p;
        }

        public static Packet pacote1A9(int ttl_milliseconds/*time to live*/, int option = 1)
        {
            var p = new Packet(0x1A9);

            p.WriteByte((byte)option);

            p.WriteInt32(ttl_milliseconds);
            return p;
        }

        public static Packet pacote095(short sub_tipo, int option = 0, PlayerInfo pi = null)
        {
            var p = new Packet(0x95);

            p.WriteInt16(sub_tipo);

            if (sub_tipo == 0x102)
                p.WriteByte((byte)option);
            else if (sub_tipo == 0x111)
            {
                p.WriteInt32(option);

                if (pi == null)
                {
                    throw new exception("Erro PlayerInfo *pi is null. packet_func::pacote095()");
                }

                p.WriteUInt64(pi.Statistics.pang);
            }
            return p;
        }

        public static List<Packet> pacote25D(List<TrophySpecialInfo> v_element, int option)
        {
            var responses = new List<Packet>();
            int elements = v_element.Count;
            int itensPorPacote = 20;

            // Divide a lista apenas se necessário
            var splitList = (elements * 200 < (MAX_BUFFERPacket - 100))
                ? new List<List<TrophySpecialInfo>> { v_element } // Envia tudo em um pacote
                : v_element.Select((item, index) => new { item, index })
                           .GroupBy(x => x.index / itensPorPacote)
                           .Select(g => g.Select(x => x.item).ToList())
                           .ToList();

            // Gera pacotes corretamente
            foreach (var lista in splitList)
            {
                var p = new Packet(0x25D);
                p.WriteByte((byte)option);
                p.WriteUInt32((uint)lista.Count);
                p.WriteUInt32((uint)lista.Count);

                foreach (var item in lista)
                {
                    if (item != null)
                        p.WriteBytes(item.ToArray());
                }

                responses.Add(p);
            }

            return responses;
        }
        public static Packet pacote156(uint _uid, UserEquip _ue, byte season)
        {
            var p = new Packet(0x156);

            p.WriteByte(season);

            p.WriteUInt32(_uid);
            p.WriteBytes(_ue.ToArray());
            return p;
        }


        public static Packet pacote157(PlayerMemberInfo _mi, byte season)
        {
            var p = new Packet(0x157);

            p.WriteByte(season);
            p.WriteUInt32(_mi.UID);
            p.WriteBytes(_mi.ToArray(IncludeRoomID :true));
            p.WriteUInt32(_mi.UID);
            p.WriteUInt32(_mi.GuildWinPoints);
            return p;
        }

        public static Packet pacote158(uint _uid, PlayerUserStatistics _ui, byte season)
        {
            var p = new Packet(0x158);

            p.WriteByte((byte)season);
            p.WriteUInt32(_uid);
            p.WriteBytes(_ui.ToArray());//new BuildVersion 
            return p;
        }

        public static Packet pacote159(uint uid, TrophyInfo ti, byte season)
        {
            var p = new Packet(0x159);
            p.WriteByte(season);
            p.WriteUInt32(uid);
            p.WriteBytes(ti.ToArray());
            return p;
        }

        public static Packet pacote15A(uint uid, List<TrophySpecialInfo> vTei, byte season)
        {
            var p = new Packet(0x15A);
            p.WriteByte(season);
            p.WriteUInt32(uid);
            p.WriteUInt16((ushort)vTei.Count);

            foreach (var item in vTei)
                p.WriteBytes(item.ToArray());

            return p;
        }

        public static Packet pacote15B(uint uid, byte season)
        {
            var p = new Packet(0x15B);
            p.WriteByte(season);
            p.WriteUInt32(uid);
            p.WriteInt16(1); // Count desconhecido
            for (int i = 0; i < 60; i++)
                p.Write(i);
            return p;
        }

        public static Packet pacote15C(uint uid, List<MapStatisticsEx> vMs, List<MapStatisticsEx> vMsa, byte season)
        {
            var p = new Packet(0x15C);
            p.WriteByte(season);
            p.WriteUInt32(uid);
            p.WriteInt32(vMs.Count);

            foreach (var item in vMs)
                p.WriteBytes(item.ToArray());

            p.WriteInt32(vMsa.Count);

            foreach (var item in vMsa)
                p.WriteBytes(item.ToArray());

            return p;
        }

        public static Packet pacote15D(uint uid, GuildInfo gi)
        {
            var p = new Packet(0x15D);
            p.WriteUInt32(uid);
            p.WriteBytes(gi.ToArray());
            return p;
        }

        public static Packet pacote15E(uint uid, CharacterInfo ci)
        {
            var p = new Packet(0x15E);
            p.WriteUInt32(uid);
            p.WriteBytes(ci.ToArray());
            return p;
        }

        public static Packet pacote096(ulong cookie)
        {
            using (var p = new Packet(0x96))
            {
                p.WriteUInt64(cookie);
                return p;
            }
        }

        public static Packet pacote181(List<ItemBuffEx> v_element, int option = 0)
        {
            using (var p = new Packet(0x181))
            {
                p.WriteInt32(option);

                if (option == 0)
                {
                    p.WriteByte(v_element.Count());
                    for (int i = 0; i < v_element.Count; i++)
                        p.WriteBytes(v_element[i].ToArray());

                }
                else if (option == 2)
                {
                    p.WriteUInt32((uint)v_element.Count);

                    for (int i = 0; i < v_element.Count; i++)
                    {
                        p.WriteUInt32(v_element[i]._typeid);
                        p.WriteBytes(v_element[i].ToArray());

                    }
                }
                else
                    p.WriteByte(0);

                return p;
            }
        }

        public static Packet pacote13F(int option = 0)
        {
            using (var p = new Packet(0x13F))
            {
                p.WriteByte(option);
                return p;
            }
        }

        public static Packet pacote135()
        {
            using (var p = new Packet(0x135))
            {
                return p;
            }
        }

        public static Packet pacote136()
        {
            using (var p = new Packet(0x136))
            {
                return p;
            }
        }

        public static Packet pacote137(CardEquipManager v_element)
        {
            using (var p = new Packet(0x137))
            {
                p.WriteInt16(v_element.Count());
                foreach (var CardEquip in v_element)
                {
                    p.WriteBytes(CardEquip.ToArray());
                }
                return p;
            }
        }

        public static Packet pacote138(CardManager v_element, int option = 0)
        {
            using (var p = new Packet())
            {
                p.init_plain(0x138);
                p.WriteInt32(option);
                p.WriteUInt16((ushort)v_element.Count);
                foreach (var Card in v_element.Values)
                    p.WriteBytes(Card.ToArray());

                return p;
            }
        }

        public static Packet pacote1F()
        {
            using (var p = new Packet(0x01F))
            {
                return p;
            }
        }

        public static Packet pacote131(int option = 1)
        {
            if (!sTreasureHunterSystem.Instance.isLoad())
                sTreasureHunterSystem.Instance.load();

            using (var p = new Packet(0x131))
            {
                p.WriteByte(Convert.ToByte(option));
                p.WriteByte(Convert.ToByte(MS_NUM_MAPS)); 
            var _TreasureHunterInfo = sTreasureHunterSystem.Instance.getAllCoursePoint();

                foreach (var _TreasureHunter in _TreasureHunterInfo)
                {
                    if (_TreasureHunter.point < 1000)//abaixo ou sem dados preciso, fica zerado
                        _TreasureHunter.point = 1000;

                    p.WriteBytes(_TreasureHunter.ToArray());
                }
                return p;
            }
        }

        public static Packet pacote072(UserEquip ue)
        {
            var p = new Packet();

            p.init_plain(0x72);
            p.WriteBytes(ue.ToArray());
            return p;
        }

        public static Packet pacote0E1(MascotManager v_element, int option = 0)
        {
            var p = new Packet(0xE1); 
            p.WriteBytes(v_element.Build());
            return p;
        }
         

        public static Packet pacote21E(List<AchievementInfoEx> v_element, int option = 0)
        {
            var p = new Packet();
            try
            {
                p.init_plain(0x21E);
                p.WriteUInt32(0); // SUCCESS    
                p.WriteUInt32((uint)v_element.Count);
                p.WriteUInt32((uint)v_element.Count);
                foreach (var ai in v_element)
                {
                    p.WriteByte(ai.active);
                    p.WriteUInt32(ai._typeid);
                    p.WriteInt32(ai.id);
                    p.WriteInt32(ai.status);
                    p.WriteUInt32((uint)ai.v_qsi.Count);

                    foreach (var qsi in ai.v_qsi)
                    {
                        CounterItemInfo cii = null;

                        p.WriteUInt32(qsi._typeid);

                        if (qsi.counter_item_id > 0 && (cii = ai.findCounterItemById(qsi.counter_item_id)) != null)
                        {
                            p.WriteUInt32(cii._typeid);
                            p.WriteInt32(cii.id);
                        }
                        else
                        {
                            p.WriteZero(8);
                        }

                        p.WriteUInt32(qsi.clear_date_unix);
                    }
                }
                return p;
            }
            catch
            {
                return p;
            }
        }

        public static Packet pacote21D(List<CounterItemInfo> v_element, int option = 0)
        {
            var p = new Packet();
            try
            {
                p.init_plain(0x21D);
                p.WriteUInt32(0); // SUCCESS    
                p.WriteUInt32((uint)v_element.Count);
                p.WriteUInt32((uint)v_element.Count);
                foreach (var counter in v_element)
                {
                    p.WriteByte(counter.active);//;
                    p.WriteUInt32(counter._typeid);//
                    p.WriteInt32(counter.id);//
                    p.WriteInt32(counter.value);//
                }
                return p;
            }
            catch
            {
                return p;
            }
        }

        public static Packet pacote22D(List<AchievementInfoEx> v_element, int option = 0)
        {
            var p = new Packet();
            try
            {
                p.init_plain(0x22D);
                p.WriteUInt32(0); // SUCCESS
                p.WriteUInt32((uint)v_element.Count());
                p.WriteUInt32((uint)v_element.Count());

                foreach (var ai in v_element)
                {
                    p.WriteUInt32(ai._typeid);
                    p.WriteInt32(ai.id);
                    p.WriteUInt32((uint)ai.v_qsi.Count);
                    CounterItemInfo cii = null;
                    foreach (var qsi in ai.v_qsi)
                    {
                        p.WriteUInt32(qsi._typeid);
                        p.WriteInt32(qsi.counter_item_id > 0 && (cii = ai.findCounterItemById(qsi.counter_item_id)) != null ? cii.value : 0);
                        p.WriteUInt32(qsi.clear_date_unix);
                    }
                }
                return p;
            }
            catch
            {
                return p;
            }
        }

        public static Packet pacote22C(int option = 0)
        {
            var p = new Packet();
            try
            {
                p.init_plain(0x22C);
                p.WriteInt32(option); // SUCCESS

                return p;
            }
            catch
            {
                return p;
            }
        }

        public static Packet pacote073(List<WarehouseItemEx> v_element, int Count = 0, int option = 0)
        {
            var p = new Packet();
            p.init_plain(0x73);
            try
            {
                p.WriteInt16(Count);
                p.WriteInt16(option);
                foreach (var item in v_element)
                {
                    p.WriteBytes(item.ToArray());
                }
                return p;
            }
            catch
            {
                if (p.Size == 2)
                {
                    p.WriteUInt16(0);
                    p.WriteUInt16(0);
                }
                return p;
            }
        }

        public static Packet pacote071(CaddieManager v_element, int option = 0)
        {
            var p = new Packet();
            try
            {
                p.init_plain(0x71);
                p.WriteInt16((short)v_element.Count);
                p.WriteInt16((short)v_element.Count);
                foreach (var char_info in v_element.Values)
                {
                    p.WriteBytes(char_info.getInfo().ToArray());
                }
                return p;
            }
            catch (Exception)
            {
                return p;
            }
        }

        /// <summary>
        /// Send Packet for Info Characters(Personagens)
        /// </summary>
        /// <param Name="v_element">object list</param>
        /// <param Name="option">what?</param>
        /// <returns>obj using for write data</returns>
        public static Packet pacote070(CharacterManager v_element, int option = 0)
        {
            var p = new Packet();
            try
            {
                p.init_plain(0x70);
                p.WriteInt16((short)v_element.Count);
                p.WriteInt16((short)v_element.Count);
                foreach (var char_info in v_element.Values)
                {
                    p.WriteBytes(char_info.ToArray());
                }
                return p;
            }
            catch (Exception)
            {
                return p;
            }
        }

        /// <summary>
        /// packet 9D use Channel list!
        /// </summary>
        /// <param Name="v_element"></param>
        /// <param Name="build_s">true is server, false is chanell call!</param>
        /// <returns></returns>
        public static Packet pacote04D(List<Channel> v_element, bool build_s = false)
        {
            try
            {
                using (var p = new Packet())
                {
                    if (!build_s)
                        p.init_plain(0x4D); //Channel list!         

                    p.WriteByte(v_element.Count);
                    foreach (var channel in v_element)
                        p.WriteBytes(channel.getInfo().ToArray());

                    return p;
                }
            }
            catch (exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
               $"[packet_func::pacote04D][ErrorSystem] {ex.getFullMessageError()}",
               type_msg.CL_FILE_LOG_AND_CONSOLE));  
                using (var p = new Packet())
                { 
                        p.init_plain(0x4D); //Channel list!         

                    p.WriteInt16(0); 
                    return p;
                } 
            }
        }

        public static Packet pacote248(
            AttendanceRewardInfo ari,
            int option = 0)
        {
            using (var p = new Packet())
            {
                p.init_plain(0x248);  
                p.WriteInt32(option);
                p.WriteBytes(ari.ToArray());
                return p;
            }
        }

        public static Packet pacote249(
            AttendanceRewardInfo ari,
            int option = 0)
        {
            using (var p = new Packet())
            {
                p.init_plain(0x249);
                p.WriteInt32(option);
                p.WriteBytes(ari.ToArray());
                return p;
            }
        }

        //public static Packet pacote24E(ClubWorkShopEvent work_Shop_Event, int option = 0)
        //{
        //    using (var p = new Packet())
        //    {
        //        work_Shop_Event.Calc();

        //        p.init_plain(0x24E); // packet Login
        //        p.WriteInt32(option);                // subcode (fixo)
        //        p.WriteInt32(3000);   // quantos holes são exigidos por fase
        //        p.WriteInt32(0);      // total  de holes jogados

        //        p.WriteByte(3000 / 30);         // valor máximo da barra
        //        p.WriteByte(0);       // valor atual da barra 
        //        p.WriteByte(work_Shop_Event.barraMax);         // valor máximo da barra
        //        p.WriteByte(10);       // valor atual da barra

        //        return p;
        //    }
        //}
          

        public static Packet pacote257(uint _uid, List<TrophySpecialInfo> v_tegi, byte season)
        {
            using (var p = new Packet())
            {
                p.init_plain(0x257);

                p.WriteByte(season);
                p.WriteUInt32(_uid);

                p.WriteInt16((short)v_tegi.Count);
                foreach (var item in v_tegi)
                    p.WriteBytes(item.ToArray());
                return p;
            }
        }

        public static Packet pacote04E(int option, int _codeErrorInfo = 0)
        {
            /* Option Values
                * 1 Sucesso
                * 2 Channel Full
                * 3 Nao encontrou canal
                * 4 Nao conseguiu pegar informções do canal
                * 6 ErrorCode Info
                */
            using (var p = new Packet(0x4E))
            {
                p.WriteByte((byte)option);

                if (_codeErrorInfo != 0)
                    p.WriteInt32(_codeErrorInfo);
                return p;
            }
        }


        public static Packet pacote040(string nick, string msg, eChatMsg option)
        {

            if ((option == eChatMsg.CHAT_NORMAL || option == eChatMsg.CHAT_GM || option == eChatMsg.CHAT_MAX) && string.IsNullOrEmpty(nick))
                throw new exception("Error PlayerInfo *pi is null. packet_func::pacote040()");

            using (var p = new Packet(0x40))
            {
                p.WriteByte(option);

                if (option == 0 || (option == eChatMsg.CHAT_GM) || option == eChatMsg.CHAT_REFUSE_WHISPER)
                {
                    p.WriteString(nick);
                    if (option != eChatMsg.CHAT_REFUSE_WHISPER)
                        p.WriteString(msg);
                }
                return p;
            }
        }

        public static Packet pacote044(ServerInfo _si, eLoginAck option, Player pi = null, int valor = 0)
        {
            var p = new Packet(0x44);

            if (option == eLoginAck.ACK_LOGIN_OK && pi == null)
                throw new exception("Erro PlayerInfo *pi is null. packet_func::pacote044()");

            p.WriteByte(option);   // Option

            switch (option)
            {
                case eLoginAck.ACK_LOGIN_FAIL: // 1:
                    p.WriteByte(0);
                    break;
                case eLoginAck.ACK_AUTO_RECONNECT:
                    p.WriteByte(0);
                    break;
                case eLoginAck.ACK_UPDATE_LOGIN_UNIT:
                    p.WriteInt32(valor);
                    break; 
            }
            return p;
        }

        public static Packet pacote0B2(List<MsgOffInfo> v_element, int option = 0)
        {
            var p = new Packet();

            p.init_plain(0xB2);

            p.WriteInt32(2); // Não sei bem o que é, mas pode ser uma opção

            p.WriteInt32(option);

            p.WriteUInt32((uint)v_element.Count);

            foreach (MsgOffInfo i in v_element)
            {
                p.WriteBytes(i.ToArray());
            }

            return p;
        }

        public static Packet pacote0D4(CaddieManager v_element)
        {
            using (var p = new Packet())
            {
                p.init_plain(0xD4);
                p.WriteUInt32((uint)v_element.Count());
                foreach (var item in v_element.Values)
                    p.WriteBytes(item.getInfo().ToArray());

                return p;
            }
        }

        // Metôdos de auxílio de criação de pacotes


        public static Packet pacote210(

                List<MailBox> v_element,
                int option = 0)
        {
            var p = new Packet();

            p.init_plain(0x210);

            p.WriteInt32(option);

            p.WriteInt32(v_element.Count);

            for (var i = 0; i < v_element.Count; ++i)
            {
                p.WriteBytes(v_element[i].ToArray());
            }

            return p;
        }



        public static Packet pacote211(
            List<MailBox> v_element,
            int pagina,
            int paginas, int error = 0)
        {
            var p = new Packet();

            p.init_plain(0x211);

            p.WriteInt32(error);

            if (error == 0)
            {
                p.WriteInt32(pagina);
                p.WriteInt32(paginas);
                p.WriteInt32(v_element.Count);

                for (var i = 0; i < v_element.Count; ++i)
                    p.WriteBytes(v_element[i].ToArray());
            }

            return p;
        }

        public static Packet pacote214(int error = 0)
        {

            var p = new Packet();
            p.init_plain(0x214);

            p.WriteInt32(error);

            return p;
        }

        public static Packet pacote215(
            List<MailBox> v_element,
            int pagina,
            int paginas, int error = 0)
        {
            var p = new Packet();

            p.init_plain(0x215);

            p.WriteInt32(error);

            if (error == 0)
            {
                p.WriteInt32(pagina);
                p.WriteInt32(paginas);
                p.WriteUInt32((uint)v_element.Count);

                for (var i = 0; i < v_element.Count; ++i)
                {
                    p.WriteBytes(v_element[i].ToArray());
                }
            }

            return p;
        }

        public static Packet pacote216(
            List<stItem> v_item,
            int option = 0)
        {

            var p = new Packet();
            p.init_plain(0x216);

            p.WriteInt32((int)UtilTime.GetSystemTimeAsUnix());

            if (v_item.Count > 0)
            {
                p.WriteInt32(v_item.Count);

                foreach (stItem i in v_item)
                {

                    // Begin Base Item
                    p.WriteByte(i.type);
                    p.WriteUInt32(i._typeid);
                    p.WriteInt32(i.id);
                    p.WriteUInt32(i.flag_time);
                    p.WriteBytes(i.stat.ToArray());
                    p.WriteInt32((i.STDA_C_ITEM_TIME > 0) ? i.STDA_C_ITEM_TIME : i.STDA_C_ITEM_QNTD);

                    // End Base Item

                    if (i.type == 2)
                    {
                        try
                        {
                            p.WriteString(i.ucc.IDX);
                        }
                        catch (exception e)
                        {
                            if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.GAME_SERVER && ExceptionError.STDA_ERROR_DECODE(e.getCodeError()) == 3)
                            {
                                p.WriteInt16(0);
                            }
                            else
                            {
                                throw;
                            }
                        }

                        p.WriteUInt32(i.ucc.status);
                        p.WriteUInt32(i.ucc.seq);
                        p.WriteZero(5); // É o Unknown de cima
                    }
                }
            }
            else
            {
                p.WriteInt32(option);
            }

            return p;
        }

        public static Packet pacote10E(Last5PlayersGame l5pg)
        {
            var p = new Packet(0x10E);
            foreach (var p_log in l5pg.players.ToArray())
            {
                p.WriteBytes(p_log.ToArray());
            }
            return p;
        }

        public static Packet pacote0FC(List<ServerInfo> v_si)
        {
            var p = new Packet(0xFC);
            p.WriteByte((byte)v_si.Count);

            foreach (ServerInfo i in v_si)
                p.WriteBytes(i.ToArray());

            return p;
        }



        public static Packet pacote101(int option = 0)
        {
            var p = new Packet(0x101);
            p.WriteByte((byte)option);
            return p;
        }
        public static Packet pacote0B4(List<TrophySpecialInfo> v_element, int option = 0)
        {
            var p = new Packet();

            p.init_plain(0xB4);

            p.WriteInt16((short)option);

            p.WriteByte((byte)v_element.Count);

            foreach (TrophySpecialInfo i in v_element)
            {
                p.Write(i.id);
                p.Write(i._typeid);
                p.Write(i.qntd);
            }

            return p;
        }

        public static Packet pacote0F1(int option = 0)
        {
            var p = new Packet();

            p.init_plain(0xF1);

            p.WriteByte((byte)option);

            return p;
        }


        public static Packet pacote0F5()
        {
            var p = new Packet(0x0F5);
            return p;
        }


        public static Packet pacote0F6()
        {
            var p = new Packet(0x0F6);
            return p;
        }


        public static Packet pacote169(
           TrophyInfo ti,
            int option = 0)
        {
            var p = new Packet(0x169);

            p.WriteByte((byte)option);

            p.WriteBytes(ti.ToArray());

            return p;
        }

        public static Packet pacote09F(List<ServerInfo> v_server, List<Channel> v_channel)
        {
            using (var p = new Packet(0x09F))
            {
                p.WriteByte((byte)v_server.Count);

                for (var i = 0; i < v_server.Count; ++i)
                    p.WriteBytes(v_server[i].ToArray());

                p.WriteBytes(pacote04D(v_channel, true).GetBytes);
                return p;
            }
        }

        public static Packet pacote089(uint _uid = 0, byte season = 0, uint err_code = 1)
        {

            using (var p = new Packet(0x089))
            {
                p.WriteUInt32(err_code);
                if (err_code > 0)
                {
                    p.WriteByte(season);
                    p.WriteUInt32(_uid);
                }
                return p;
            }
        }

        public static Packet pacote211(List<MailBox> v_element, uint pagina, uint paginas, uint error = 0)
        {

            using (var p = new Packet(0x211))
            {
                p.WriteUInt32(error);

                if (error == 0)
                {
                    p.WriteUInt32(pagina);
                    p.WriteUInt32(paginas);
                    p.WriteInt32(v_element.Count);

                    for (int i = 0; i < v_element.Count; ++i)
                        p.WriteBytes(v_element[i].ToArray());
                }

                return p;
            }
        }

        public static Packet pacote212(EmailInfo ei, uint error = 0)
        {

            using (var p = new Packet(0x212))
            {
                p.WriteUInt32(error);

                if (error == 0)
                    p.WriteBytes(ei.ToArray());

                return p;
            }
        }


        public static Packet pacote06B(InventoryInfo pi, byte type, int err_code = 4)
        {

            if (pi == null)
            {
                throw new exception("Erro PlayerInfo *pi is null. packet_func::pacote06B()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                    1, 0));
            }
            var p = new Packet(0x06B);
            p.WriteByte(err_code); // Error Code, 4 Sucesso, diferente é erro
            p.WriteByte(type);

            if (err_code == 4)
            {
                switch (type)
                {
                    case 0: // Character Equipado Com os Parts Equipado
                        if (pi.UserEquippedItem.CharacterEquiped != null)
                            p.WriteBytes(pi.UserEquippedItem.CharacterEquiped.ToArray());
                        else
                            p.WriteZero(513);
                        break;
                    case 1: // Caddie Equipado
                        if (pi.UserEquippedItem.CaddieEquiped != null)
                            p.WriteInt32(pi.UserEquippedItem.CaddieEquiped.id);
                        else
                            p.WriteInt32(0);
                        break;
                    case 2: // Itens Equipáveis
                        p.WriteUInt32(pi.UserEquipment.item_slot);
                        break;
                    case 3: // Ball e Clubset Equipado
                        if (pi.UserEquippedItem.Ball_WI != null) // Ball
                            p.WriteUInt32(pi.UserEquippedItem.Ball_WI._typeid);
                        else
                            p.WriteInt32(0);

                        p.WriteInt32(pi.UserEquippedItem.ClubEquiped.id); // ClubSet ID
                        break;
                    case 4: // Skins
                        p.WriteUInt32(pi.UserEquipment.skin_typeid);
                        break;
                    case 5: // Only Chracter Equipado
                        if (pi.UserEquippedItem.CharacterEquiped != null)
                        {
                            p.WriteInt32(pi.UserEquippedItem.CharacterEquiped.id);
                        }
                        else
                        {
                            p.WriteZero(4);
                        }
                        break;
                    case 8: // Mascot Equipado
                        if (pi.UserEquippedItem.MascotEquiped != null)
                        {
                            p.WriteBytes(pi.UserEquippedItem.MascotEquiped.ToArray());
                        }
                        else
                        {
                            p.WriteZero(62);
                        }
                        break;
                    case 9: // Character Cutin Equipado
                        if (pi.UserEquippedItem.CharacterEquiped != null)
                        {
                            p.WriteInt32(pi.UserEquippedItem.CharacterEquiped.id);
                            p.WriteUInt32(pi.UserEquippedItem.CharacterEquiped.cut_in);
                        }
                        else
                        {
                            p.WriteZero(20);
                        }
                        break;
                    case 10: // Poster Equipado
                        p.WriteUInt32(pi.UserEquipment.poster);
                        break;
                }
            }

            return p;
        }

        public static Packet pacote1D4(string _AuthKeyLogin, int option = 0)
        {
            using (var p = new Packet(0x1D4))
            {
                p.WriteInt32(option);

                if (option == 0 && !string.IsNullOrEmpty(_AuthKeyLogin))
                    p.WriteString(_AuthKeyLogin);

                return p;
            }
        }

        public static Packet pacote04B(Player _session, byte _type,
         int error = 0, int _valor = 0)
        {

            var p = new Packet(0x4B);

            if (_session == null)
                throw new exception("Error _session is null. Em packet_func::pacote04B()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                       1, 0));

            if (!_session.getState())
                throw new exception("Error _session nao esta mais connectado. Em packet_func::pacote04B()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                       2, 0));


            p.WriteInt32(error);

            if (error == 0)
            {
                p.WriteByte(_type);

                p.WriteInt32(_session.ConnectionID);

                switch (_type)
                {
                    case 1: // Caddie
                        if (_session.Inventory.UserEquippedItem.CaddieEquiped != null)
                        {
                            p.WriteBytes(_session.Inventory.UserEquippedItem.CaddieEquiped.ToArray());
                        }
                        else
                        {
                            p.WriteZero(25);
                        }
                        break;
                    case 2: // Ball(Comet)
                        if (_session.Inventory.UserEquippedItem.Ball_WI != null)
                        {
                            p.WriteUInt32(_session.Inventory.UserEquippedItem.Ball_WI._typeid);
                        }
                        else
                        {
                            p.WriteZero(4);
                        }
                        break;
                    case 3: // ClubSet
                        p.WriteBytes(_session.Inventory.UserEquippedItem.ClubEquiped.ToArray());
                        break;
                    case 4: // Character
                        if (_session.Inventory.UserEquippedItem.CharacterEquiped != null)
                        {
                            p.WriteBytes(_session.Inventory.UserEquippedItem.CharacterEquiped.ToArray());
                        }
                        else
                        {
                            p.WriteZero(513);
                        }
                        break;
                    case 5: // Mascot
                        if (_session.Inventory.UserEquippedItem.MascotEquiped != null)
                        {
                            p.WriteBytes(_session.Inventory.UserEquippedItem.MascotEquiped.ToArray());
                        }
                        else
                        {
                            p.WriteZero(62);
                        }
                        break;
                    case 6: // Itens Active 1 = Jester big cabeça, 2 = Hermes velocidade x2, 3 = Twilight Fogos na cabeça
                        {
                            p.WriteInt32(_valor);

                            if (_valor == (int)ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_TWILIGHT)
                            {
                                p.WriteInt32(1); // Ativa Fogos
                            }
                            else
                            {

                                if (_session.Inventory.UserEquippedItem.CharacterEquiped != null)
                                {
                                    var it = _session.UserInfo.CharacterLoungeStates.FirstOrDefault(c => c.Key == _session.Inventory.UserEquippedItem.CharacterEquiped.id);

                                    if (it.Value == null)
                                    {

                                        _smp.LogManager.Instance.push(new AppMessage("[Channel::pacote04B][Error] Normal[UID=" + Convert.ToString(_session.Inventory.uid) + "] nao tem os estados do character na Lounge. Criando um novo para ele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                                        // Add New State Character Lounge
                                        _session.UserInfo.CharacterLoungeStates.Add(_session.Inventory.UserEquippedItem.CharacterEquiped.id, new StateCharacterLounge());

                                        it = _session.UserInfo.CharacterLoungeStates.FirstOrDefault(c => c.Key == _session.Inventory.UserEquippedItem.CharacterEquiped.id);
                                    }

                                    switch ((ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT)_valor)
                                    {
                                        case ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_BIG_HEAD: // Jester (Big head)
                                            p.WriteFloat(it.Value.scale_head);
                                            break;
                                        case ChangePlayerItemRoom.stItemEffectLounge.TYPE_EFFECT.TE_FAST_WALK: // Hermes (Velocidade x2)
                                            p.WriteFloat(it.Value.walk_speed);
                                            break;
                                    }
                                }
                            }
                        }
                        break;
                    case 7: // _session game
                            // Nada Aqui
                        {
                        }
                        break;
                    default:
                        throw new exception("Error Type desconhecido. Em packet_func::pacote04B()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                            3, 0));
                }
            }

            return p;
        }

        public static Packet pacote1AD(string webKey, int option)
        {
            using (var p = new Packet(0x1AD))
            {
                p.WriteInt32(option);

                if (webKey.empty())
                    p.WriteInt16(0);
                else
                    p.WriteString(webKey);

                return p;
            }
        }

        public static Packet pacote102(PlayerInfo pi, CouponGacha cg)
        {
            if (pi == null)
            {
                throw new exception("[packet_func::pacote12][Error] PlayerInfo *pi is null.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                    1, 0));
            }
            using (var p = new Packet(0x102))
            {
                p.WriteInt32(cg.normal_ticket);
                p.WriteInt32(cg.partial_ticket);

                p.WriteUInt64(pi.Statistics.pang);
                p.WriteUInt64(pi.Cookie);


                return p;
            }
        }

        public static Packet pacote144(int option = 0)
        {
            var p = new Packet(0x144);
            p.WriteByte((byte)option);

            return p;
        }

        public static Packet pacote09A(int ulCapability)
        {        // UPDATE ON GAME
            var p = new Packet(0x9A);

            p.WriteInt32(ulCapability);
            return p;
        }

        //tested, melhorar com tempo@@@@ 
        /// <summary>
        /// Constrói e envia o pacote de rede 0x48 (gerenciamento de ações e listagem de jogadores na sala) do servidor Pangya.
        /// </summary>
        /// <param Name="p">Instância do pacote binário a ser construído.</param>
        /// <param Name="_session">Sessão atual do jogador que receberá ou disparará a ação.</param>
        /// <param Name="playerInfoList">Lista contendo as informações dos jogadores presentes na sala.</param>
        /// <param Name="option">Código de opção/ação combinada (bits de ação e flags de tamanho/formato).</param>
        /// <returns>Retorna <c>true</c> indicando que o processamento do pacote foi concluído com sucesso.</returns>
        public static bool MakePlayerRoomInfo(Packet p, Player _session, List<PlayerRoomInfo> playerInfoList, int option = 0)
        {
            int actionCode = option & 0xFF;
            bool isExtendedFormat = (option & 0x100) != 0;

            TPlayerRoom_Action opt = (TPlayerRoom_Action)actionCode;
            Debug.WriteLine($"MakePlayerRoomInfo => enum: {opt}, code: {actionCode}, extendedFormat: {isExtendedFormat}");

            try
            {
                p.init_plain(0x48); 
                if (actionCode == 2)
                {
                    p.WriteSByte((sbyte)option);
                    p.WriteInt16(-1);
                    p.WriteInt32(_session.ConnectionID);
                    _session.Send(p);
                    return true;
                }
                p.WriteByte((byte)option);
                p.WriteInt16(-1);
                if (actionCode == 0 || actionCode == 5)
                {
                    p.WriteByte((byte)playerInfoList.Count);
                }
                else if (actionCode == 3)
                {
                    p.WriteInt32(_session.ConnectionID);
                }
                else if (actionCode == 7)
                {
                    p.WriteByte((byte)playerInfoList.Count);
                }
                 
                // Serializa cada jogador da lista sequencialmente
                foreach (var sessionRoom in playerInfoList)
                {
                    p.WriteBytes(sessionRoom.ToArray(!isExtendedFormat && actionCode != 3));
                }

                // Marca o byte finalizador da lista de jogadores
                p.WriteByte(0);
                _session.Send(p);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Error] Falha ao processar o pacote 0x48: {ex.Message}");
            }

            return true;
        }

        public static Packet pacote04A(GameRoomInfoModel _ri, short option)
        {
            var p = new Packet();
            p.init_plain(0x4A);

            p.WriteInt16(_ri.RoomID);      // pode ser valor constante da sala ou o número, ainda não descobri, sempre passa -1 des vezes que vi
            // Tem que ser o RoomType, por que ele é o que o cliente quer,
            // o Type(real) só server conhece para poder fazer o jogo direito 
            p.WriteBytes(_ri.ToArrayEx());
            return p;
        }

        public static Packet pacote049(Room _room, TGAME_CREATE_RESULT option = 0)
        {
            try
            {
                if (option != TGAME_CREATE_RESULT.CREATE_GAME_RESULT_SUCCESS && _room == null)
                    throw new exception("Error _room is null. EM packet_func::pacote049()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 3, 0));

                var p = new Packet();

                p.init_plain(0x49);
                if (option == 0)//sucess
                {
                    p.WriteInt16((short)option);
                    p.WriteBytes(_room.GetInfo().ToArray());
                }
                else
                    p.WriteByte((byte)option);//write error code packet 
                return p;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static Packet pacote225(DailyQuestInfoUser _dq, List<RemoveDailyQuestUser> _delete_quest, int option = 0)
        {
            if (_delete_quest == null)
            {
                _delete_quest = new List<RemoveDailyQuestUser>();
            }

            var p = new Packet(0x225); 
            p.WriteInt32(option);
            if (option == 0)
            {
                // Convert to UTC send to client
                p.WriteUInt32((uint)UtilTime.TzLocalUnixToUnixUTC(_dq.current_date));//data em unix
                p.WriteUInt32((uint)UtilTime.TzLocalUnixToUnixUTC(_dq.accept_date));//data em unix 
                p.WriteUInt32(_dq.count);
                p.WriteUInt32(_dq._typeid); //a quest sao 3 
                p.WriteInt32(_delete_quest.Count);
                foreach (RemoveDailyQuestUser it in _delete_quest)//quest of delete
                {
                    p.WriteInt32(it.id);
                }
            }
            return p;
        }

        public static Packet pacote226(List<AchievementInfoEx> v_element, int option = 0)
        {
            var p = new Packet();
            p.init_plain(0x226);

            p.WriteInt32(option);

            if (option == 0)
            {
                if (v_element.Count > 0)
                {
                    CounterItemInfo cii = null;

                    p.WriteInt32(v_element.Count);

                    foreach (AchievementInfoEx i in v_element)
                    {
                        p.WriteByte(i.active);
                        p.WriteUInt32(i._typeid);
                        p.WriteInt32(i.id);
                        p.WriteInt32(i.status);
                        p.WriteUInt32((uint)i.v_qsi.Count);
                        foreach (var ii in i.v_qsi)
                        {
                            p.WriteUInt32(ii._typeid);

                            if (ii.counter_item_id > 0 && (cii = i.findCounterItemById(ii.counter_item_id)) != null)
                            {
                                p.WriteUInt32(cii._typeid);
                                p.WriteInt32(cii.id);
                            }
                            else // não tem o counter Login e nem o typeid
                            {
                                p.WriteZero(8);
                            }

                            p.WriteUInt32(ii.clear_date_unix);
                        }
                    }
                }
            }
            else
            {
                p.WriteInt32(0);
            }

            return p;
        }

        public static Packet pacote227(List<AchievementInfoEx> v_element,
            int option = 0)
        {

            var p = new Packet();
            p.init_plain(0x227);

            p.WriteInt32(option);

            if (v_element.Count > 0)
            {

                p.WriteInt32(v_element.Count);

                foreach (var el in v_element)
                {
                    p.WriteInt32(el.id);
                }
            }
            else
            {
                p.WriteInt32(0);
            }

            return p;
        }

        public static Packet pacote228(List<AchievementInfoEx> v_element, int option = 0)
        {

            var p = new Packet();
            p.init_plain(0x228);

            p.WriteInt32(option);

            if (option == 0)
            {
                if (v_element.Count > 0)
                {
                    p.WriteInt32(v_element.Count);

                    foreach (var el in v_element)
                    {
                        p.WriteInt32(el.id);
                    }
                }
            }

            return p;
        }



        public static Packet pacote04C(int option)
        {
            var p = new Packet();
            p.init_plain(0x4C);

            p.WriteInt16((short)option);
            return p;
        }

        public static Packet pacote0AA(Player _session, List<stItem> v_item)
        {
            if (_session == null || !_session.getState())
                throw new exception("Error _session nao esta conectado. Em packet_func::pacote0AA()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 50, 0));

            var p = new Packet();
            if (v_item.Count() > 0)
            {
                p.init_plain(0xAA);

                p.WriteUInt16((ushort)v_item.Count()); // Count, ele só manda de 1 msm não manda todos, não sei por que

                for (var i = 0; i < v_item.Count(); ++i)
                {

                    p.WriteUInt32(v_item[i]._typeid);
                    p.WriteInt32(v_item[i].id);
                    p.WriteInt16(v_item[i].STDA_C_ITEM_TIME);
                    p.WriteByte(v_item[i].flag_time);
                    p.WriteUInt16((ushort)v_item[i].stat.qntd_dep);
                    p.WriteTime(v_item[i].date.date.sysDate[1]);
                    p.WriteString(v_item[i].ucc.IDX, 9);

                    // Aqui é a reflexão desse pacote, usa no ticket report
                    if (v_item[i]._typeid == 0x1A000042)
                    {
                        p.WriteInt16(v_item[i].STDA_C_ITEM_TICKET_REPORT_ID_HIGH);
                        p.WriteInt16(v_item[i].STDA_C_ITEM_TICKET_REPORT_ID_LOW);

                        p.WriteTime(v_item[i].date.date.sysDate[1]);
                    }
                }

                p.WriteUInt64(_session.UserInfo.Statistics.pang);
                p.WriteUInt64(_session.UserInfo.Cookie);
            }
            return p;
        }

        public static Packet pacote196(Player _session, StateCharacterLounge stateCharacterLounge)
        {
            var p = new Packet(0x196);

            p.WriteInt32(_session.ConnectionID);//coloquei 1 pra testar

            p.WriteBytes(stateCharacterLounge.ToArray());

            return p;
        }

        public static Packet pacote26D(int _unix_end_date)
        {
            var p = new Packet(0x26D);

            p.WriteInt32(_unix_end_date);

            return p;
        } 
    }
}
