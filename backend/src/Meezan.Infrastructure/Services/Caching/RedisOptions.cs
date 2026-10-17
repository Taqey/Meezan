namespace Meezan.Infrastructure.Services.Caching;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 6379;
    public string User { get; set; } = "default";
    public string Password { get; set; } = string.Empty;
    public bool Ssl { get; set; } = false;
    public string KeyPrefix { get; set; } = string.Empty;
}
