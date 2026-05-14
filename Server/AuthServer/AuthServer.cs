using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pangya_AuthServer
{
    public class AuthServer
    {
        static void Main()
        {

            var sjis = Encoding.GetEncoding("Shift_JIS");

            Console.InputEncoding = sjis;
            Console.OutputEncoding = sjis;

            try
            {
                sas.@as.getInstance().Start();

                for (; ; )
                {
                    var input = Console.ReadLine();
                    var comando = new Queue<string>(input.Split(' '));
                    if (sas.@as.getInstance().CheckCommand(comando))
                    {
                        _smp.message_pool.getInstance().push(new message($"[AuthServer::CheckCommand][Log] Command Executed-> {input}", type_msg.CL_ONLY_CONSOLE));
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[AuthServer::Main][Error] " + e.getFullMessageError() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw e;
            }
        }
    }
}
