using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Models;
using PangyaAPI.Utilities.Log;
namespace PangyaAPI.Network.PangyaPacket
{
    public class packet_func_base
    {
        public static func_arr funcs = new func_arr();      // Cliente
        public static func_arr funcs_sv = new func_arr();   // Server (Retorno)
        public static func_arr funcs_as = new func_arr(); // Auth Server


        public static int MAX_BUFFER_PACKET = 1000;
        public static void MakeBeginPacket(object arg)
        {
            var pd = (ParamDispatch)arg;
            _smp.message_pool.getInstance().push(new message($"Trata pacote {pd._packet.getTipo()}(0x{pd._packet.getTipo():X})", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }

        public static void MakeBeginSplitPacket<T>(ushort packetId, Session session, int elementSize, int maxPacket, List<T> elements, bool debug)
        {
            int porPacket = (maxPacket - 100) > elementSize ? (maxPacket - 100) / elementSize : 1;
            int total = elements.Count;
            int index = 0;

            foreach (var element in elements)
            {
                var p = new PangyaBinaryWriter();
                p.init_plain(packetId);

                p.WriteInt16((short)total);
                p.WriteInt16((short)(total > porPacket ? porPacket : total));

                for (int i = 0; i < porPacket && index < elements.Count; i++, index++)
                {
                    // p.WriteBuffer(element, elementSize);
                }
            }
        }
        public static void MakeSplitPacket<T>(
       ushort packetId,
       Session session,
       List<T> v_element,
       int elementSize,
       int maxPacket,
       byte tipo, // 0 = short, 1 = uint
       Action<PangyaBinaryWriter, T> addElementToPacket,
       string debug = null)
        {
            int elements = v_element.Count;
            int porPacket = ((maxPacket - 100) > elementSize) ? (maxPacket - 100) / elementSize : 1;

            int index = 0;
            int total = elements;

            var it = v_element.GetEnumerator();

            while (index < elements && it.MoveNext())
            {
                PangyaBinaryWriter p = new PangyaBinaryWriter();
                p.init_plain(packetId);

                // MAKE_MED_SPLIT_PACKET equivalent
                if (tipo == 0)
                {
                    p.WriteInt16((short)total);
                    p.WriteInt16((short)((total > porPacket) ? porPacket : total));
                }
                else
                {
                    p.WriteUInt32((uint)total);
                    p.WriteUInt32((uint)((total > porPacket) ? porPacket : total));
                }

                int i = 0;
                do
                {
                    addElementToPacket(p, it.Current);
                    index++;
                    i++;
                } while (i < porPacket && index < elements && it.MoveNext());

                // MAKE_END_SPLIT_PACKET equivalent
                total -= porPacket;
            }
        }

        public static void MakeSplitPacketFromMap<TKey, TValue>(
            ushort packetId,
            Session session,
            Dictionary<TKey, TValue> v_element,
            int elementSize,
            int maxPacket,
            byte tipo,
            Action<PangyaBinaryWriter, TValue> addElementToPacket,
            string debug = null)
        {
            int elements = v_element.Count;
            int porPacket = ((maxPacket - 100) > elementSize) ? (maxPacket - 100) / elementSize : 1;

            int index = 0;
            int total = elements;

            var it = v_element.Values.GetEnumerator();

            while (index < elements && it.MoveNext())
            {
                var p = new PangyaBinaryWriter();
                p.init_plain(packetId);

                // MAKE_MED_SPLIT_PACKET equivalent
                if (tipo == 0)
                {
                    p.WriteInt16((short)total);
                    p.WriteInt16((short)((total > porPacket) ? porPacket : total));
                }
                else
                {
                    p.WriteUInt32((uint)total);
                    p.WriteUInt32((uint)((total > porPacket) ? porPacket : total));
                }

                int i = 0;
                do
                {
                    addElementToPacket(p, it.Current);
                    index++;
                    i++;
                } while (i < porPacket && index < elements && it.MoveNext());

                // MAKE_END_SPLIT_PACKET_REF equivalent
                total -= porPacket;
            }
        }

        public static void MAKE_SEND_BUFFER(byte[] rawPacket, Session _session)
        {

            try
            {
                if (_session.m_client != null && _session.m_client.Connected)
                {

                    _session.requestSendBuffer(rawPacket);

                    if (_session.devolve())
                        _session.Disconnect();
                }
            }
            catch (exception e)
            {

                if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), STDA_ERROR_TYPE.SESSION, 6/*n�o pode usa session*/))
                    if (_session.devolve())
                        _session.Disconnect();

                if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), STDA_ERROR_TYPE.SESSION, 2))
                    throw;
            }
        }


        public static void MAKE_SEND_CLIENT_BUFFER(byte[] rawPacket, Session _session)
        {

            try
            {
                if (_session.m_client != null && _session.m_client.Connected)
                {

                    _session.requestSendClientBuffer(rawPacket);

                    if (_session.devolve())
                        _session.Disconnect();
                }
            }
            catch (exception e)
            {

                if (!ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), STDA_ERROR_TYPE.SESSION, 6/*n�o pode usa session*/))
                    if (_session.devolve())
                        _session.Disconnect();

                if (ExceptionError.STDA_ERROR_CHECK_SOURCE_AND_ERROR_TYPE(e.getCodeError(), STDA_ERROR_TYPE.SESSION, 2))
                    throw;
            }
        }


}
}
