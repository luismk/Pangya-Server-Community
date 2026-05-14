using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
namespace Pangya_GameServer
{
    /// <summary>
    /// idea: Luiz Lopes
    /// luizinrc@hotmail.com
    /// </summary>
    public class GameServer
    {

        static void SetupHandleExceptions()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, ex) =>
            {
                if (ex.ExceptionObject is Exception e)
                {
                    // 1. Grava o erro primeiro para não perder nada
                    _smp.message_pool.getInstance().LogEmergency(e.ToString(), "CRITICAL_PAUSE");

                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("\n" + new string('!', 60));
                    Console.WriteLine("!!! SERVER CRASHED - MEMORY/EXCEPTION DETECTED !!!");
                    Console.WriteLine("Pressione 'Y' para abrir o Debugger ou qualquer tecla para fechar.");
                    Console.WriteLine(new string('!', 60));
                    Console.ResetColor();

                    // 2. O ESTADO DE PAUSE:
                    // Se você estiver no Windows, isso abrirá a janela "Deseja depurar este programa?"
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break(); // Se já estiver debugando, ele para aqui.
                    }
                    else
                    {
                        // Isso força o Windows a oferecer o Visual Studio para você 'atachar' no processo
                        Debugger.Launch();
                    }

                    // Mantém o console aberto infinitamente para você ler o erro antes de morrer
                    Console.WriteLine("Aguardando ação manual. O processo está em modo de pausa...");
                    while (true) { System.Threading.Thread.Sleep(1000); }
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, ex) =>
            {
                string errorDetail = ex.Exception.Flatten().ToString();

                _smp.message_pool.getInstance().push("[TASK_ERROR] " + errorDetail, type_msg.CL_FILE_LOG_AND_CONSOLE);
                _smp.message_pool.getInstance().LogEmergency(errorDetail, "UnobservedTaskException");

                ex.SetObserved();
            };

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                // Captura o código de saída do processo atual
                int exitCode = Environment.ExitCode;

                // Tradução amigável para o log
                string reason = (exitCode == 0) ? "Encerramento Normal" : "Encerramento Forçado/Erro";

                string logMsg = $"Servidor encerrado. [ExitCode: {exitCode}] [Motivo: {reason}] [Hora: {DateTime.Now}]";

                // 1. Grava no Log de Emergência (Garante o disco)
                _smp.message_pool.getInstance().LogEmergency(logMsg, "TERMINATE");

                // 2. Tenta fechar e dar flush nos arquivos de log padrão
                _smp.message_pool.getInstance().close_log_files();
            };
        }

        static void Main()
        {

            var sjis = Encoding.GetEncoding("Shift_JIS");

            Console.InputEncoding = sjis;
            Console.OutputEncoding = sjis;

            try
            {
                SetupHandleExceptions();

                sgs.gs.getInstance().Start();

                for (; ; )
                {
                    var input = Console.ReadLine();
                    var comando = new Queue<string>(input.Split(' '));
                    if (sgs.gs.getInstance().CheckCommand(comando))
                    {
                        _smp.message_pool.getInstance().push(new message($"[GameServer::CheckCommand][Log] Command Executed-> {input}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    }
                }
            }
            catch (Exception e)
            {
                _smp.message_pool.getInstance().LogEmergency(e.ToString(), "MAIN_PAUSE");

                Console.WriteLine("\n--- PAUSE ATIVADO DEVIDO A ERRO ---");
                Console.WriteLine("Erro: " + e.Message);
                Console.WriteLine("O processo foi congelado para análise. Feche esta janela manualmente.");

                // Trava a Thread principal aqui
                System.Threading.Thread.Sleep(-1);
            }
        }
    }
}
