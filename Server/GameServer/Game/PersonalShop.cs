using System;
using System.Collections.Generic;
using System.Linq;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.Models;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.UTIL;
using PangyaAPI.IFF.JP.Extensions;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using static Pangya_GameServer.Models.DefineConstants;
using PangyaAPI.IFF.JP.Models.Flags;
namespace Pangya_GameServer.Game
{
    public class PersonalShop
    {
        //-----------------SHOP PERSONAL--------------------
        public int LIMIT_VISIT_ON_SAME_TIME = 15;
        // Card limit price
        public uint CARD_NORMAL_LIMIT_PRICE = 200000u;
        public uint CARD_RARE_LIMIT_PRICE = 400000u;
        public uint CARD_SUPER_RARE_LIMIT_PRICE = 1000000u;
        public uint CARD_SECRET_LIMIT_PRICE = 2000000u;
        // Shop min and max price item
        public uint ITEM_MIN_PRICE = 1u;
        public uint ITEM_MAX_PRICE = 9999999u;
        //----------------SHOP PERSONAL END---------------------
        public enum STATE : uint
        {
            OPEN_EDIT,
            OPEN
        }

        public PersonalShop(Player _session)
        {
            lock (_lockObj)
            {
                var m_reader_ini = new IniHandle("config/personal_config.ini");
                 
                // Card limit price
                CARD_NORMAL_LIMIT_PRICE = m_reader_ini.ReadUInt32("CONFIG", "CARD_NORMAL_LIMIT_PRICE");//cmd_psc.getPrice(1);
                CARD_RARE_LIMIT_PRICE = m_reader_ini.ReadUInt32("CONFIG", "CARD_RARE_LIMIT_PRICE");//cmd_psc.getPrice(2);
                CARD_SUPER_RARE_LIMIT_PRICE = m_reader_ini.ReadUInt32("CONFIG", "CARD_SUPER_RARE_LIMIT_PRICE");//cmd_psc.getPrice(3);
                CARD_SECRET_LIMIT_PRICE = m_reader_ini.ReadUInt32("CONFIG", "CARD_SECRET_LIMIT_PRICE");//cmd_psc.getPrice(4);
                // Shop min and max price item
                ITEM_MIN_PRICE = m_reader_ini.ReadUInt32("CONFIG", "ITEM_MIN_PRICE");//cmd_psc.getPrice(5);
                ITEM_MAX_PRICE = m_reader_ini.ReadUInt32("CONFIG", "ITEM_MAX_PRICE");//cmd_psc.getPrice(6);
                this.m_owner = _session;
                this.m_name = "";
                this.m_visit_count = 0;
                this.m_pang_sale = 0Ul;
                this.m_state = STATE.OPEN_EDIT;
            }
        }

        // Gets
        public string getName()
        {
            return m_name;
        }

        public uint getVisitCount()
        {
            return m_visit_count;
        }

        public ulong getPangSale()
        {
            return m_pang_sale;
        }

        public Player getOwner()
        {
            lock (_lockObj)
            {
                return m_owner;
            }
        }

        public STATE getState()
        {
            lock (_lockObj)
            {
                return m_state;
            }
        }

        public uint getCountItem()
        {
            lock (_lockObj)
            {
                return (uint)v_item.Count();
            }
        }

        public List<Player> getClients()
        {
            return v_open_shop_visit;
        }

        // Sets
        public void setName(string _name)
        {
            if (_name.Length == 0)
            {
                throw new exception("[PersonalShop::setName][Error] _name is empty", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    1, 0));
            }

            m_name = _name;
        }

        public void setState(STATE _state)
        {
            m_state = _state;
        }

        public void clearItem()
        {
            v_item.Clear();
        }

