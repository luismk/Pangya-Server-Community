using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Text;
namespace Pangya_MessengerServer
{
    public class MessengerServer
    {
        static void Main()
        {
            var sjis = Encoding.GetEncoding("Shift_JIS");

            Console.InputEncoding = sjis;
            Console.OutputEncoding = sjis;

            try
            {
                sms.ms.getInstance().Start();

                for (; ; )
                {
                    var input = Console.ReadLine();
                    var comando = new Queue<string>(input.Split(' '));
                    if (sms.ms.getInstance().CheckCommand(comando))
                    {
                        _smp.message_pool.getInstance().push(new message($"[MessengerServer::CheckCommand][Log] Command Executed-> {input}", type_msg.CL_ONLY_CONSOLE));
                    }
                }
            }
            catch (exception e)
            {
                _smp.message_pool.getInstance().push(new message("[MessengerServer::Main][Error] " + e.getFullMessageError() + "]", type_msg.CL_FILE_LOG_AND_CONSOLE));

                throw e;
            }
        } 
    }
}
