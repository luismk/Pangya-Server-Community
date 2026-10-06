using PangyaAPI.DataBase;
using PangyaAPI.Network.Config;
using PangyaAPI.Network.Core;
using PangyaAPI.Network.Handle;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Security;
using PangyaAPI.Network.Session;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using PangyaAPI.Utilities.Models;
using System;
using System.Net;
using System.Numerics;

namespace PangyaAPI.Network.Service
{
    public abstract class AppServer<T, TId> : AppServerBase<T, TId> where T : class, IAppSession where TId : struct, Enum
    {
        private List<IPBan> v_ip_ban_list;
        private List<string> v_mac_ban_list;
        public List<ServerInfo> m_server_list;
        public ServerInfo getInfo() => m_si;

        protected AppServer(AppSessionManager<T> sessionManager, PacketDispatcher<T, TId> dispatcher, ServerType typeServer) : base(sessionManager, dispatcher, typeServer)
        {
        }

        protected override void OnStop()
        {
            Console.WriteLine("[TcpServer] Stopping...");

            // limpar recursos globais
        }

        protected override void OnMonitor()
        {
            try
            {
                // 1. Log de rotação diária
                if (_smp.LogManager.Instance.check_update_day_log())
                {
                    _smp.LogManager.Instance.push(new AppMessage($"[{GetType().Name}::Monitor][Sucess] Update File Log.", type_msg.CL_FILE_LOG_AND_CONSOLE));
                }

                // 2. Sincronização com o Banco
                m_si.CurrentUsers = Sessions.Count;
                // 3. Atualização de listas
                CmdUpdateServerList();
                CmdUpdateListBlock_IP_MAC();
            }
            catch (Exception ex)
            {
                _smp.LogManager.Instance.push(new AppMessage(
                     $"[{GetType().Name}::Monitor][Error] {ex.Message}",
                     type_msg.CL_FILE_LOG_AND_CONSOLE));
            }
        }

        private void CmdUpdateServerList()
        {
            DBCommand.RegisterServer(m_si); //atualiza
            this.m_server_list = DBCommand.GetGame();//pegar todos
        }

        private void CmdUpdateListBlock_IP_MAC()
        {
            v_ip_ban_list = DBCommand.ListIPBan();

            v_mac_ban_list = DBCommand.ListMacBan();
        }

        public bool haveBanList(string _ip_address, string _mac_address, bool _check_mac = true)
        {
            if (_check_mac)
            {
                // Verifica primeiro se o MAC Address foi bloqueado

                // Cliente não enviou um MAC Address válido, bloquea essa conexão que é hacker que mudou o ProjectG
                if (string.IsNullOrEmpty(_mac_address))
                    return true;    // Cliente não enviou um MAC Address válido, bloquea essa conexão que é hacker que mudou o ProjectG

                foreach (var el in v_mac_ban_list)
                {
                    if (!string.IsNullOrEmpty(el) && string.Compare(el, _mac_address, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return true;
                    }
                }
            }
            // IP Address inválido, bloquea essa conexão que é Hacker ou Bug
            if (string.IsNullOrEmpty(_ip_address))
            {
                return true;
            }
            uint ip = 0;
            if (IPAddress.TryParse(_ip_address, out IPAddress ipAddress))
            {
                byte[] ipBytes = ipAddress.GetAddressBytes();
                ip = BitConverter.ToUInt32(ipBytes, 0);
                ip = (uint)IPAddress.NetworkToHostOrder((int)ip);
            }
            foreach (IPBan el in v_ip_ban_list)
            {
                if (el.type == IPBan._TYPE.IP_BLOCK_NORMAL)
                {
                    if ((ip & el.mask) == (el.ip & el.mask))
                    {
                        return true;
                    }
                }
                else if (el.type == IPBan._TYPE.IP_BLOCK_RANGE)
                {
                    if (el.ip <= ip && ip <= el.mask)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<T> GetAllSessions()
        {
            return SessionsManager.GetAllSessions();
        }

        public virtual List<T> FindAllGM()
        {
            return SessionsManager.FindAllGM();
        }

        public virtual T FindSessionByOid(int oid)
        {
            return SessionsManager.FindSessionByOid(oid);
        }

        public virtual T FindSessionByUid(uint uid)
        {
            return SessionsManager.FindSessionByUID(uid);
        }

        public virtual List<T> FindAllSessionByUid(uint uid)
        {
            return SessionsManager.FindAllSessionByUid(uid);
        }

        public virtual IAppSession FindSessionByNickname(string nickname)
        {
            return SessionsManager.FindSessionByNickname(nickname);
        }

        public virtual IAppSession? HasLoggedWithOuterSocket(T _session)
        {
            // Buscamos todas as sessões que possuem o mesmo UID do banco de dados
            // O FindAllSessionByUid já retorna uma List<T> filtrada pelo UID
            var sessions = SessionsManager.FindAllSessionByUid(_session.GetUID());

            if (sessions == null || sessions.Count == 0)
                return null;

            return sessions.FirstOrDefault(el =>
                el.ConnectionID != -1 &&
                _session.ConnectionID != -1 &&
                el.ConnectionID != _session.ConnectionID &&
                el.Connected);
        }
    }
}
