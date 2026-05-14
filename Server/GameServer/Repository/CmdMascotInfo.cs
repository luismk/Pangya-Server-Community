using System;
using System.Data;
using System.Linq;
using Pangya_GameServer.Game.Manager;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities.Log;


namespace Pangya_GameServer.Repository
{
    public class CmdMascotInfo : Pangya_DB
    {
        uint m_uid = uint.MaxValue;
        public enum TYPE : int
        {
            ALL,
            ONE,
        }
        TYPE m_type;
        uint m_item_id;
        MascotManager v_mi;
        public CmdMascotInfo(uint _uid, TYPE _type, uint _item_id = 0)
        {
            m_uid = _uid;
            m_type = _type;
            m_item_id = _item_id;
            v_mi = new MascotManager();

        }

        public CmdMascotInfo(uint _uid, int _type, uint _item_id)
        {
            m_uid = _uid;
            m_type = (TYPE)_type;
            m_item_id = _item_id;
            v_mi = new MascotManager();

        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {
            checkColumnNumber(45);
            try
            {

                MascotInfoEx mi = new MascotInfoEx
                {
                    id = Convert.ToInt32(_result.data[0]),
                    _typeid = Convert.ToUInt32(_result.data[2]),
                    level = (byte)Convert.ToUInt32(_result.data[3]),
                    exp = Convert.ToInt32(_result.data[4]),
                    flag = (byte)Convert.ToUInt32(_result.data[5])
                };
                if (_result.IsNotNull(6))
                    mi.message = _result.data[6].ToString();
                mi.tipo = (short)Convert.ToUInt32(_result.data[7]);
                mi.is_cash = (byte)Convert.ToUInt32(_result.data[8]);
                mi.data.CreateTime(_translateDate(_result.data[9]));

                var it = v_mi.Where(c => c.Key == mi.id);

                if (it.FirstOrDefault().Value == null || (it.Count() == 1 && it.FirstOrDefault().Value._typeid != mi._typeid))
                    v_mi.Add(mi.id, mi);
                else if (v_mi.Where(c => c.Key == mi.id).Count() > 1)
                {

                    var er = v_mi.Where(c => c.Key == mi.id);

                    it = er.Where(c => c.Value._typeid == mi._typeid);

                    // N�o tem um igual add um novo
                    if (it == er/*End*/)
                    {

                        v_mi.Add(mi.id, mi);

                        _smp.message_pool.getInstance().push(new message("[CmdMascotInfoInfo::lineResult][Warning] PLAYER[UID=" + (m_uid) + "] adicionou MascotInfo[TYPEID="
                                 + (mi._typeid) + ", ID=" + (mi.id) + "], com mesmo id e typeid diferente de outro MascotInfoEx que tem no multimap", 0));
                    }
                    else
                    {
                        // Tem um MascotInfoEx com o mesmo ID e TYPEID (DUPLICATA)
                        _smp.message_pool.getInstance().push(new message("[CmdMascotInfoInfo::lineResult][Warning] PLAYER[UID=" + (m_uid) + "] tentou adicionar no multimap um MascotInfo[TYPEID="
                                 + (it.First().Value._typeid) + ", ID=" + (it.First().Value.id) + "] com o mesmo ID e TYPEID, DUPLICATA", 0));

                    }
                }
                else
                    // Tem um MascotInfoEx com o mesmo ID e TYPEID (DUPLICATA)
                    _smp.message_pool.getInstance().push(new message("[CmdMascotInfoInfo::lineResult][Warning] PLAYER[UID=" + (m_uid) + "] tentou adicionar no multimap um MascotInfo[TYPEID="
                             + (it.First().Value._typeid) + ", ID=" + (it.First().Value.id) + "] com o mesmo ID e TYPEID, DUPLICATA", 0));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }
        }
protected override Response prepareConsulta()
{
    // 1. Define the procedures
    string procName = (m_type == TYPE.ALL) 
        ? "pangya.ProcGetMascotInfo" 
        : "pangya.ProcGetMascotInfo_One";

    // 2. Define the parameters
    string parameters = (m_type == TYPE.ALL) 
        ? m_uid.ToString() 
        : $"{m_uid}, {m_item_id}";

    // 3. Execute and Validate
    var r = procedure(procName, parameters);
    
    checkResponse(r, $"Não foi possível carregar as informações do Mascot do player: {m_uid}");
    
    return r;
}


        public MascotManager getInfo()
        {
            return v_mi;
        }

        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public TYPE getType()
        {
            return m_type;
        }

        public void setType(TYPE _type)
        {
            m_type = _type;
        }

        public uint getItemID()
        {
            return m_item_id;
        }

        public void setItemID(uint _item_id)
        {
            m_item_id = _item_id;
        }
    }
}
