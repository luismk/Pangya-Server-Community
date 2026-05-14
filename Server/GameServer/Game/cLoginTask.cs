using System;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Game.System;
using Pangya_GameServer.PacketFunc;
using Pangya_GameServer.PangyaEnums;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Network.Repository;

namespace Pangya_GameServer.Game
{
    public class LoginTask : IDisposable
    {
        private Player m_session;
        private uint m_count;
        private bool m_finish;
        private bool disposedValue;

        public LoginTask(Player session)
        {
            m_session = session;
            m_count = 0;
            m_finish = false;
        }

        public void exec()
        {
            snmdb.NormalManagerDB.getInstance().add(2, new CmdUserEquip(m_session.m_pi.uid), LoginManager.SQLDBResponse, this);
        }

        public Player getSession { get => m_session; set => m_session = value; }
        public void finishSessionInvalid() => m_finish = true;

        public void sendFailLogin()
        {
            try
            {
                var p = new PangyaBinaryWriter(0x44);
                p.WriteByte(eLoginAck.ACK_LOGIN_FAIL);
                p.WriteInt32(1);
                packet_func.session_send(p, m_session, 1);
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[LoginTask::sendFailLogin][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
            m_finish = true;
        }

        public void sendCompleteData()
        {
            if (!m_session.isConnected() || !m_session.isCreated() || !m_session.getState())
            {
                _smp.message_pool.getInstance().push(new message("[LoginTask::sendCompleteData][Error] session is invalid.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                finishSessionInvalid();
                return;
            }

            try
            {
                var p = new PangyaBinaryWriter();
                var pi = m_session.m_pi;

                //// Check All Character All Item Equiped is on Warehouse Item of Player
                lock (pi.mp_ce)
                {
                    foreach (var el in pi.mp_ce)
                    {
                        // Check Parts of Character e Check Aux Part of Character
                        m_session.checkCharacterAllItemEquiped(el.Value);
                    } 
                }
                // Check All Item Equiped
                m_session.checkAllItemEquiped(pi.ue);

                // Envia todos pacotes aqui, alguns envia antes, por que agora estou usando o jeito o pangya original   

                packet_func.session_send(packet_func.pacote044(sgs.gs.getInstance().getInfo(), 0, pi), m_session);

                packet_func.session_send(packet_func.pacote070(pi.mp_ce), m_session); // characters

                packet_func.session_send(packet_func.pacote071(pi.mp_ci), m_session); //caddies   

                packet_func.session_send(pi.mp_wi.Build(), m_session); //inventory(warehouse)   

                packet_func.session_send(packet_func.pacote0E1(pi.mp_mi), m_session); //mascots

                packet_func.session_send(packet_func.pacote072(pi.ue), m_session); // equip selected                     

                sgs.gs.getInstance().sendChannelListToSession(m_session);

                packet_func.session_send(packet_func.pacote102(pi), m_session);        // Pacote novo do JP, passa os coupons do Gacha JP

                // Treasure Hunter Info
                packet_func.session_send(packet_func.pacote131(), m_session);

                pi.mgr_achievement.sendCounterItemToPlayer(m_session);

                pi.mgr_achievement.sendAchievementToPlayer(m_session);

                //call messenger server
                packet_func.session_send(packet_func.pacote0F1(), m_session);

                packet_func.session_send(packet_func.pacote135(), m_session);

                packet_func.session_send(packet_func.pacote144(), m_session);        // Pacote novo do JP

                packet_func.session_send(packet_func.pacote138(pi.v_card_info), m_session);

                packet_func.session_send(packet_func.pacote136(), m_session);

                packet_func.session_send(packet_func.pacote137(pi.v_cei), m_session);
                //call messenger server
                packet_func.session_send(packet_func.pacote13F(), m_session);
                packet_func.session_send(packet_func.pacote181(pi.v_ib), m_session);
                packet_func.session_send(packet_func.pacote096(pi), m_session);
                packet_func.session_send(packet_func.pacote169(pi.ti_current_season, 5/*season atual*/), m_session);
                packet_func.session_send(packet_func.pacote169(pi.ti_rest_season), m_session);
                packet_func.session_send(packet_func.pacote0B4(pi.v_tsi_current_season, 5/*season atual*/), m_session);
                packet_func.session_send(packet_func.pacote0B4(pi.v_tsi_rest_season), m_session);
                packet_func.session_send(packet_func.pacote158(pi.uid, pi.ui, 0), m_session);
                //// Total de season, 5 atual season  
                packet_func.session_send(packet_func.pacote25D(pi.v_tgp_current_season, 5/*season atual*/), m_session);
                packet_func.session_send(packet_func.pacote25D(pi.v_tgp_rest_season, 0), m_session); 

                if (sgs.gs.getInstance().getInfo().rate.login_reward_event == 1)
                    sLoginRewardSystem.getInstance().checkRewardLoginAndSend(m_session);

            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().push(new message("[LoginTask::sendCompleteData][ErrorSystem] " + e.Message, type_msg.CL_FILE_LOG_AND_CONSOLE));
            }

            m_finish = true;
        }

        public void sendReply(int msg_id)
        {
            var p = new PangyaBinaryWriter(0x44);
            p.WriteByte(eLoginAck.ACK_UPDATE_LOGIN_UNIT);
            p.WriteInt32(msg_id);
            packet_func.session_send(p, m_session, 1);
        }

        public uint getCount() => m_count;

        public uint incremenetCount() => ++m_count;

        public bool isFinished() => m_finish;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_session = null;
                    m_count = 0;
                    m_finish = false;
                }

                // TODO: liberar recursos não gerenciados (objetos não gerenciados) e substituir o finalizador
                // TODO: definir campos grandes como nulos
                disposedValue = true;
            }
        }

        // // TODO: substituir o finalizador somente se 'Dispose(bool disposing)' tiver o código para liberar recursos não gerenciados
        ~LoginTask()
        {
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Não altere este código. Coloque o código de limpeza no método 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