        public void pushItem(PersonalShopItem _psi)
        {
            // Verifica aqui se esse item por ser colocar no shop

            if (_psi.item._typeid == 0)
            {
                throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um invalid item no Personal Shop dele. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    7, 0));
            }

            var @base = sIff.getInstance().findCommomItem(_psi.item._typeid);

            if (@base == null)
            {
                throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um item[TYPEID=" + Convert.ToString(_psi.item._typeid) + "] que nao existe no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    8, 0));
            }

            if (!@base.Shop.flag_shop.can_send_mail_and_personal_shop)
            {
                throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um item[TYPEID=" + Convert.ToString(_psi.item._typeid) + "] que nao pode ser vendido no Personal Shop. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    9, 0));
            }

            // Verifica o pre�o do item
            if (_psi.item.pang < ITEM_MIN_PRICE || _psi.item.pang > ITEM_MAX_PRICE)
            {
                throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um item[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", Price=" + Convert.ToString(_psi.item.pang) + "] que o preco esta fora do limite[MIN=" + Convert.ToString(ITEM_MIN_PRICE) + ", MAX=" + Convert.ToString(ITEM_MAX_PRICE) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 24, 0));
            }

            // Card pre�o controle
            if (sIff.getInstance().getItemGroupIdentify(_psi.item._typeid) == IFF_GROUP.CARD)
            {

                var card = sIff.getInstance().findCard(_psi.item._typeid);

                if (card == null)
                {
                    throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um card[TYPEID=" + Convert.ToString(_psi.item._typeid) + "] que nao existe no IFF_STRUCT do server. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                        21, 0));
                }

                switch (card.Rarity)
                {
                    case 0: // Normal
                        if (_psi.item.pang > CARD_NORMAL_LIMIT_PRICE)
                        {
                            throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", TYPE=Normal, Price=" + Convert.ToString(_psi.item.pang) + "] que o preco passa do limite(" + Convert.ToString(CARD_NORMAL_LIMIT_PRICE) + "). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                                23, 0));
                        }
                        break;
                    case 1: // Rare
                        if (_psi.item.pang > CARD_RARE_LIMIT_PRICE)
                        {
                            throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", TYPE=Rare, Price=" + Convert.ToString(_psi.item.pang) + "] que o preco passa do limite(" + Convert.ToString(CARD_RARE_LIMIT_PRICE) + "). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                                23, 0));
                        }
                        break;
                    case 2: // Super Rare
                        if (_psi.item.pang > CARD_SUPER_RARE_LIMIT_PRICE)
                        {
                            throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", TYPE=Super Rare, Price=" + Convert.ToString(_psi.item.pang) + "] que o preco passa do limite(" + Convert.ToString(CARD_SUPER_RARE_LIMIT_PRICE) + "). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                                23, 0));
                        }
                        break;
                    case 3: // Secret
                        if (_psi.item.pang > CARD_SECRET_LIMIT_PRICE)
                        {
                            throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", TYPE=Secret, Price=" + Convert.ToString(_psi.item.pang) + "] que o preco passa do limite(" + Convert.ToString(CARD_SECRET_LIMIT_PRICE) + "). Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                                23, 0));
                        }
                        break;
                    default: // Unknown Type
                        throw new exception("[PersonalShop::pushItem][Error] PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "] tentou colocar um card[TYPEID=" + Convert.ToString(_psi.item._typeid) + ", TYPE=" + Convert.ToString((ushort)card.Rarity) + "] que o tipo é desconhecido. (N,R,SR e SC) Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 22, 0));
                }
            }

            v_item.Add(_psi);
        }

        public void deleteItem(PersonalShopItem _psi)
        {

            var item = findItemIndexById(_psi.item.id);

            if (v_item[item].item.id == _psi.item.id)
            {
                v_item.RemoveAt(item);
            }
            else
            {
                for (int ii = 0; ii < v_item.Count; ii++)
                {
                    if (v_item[ii].item.id == _psi.item.id)
                    {
                        v_item.RemoveAt(ii);
                        break; // Importante para evitar problemas após remover um item
                    }
                }
            }

        }

        public void putItemOnPacket(PangyaBinaryWriter _p)
        {

            if (v_item.Count() == 0)
            {
                throw new exception("[PersonalShop::putItemOnPacket][Error] size vector item shop is zero", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    2, 0));
            }

            _p.WriteUInt32((uint)v_item.Count());

            for (var i = 0; i < v_item.Count(); ++i)
            {
                _p.WriteBytes(v_item[i].ToArray());
            }
        }

        // Find
        public PersonalShopItem findItemById(int _id)
        {

            if (_id <= 0)
            {
                throw new exception("[PersonalShop::findItemById][Error] _id[value=" + Convert.ToString(_id) + "] is invalid", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    3, 0));
            }

            PersonalShopItem psi = null;

            foreach (var el in v_item)
            {
                if (el.item.id == _id)
                {
                    psi = el;
                    break;
                }
            }

            return psi;
        }

        public PersonalShopItem findItemByIndex(uint _index)
        {

            PersonalShopItem psi = null;

            foreach (var el in v_item)
            {
                if (el.index == _index)
                {
                    psi = el;
                    break;
                }
            }

            return psi;
        }

        public int findItemIndexById(int _id)
        {

            int index = -1;

            for (var i = 0; i < v_item.Count(); ++i)
            {
                if (v_item[i].item.id == _id)
                {
                    index = (int)i;
                    break;
                }
            }

            return (index);
        }

        public Player findClientByUID(uint _uid)
        {

            Player client = null;

            foreach (var el in v_open_shop_visit)
            {
                if (el.m_pi.uid == _uid)
                {
                    client = el;
                    break;
                }
            }

            return client;
        }

        public int findClientIndexByUID(uint _uid)
        {

            int index = -1;

            for (var i = 0; i < v_open_shop_visit.Count(); ++i)
            {
                if (v_open_shop_visit[i].m_pi.uid == _uid)
                {
                    index = (int)i;
                    break;
                }
            }

            return index;
        }

        // Visit
        public void addClient(Player _session)
        {

            if (m_state != STATE.OPEN)
            {
                throw new exception("[PersonalShop::addClient][Error] client[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou entrar no shop do PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "], mas ele nao esta aberto no momento. Hacker ou Bug.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    25, 0));
            }

            var client = findClientByUID(_session.m_pi.uid);

            if (client != null)
            {
                throw new exception("[PersonalShop::addClient][Error] client[UID=" + Convert.ToString(_session.m_pi.uid) + "] ja existe no Personal Shop do PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    4, 0));
            }

            if (v_open_shop_visit.Count() >= LIMIT_VISIT_ON_SAME_TIME)
            {
                throw new exception("[PersonalShop::addClient][Error] client[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao pode entrar no shop por que ja chegou ao limit de clientes ao mesmo tempo no Personal Shop do PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "]", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    6, 0));
            }

            v_open_shop_visit.Add(_session);

            // Add Contador de visitas
            m_visit_count++;
        }

        public void deleteClient(Player _session)
        {

            var client = findClientIndexByUID(_session.m_pi.uid);

            if (client == -1)
            {
                throw new exception("[PersonalShop::deleteClient][Error] client[UID=" + Convert.ToString(_session.m_pi.uid) + "] nao existe no vector de clientes do Personal Shop do PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "]. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    5, 0));
            }

            if (v_open_shop_visit[client].m_pi.uid == _session.m_pi.uid)
            {
                v_open_shop_visit.RemoveAt(client);
            }
            else
            {
                for (int ii = 0; ii < v_open_shop_visit.Count; ii++)
                {
                    if (v_open_shop_visit[ii].m_pi.uid == _session.m_pi.uid)
                    {
                        v_open_shop_visit.RemoveAt(ii);
                        break; // Para evitar problemas de indexação após a remoção
                    }
                }
            }
        }
        /// <summary>
        /// 100%
        /// </summary>
        /// <param name="_session">cliente</param>
        /// <param name="_psi">dados recebidos pelo cliente</param>
        /// <exception cref="exception">retorna um erro em caso</exception>
        public void buyItem(Player _session, PersonalShopItem _psi)
        {
            // 1. SEGURANÇA: Verifica se o comprador não é o próprio dono (Anti-Exploit de Achievements/Pangs)
            if (_session.m_pi.uid == m_owner.m_pi.uid)
            {
                throw new exception("[PersonalShop::buyItem][HACK] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comprar de si mesmo no próprio Shop. Hacker detectado.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 50, 0));
            }

            // 2. ESTADO DO SHOP: Verifica se a loja ainda está aberta
            if (m_state != STATE.OPEN)
            {
                throw new exception("[PersonalShop::buyItem][Error] client[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comprar no shop do PLAYER[UID=" + Convert.ToString(m_owner.m_pi.uid) + "], mas a loja foi fechada. Hacker ou Bug.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 25, 0));
            }

            // 3. EXISTÊNCIA DO CLIENTE: Verifica se o comprador está visualizando a loja
            if (findClientByUID(_session.m_pi.uid) == null)
            {
                throw new exception("[PersonalShop::buyItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comprar sem estar na lista de visualização da loja.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 10, 0));
            }

            // 4. INTEGRIDADE DO ITEM: Localiza o item na memória do SERVIDOR (Vendedor)
            // NUNCA usamos o preço ou dados que vêm do pacote do comprador (_psi) como verdade.
            var item_real_vendedor = findItemById(_psi.item.id);
            if (item_real_vendedor == null)
            {
                throw new exception("[PersonalShop::buyItem][HACK] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comprar um Item ID[" + _psi.item.id + "] que não existe nesta loja.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 11, 0));
            }

            var psi_owner = new PersonalShopItem(item_real_vendedor);

            // 5. VERIFICAÇÃO DE QUANTIDADE (Anti-Overflow e Valores Negativos)
            if (_psi.item.qntd <= 0 || _psi.item.qntd > 30000)
            {
                throw new exception("[PersonalShop::buyItem][HACK] Quantidade de compra inválida enviada pelo cliente: " + _psi.item.qntd,
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 26, 0));
            }

            if (_psi.item.qntd > psi_owner.item.qntd)
            {
                throw new exception("[PersonalShop::buyItem][Error] PLAYER[UID=" + Convert.ToString(_session.m_pi.uid) + "] tentou comprar QNTD[" + _psi.item.qntd + "], mas o estoque é apenas [" + psi_owner.item.qntd + "].",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 26, 1));
            }

            // 6. VALIDAÇÃO DO IFF: Verifica se o item ainda é válido no servidor
            var @base = sIff.getInstance().findCommomItem(psi_owner.item._typeid);
            if (@base == null || psi_owner.item._typeid == 0)
            {
                throw new exception("[PersonalShop::buyItem][Error] Item TYPEID[" + psi_owner.item._typeid + "] inválido ou não existe no IFF.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 13, 0));
            }

            // 7. PERMISSÕES DO ITEM: Verifica se o item pode ser vendido em Personal Shop
            if (!@base.Shop.flag_shop.can_send_mail_and_personal_shop)
            {
                throw new exception("[PersonalShop::buyItem][HACK] Item TYPEID[" + psi_owner.item._typeid + "] não é permitido em Personal Shop.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 14, 0));
            }

            // 8. CÁLCULO DE CUSTO SEGURO: Usamos o preço definido pelo DONO da loja
            ulong total_pang_cost = (ulong)psi_owner.item.pang * (ulong)_psi.item.qntd;

            // 9. SALDO DO COMPRADOR
            if (_session.m_pi.ui.pang < total_pang_cost)
            {
                throw new exception("[PersonalShop::buyItem][Error] Comprador não possui Pangs suficientes. Requerido: " + total_pang_cost,
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 15, 0));
            }

            // 10. REQUISITO DE NÍVEL
            if (!@base.Level.GoodLevel((byte)_session.m_pi.level))
            {
                throw new exception("[PersonalShop::buyItem][Error] Level insuficiente para este item.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 16, 0));
            }

            // --- INÍCIO DA TRANSFERÊNCIA ---
            var psi_r = new PersonalShopItem(_psi); // Cópia para o comprador
            object pWi = null;

            if ((pWi = ItemManager.transferItem(m_owner, _session, _psi, psi_r)) != null)
            {
                // Atualiza o estoque do vendedor
                if (psi_owner.item.qntd == _psi.item.qntd)
                {
                    deleteItem(psi_owner);
                }
                else
                {
                    psi_owner.item.qntd -= _psi.item.qntd;
                    UpdateItemList(psi_owner);
                }

                // Executa a transação financeira
                _session.m_pi.consomePang(total_pang_cost);

                // Vendedor recebe 95% (Taxa de 5% do servidor)
                ulong pang_to_owner = (ulong)Math.Round(total_pang_cost * 0.95f);
                m_owner.m_pi.addPang(pang_to_owner);

                m_pang_sale += pang_to_owner;

                // --- ATUALIZAÇÃO DE PACOTES (0xEC / 0xED) ---
                var p = new PangyaBinaryWriter();
                IFF_GROUP group = sIff.getInstance().getItemGroupIdentify(_psi.item._typeid);

                // Lógica de pacotes para Cartões ou Itens Normais
                if (group == IFF_GROUP.CARD)
                {
                    var ci_r = (CardInfo)pWi;
                    // Pacote para o Vendedor (Remover Item)
                    p.init_plain(0xEC);
                    p.WriteUInt32(1); p.WriteByte(1); p.WriteUInt64(pang_to_owner);
                    p.WriteBytes(_psi.ToArray());
                    p.WriteByte(5); // Card Group
                    ci_r.id = _psi.item.id; ci_r.qntd = _psi.item.qntd;
                    p.WriteBytes(ci_r.ToArray());
                    packet_func.session_send(p, m_owner, 1);

                    // Pacote para o Comprador (Adicionar Item)
                    p.init_plain(0xEC);
                    p.WriteUInt32(1); p.WriteByte(0); p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteBytes(psi_r.ToArray());
                    p.WriteByte(5);
                    ci_r.id = psi_r.item.id; ci_r.qntd = psi_r.item.qntd;
                    p.WriteBytes(ci_r.ToArray());
                    packet_func.session_send(p, _session, 1);
                }
                else
                {
                    var wi_source = new WarehouseItemEx(pWi as WarehouseItemEx) { id = _psi.item.id, STDA_C_ITEM_QNTD = (short)_psi.item.qntd };
                    var wi_dest = new WarehouseItemEx(pWi as WarehouseItemEx) { id = psi_r.item.id, STDA_C_ITEM_QNTD = (short)psi_r.item.qntd };

                    byte itemGroupByte = (byte)(group == IFF_GROUP.ITEM ? 1 : 3);

                    // Tira de quem vendeu
                    p.init_plain(0xEC);
                    p.WriteUInt32(1); p.WriteByte(1); p.WriteUInt64(pang_to_owner);
                    p.WriteBytes(_psi.ToArray());
                    p.WriteByte(itemGroupByte);
                    p.WriteBytes(wi_source.ToArray());
                    packet_func.session_send(p, m_owner, 1);

                    // Add para quem comprou
                    p.init_plain(0xEC);
                    p.WriteUInt32(1); p.WriteByte(0); p.WriteUInt64(_session.m_pi.ui.pang);
                    p.WriteBytes(psi_r.ToArray());
                    p.WriteByte(itemGroupByte);
                    p.WriteBytes(wi_dest.ToArray());
                    packet_func.session_send(p, _session, 1);
                }

                // Notifica todos na loja que o item foi vendido
                p.init_plain(0xED);
                p.WritePStr(m_owner.m_pi.nickname);
                p.WriteUInt32(m_owner.m_pi.uid);
                p.WriteBytes(_psi.ToArray());
                p.WriteInt32(v_item.Count() == 0 ? 3 : 1);
                shop_broadcast(p, _session, 1);

                // Mensagem de Sucesso para o Vendedor
                p.init_plain(0x40);
                p.WriteByte(7); // Notice
                p.WriteString("@INI3");
                p.WriteString("\\c0xff00ff00\\cParabéns, sua venda foi um sucesso!.");
                packet_func.session_send(p, m_owner, 1);

                // Atualiza Conquista (Achievement)
                AchievementSystem sys_achieve = new AchievementSystem();
                sys_achieve.incrementCounter(0x6C400083u);
                sys_achieve.finish_and_update(_session);
            }
            else
            {
                throw new exception("[PersonalShop::buyItem][Error] Falha crítica no ItemManager.transferItem.",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP, 19, 0));
            }
        }

        public void shop_broadcast(PangyaBinaryWriter _p,
            Player _s, byte _debug)
        {

            if (_s == null)
            {
                throw new exception("[PersonalShop::shop_broadcast][Error] Session *_s is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    20, 0));
            }
             
            var clients = v_open_shop_visit;

            // Envia para o dono do Personal Shop
            packet_func.session_send(_p,
                m_owner, 1);

            foreach (var el in clients)
            {
                packet_func.session_send(_p,
                    el, _debug);
            }
        }

        public void shop_broadcast(List<PangyaBinaryWriter> _v_p,
            Player _s, byte _debug)
        {

            if (_s == null)
            {
                throw new exception("[PersonalShop::shop_broadcast][Error] Session *_s is null", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PERSONAL_SHOP,
                    20, 0));
            }
             
            var clients = v_open_shop_visit;

            // Envia para o dono do Personal Shop
            packet_func.session_send(_v_p,
                m_owner, 1);

            foreach (var el in clients)
            {
                packet_func.session_send(_v_p,
                    el, _debug);
            }
        }

        public void UpdateItemList(PersonalShopItem _psi)
        {
            var idx = v_item.FindIndex(c => c.item.id == _psi.item.id);
            if (idx >= 0)
                v_item[idx] = _psi;
        }

        protected string m_name = ""; // Nome da Loja
        protected Player m_owner; // Dono da Loja

        protected STATE m_state = new STATE(); // estado da loja, aberta ou editando

        protected uint m_visit_count = new uint(); // N�mero de visitantes que visitaram a loja
                                                                                                        
        protected ulong m_pang_sale = new ulong(); // pangs em caixa

        protected List<PersonalShopItem> v_item = new List<PersonalShopItem>(); // Itens da Loja   
        protected List<Player> v_open_shop_visit = new List<Player>(); // Os visitantes que est�o com o shop aberto
        private readonly object _lockObj = new object();
    }
}
