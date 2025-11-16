namespace NxDataManager.Models;

/// <summary>
/// SMB连接配置
/// </summary>
public class SmbConnectionConfig
{
    public string Name { get; set; } = string.Empty;
    public string ServerAddress { get; set; } = string.Empty;
    public string ShareName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool UseEncryption { get; set; } = true;
}

/// <summary>
/// WebDAV连接配置
/// </summary>
public class WebDavConnectionConfig
{
    public string Name { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSSL { get; set; } = true;
    public int Port { get; set; } = 443;
}
