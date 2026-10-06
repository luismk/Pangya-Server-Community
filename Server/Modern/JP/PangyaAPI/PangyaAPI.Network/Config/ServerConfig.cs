using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
namespace PangyaAPI.Network.Config;

public class ServerConfig
{
    public ServerInfo _ServerInfo { get; protected set; }
    public ConfigTimeOut Timeouts { get; protected set; }
    public int TimeTickBotLimit { get; protected set; }

    public static void LoadConfig(ref ServerInfo serverInfo, ref ConfigTimeOut timeouts, ref int timeTickBotLimit, ServerType serverType)
    {
        var config = new ServerConfig();
        config._ServerInfo = serverInfo;
        config.Timeouts = timeouts;
        config.TimeTickBotLimit = timeTickBotLimit;
        config.LoadConfig(serverType);

        serverInfo = config._ServerInfo;
        timeouts = config.Timeouts;
        timeTickBotLimit = config.TimeTickBotLimit;
    }

    public virtual void LoadConfig(ServerType serverType = ServerType.GameServer)
    {
        try
        {
            string prefix = GetPrefixByServerType(serverType);
            string serverIniFileName = $"{prefix}.ini";
            string serverIniPath = File.Exists($"Config/{serverIniFileName}") ? $"Config/{serverIniFileName}" : serverIniFileName;
            if (!File.Exists(serverIniPath))
            {
                throw new exception($"[{GetType().Name}::LoadConfig][Error] Arquivo '{serverIniPath}' não encontrado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.READER_INI, 5, 0x5200105)); 
            }

            using (var ini = new IniHandle(serverIniPath))
            {
                _ServerInfo = new ServerInfo
                {
                    BuildVersion = ini.ReadString("SERVERINFO", "VERSION"),
                    ClientVersion = ini.ReadString("SERVERINFO", "CLIENTVERSION"),
                    Name = ini.ReadString("SERVERINFO", "NAME", GetDefaultNameByServerType(serverType)),
                    UID = ini.ReadInt32("SERVERINFO", "GUID"),
                    Port = ini.ReadInt32("SERVERINFO", "PORT"),
                    IpAddress = ini.ReadString("SERVERINFO", "IP", "127.0.0.1"),
                    MaxUsers = ini.ReadInt32("SERVERINFO", "MAXUSER", 2000),
                    Property = new ServerProperty(ini.ReadUInt32("SERVERINFO", "PROPERTY")),
                    VersionPacket = ini.ReadUInt32("SERVERINFO", "PACKETVERSION")
                };
                TimeTickBotLimit = ini.ReadInt32("OPTION", "ANTIBOTTTL", 1000);
                Timeouts = ConfigTimeOut.FromIni(ini); 
            }
            _smp.LogManager.Instance.push($"[{GetType().Name}::LoadConfig][Sucess] Server[Type: {serverType}, Name: {_ServerInfo.Name}] Sucess!", type_msg.CL_FILE_LOG_AND_CONSOLE);
        }
        catch (exception ex)
        {
            _smp.LogManager.Instance.push($"[{GetType().Name}::LoadConfig][ErrorSystem] " + ex.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE);
            SetDefaultFallback();
        }
        catch (Exception ex)
        {
            _smp.LogManager.Instance.push($"[{GetType().Name}::LoadConfig][ErrorSystem] " + ex.Message, type_msg.CL_FILE_LOG_AND_CONSOLE);
            SetDefaultFallback();
        }
    }

    private void SetDefaultFallback()
    {
        Timeouts = ConfigTimeOut.Default;
        TimeTickBotLimit = 1000;
    }

    private static string GetDefaultNameByServerType(ServerType type) => type switch
    {
        ServerType.GameServer => "Pangya Game Server",
        ServerType.MessengerServer => "Pangya Messenger Server",
        ServerType.LoginServer => "Pangya Login Server",
        ServerType.RankServer => "Pangya Rank Server",
        ServerType.AuthServer => "Pangya Auth Server",
        _ => "Pangya Server"
    };

    private static string GetPrefixByServerType(ServerType type) => type switch
    {
        ServerType.GameServer => "GS",
        ServerType.MessengerServer => "MS",
        ServerType.LoginServer => "LS",
        ServerType.RankServer => "RS",
        ServerType.AuthServer => "AS",
        _ => "SV"
    };

    public static string GetLoadConfig(ServerType serverType)
    {
        return GetPrefixByServerType(serverType) + ".ini";
    }

    public static IniHandle GetLoadConfigIni(ServerType serverType)
    {
        string prefix = GetPrefixByServerType(serverType);
        string serverIniFileName = $"{prefix}.ini";
        string serverIniPath = File.Exists($"Config/{serverIniFileName}") ? $"Config/{serverIniFileName}" : serverIniFileName;
        if (!File.Exists(serverIniPath))
        {
            throw new exception($"[ServerConfig::GetLoadConfigIni][Error] Arquivo '{serverIniPath}' não encontrado.", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.READER_INI, 5, 0x5200105)); 
        }

        return new IniHandle(serverIniPath);
    }
}
