using Pangya_LoginServer.Server;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Text;


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var sjis = Encoding.GetEncoding("Shift_JIS");

Console.InputEncoding = sjis;
Console.OutputEncoding = sjis;
// 1. Configurações iniciais de Console
Console.Title = "Login Service";
ShowBanner();

try
{
    // 2. Instanciação Única 
    await LoginServer.Instance.StartAsync();

    LogMessage("Digite 'help' ver os comandos disponiveis.");
    // 5. Loop de Comandos (Moderno e Simples)
    while (true)
    {
        var input = Console.ReadLine()?.ToLower();

        var comando = new Queue<string>(input.Split(' '));
        if (LoginServer.Instance.CheckCommand(comando))
        {
            _smp.LogManager.Instance.push(new AppMessage($"[LoginService::CheckCommand][Log] Command Executed-> {input}", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
        else
        {
            _smp.LogManager.Instance.push(new AppMessage($"[LoginService::CheckCommand][Log] Command no exist-> {input}", type_msg.CL_FILE_LOG_AND_CONSOLE));
        }
    }
}
catch (Exception ex)
{
    LogMessage($"[FATAL] Erro crítico na inicialização: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ReadLine();
}

// --- Métodos Auxiliares locais ---

void ShowBanner()
{
    ConsoleEx.Log();
}

void LogMessage(string text)
{
    _smp.LogManager.Instance.push(new AppMessage($"[LoginService::System][Debug] {text}", 0));
}