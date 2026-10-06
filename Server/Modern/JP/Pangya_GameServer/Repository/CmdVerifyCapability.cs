
using Pangya_GameServer.Feature;
using Pangya_GameServer.Models;
using PangyaAPI.DataBase;
using System;

namespace Pangya_GameServer.Repository
{
    public class CmdVerifyCapability : Pangya_DB
    {
        private uint m_uid;
        private PlayerCapability m_cap; 

        public CmdVerifyCapability(uint uid)
        {
            m_uid = uid;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(2);

            try
            {
                int db_uid = _result.GetInt32(0);
                m_cap = new PlayerCapability(_result.GetInt32(1));

                if (db_uid != m_uid)
                    throw new Exception($"[CmdVerifyCapability][Error] UID não bate. Req: {m_uid}, DB: {db_uid}");

                if (4 != m_cap.Value)
                    throw new Exception($"[CmdVerifyCapability][Error] Capacidade não bate. Req: {m_cap.Value}, DB: {4}"); 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); 
            }
        }

        protected override Response prepareConsulta()
        {
            var r = consulta($"SELECT UID, Capability FROM pangya.account WHERE UID = {m_uid}");
            checkResponse(r, $"Não conseguiu verificar Capability do UID: {m_uid}");
            return r;
        }

        public bool IsValid()
        {
            return m_cap.IsGameMaster;
        } 
    }
}
