using System;
using System.Collections.Generic;
using System.Diagnostics;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Utilities;

namespace PangyaAPI.Network.PangyaPacket
{
    /// <summary>
    /// get packet and Session
    /// </summary>
    public class ParamDispatch
    {
        public ParamDispatch(Session session, packet packet)
        {
            _session = session;
            _packet = packet;
        }
        public ParamDispatch()
        {

        }

        public Session _session { get; set; }
        public packet _packet { get; set; }
    }
    public delegate int call_func(object param, ParamDispatch pd);

    public class func_arr
    {
        static int MAX_CALL_FUNC_ARR = 30000; // Era 500, era 1000
        protected Dictionary<ushort, func_arr_ex> m_func; // pacotes de recv
        public func_arr()
        {
            m_func = new Dictionary<ushort, func_arr_ex>(MAX_CALL_FUNC_ARR);

            for (ushort i = 0; i < MAX_CALL_FUNC_ARR; i++)
            {
                m_func[i] = new func_arr_ex();
            }
        }
        public class func_arr_ex
        {
            public func_arr_ex()
            {
            }
            public call_func cf { get; set; }
            public object Param { get; set; }

            public int ExecCmd(ParamDispatch pd)
            {
                // Captura o nome do método atual
                string methodName = cf != null ? cf.Method.Name : "NULL";

                // Cria um stopwatch para medir o tempo
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start(); // Inicia a contagem do tempo

                try
                {
                    if (cf == null)
                    {
                        return 1; // Retorna 1 se o ID não existir (cf é nulo).
                    }

                    // Invoca a função callback e pega o resultado
                    int result = cf.Invoke(Param, pd);

                    // Parar o stopwatch
                    stopwatch.Stop();

                    // Exibe o nome da função e o tempo que demorou para ser executada
                    //Debug.WriteLine($"Function: {methodName}, Execution Time: {stopwatch.ElapsedMilliseconds} ms");

                    return result; // Retorna o resultado do callback
                }
                catch (exception e)
                {
                    // Parar o stopwatch em caso de exceção
                    stopwatch.Stop();

                    // Exibe o erro e o tempo gasto até a exceção
                    Console.WriteLine($"Function: {methodName}, Error: {e.getFullMessageError()}");

                    throw; // Re-throw the exception
                }
            }
        }

        /// <summary>
        /// adiciona o pacote e chama a sua devida funcao
        /// </summary>
        /// <param db_name="_tipo">id do pacote</param>
        /// <param db_name="_func"> funcao a ser chamada</param>

        public void addPacketCall(ushort tipo, call_func func, object param)
        {
            if (tipo < MAX_CALL_FUNC_ARR)
            {
                m_func[tipo].cf = func;
                m_func[tipo].Param = param;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(tipo), "Tipo excede o limite máximo permitido.");
            }
        }

        public void addPacketCall(Enum _tipo, call_func func, object param)
        {
            var tipo = Convert.ToUInt16(_tipo);
            if (tipo < MAX_CALL_FUNC_ARR)
            {
                m_func[tipo].cf = func;
                m_func[tipo].Param = param;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(tipo), "Tipo excede o limite máximo permitido.");
            }
        }

        public func_arr_ex getPacketCall(short _tipo)
        {
            if (_tipo >= 0 && m_func.ContainsKey((ushort)_tipo))
            {
                return m_func[(ushort)_tipo];
            }
            else
            {
                throw new Exception("[new func_arr().getPacketCall][Error] Tipo: " + Convert.ToString(_tipo) + "(0x" + _tipo + "), desconhecido ou nao implementado.");
            }
        }
    }
}
