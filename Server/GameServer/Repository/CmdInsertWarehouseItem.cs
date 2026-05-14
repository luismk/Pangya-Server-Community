
using System;
using System.Diagnostics;
using System.Reflection;
using PangyaAPI.SQL; 
namespace Pangya_GameServer.Repository
{
    public class CmdInsertWarehouseItem : Pangya_DB
    {
        uint m_uid = uint.MaxValue; 
        uint m_typeid;  
        public CmdInsertWarehouseItem(uint _uid, uint _item_id = 0)
        {
            m_uid = _uid; 
            m_typeid = _item_id;  
        } 

        protected override void lineResult(ctx_res _result, uint _index_result)
        { 
        }
         
        protected override Response prepareConsulta()
        {
            var r = consulta($"INSERT INTO [pangya].[pangya_item_warehouse] ([UID],[typeid],[valid],[regdate],[Gift_flag],[flag],[Applytime],[EndDate],[C0],[C1],[C2],[C3],[C4],[Purchase],[ItemType],[ClubSet_WorkShop_Flag],[ClubSet_WorkShop_C0],[ClubSet_WorkShop_C1],[ClubSet_WorkShop_C2],[ClubSet_WorkShop_C3],[ClubSet_WorkShop_C4],[Mastery_Pts],[Recovery_Pts],[Level],[Up],[Total_Mastery_Pts],[Mastery_Gasto]) VALUES ({m_uid},{m_typeid},1,'2024-12-21 10:17:18',0,0,'2024-12-21 10:17:18','2024-12-21 10:17:18',0,0,0,0,0,0,2,0,0,0,0,0,0,0,0,0,0,0,0)");
            checkResponse(r, "nao conseguiu pegar o member info do player: " + (m_uid));
            return r;
        }

 
        public uint getUID()
        {
            return m_uid;
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

     
        public uint getItemID()
        {
            return m_typeid;
        }

        public void setItemID(uint _item_id)
        {
            m_typeid = _item_id;
        }
    }
}
