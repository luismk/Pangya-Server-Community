using Pangya_GameServer.Channels;
using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Roms;
using Pangya_GameServer.Roms.GameBase;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.IFF.Regions.JP.Models.IFF;
using PangyaAPI.Network;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using snmdb;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static Pangya_GameServer.Models.DefineConstants;
using static Pangya_GameServer.ModelsGameInfo;

namespace Pangya_GameServer.Roms.GameBase
{
    public abstract partial class Game 
    {
        public short getNumHole(Player session)
        {

            if (!session.getState())
            {
                throw new exception("[GameBase::getNumHole][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                    1, 0));
            }

            // Valor padrão
            short hole = 0;

            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception("[GameBase::" + "requestPlace][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou pegar o lugar[Hole] do player no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            if (pgi.hole != 255)
            {

                hole = Course.findHoleSeq(pgi.hole);

                if (hole == -1)
                {
                    // Valor padrão
                    hole = 0;

                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::requestPlace][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou pegar a sequencia do hole[NUMERO=" + Convert.ToString(pgi.hole) + "], mas ele nao encontrou no CourseIndex do game na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

            }
            else if (pgi.init_first_hole) // Só cria mensagem de log se o player já inicializou o primeiro hole do jogo e tem um valor inválido no pgi->hole (não é uma sequência de hole válida)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::requesPlace][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou pegar o hole[NUMERO=" + Convert.ToString(pgi.hole) + "] em que o player esta na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "], mas ele esta carregando o CourseIndex ou tem algum error.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return hole;
        }

        public void RequestActiveAutoCommand(Player session, Packet packet)
        {
            if (!session.getState())
            {
                throw new exception("[GameBase::RequestActiveAutoCommand][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }

            if (packet == null)
            {
                throw new exception("[GameBase::RequestActiveAutoCommand][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    6, 0));
            }

            Packet p = new Packet();

            try
            {

                var pgi = GetPlayerInfo(session);
                if (pgi == null)
                {
                    throw new exception("[GameBase::RequestActiveAutoCommand][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou ativar var Command no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                        1, 4));
                }

                if (!pgi.premium_flag)
                { // (não é)!PREMIUM USER

                    var pWi = session.Inventory.FindWarehouseItemByTypeid(AUTO_COMMAND_TYPEID);

                    if (pWi == null)
                    {
                        throw new exception("[GameBase::RequestActiveAutoCommand][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar o var Command Item[TYPEID=" + Convert.ToString(AUTO_COMMAND_TYPEID) + "], mas ele nao tem o item. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 0x550001));
                    }

                    if (pWi.STDA_C_ITEM_QNTD < 1)
                    {
                        throw new exception("[GameBase::RequestActiveAutoCommand][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar o var Command Item[TYPEID=" + Convert.ToString(AUTO_COMMAND_TYPEID) + "], mas ele nao tem quantidade suficiente do item[QNTD=" + Convert.ToString(pWi.STDA_C_ITEM_QNTD) + ", QNTD_REQ=1]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            2, 0x550002));
                    }

                    var it = pgi.used_item.v_passive.FirstOrDefault(c=> c.Key == pWi._typeid);

                    if (it.Value == null)
                    {
                        throw new exception("[GameBase::RequestActiveAutoCommand][Error] Normal[UID = " + Convert.ToString(session.UserInfo.UID) + "] tentou ativar var Command, mas ele nao tem ele no item passive usados do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            13, 0));
                    }

                    if ((short)it.Value.count >= pWi.STDA_C_ITEM_QNTD)
                    {
                        throw new exception("[GameBase::RequestActiveAutoCommand][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar var Command, mas ele ja usou todos os var Command. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.TOURNEY_BASE,
                            14, 0));
                    }

                    // Add +1 ao item passive usado
                    it.Value.count++;
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestActiveAutoCommand][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // !@ Não sei o que esse pacote faz, não encontrei no meu antigo pangya
                // Resposta Error
                p.init_plain(0x22B);

                var errorCode = ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) == (uint)STDA_ERROR_TYPE.GAME ? ExceptionError.STDA_SOURCE_ERROR_DECODE(e.getCodeError()) : 0x550001;
                p.WriteUInt32(errorCode);

                session.Send(p);
            }
        }

        public void RequestActiveAssistGreen(Player session, Packet packet)
        {
            if (!session.getState())
            {
                throw new exception("[GameBase::RequestActiveAssistGreen][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }

            if (packet == null)
            {
                throw new exception("[GameBase::RequestActiveAssistGreen][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    6, 0));
            }

            Packet p = new Packet();

            try
            {

                uint itemtypeid = packet.ReadUInt32();

                if (itemtypeid == 0)
                    throw new exception("[GameBase::RequestActiveAssistGreen][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Assist[TYPEID=" + Convert.ToString(itemtypeid) + "] do Green, mas o item_typeid is invalid(zero). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            1, 0x5200101));


                if (itemtypeid != ASSIST_ITEM_TYPEID)
                    throw new exception("[GameBase::RequestActiveAssistGreen][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Assist[TYPEID=" + Convert.ToString(itemtypeid) + "] do Green, mas o item_typeid esta errado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                               1, 0x5200101));

                var pWi = session.Inventory.FindWarehouseItemByTypeid(itemtypeid);

                if (pWi == null)
                    throw new exception("[GameBase::RequestActiveAssistGreen][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Assist[TYPEID=" + Convert.ToString(itemtypeid) + "] do Green, mas o Assist Mode do player nao esta ligado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            2, 0x5200102));

                if (session.UserInfo.AssistFlag)
                { 
                    // Resposta para Active Assist Green
                    p.init_plain(0x26B);//get assist 
                    p.WriteUInt32(0); // OK 
                    p.WriteUInt32(pWi._typeid);
                    p.WriteUInt32(session.UserInfo.UID); 
                    session.Send(p);
                }
                else
                    throw new exception("[GameBase::RequestActiveAssistGreen][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou ativar Assist[TYPEID=" + Convert.ToString(itemtypeid) + "] do Green, mas o Assist Mode do player nao esta ligado. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                       2, 0x5200102));
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestActiveAssistGreen][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x26B); 
                p.WriteUInt32(1); 
                session.Send(p);
            }
        }

        public void RequestReadSyncShotData(Player session, Packet packet, ref ShotSyncData ssd)
        {
            try
            {
                //check player connection
                if (!session.getState())
                    throw new exception("[GameBase::RequestReadSyncShotData][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                           1, 0));

                //check packet
                if (packet == null)
                    throw new exception("[GameBase::RequestReadSyncShotData][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            6, 0));

                //check size packet — simplificado: < 38 || > 38 equivale a != 38
                if (packet.Message.Length != 38)
                    throw new exception($"[GameBase::RequestReadSyncShotData][Error] Tamanho inválido do pacote: esperado 38, recebido {packet.Message.Length}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                       7, 0));

                //decript shot 
                ssd = DecryptShot(packet.Message);

                if (ssd == null)
                    throw new exception($"[GameBase::RequestReadSyncShotData][Error] DecryptShot retornou null para pacote de tamanho {packet.Message.Length}", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                           8, 0));

                var oid = ssd.oid;

                if (ssd.oid == -1)
                    throw new exception($"[GameBase::RequestReadSyncShotData][Error] Player no exist:" + oid, ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                              9, 0));

                if (ssd.pang > 37000u)
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestReadSyncShotDate][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] pode esta usando hack, PANG[" + Convert.ToString(ssd.pang) + "] maior que 40k. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                if (ssd.bonus_pang > 10000u)
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestReadSyncShotDate][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] pode esta usando hack, BONUS PANG[" + Convert.ToString(ssd.bonus_pang) + "] maior que 10k. Hacker ou Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestReadSyncShotData][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        // Report Game
        public void RequestPlayerReportChatGame(Player session, Packet packet)
        {
            if (!session.getState())
            {
                throw new exception("[GameBase::RequestPlayerReportChatGame][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }
            ;
            if (packet == null)
            {
                throw new exception("[GameBase::RequestPlayerReportChatGame][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    6, 0));
            }

            Packet p = new Packet();

            try
            {

                // Verifica se o player já reportou o jogo
                var it = PlayerReportGame.FirstOrDefault(c => c.Key == session.UserInfo.UID);

                if (it.Key != 0)
                {

                    // Player já reportou o jogo
                    p.init_plain(0x94);

                    p.WriteByte(1); // Player já reportou o jogo

                    session.Send(p);
                }
                else
                { // Primeira vez que o palyer report o jogo

                    // add ao mapa de UID de player que reportaram o jogo
                    PlayerReportGame[session.UserInfo.UID] = session.UserInfo.UID;

                    // Faz Log de quem está na sala, quando pangya, o update enviar o chat log verifica o chat
                    // por que parece que o pangya não envia o chat, ele só cria um arquivo, acho que quem envia é o update
                    string log = "";

                    foreach (var el in Players)
                    {
                        if (el != null)
                        {
                            // Corrigido: era session.PlayerUserStatistics.UID — deve ser el.PlayerUserStatistics.UID para cada player
                            log = log + "UID: " + Convert.ToString(el.UserInfo.UID) + "\tID: " + el.UserInfo.Login + "\tNICKNAME: " + el.UserInfo.NickName + "\n";
                        }
                    }

                    // Log
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestPlayerReportChatGame][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] reportou o chat do jogo na sala[NUMERO=" + Convert.ToString(RoomInfo.RoomID) + "] Log{" + log + "}", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    // Reposta para o cliente
                    p.init_plain(0x94);

                    p.WriteByte(0); // Sucesso

                    session.Send(p);
                }

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestPlayerReportChatGame][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                p.init_plain(0x94);

                p.WriteByte(1); // 1 já foi feito report do jogo por esse player

                session.Send(p);
            }
        }

        public virtual DropItemRet RequestInitDrop(Player session)
        {

            if (!sDropSystem.Instance.isLoad())
            {
                sDropSystem.Instance.load();
            }

            DropItemRet dir = new DropItemRet();

            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception("[GameBase::RequestInitDrop][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou inicializar drop do hole no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            DropSystem.stCourseInfo ci = new DropSystem.stCourseInfo();

            var hole = Course.findHole(pgi.hole);

            if (hole == null)
            {
                throw new exception("[GameBase::RequestInitDrop][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou inicializar Drop System do hole[NUMERO=" + Convert.ToString(pgi.hole) + "] no jogo, mas nao encontrou o hole no CourseIndex do game. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    200, 0));
            }

            // Init Course Info Drop System
            ci.artefact = RoomInfo.ItemIDArtifact;
            ci.char_motion = pgi.char_motion_item;
            ci.course = (byte)(hole.getCourse() & 0x7F); // Course do Hole, Por que no SSC, cada hole é um CourseIndex
            ci.hole = pgi.hole;
            ci.seq_hole = (byte)Course.findHoleSeq(pgi.hole);
            ci.qntd_hole = RoomInfo.HoleCount;
            ci.rate_drop = pgi.used_item.rate.drop;

            if (session.Inventory.UserEquippedItem.CharacterEquiped != null && session.UserInfo.Statistics.getQuitRate() < GOOD_PLAYER_ICON)
            {
                ci.angel_wings = session.Inventory.UserEquippedItem.CharacterEquiped.AngelEquiped();
            }
            else
            {
                ci.angel_wings = 0;
            }

            // Artefact Pang Drop
            if (RoomInfo.HoleCount == ci.seq_hole && RoomInfo.HoleCount == 18)
            { // Ultimo Hole, de 18h Game
                var art_pang = sDropSystem.Instance.drawArtefactPang(ci, (uint)Players.Count());

                if (art_pang._typeid != 0)
                { // Dropou

                    dir.v_drop.Add(art_pang);

                    if (art_pang.qntd >= 30)
                    { // Envia notice que o player ganhou jackpot

                        Packet p = new Packet((ushort)0x40);

                        p.WriteByte(10); // JackPot

                        p.WriteString(session.UserInfo.NickName);

                        p.WriteUInt16(0); // size Msg

                        p.WriteInt32(art_pang.qntd * 500);

                        SendBroadCast(p);
                    }
                }
            }

            // Drop Event Course
            var course = sDropSystem.Instance.findCourse((byte)(ci.course & 0x7F));

            if (course != null)
            { // tem Drop nesse Course
                var drop_event = sDropSystem.Instance.drawCourse(course, ci);

                if (drop_event.Any()) // Dropou
                {
                    dir.v_drop.AddRange(dir.v_drop);
                }
            }

            // Drop Mana Artefact
            var mana_drop = sDropSystem.Instance.drawManaArtefact(ci);

            if (mana_drop._typeid != 0) // Dropou
            {
                dir.v_drop.Add(mana_drop);
            }

            // Drop Grand Prix Ticket, não drop no Grand Prix
            if (RoomInfo.HoleCount == ci.seq_hole && RoomInfo.GetRoomType() != RoomTypeFlags.GRAND_PRIX)
            {
                var gp_ticket = sDropSystem.Instance.drawGrandPrixTicket(ci, session);

                if (gp_ticket._typeid != 0) // Dropou
                {
                    dir.v_drop.Add(gp_ticket);
                }
            }

            // SSC Ticket
            var ssc = sDropSystem.Instance.drawSSCTicket(ci);

            if (ssc.Any())
            {
                dir.v_drop.AddRange(ssc);

                // SSC Ticket Achievement
                pgi.sys_achieve.incrementCounter(0x6C400053u, (int)ssc.Count);
            }

            // Adiciona para a lista de drop's do player
            if (dir.v_drop.Any())
            {
                pgi.drop_list.v_drop.AddRange(dir.v_drop);

            }

            return (dir);
        }


        public void RequestSaveDrop(Player session)
        {

            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception("[GameBase::RequestSaveDrop][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou salvar drop item no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }
            pgi.drop_list.v_drop = pgi.drop_list.v_drop.Where(c => c._typeid != 0).ToList();
            if (pgi.drop_list.v_drop.Count > 0)
            {
                List<stItem> v_item = new List<stItem>();

                foreach (var el in pgi.drop_list.v_drop)
                {
                    stItem item = new stItem
                    {
                        type = 2,
                        _typeid = el._typeid,
                        qntd = (int)((el.type == DropItem.eTYPE.QNTD_MULTIPLE_500) ? el.qntd * 500 : el.qntd)
                    }; // <- cria um NOVO objeto a cada iteração 
                    item.STDA_C_ITEM_QNTD = (short)item.qntd;

                    var existente = v_item.FirstOrDefault(c => c._typeid == item._typeid);
                    if (existente == null)
                    {
                        // Novo item
                        v_item.Add(new stItem(item));
                    }
                    else
                    {
                        // Já existe — atualiza in-LadderGrade sem adicionar duplicata à lista
                        existente.qntd += item.qntd;
                        existente.STDA_C_ITEM_QNTD = (short)existente.qntd;
                    }
                }

                var rai = ItemManager.addItem(v_item, session, 0, 0);

                if (rai.fails.Any() && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                {
                    _smp.LogManager.Instance.push(
                        new AppMessage("[Game:RequestSaveDrop][WARNIG] nao conseguiu adicionar os drop itens. Bug",
                        type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                Packet p = new Packet(0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZero(25);
                }

                session.Send(p);
            }
        }

        public DropItemRet RequestInitCubeCoin(Player session, Packet packet)
        {
            if (!session.getState())
            {
                throw new exception("[GameBase::RequestInitCubeCoin][Error] player nao esta connectado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 0));
            }
            ;
            if (packet == null)
            {
                throw new exception("[GameBase::RequestInitCubeCoin][Error] _packet is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    6, 0));
            }

            try
            {

                byte opt = packet.ReadByte();
                byte count = packet.ReadByte();

                // Player que tacou e tem drops (Coin ou Cube)
                if (opt == 1 && count > 0)
                {

                    DropItemRet dir = new DropItemRet();

                    var pgi = GetPlayerInfo(session);
                    if (pgi == null)
                    {
                        throw new exception("[GameBase::initCubeCoin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou terninar o hole no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            1, 4));
                    }

                    var hole = Course.findHole(pgi.hole);

                    if (hole == null)
                    {
                        throw new exception("[GameBase::RequestInitCubeCoin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou terminar hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas no CourseIndex nao tem esse hole. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                            250, 0));
                    }

                    uint tipo = 0;
                    uint id = 0;

                    CubeEx pCube = null;

                    for (var i = 0; i < count; ++i)
                    {

                        tipo = packet.ReadByte();
                        id = packet.ReadUInt32();

                        pCube = hole.FindCubeCoin(id);

                        if (pCube == null)
                        {
                            throw new exception("[GameBase::RequestInitCubeCoin][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou terminar hole[NUMERO=" + Convert.ToString((ushort)pgi.hole) + "], mas o cliente forneceu um cube/coin Login[ID=" + Convert.ToString(id) + "] invalido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                                251, 0));
                        }
                        if (tipo == 0)
                        // Coin
                        {
                            // Tipo 3 Coin da borda do green ganha menos pangs ganha de 1 a 50, Tipo 4 Coin no chão qualquer lugar ganha mais Pang de 1 a 200
                            dir.v_drop.Add(new DropItem(
                                COIN_TYPEID,
                                (byte)hole.getCourse(),
                                (byte)hole.GetRoomId(),
                                (short)((Random.Shared.Next() % ((uint)(pCube.flag_location == 0 ? 50 : 200))) + 1),
                                (pCube.flag_location == 0) ? DropItem.eTYPE.COIN_EDGE_GREEN : DropItem.eTYPE.COIN_GROUND
                            ));

                            pgi.drop_list.v_drop.Add((new DropItem(
                                COIN_TYPEID,
                                (byte)hole.getCourse(),
                                (byte)hole.GetRoomId(),
                                (short)((Random.Shared.Next() % ((uint)(pCube.flag_location == 0 ? 50 : 200))) + 1),
                                (pCube.flag_location == 0) ? DropItem.eTYPE.COIN_EDGE_GREEN : DropItem.eTYPE.COIN_GROUND
                            )));
                        }
                        if (tipo == 1) // Cube
                        {
                            // Add os Cube Coin para o player list drop
                            pgi.drop_list.v_drop.Add(new DropItem(SPINNING_CUBE_TYPEID, (byte)hole.getCourse(), (byte)hole.GetRoomId(), 1, DropItem.eTYPE.CUBE));
                            dir.v_drop.Add(new DropItem(SPINNING_CUBE_TYPEID, (byte)hole.getCourse(), (byte)hole.GetRoomId(), 1, DropItem.eTYPE.CUBE));
                        }
                    }
                    return (dir);
                }
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestInitCubeCoin][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            return new DropItemRet();
        }

        public virtual void CalculePang(Player session)
        {
            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception("[GameBase::RequestCalculePang][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou calcular o Pang, mas o info não existe.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));
            }

            // 1. Busca informações do Course (Campo)
            var course = sIff.Instance.findCourse((uint)((int)RoomInfo.CourseIndex & 0x7F) | 0x28000000u);

            // 2. Define o Rate do Course (Padrão 1.0f se nulo)
            float course_rate = (course != null && course.RatePang >= 1.0f) ? course.RatePang : 1.0f;
            float pang_rate = 0.0f;

            // 3. Cálculo do Rate Total (Itens + Eventos do Servidor + Course)
            // Adicionado bônus de 10 se for HoleMode Shuffle
            uint base_rate = (uint)(RateValue.pang + (RoomInfo.HoleMode == (byte)RoomHoleType.M_SHUFFLE ? 10 : 0));

            pang_rate = TRANSF_SERVER_RATE_VALUE(pgi.used_item.rate.pang) * TRANSF_SERVER_RATE_VALUE(base_rate);

            // 4. Cálculo do Bônus
            uint novo_bonus = (uint)(((pgi.data.pang * pang_rate) - pgi.data.pang) + (pgi.data.bonus_pang));

            // 5. TRAVA DE SEGURANÇA: Máximo 20.000 Pangs 
            if (novo_bonus > 15000)
                novo_bonus = (novo_bonus / 2);

            pgi.data.bonus_pang = novo_bonus;
        }

        public virtual void RequestSaveInfo(Player session, int option)
        {

            var pgi = GetPlayerInfo(session);
            if (pgi == null)
                return;

            try
            {

                // Aqui dados do jogo ele passa o holein no lugar do mad_conduta <-> holein, agora quando ele passa o info user é invertido(Normal)
                // Inverte para salvar direito no banco de dados
                var tmp_holein = pgi.ui.hole_in; 
                pgi.ui.hole_in = pgi.ui.mad_conduta;
                pgi.ui.mad_conduta = tmp_holein;

                if (option == 0)
                { // Terminou VS

                    // Verifica se o Angel Event está ativo de tira 1 quit do player que concluí o jogo
                    if (RoomInfo.IsAngelQuiterEvent)
                    {
                        pgi.ui.quitado = -1;
                    }

                    pgi.ui.exp = 0;
                    pgi.ui.combo = 1;
                    pgi.ui.jogado = 1;
                    pgi.ui.media_score = pgi.data.score;

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(StartTime);

                    if (diff > 0)
                    {
                        diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds
                    }

                    pgi.ui.tempo = (int)diff;

                }
                else if (option == 1)
                { 
                    // Quitou ou tomou DC 
                    // Quitou ou saiu não ganha pangs
                    pgi.data.pang = 0;
                    pgi.data.bonus_pang = 0; 
                    pgi.ui.exp = 0;
                    pgi.ui.combo = (int)(DECREASE_COMBO_VALUE * -1);
                    pgi.ui.jogado = 1;

                    // Verifica se tomou DC ou Quitou, ai soma o membro certo
                    if (session.ConnectionTimeOut)
                        pgi.ui.quitado = 1;
                    else
                        pgi.ui.disconnect = 1;

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    pgi.ui.media_score = pgi.data.score;

                    var diff = UtilTime.GetLocalDateDiff(StartTime);

                    if (diff > 0)
                        diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds

                    pgi.ui.tempo = (int)diff;

                }
                else if (option == 2)
                { // Não terminou o hole 1, alguem saiu ai volta para sala sem contar o combo, só conta o jogo que começou

                    pgi.data.pang = 0;
                    pgi.data.bonus_pang = 0;

                    pgi.ui.exp = 0;
                    pgi.ui.jogado = 1;

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(StartTime);

                    if (diff > 0)
                        diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds

                    pgi.ui.tempo = (int)diff;

                }
                else if (option == 4)
                { // SSC

                    pgi.ui.clear();

                    // Verifica se o Angel Event está ativo de tira 1 quit do player que concluí o jogo
                    if (RoomInfo.IsAngelQuiterEvent)
                    { 
                        pgi.ui.quitado = -1;
                    }

                    pgi.ui.exp = 0;
                    pgi.ui.combo = 1;
                    pgi.ui.jogado = 1;
                    pgi.ui.media_score = 0;

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(StartTime);

                    if (diff > 0)
                    {
                        diff /= STDA_10_MICRO_PER_SEC;
                    }

                    pgi.ui.tempo = (int)diff;

                }
                else if (option == 5)
                {

                    // Quitou ou saiu não ganha pangs
                    pgi.data.pang = 0;
                    pgi.data.bonus_pang = 0;

                    pgi.ui.exp = 0;
                    pgi.ui.jogado = 1;
                    pgi.ui.media_score = pgi.data.score;

                    // Os valores que eu não colocava
                    pgi.ui.jogados_disconnect = 1; // Esse aqui é o contador de jogos que o player começou é o mesmo do jogado, só que esse aqui usa para o disconnect

                    var diff = UtilTime.GetLocalDateDiff(StartTime);

                    if (diff > 0)
                    {
                        diff /= STDA_10_MICRO_PER_SEC; // NanoSeconds To Seconds
                    }

                    pgi.ui.tempo = (int)diff;
                }

                // Achievement Records
                RecordsPlayerAchievement(session);

                // Pode tirar pangs
                Int64 total_pang = (long)(pgi.data.pang + pgi.data.bonus_pang);

                // UPDATE ON SERVER AND DB
                session.UserInfo.addUserInfo(pgi.ui, (ulong)total_pang); // add User Info

                if (total_pang > 0)
                {
                    session.UserInfo.addPang((ulong)total_pang); // add Pang
                }
                else if (total_pang < 0)
                {
                    session.UserInfo.consomePang((ulong)(total_pang * -1)); // consome Pangs
                }

                // Game Combo
                if (session.UserInfo.Statistics.combo > 0)
                {
                    pgi.sys_achieve.incrementCounter(0x6C40004Bu, session.UserInfo.Statistics.combo);
                }

            }
            catch (exception e)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestSaveInfo][Error] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public virtual void RequestUpdateItemUsedGame(Player session)
        {

            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception("[GameBase::RequestUpdateItemUsedGame][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou atualizar itens usado no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            var ui = pgi.used_item;

            ui.club.count += (uint)(1.0f * 10.0f * ui.club.rate * TRANSF_SERVER_RATE_VALUE(RateValue.clubset) * TRANSF_SERVER_RATE_VALUE(ui.rate.club));

            // Passive Item exceto Time Booster e var Command, que soma o contador por uso, o cliente passa o pacote, dizendo que usou o item
            foreach (var el in ui.v_passive)
            {

                // Verica se é o ultimo hole, terminou o jogo, ai tira soma 1 ao count do pirulito que consome por jogo
                if (CHECK_PASSIVE_ITEM(el.Value._typeid)
                    && el.Value._typeid != TIME_BOOSTER_TYPEID
                    && el.Value._typeid != AUTO_COMMAND_TYPEID)
                {

                    // Item de Exp Boost que só consome 1 Por Jogo, só soma no RequestFinishItemUsedGame
                    if (passive_item_exp_1perGame.Contains(el.Value._typeid))
                    {
                        el.Value.count++;
                    }

                }
                else if (sIff.Instance.getItemGroupIdentify(el.Value._typeid) ==IFF_GROUP.BALL || sIff.Instance.getItemGroupIdentify(el.Value._typeid) ==IFF_GROUP.AUX_PART) //AuxPart(Anel)
                {
                    el.Value.count++;
                }
            }
        }

        public void RequestFinishItemUsedGame(Player session)
        {

            List<stItemEx> v_item = new List<stItemEx>();

            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception("[GameBase::RequestFinishItemUsedGame][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou finalizar itens usado no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            // Player já finializou os itens usados, verifica para não finalizar dua vezes os itens do player
            if (pgi.finish_item_used == 1)
            {

                _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestFinishItemUsedGame][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] ja finalizou os itens. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                return;
            }

            var ui = pgi.used_item;

            // Add +1 ao itens que consome 1 só por jogo
            // Item de Exp Boost que só consome 1 Por Jogo
            foreach (var el in ui.v_passive)
            {
                if (passive_item_exp_1perGame.Contains(el.Value._typeid))
                {
                    el.Value.count++;
                }
            }


            // Verifica se é premium 2 e se ele tem o var caliper para poder somar no Achievement
            if (session.UserInfo.UserCapabilities.UserPremium && sPremiumSystem.Instance.isPremium(session.Inventory.PremiumTicket._typeid))
            {

                var it_ac = ui.v_passive.FirstOrDefault(c => c.Key == AUTO_CALIPER_TYPEID);

                if (it_ac.Value != null)
                {

                    int qntd = Course.findHoleSeq(pgi.hole);

                    if (qntd == -1)
                    {
                        qntd = RoomInfo.HoleCount;
                    }

                    // Adiciona Auto Caliper para ser contado no Achievement
                    ui.v_passive.AddOrUpdate(AUTO_CALIPER_TYPEID,
                        new UsedItem.Passive(typeid: AUTO_CALIPER_TYPEID, _count: qntd));
                    // Nota: verificação de !Any() após AddOrUpdate foi removida — é sempre falsa
                    // (AddOrUpdate garante que a chave existe imediatamente após a chamada).
                }
            }

            // Passive Item
            foreach (var el in ui.v_passive)
            {

                if (el.Value.count > 0)
                {

                    // Item Aqui tem o Achievemente de passive item
                    if (sIff.Instance.getItemGroupIdentify(el.Value._typeid) ==IFF_GROUP.ITEM && !sIff.Instance.IsItemEquipable(el.Value._typeid))
                    {

                        pgi.sys_achieve.incrementCounter(0x6C400075u, (int)el.Value.count);

                        uint tmp_countertypeid;
                        if ((tmp_countertypeid = AchievementSystem.getPassiveItemCounterTypeId(el.Value._typeid)) > 0)
                        {
                            pgi.sys_achieve.incrementCounter(tmp_countertypeid, (int)el.Value.count);
                        }
                    }

                    // Só atualiza o var Caliper se não for Premium 2
                    if (!session.UserInfo.UserCapabilities.UserPremium
                        || !sPremiumSystem.Instance.isPremium(session.Inventory.PremiumTicket._typeid)
                        || el.Value._typeid != AUTO_CALIPER_TYPEID)
                    {

                        // Tira todos itens passivo, antes estava Item e AuxPart, não ia Ball por que eu fiz errado, só preciso verifica se é item e passivo para somar o achievement
                        // Para tirar os itens, tem que tirar(atualizar) todos.
                        var pWi = session.Inventory.FindWarehouseItemByTypeid(el.Value._typeid);

                        if (pWi != null)
                        {

                            // Init Item
                            var item = new stItemEx();

                            item.type = 2;
                            item._typeid = pWi._typeid;
                            item.id = pWi.id;
                            item.qntd = (int)el.Value.count;
                            item.STDA_C_ITEM_QNTD = (short)((short)item.qntd * -1);

                            // Add On Vector
                            v_item.Add(new stItemEx(item));


                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestFinishItemUsedGame][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou atualizar item[TYPEID=" + Convert.ToString(el.Value._typeid) + "] que ele nao possui. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            // Active Item
            foreach (var el in ui.v_active)
            {

                if (el.Value.count > 0)
                {

                    // Aqui tem achievement de Item Active
                    if (sIff.Instance.getItemGroupIdentify(el.Value._typeid) ==IFF_GROUP.ITEM && sIff.Instance.IsItemEquipable(el.Value._typeid))
                    {

                        pgi.sys_achieve.incrementCounter(0x6C40004Fu, (int)el.Value.count);

                        uint tmp_countertypeid;
                        if ((tmp_countertypeid = AchievementSystem.getActiveItemCounterTypeId(el.Value._typeid)) > 0)
                        {
                            pgi.sys_achieve.incrementCounter(tmp_countertypeid, (int)el.Value.count);
                        }
                    }

                    // Só tira os itens Active se a sala não estiver com o artefact Frozen Flame,
                    // se ele estiver com artefact Frozen Flame ele mantém os Itens Active, não consome e nem desequipa do inventório do player
                    if (RoomInfo.ItemIDArtifact != ART_FROZEN_FLAME)
                    {

                        // Limpa o Item Slot do player, dos itens que foram usados(Ativados) no jogo
                        if (el.Value.count <= el.Value.v_slot.Count)
                        {

                            for (var i = 0; i < el.Value.count; ++i)
                            {
                                session.Inventory.UserEquipment.item_slot[el.Value.v_slot[i]] = 0;
                            }
                        }

                        var pWi = session.Inventory.FindWarehouseItemByTypeid(el.Value._typeid);

                        if (pWi != null)
                        {
                            // Init Item
                            var item = new stItemEx();

                            item.type = 2;
                            item._typeid = pWi._typeid;
                            item.id = pWi.id;
                            item.qntd = (int)el.Value.count;
                            item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                            // Add On Vector
                            v_item.Add(new stItemEx(item));

                        }
                        else
                        {
                            _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestFinishItemUsedGame][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou atualizar item[TYPEID=" + Convert.ToString(el.Value._typeid) + "] que ele nao possui. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                        }
                    }
                }
            }

            // Update Item Equiped Slot ON DB
            NormalManagerDB.Instance.add(25,
                new CmdUpdateItemSlot(session.UserInfo.UID, session.Inventory.UserEquipment.item_slot),
                OnDatabaseResponse, this);

            // Se for o Master da sala e ele estiver com artefato tira o mana dele
            // Antes tirava assim que começava o jogo, mas aí o cliente atualizava a sala tirando o artefact aí no final não tinha como ver se o frozen flame estava equipado
            // e as outras pessoas que estão na lobby não sabe qual artefect que está na sala, por que o Master mesmo mando o pacote pra tirar da sala quando o server tira o mana dele no init game
            if (RoomInfo.ItemIDArtifact != 0 && RoomInfo.OwnerUID == session.UserInfo.UID)
            {

                // Tira Artefact Mana do Master da sala
                var pWi = session.Inventory.FindWarehouseItemByTypeid(RoomInfo.ItemIDArtifact + 1);
                if (pWi != null)
                {

                    var item = new stItemEx();

                    item.type = 2;
                    item.id = pWi.id;
                    item._typeid = pWi._typeid;
                    item.qntd = (int)((pWi.STDA_C_ITEM_QNTD <= 0) ? 1 : pWi.STDA_C_ITEM_QNTD);
                    item.STDA_C_ITEM_QNTD = (short)(item.qntd * -1);

                    // Add on Vector Update Itens
                    v_item.Add(new stItemEx(item));

                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestFinishItemUsedGame][Warning] Master[UID=" + Convert.ToString(session.UserInfo.UID) + "] do jogo nao tem Mana do Artefect[TYPEID=" + Convert.ToString(RoomInfo.ItemIDArtifact) + ", MANA=" + Convert.ToString(RoomInfo.ItemIDArtifact + 1) + "] e criou e comecou um jogo com artefact sem mana. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // Update Item ON Server AND DB
            if (v_item.Count > 0)
            {

                if (ItemManager.removeItem(v_item, session) <= 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestFinishItemUsedGame][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao conseguiu deletar os item do player. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // Club Mastery
            if (ui.club.count != 0 && ui.club._typeid != 0)
            {

                var pClub = session.Inventory.FindWarehouseItemByTypeid(ui.club._typeid);

                if (pClub != null)
                {

                    pClub.clubset_workshop.mastery += ui.club.count;

                    var item = new stItemEx();

                    item.type = 0xCC;
                    item.id = (int)pClub.id;
                    item._typeid = pClub._typeid;

                    item.clubset_workshop.c = pClub.clubset_workshop.c;

                    item.clubset_workshop.level = (byte)pClub.clubset_workshop.level;
                    item.clubset_workshop.mastery = pClub.clubset_workshop.mastery;
                    item.clubset_workshop.rank = (uint)pClub.clubset_workshop.rank;
                    item.clubset_workshop.recovery = pClub.clubset_workshop.recovery_pts;

                    NormalManagerDB.Instance.add(12,
                        new CmdUpdateClubSetWorkshop(session.UserInfo.UID,
                            pClub,
                            CmdUpdateClubSetWorkshop.FLAG.F_TRANSFER_MASTERY_PTS),
                        OnDatabaseResponse, this);

                    v_item.Add(new stItemEx(item));
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestFinishItemUsedGame][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou salvar mastery do ClubSet[TYPEID=" + Convert.ToString(ui.club._typeid) + "] que ele nao tem. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            // ServerFlag de que o palyer já finalizou os itens usados no jogo, para não finalizar duas vezes
            pgi.finish_item_used = 1;

            // Atualiza ON Jogo
            if (v_item.Count > 0)
            {
                Packet p = new Packet((ushort)0x216);

                p.WriteUInt32((uint)UtilTime.GetSystemTimeAsUnix());
                p.WriteUInt32((uint)v_item.Count);

                foreach (var el in v_item)
                {
                    p.WriteByte(el.type);
                    p.WriteUInt32(el._typeid);
                    p.WriteInt32(el.id);
                    p.WriteUInt32(el.flag_time);
                    p.WriteBytes(el.stat.ToArray());
                    p.WriteInt32((el.STDA_C_ITEM_TIME > 0) ? el.STDA_C_ITEM_TIME : el.STDA_C_ITEM_QNTD);
                    p.WriteZero(25); // 10 PCL[C0~C4] 2 Bytes cada, 15 bytes desconhecido
                    if (el.type == 0xCC)
                    {
                        p.WriteBytes(el.clubset_workshop.ToArray());
                    }
                }

                session.Send(p);
            }
        }
         
        public virtual void RequestFinishHole(Player session, int option)
        {
            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception($"[GameBase::RequestFinishHole][Error] Normal[UID={session.UserInfo.UID}] tentou finalizar dados do hole, mas info não guardada.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));
            }

            if (pgi.hole == 255)
                return;

            var hole = Course.findHole(pgi.hole);

            if (hole == null)
            {
                throw new exception($"[GameBase::finishHole][Error] Normal[UID={session.UserInfo.UID}] hole[NUMERO={(ushort)pgi.hole}] inválido.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 20, 0));
            }

            // Variáveis locais para garantir a precisão do cálculo ANTES de manipular o objeto global
            int score_hole = 0;
            int tacada_hole_atual = pgi.data.tacada_num;
            int par_do_hole = hole.getPar().par;

            // --- BLOCO DE CÁLCULO ---
            if (option == 0) // Terminou o buraco normalmente
            {
                // 1. Atualiza totais da partida
                pgi.data.total_tacada_num += tacada_hole_atual;

                // 2. Calcula score (negativo ou positivo)
                score_hole = (int)(tacada_hole_atual - par_do_hole);
                pgi.data.score += score_hole;

                // 3. Sync Database (Log da sala)
                UpdateRoomLogSql(session);

                // 4. Conquistas (Achievement)
                var tmp_countertypeid = AchievementSystem.getScoreCounterTypeId((int)tacada_hole_atual, par_do_hole);
                if (tmp_countertypeid > 0)
                    pgi.sys_achieve.incrementCounter(tmp_countertypeid);

                // --- RESET DE DADOS DO BURACO ATUAL ---
                pgi.data.time_out = 0;
                pgi.data.giveup = 0;
                pgi.data.penalidade = 0;
            }
            else if (option == 1) // Quit/GiveUp (Calcula o resto do campo como Max Score)
            {
                var range = Course.findRange(pgi.hole);
                foreach (var kv in range)
                {
                    if (kv.Key > RoomInfo.HoleCount) break;

                    pgi.data.total_tacada_num += kv.Value.getPar().total_shot;
                    pgi.data.score += kv.Value.getPar().range_score[1]; // Geralmente +3 ou +4 por buraco
                }
                pgi.data.time_out = 0;
                pgi.data.tacada_num = 0;
                pgi.data.giveup = 0;
                pgi.data.penalidade = 0;
            }

            // --- ATUALIZAÇÃO DO PROGRESSO (CARD DE SCORE) ---
            pgi.progress.hole = (short)Course.findHoleSeq(pgi.hole);

            if (option == 0)
            {
                int index = pgi.progress.hole - 1;
                if (index >= 0 && index < 18)
                {
                    if (pgi.shot_sync.state_shot.display.acerto_hole)
                        pgi.progress.finish_hole[index] = 1;

                    pgi.progress.par_hole[index] = (int)par_do_hole;
                    pgi.progress.score[index] = score_hole;
                    pgi.progress.tacada[index] = tacada_hole_atual; // Usa a local, pois a global já foi zerada
                }
            }
            else
            {
                var range = Course.findRange(pgi.hole);
                foreach (var kv in range)
                {
                    int index = kv.Key - 1;
                    if (index >= 0 && index < 18)
                    {
                        pgi.progress.finish_hole[index] = 0;
                        pgi.progress.par_hole[index] = kv.Value.getPar().par;
                        pgi.progress.score[index] = kv.Value.getPar().range_score[1];
                        pgi.progress.tacada[index] = kv.Value.getPar().total_shot;
                    }
                }
            }
        }

        public void RequestSaveRecordCourse(Player session,
            int game, int option)
        {

            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception("[GameBase::RequestSaveRecordCourse][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou salvar record do CourseIndex do player no jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
            { // Player não está com character equipado, kika dele do jogo
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestSaveRecordCourse][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            MapStatistics pMs = null;

            if (pgi.assist_flag == 1)
            { // Assist

                if (game == 52)
                {
                    pMs = session.UserInfo.GrandPrixMapStatisticsAll[(int)((int)RoomInfo.CourseIndex & 0x7F)];
                }
                else if (RoomInfo.SpecialModeRoom.IsNaturalMode)
                { // Natural
                    pMs = session.UserInfo.NaturalMapStatisticsAll[(int)((int)RoomInfo.CourseIndex & 0x7F)];

                    game = 51; // Natural
                }
                else
                { // Normal
                    pMs = session.UserInfo.NormalMapStatisticsAll[(int)((int)RoomInfo.CourseIndex & 0x7F)];
                }

            }
            else
            { // Sem Assist

                if (game == 52)
                {
                    pMs = session.UserInfo.GrandPrixMapStatistics[(int)((int)RoomInfo.CourseIndex & 0x7F)];
                }
                else if (RoomInfo.SpecialModeRoom.IsNaturalMode)
                { // Natural
                    pMs = session.UserInfo.NaturalMapStatistics[(int)((int)RoomInfo.CourseIndex & 0x7F)];

                    game = 51; // Natural
                }
                else
                { // Normal
                    pMs = session.UserInfo.NormalMapStatistics[(int)((int)RoomInfo.CourseIndex & 0x7F)];
                }
            }

            bool make_record = false;

            // UPDATE ON SERVER
            if (option == 1)
            { // 18h pode contar record

                // Fez Record
                if (pMs.best_score == 127
                    || pgi.data.score < pMs.best_score
                    || pgi.data.pang > pMs.best_pang)
                {

                    // Update Best Score Record
                    if (pgi.data.score < pMs.best_score)
                    {
                        pMs.best_score = (sbyte)pgi.data.score;
                    }

                    // Update Best Pang Record
                    if (pgi.data.pang > pMs.best_pang)
                    {
                        pMs.best_pang = pgi.data.pang;
                    }

                    // Update Character Record
                    pMs.character_typeid = session.Inventory.UserEquippedItem.CharacterEquiped._typeid;

                    make_record = true;
                }
            }

            // Salva os dados normais
            pMs.tacada += (uint)pgi.ui.tacada;
            pMs.putt += (uint)pgi.ui.putt;
            pMs.hole += (uint)pgi.ui.hole;
            pMs.fairway += (uint)pgi.ui.fairway;
            pMs.hole_in += (uint)pgi.ui.hole_in;
            pMs.putt_in += (uint)pgi.ui.putt_in;
            pMs.total_score += pgi.data.score;
            pMs.event_score = 0;

            MapStatisticsEx ms = new MapStatisticsEx(pMs)
            {
                tipo = (byte)game
            };

            // UPDATE ON DB
            NormalManagerDB.Instance.add(5,
                new CmdUpdateMapStatistics(session.UserInfo.UID,
                    ms, pgi.assist_flag),
                OnDatabaseResponse, this);

            // UPDATE ON GAME, se ele fez record, e add 1000 para ele
            if (make_record)
            {
                // Add 1000 Pang por ele ter quebrado o  record dele
                session.UserInfo.addPang(1000);

                // Resposta para make record
                Packet p = new Packet((ushort)0xB9);

                p.WriteByte(((int)RoomInfo.CourseIndex) & 0x7F);

                session.Send(p);
            }
        }

        public void RequestInitItemUsedGame(Player session, PlayerGameInfo pgi)
        {

            //InitPlayerInfo("RequestInitItemUsedGame", "tentou inicializar itens usado no jogo", session, out PlayerGameInfo pgi);

            // Characters Equip
            if (session.getState())
            { // Check Player Connected

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                { // Player não está com character equipado, kika dele do jogo
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestInitItemUsedGame][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                if (session.Inventory.UserEquippedItem.Ball_WI == null)
                { // Player não está com Comet(Ball) equipado, kika dele do jogo
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestInitItemUsedGame][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao esta com Ball equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    return;
                }

                var ui = pgi.used_item;

                // Zera os Itens usados
                ui.clear();

                /// ********** Itens Usado **********

                // Passive Item Equipado
                session.Inventory.WarehouseItems.ToList().ForEach(el =>
                {
                    if (passive_item.Any(c => c == el.Value._typeid))
                    {
                        ui.v_passive.AddOrUpdate(el.Value._typeid, new UsedItem.Passive(el.Value._typeid, 0));
                    }
                });
                // Ball Equiped 
                if (session.Inventory.UserEquippedItem.Ball_WI._typeid != DEFAULT_COMET_TYPEID && (!session.UserInfo.UserCapabilities.UserPremium || session.Inventory.UserEquippedItem.Ball_WI._typeid != sPremiumSystem.Instance.getPremiumBallByTicket(session.Inventory.PremiumTicket._typeid)))
                {
                    ui.v_passive.AddOrUpdate(session.Inventory.UserEquippedItem.Ball_WI._typeid, new UsedItem.Passive(session.Inventory.UserEquippedItem.Ball_WI._typeid, 0));
                }

                // AuxParts
                for (var i = 0u; i < (session.Inventory.UserEquippedItem.CharacterEquiped.auxparts.Length); ++i)
                {
                    if (session.Inventory.UserEquippedItem.CharacterEquiped.auxparts[i] >= 0x70000000 && session.Inventory.UserEquippedItem.CharacterEquiped.auxparts[i] < 0x70010000)
                    {
                        ui.v_passive.AddOrUpdate(session.Inventory.UserEquippedItem.CharacterEquiped.auxparts[i], new UsedItem.Passive(session.Inventory.UserEquippedItem.CharacterEquiped.auxparts[i], 0));
                    }
                }

                // Item Active Slot 
                for (var i = 0; i < (session.Inventory.UserEquipment.item_slot.Length); ++i)
                {
                    // Diferente de 0 item está equipado
                    if (session.Inventory.UserEquipment.item_slot[i] != 0)
                    {
                        if (!ui.v_active.ContainsKey(session.Inventory.UserEquipment.item_slot[i])) // Não tem add o novo
                        {
                            ui.v_active.Add(session.Inventory.UserEquipment.item_slot[i], new UsedItem.Active(session.Inventory.UserEquipment.item_slot[i], 0u, new List<byte> { (byte)i }));
                        }

                        else // Já tem add só o slot
                        {
                            ui.v_active[session.Inventory.UserEquipment.item_slot[i]].v_slot.Add((byte)i); // Slot
                        }
                    }
                }

                // ClubSet For ClubMastery
                ui.club._typeid = session.Inventory.UserEquippedItem.ClubEquiped._typeid;
                ui.club.count = 0;
                ui.club.rate = 1.0f;

                var club = sIff.Instance.findClubSet(ui.club._typeid);

                if (club != null)
                {
                    ui.club.rate = club.work_shop.rate;
                }
                else
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestIniItemUsedGame][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] esta equipado com um ClubSet[TYPEID=" + Convert.ToString(session.Inventory.UserEquippedItem.ClubEquiped._typeid) + ", ID=" + Convert.ToString(session.Inventory.UserEquippedItem.ClubEquiped.id) + "] que nao tem no IFF_STRUCT do Server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                /// ********** Itens Usado **********

                /// ********** Itens Exp/Pang Rate **********
                // Item Buff
                var time_limit_item = sIff.Instance.getTimeLimitItem();

                session.Inventory.ItemBuffs.ForEach(el =>
                {
                    var item_ = time_limit_item.FirstOrDefault(el2 => el2.Value._typeid == el._typeid);

                    if (item_.Value != null)
                    {
                        switch ((ItemBuff.eTYPE)item_.Value.type)
                        {
                            case ItemBuff.eTYPE.YAM_AND_GOLD:
                                ui.rate.exp += item_.Value.percent;
                                break;

                            case ItemBuff.eTYPE.RAINBOW:
                            case ItemBuff.eTYPE.RED:
                                ui.rate.exp += (item_.Value.percent > 0) ? item_.Value.percent : 100;
                                ui.rate.pang += (item_.Value.percent > 0) ? item_.Value.percent : 100;
                                break;

                            case ItemBuff.eTYPE.GREEN:
                                ui.rate.exp += (item_.Value.percent > 0) ? item_.Value.percent : 100;
                                break;

                            case ItemBuff.eTYPE.YELLOW:
                                ui.rate.pang += (item_.Value.percent > 0) ? item_.Value.percent : 100;
                                break;
                        }
                    }
                });

                // Card Equipado, Special, NPC, e Caddie
                session.Inventory.CardEquipment.ToList().ForEach(el =>
                {
                    if (el.parts_id == session.Inventory.UserEquippedItem.CharacterEquiped.id
                        && el.parts_typeid == session.Inventory.UserEquippedItem.CharacterEquiped._typeid
                        && sIff.Instance.getItemSubGroupIdentify22(el._typeid) == 5)
                    {
                        if (el.efeito == 2)
                        {
                            ui.rate.exp += el.efeito_qntd;
                        }
                        else if (el.efeito == 1)
                        {
                            ui.rate.pang += el.efeito_qntd;
                        }
                    }
                    else if (el.parts_id == 0 && el.parts_typeid == 0 && sIff.Instance.getItemSubGroupIdentify22(el._typeid) == 2)
                    {
                        if (el.efeito == 3)
                        {
                            ui.rate.exp += el.efeito_qntd;
                        }
                        else if (el.efeito == 2)
                        {
                            ui.rate.pang += el.efeito_qntd;
                        }
                        else if (el.efeito == 34)
                        {
                            ui.rate.club += el.efeito_qntd;
                        }
                    }
                });

                // Item Passive Boost Exp, Pang and Club Mastery

                // Pang
                ui.v_passive.ToList().ForEach(el =>
                {
                    if (Array.IndexOf(passive_item_pang_x2, el.Value._typeid) != -1)
                    {
                        ui.rate.pang += 200;
                        pgi.boost_item_flag.pang = 1;
                    }

                    if (Array.IndexOf(passive_item_pang_x4, el.Value._typeid) != -1)
                    {
                        ui.rate.pang += 400;
                        pgi.boost_item_flag.pang_nitro = 1;
                    }

                    if (Array.IndexOf(passive_item_pang_x1_5, el.Value._typeid) != -1)
                    {
                        ui.rate.pang += 50;
                        pgi.boost_item_flag.pang = 1;
                    }

                    if (Array.IndexOf(passive_item_pang_x1_4, el.Value._typeid) != -1)
                    {
                        ui.rate.pang += 40;
                        pgi.boost_item_flag.pang = 1;
                    }

                    if (Array.IndexOf(passive_item_pang_x1_2, el.Value._typeid) != -1)
                    {
                        ui.rate.pang += 20;
                        pgi.boost_item_flag.pang = 1;
                    }
                });


                // Exp
                ui.v_passive.ToList().ForEach(el =>
                {
                    if (Array.IndexOf(passive_item_exp, el.Value._typeid) != -1)
                    {
                        ui.rate.exp += 200;
                    }
                });

                // Club Mastery Boost
                ui.v_passive.ToList().ForEach(el =>
                {
                    if (Array.IndexOf(passive_item_club_boost, el.Value._typeid) != -1)
                    {
                        ui.rate.club += 200;
                    }
                });

                // Character Parts Equipado
                if (session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
                    Array.IndexOf(hat_birthday, element) != -1))
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestInitItemUsedGame][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] esta equipado com Hat Birthday no Character[TYPEID=" + Convert.ToString(session.Inventory.UserEquippedItem.CharacterEquiped._typeid) + ", ID=" + Convert.ToString(session.Inventory.UserEquippedItem.CharacterEquiped.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    ui.rate.exp += 20; // 20% Hat Birthday
                }

                // Hat Lua e Sol
                if (session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
                    Array.IndexOf(hat_lua_sol, element) != -1))
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestInitItemUsedGame][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] esta equipado com Hat Lua e Sol no Character[TYPEID=" + Convert.ToString(session.Inventory.UserEquippedItem.CharacterEquiped._typeid) + ", ID=" + Convert.ToString(session.Inventory.UserEquippedItem.CharacterEquiped.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    ui.rate.exp += 20;  // 20% Hat Lua e Sol
                    ui.rate.pang += 20; // 20% Hat Lua e Sol
                }

                // Kurafaito Ring Club Mastery
                if (Array.IndexOf(session.Inventory.UserEquippedItem.CharacterEquiped.auxparts, KURAFAITO_RING_CLUBMASTERY) != -1)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestInitItemUsedGame][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] esta equipado com Anel (Kurafaito) que da Club Mastery +1.1% no Character[TYPEID=" + Convert.ToString(session.Inventory.UserEquippedItem.CharacterEquiped._typeid) + ", ID=" + Convert.ToString(session.Inventory.UserEquippedItem.CharacterEquiped.id) + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                    ui.rate.club += 10; // Kurafaito Ring da + 10% no Club Mastery
                }

                // Character AuxParts Equipado
                // Aux parts tem seus próprios valores de Rate no iff
                foreach (var el in session.Inventory.UserEquippedItem.CharacterEquiped.auxparts)
                {
                    if (el != 0 && sIff.Instance.getItemGroupIdentify(el) ==IFF_GROUP.AUX_PART)
                    {
                        var auxpart = sIff.Instance.findAuxPart(el);
                        if (auxpart != null)
                        {
                            if (auxpart.Pang_Rate > 100)
                            {
                                ui.rate.pang += (uint)(auxpart.Pang_Rate - 100);
                            }
                            else if (auxpart.Pang_Rate > 0)
                            {
                                ui.rate.pang += auxpart.Pang_Rate;
                            }

                            if (auxpart.Exp_Rate > 100)
                            {
                                ui.rate.exp += (uint)(auxpart.Exp_Rate - 100);
                            }
                            else if (auxpart.Exp_Rate > 0)
                            {
                                ui.rate.exp += auxpart.Exp_Rate;
                            }

                            if (auxpart.Drop_Rate > 100)
                            {
                                ui.rate.drop += (uint)(auxpart.Drop_Rate - 100);
                            }
                            else if (auxpart.Drop_Rate > 0)
                            {
                                ui.rate.drop += auxpart.Drop_Rate;
                            }

                            pgi.thi.all_score += 15;
                        }
                    }
                }

                // Mascot Equipado Rate Exp And Pang, Drop item e Treasure Hunter Rate
                if (session.Inventory.UserEquippedItem.MascotEquiped != null && session.Inventory.UserEquippedItem.MascotEquiped._typeid > 0)
                {

                    var mascot = sIff.Instance.findMascot(session.Inventory.UserEquippedItem.MascotEquiped._typeid);

                    if (mascot != null)
                    {
                        // Pang
                        if (mascot.efeito.pang_rate > 100)
                        {
                            ui.rate.pang += (uint)(mascot.efeito.pang_rate - 100);
                        }
                        else if (mascot.efeito.pang_rate > 0)
                        {
                            ui.rate.pang += (uint)mascot.efeito.pang_rate;
                        }

                        // Exp
                        if (mascot.efeito.exp_rate > 100)
                        {
                            ui.rate.exp += (uint)(mascot.efeito.exp_rate - 100);
                        }
                        else if (mascot.efeito.exp_rate > 0)
                        {
                            ui.rate.exp += (uint)mascot.efeito.exp_rate;
                        }

                        // Drop item, aqui ele add os 120% e no Drop System ele trata isso direito
                        // Todos itens que dá drop Rate da Treasure hunter point
                        if (mascot.efeito.drop_rate > 100)
                        {

                            if (mascot.efeito.drop_rate > 100)
                            {
                                ui.rate.drop += (uint)(mascot.efeito.drop_rate - 100);
                            }
                            else if (mascot.efeito.drop_rate > 0)
                            {
                                ui.rate.drop += (uint)mascot.efeito.drop_rate;
                            }

                            // Passaro gordo que usa isso aqui, mas pode adicionar mais mascot que dé drop Rate e Treasure hunter point
                            pgi.thi.all_score += 15; // Add +15 ao all score
                        }
                    }
                    else
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestInitItemUsedGame][Warning] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] esta equipado com um mascot[TYPEID=" + Convert.ToString(session.Inventory.UserEquippedItem.MascotEquiped._typeid) + ", ID=" + Convert.ToString(session.Inventory.UserEquippedItem.MascotEquiped.id) + "] que nao tem no IFF_STRUCT do Server. Hacker ou Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }

                /// ********** Premium User +10% EXP and PANG *********************

                //if (pgi.premium_flag)
                //{
                //    var rate_premium = sPremiumSystem.Instance.getExpPangRateByTicket(session._Inventory.pt._typeid);
                //    ui.Rate.Experience += rate_premium;
                //    ui.Rate.Pang += rate_premium;
                //}

                /// ********** Itens Exp/Pang Rate **********
            }
        }

        public void RequestSendTreasureHunterItem(Player session)
        {

            var pgi = GetPlayerInfo(session);

            if (pgi == null)
            {
                throw new exception("[GameBase::RequestSendTreasureHunterItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou enviar os itens ganho no Treasure Hunter do jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            List<stItem> v_item = new List<stItem>();

            if (pgi.thi.v_item.Any())
            {
                foreach (var el in pgi.thi.v_item)
                {

                    var bi = new BuyItem();
                    var item = new stItem();

                    bi.id = -1;
                    bi._typeid = el._typeid;
                    bi.qntd = el.qntd;

                    ItemManager.initItemFromBuyItem(session.UserInfo,
                        item, bi, false, 0, 0, 1);

                    if (item._typeid == 0)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestSendTreasureHunterItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] tentou inicializar item[TYPEID=" + Convert.ToString(bi._typeid) + "], mas nao consgeuiu. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));

                        continue;
                    }

                    v_item.Add(new stItem(item));
                }

                // Add Item, se tiver Item
                if (v_item.Count > 0)
                {

                    var rai = ItemManager.addItem(v_item,
                        session, 0, 0);

                    if (rai.fails.Count > 0 && rai.type != RetAddItem.SUCCESS_PANG_AND_EXP_AND_CP_POUCH)
                    {
                        _smp.LogManager.Instance.push(new AppMessage("[GameBase::RequestSendTreasureHunterItem][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao conseguiu adicionar os itens que ele ganhou no Treasure Hunter. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }

            // UPDATE ON GAME
            Packet p = new Packet((ushort)0x134);

            p.WriteByte((byte)v_item.Count);

            foreach (var el in v_item)
            {
                p.WriteUInt32(session.UserInfo.UID);

                p.WriteUInt32(el._typeid);
                p.WriteInt32(el.id);
                p.WriteInt32(el.qntd);
                p.WriteByte(0); // Opt Acho, mas nunca vi diferente de 0

                p.WriteUInt16((ushort)(el.stat.qntd_dep / 0x8000));
                p.WriteUInt16((ushort)(el.stat.qntd_dep % 0x8000));
            }

            session.Send(p);
        }

        public byte CheckCharMotionItem(Player session)
        {

            // Characters Equip
            if (session.getState())
            { // Check Player Connected

                if (session.Inventory.UserEquippedItem.CharacterEquiped == null)
                { // Player não está com character equipado, kika dele do jogo
                    _smp.LogManager.Instance.push(new AppMessage("[GameBase::checkCharMotionItem][Log] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] nao esta com Character equipado. kika ele do jogo. pode ser Bug.", type_msg.CL_FILE_LOG_AND_CONSOLE));


                    return 0;
                }

                // Motion Item
                if (session.Inventory.UserEquippedItem.CharacterEquiped.parts_typeid.Any(element =>
    motion_item.Contains(element)))
                {
                    return 1;
                }

            }

            return 0;
        }

        // Atualiza o Info do usuario, Info Trofel e Map Statistics do Course
        // Opt 0 Envia tudo, -1 não envia o map statistics
        public void SendUpdateInfoAndMapStatistics(Player session, int option)
        {

            Packet p = new Packet((ushort)0x45);

            p.WriteBytes(session.UserInfo.Statistics.ToArray());

            p.WriteBytes(session.Inventory.CurrentTrophy.ToArray());

            // Ainda tenho que ajeitar esses Map Statistics no Pacote Principal, No Banco de dados e no player_info class
            if (option == -1)
            {

                // -1 12 Bytes, os 2 tipos de dados do Map Statistics
                p.WriteInt64(-1);
                p.WriteInt32(-1);

            }
            else
            {
                // Normal essa season
                if (session.UserInfo.NormalMapStatistics[(int)((int)RoomInfo.CourseIndex & 0x7F)].course != ((int)RoomInfo.CourseIndex & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)RoomInfo.CourseIndex & 0x7F);
                    p.WriteBytes(session.UserInfo.NormalMapStatistics[(int)((int)RoomInfo.CourseIndex & 0x7F)].ToArray());
                }

                p.WriteSByte(-1); // Não tem

                // Natural essa season
                if (session.UserInfo.NaturalMapStatistics[(int)((int)RoomInfo.CourseIndex & 0x7F)].course != ((int)RoomInfo.CourseIndex & 0x7F))
                {
                    p.WriteSByte(-1); // N�o tem
                }
                else
                {
                    p.WriteByte((char)RoomInfo.CourseIndex & 0x7F);
                    p.WriteBytes(session.UserInfo.NaturalMapStatistics[(int)((int)RoomInfo.CourseIndex & 0x7F)].ToArray());
                }
                p.WriteSByte(-1); // Não tem

                // Normal Assist essa season
                if (session.UserInfo.NormalMapStatisticsAll[(int)((int)RoomInfo.CourseIndex & 0x7F)].course != ((int)RoomInfo.CourseIndex & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)RoomInfo.CourseIndex & 0x7F);
                    p.WriteBytes(session.UserInfo.NormalMapStatisticsAll[(int)((int)RoomInfo.CourseIndex & 0x7F)].ToArray());
                }
                p.WriteSByte(-1); // Não tem

                // Natural Assist essa season
                if (session.UserInfo.NaturalMapStatisticsAll[(int)((int)RoomInfo.CourseIndex & 0x7F)].course != ((int)RoomInfo.CourseIndex & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)RoomInfo.CourseIndex & 0x7F);
                    p.WriteBytes(session.UserInfo.NaturalMapStatisticsAll[(int)((int)RoomInfo.CourseIndex & 0x7F)].ToArray());
                }
                p.WriteSByte(-1); // Não tem

                // Grand Prix essa season
                if (session.UserInfo.GrandPrixMapStatistics[(int)((int)RoomInfo.CourseIndex & 0x7F)].course != ((int)RoomInfo.CourseIndex & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)RoomInfo.CourseIndex & 0x7F);
                    p.WriteBytes(session.UserInfo.GrandPrixMapStatistics[(int)((int)RoomInfo.CourseIndex & 0x7F)].ToArray());
                }
                p.WriteSByte(-1); // Não tem

                // Grand Prix Assist essa season
                if (session.UserInfo.GrandPrixMapStatisticsAll[(int)((int)RoomInfo.CourseIndex & 0x7F)].course != ((int)RoomInfo.CourseIndex & 0x7F))
                {
                    p.WriteSByte(-1); // Não tem
                }
                else
                {
                    p.WriteByte((char)RoomInfo.CourseIndex & 0x7F);
                    p.WriteBytes(session.UserInfo.GrandPrixMapStatisticsAll[(int)((int)RoomInfo.CourseIndex & 0x7F)].ToArray());
                }
                p.WriteSByte(-1); // Não tem
            }

            session.Send(p);
        }

        // Envia a AppMessage no char para todos player do Game que o player terminou o jogo
        public void SendFinishMessage(Player session)
        {

            var pgi = GetPlayerInfo(session);
            if (pgi == null)
            {
                throw new exception("[GameBase::sendFinishMessage][Error] Normal[UID=" + Convert.ToString(session.UserInfo.UID) + "] " + "tentou enviar AppMessage no chat que o player terminou o jogo" + ", mas o game nao tem o info dele guardado. Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME,
                    1, 4));
            }

            Packet p = new Packet((ushort)0x40);

            p.WriteByte(16); // Msg que terminou o game

            p.WriteString(session.UserInfo.NickName);
            p.WriteUInt16(0); // Size Msg

            p.WriteInt32(pgi.data.score);
            p.WriteUInt64(pgi.data.pang);
            p.WriteByte(pgi.assist_flag);

            SendBroadCast(p);
        }

        public virtual void CalculeRankPlace()
        {
            if (PlayerOrder.Count > 0)
            {
                PlayerOrder.Clear();
            }

            foreach (var el in PlayerInfo)
            {
                if (el.Value.flag != eFLAG_GAME.QUIT) // menos os que quitaram
                {
                    PlayerOrder.Add(el.Value);
                }
            }

            PlayerOrder.Sort(SortPlayerRank);
        }

        public virtual bool CheckEndGame(Player session)
        {
            var info = GetPlayerInfo(session);

            if (info == null)
            {
                string errorMsg = $"[GameBase::CheckEndGame][Error] Normal[UID={session.UserInfo.UID}] " +
                                  "tentou verificar se é o final do jogo, mas o game não tem o info dele guardado. Bug";

                throw new exception(errorMsg, ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4));
            }

            // Check if the current hole sequence matches the total quantity of holes
            return Course.findHoleSeq(info.hole) == RoomInfo.HoleCount;
        }

        public bool PlayersCompleteGameAndClear()
        { 
            var playersSnapshot = Players.ToList();
            int finishedCount = 0;

            foreach (var player in playersSnapshot)
            {
                try
                {
                    var info = GetPlayerInfo(player);

                    if (info == null)
                    {
                        throw new exception(
                            $"[GameBase::PlayersCompleteGameAndClear][Error] Normal[UID={player.UserInfo.UID}] " +
                            "tentou verificar se o player terminou o jogo, mas o game nao tem o info dele guardado. Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4)
                        );
                    }

                    if (info.finish_game == 1)
                    {
                        finishedCount++;
                    }
                }
                catch (exception e)
                {
                    var logMsg = $"[GamePlayersCompleteGameAndClear][ErrorSystem] {e.getFullMessageError()}";
                    _smp.LogManager.Instance.push(new AppMessage(logMsg, type_msg.CL_FILE_LOG_AND_CONSOLE));
                }
            }

            return finishedCount == playersSnapshot.Count;
        }

        public void SetGameFlag(PlayerGameInfo info, PlayerGameInfo.eFLAG_GAME flag)
        {
            if (info == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::setGameFlag][Error] PlayerGameInfo is null.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }
            info.flag = flag;
        }

        public void SetFinishGameFlag(PlayerGameInfo info, byte finishGame)
        {
            if (info == null)
            {
                _smp.LogManager.Instance.push(new AppMessage("[GameBase::setFinishGameFlag][Error] PlayerGameInfo is null.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }
            info.finish_game = finishGame;
        }

        public bool AllCompleteGameAndClear()
        {
            // Fix: Create a snapshot with .ToArray() to prevent "Collection Modified" errors
            var playerList = Players.ToArray();
            int finishedCount = 0;

            foreach (var player in playerList)
            {
                try
                {
                    var info = GetPlayerInfo(player);
                    if (info == null)
                    {
                        throw new exception(
                            $"[GameBase::AllCompleteGameAndClear][Error] Normal[UID={player.UserInfo.UID}] info missing. Bug",
                            ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME, 1, 4)
                        );
                    }

                    // A player is "complete" if they are no longer in the PLAYING StateRoom
                    if (info.flag != PlayerGameInfo.eFLAG_GAME.PLAYING)
                    {
                        finishedCount++;
                    }
                }
                catch (exception e)
                {
                    _smp.LogManager.Instance.push(new AppMessage(
                        $"[GameBase::AllCompleteGameAndClear][ErrorSystem] {e.getFullMessageError()}",
                        type_msg.CL_FILE_LOG_AND_CONSOLE)
                    );
                }
            }

            // Ensure we compare against the count of the snapshot we just checked
            return finishedCount == playerList.Length;
        }
    }
}
