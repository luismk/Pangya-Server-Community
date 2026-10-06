using Pangya_GameServer.Engine;
using Pangya_GameServer.Feature;
using Pangya_GameServer.Flags;
using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.DataBase;
using PangyaAPI.IFF.Flags;
using PangyaAPI.IFF.Handle.JP;
using PangyaAPI.Network;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.Manager
{
    /// <summary>
    /// sistema proprio, compra de itens na loja do pangya
    /// converte em brindes como cashback
    /// </summary>
    public class ShopGiftSystem
    {
        List<ShopGift> m_shop_gift; // Todos os brindes
        bool m_load; // Status de carregamento do ShopGiftSystem
        public ShopGiftSystem()
        {
            this.m_load = false;
            this.m_shop_gift = new List<ShopGift>();
        }

        private void initialize()
        {
            //// Load System
            var cmd_sg = new CmdShopGift(); // Waiter

            NormalManagerDB.Instance.add(0,
                  cmd_sg, SQLDBResponse, null);

            if (cmd_sg.getException().getCodeError() != 0)
            {
                throw cmd_sg.getException();
            }

            m_shop_gift = cmd_sg.getInfo();

            // Carregado com sucesso!
            m_load = true;
        }

        private static void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {

            if (_arg == null)
            {
                return;
            }

            // Por Hora só sai, depois faço outro Type de tratamento se precisar
            if (_pangya_db.getException().getCodeError() != 0)
            {
                _smp.LogManager.Instance.push(new AppMessage("[ShopGiftSystem::SQLDBResponse][Error] " + _pangya_db.getException().getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
                return;
            }

            // isso aqui depois pode mudar para o Item_manager, que vou tirar de ser uma classe static e usar ela como objeto(instancia)
            //auto _session = reinterpret_cast< player* >(_arg);

            switch (_msg_id)
            {
                case 0:
                default:
                    break;
            }
        }

        public void clear()
        {
            if (m_shop_gift.Any())
            {
                m_shop_gift.Clear();
            }

            m_load = false;
        }

        public void load()
        {

            if (isLoad())
            {
                clear();
            }

            initialize();
        }
        public bool isLoad()
        {
            return m_load && m_shop_gift.Count > 0;
        }

        public void checkAndGrantGifts(Player _session, ulong totalSpent)
        {
            if (!isLoad())
            {
                throw new exception("[ShopGiftSystem::checkAndGrantGifts][Error] Shop Gift System not loaded, load system first.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                    5, 0));
            }


            foreach (var gift in m_shop_gift)
            {
                if (totalSpent >= gift.required_price)
                {
                    sendGiftToPlayer(_session, gift);
                }
            }
        }

        public async void sendGiftToPlayer(Player _session, ShopGift gift)
        {

            // Lambda[getItemName]
            Func<uint, string> getItemName = delegate (uint _typeid)
            {
                string ret = "";

                var @base = sIff.Instance.findCommomItem(_typeid);

                if (@base != null)
                    ret = @base.Name;

                return ret;
            };


            try
            {

                stItem item = new stItem();
                BuyItem bi = new BuyItem();

                // Limpa
                bi.clear();
                item.clear();

                // Initialize
                bi.id = -1;
                bi._typeid = (uint)gift.item_typeid;
                bi.qntd = (uint)gift.item_qntd;
                bi.time = 0;

                ItemManager.initItemFromBuyItem(_session.UserInfo,
                    item, bi, false, 0, 0,
                    1 /*nao Check Level*/);

                if (item._typeid == 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[ShopGiftSystem::sendGiftToPlayer][Error][Warning] tentou enviar o reward para o Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] o Item[" + Convert.ToString(gift.item_typeid) + "], mas nao conseguiu inicializar o item. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                var msg = "Shop Gift Reward System";

                if (MailManager.SendMessageWithItem(0,
                    _session.UserInfo.UID, msg, item) <= 0)
                {
                    _smp.LogManager.Instance.push(new AppMessage("[ShopGiftSystem::sendGiftToPlayer][Error][Warning] tentou enviar reward para o Normal[UID=" + Convert.ToString(_session.UserInfo.UID) + "] o Item[" + Convert.ToString(gift.item_typeid) + "], mas nao conseguiu colocar o item no mail box dele. Bug", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }


                NormalManagerDB.Instance.add(0,
                      new CmdInsertShopGiftLog(_session.UserInfo.UID,
                          gift.gift_id, gift.item_typeid,
                          gift.item_qntd),
                      null, null);
            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[ShopGiftSystem::sendGiftToPlayer][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        public void UpdateItemList()
        {

        }
    }


    public class sShopGiftSystem : Singleton<ShopGiftSystem>
    { }
}
