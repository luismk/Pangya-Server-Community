//Convertion By LuisMK
using System;
using System.Data;
using System.Globalization;
using Pangya_GameServer.Models;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.Repository
{
    public class CmdAddFurniture : Pangya_DB
    {
        public CmdAddFurniture()
        {
            this.m_uid = 0;
            this.m_mri = new MyRoomItem();
        }

        public CmdAddFurniture(uint _uid,
            MyRoomItem _mri)
        {
            this.m_uid = _uid;
            this.m_mri = (_mri);
        }

        public uint getUID()
        {
            return (m_uid);
        }

        public void setUID(uint _uid)
        {
            m_uid = _uid;
        }

        public MyRoomItem getInfo()
        {
            return m_mri;
        }

        public void setInfo(MyRoomItem _mri)
        {
            m_mri = _mri;
        }

        protected override void lineResult(ctx_res _result, uint _index_result)
        {

            checkColumnNumber(1);

            m_mri.id = (int)IFNULL(_result.data[0]);
        }

        protected override Response prepareConsulta()
        {

            if (m_uid == 0)
            {
                throw new exception("[CmdAddFurniture::prepareConsulta][Error] m_uid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            if (m_mri._typeid == 0)
            {
                throw new exception("[CmdAddFurniture::prepareConsulta][Error] m_mri._typeid is invalid(zero)", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB,
                    4, 0));
            }

            m_mri.id = -1;
             string parametros = string.Format(CultureInfo.InvariantCulture, 
    "{0}, {1}, {2}, {3}, {4}, {5}", 
    m_uid, 
    m_mri._typeid, 
    m_mri.location.x, 
    m_mri.location.y, 
    m_mri.location.z, 
    m_mri.location.r);
            var r = procedure(m_szConsulta,  parametros);
                
            checkResponse(r, "nao conseguiu adicionar o Furniture[TYPEID=" + m_mri._typeid + "] para o PLAYER[UID=" + m_uid +"]");

            return r;
        }

        private uint m_uid = new uint();
        private MyRoomItem m_mri = new MyRoomItem();

        private const string m_szConsulta = "pangya.ProcAddFurniture";
    }
}