namespace PangyaAPI.Discord.Models
{
    public enum LogType
    {
        Info,
        Warning,
        Error,
        Dev
    }

    public class PangyaLog
    {
        public string Message { get; set; }
        public LogType Type { get; set; }
        public string Channel { get; set; }
    }
}