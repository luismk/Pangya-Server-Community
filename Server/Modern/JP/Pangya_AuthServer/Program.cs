using Pangya_AuthServer.Server;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System.Text;


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var sjis = Encoding.GetEncoding("Shift_JIS");

Console.InputEncoding = sjis;
Console.OutputEncoding = sjis;
// 1. Configurações iniciais de Console
Console.Title = "Auth Service";
ShowBanner();

try
{
    // 2. Instanciação Única 
    await AuthServer.Instance.StartAsync();

    LogMessage("Digite 'exit' para fechar ou 'status' para informações.");

    // 5. Loop de Comandos (Moderno e Simples)
    while (true)
    {
        var input = Console.ReadLine()?.ToLower();

        if (input == "exit" || input == "quit")
        {
            LogMessage("Desligando servidor...");
            AuthServer.Instance.Stop();
            break;
        }

        if (input == "status")
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryUsage = process.PrivateMemorySize64 / 1024 / 1024; // MB

            LogMessage($"--- Auth Status ---");
            LogMessage($"Players Online: {AuthServer.Instance?.SessionsManager.Count ?? 0}");
            LogMessage($"Memória em uso: {memoryUsage} MB");
            LogMessage($"Uptime: {DateTime.Now - process.StartTime}");
        }

        if (input == "cls") Console.Clear();
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
    _smp.LogManager.Instance.push(new AppMessage($"[System] {text}", 0));
}