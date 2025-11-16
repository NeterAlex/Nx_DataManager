using Dapper;
using NxDataManager.Data;
using NxDataManager.Models;
using System.Data;

namespace NxDataManager.Services;

/// <summary>
/// 远程连接配置存储服务
/// </summary>
public class RemoteConnectionStorageService
{
    private readonly DatabaseContext _dbContext;

    public RemoteConnectionStorageService(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
        InitializeTables();
    }

    private void InitializeTables()
    {
        using var connection = _dbContext.GetConnection();

        // SMB连接配置表
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS SmbConnections (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                ServerAddress TEXT NOT NULL,
                ShareName TEXT NOT NULL,
                Username TEXT,
                Password TEXT,
                Domain TEXT,
                UseEncryption INTEGER NOT NULL DEFAULT 1,
                CreatedTime TEXT NOT NULL,
                LastUsedTime TEXT
            );
        ");

        // WebDAV连接配置表
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS WebDavConnections (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                ServerUrl TEXT NOT NULL,
                Username TEXT,
                Password TEXT,
                UseSSL INTEGER NOT NULL DEFAULT 1,
                Port INTEGER NOT NULL DEFAULT 443,
                CreatedTime TEXT NOT NULL,
                LastUsedTime TEXT
            );
        ");
    }

    #region SMB连接管理

    public async Task<List<SmbConnectionConfig>> GetAllSmbConnectionsAsync()
    {
        using var connection = _dbContext.GetConnection();

        var items = await connection.QueryAsync<dynamic>(@"
            SELECT * FROM SmbConnections ORDER BY CreatedTime DESC
        ");

        return items.Select(item => new SmbConnectionConfig
        {
            Id = Guid.Parse((string)item.Id),
            Name = (string)item.Name,
            ServerAddress = (string)item.ServerAddress,
            ShareName = (string)item.ShareName,
            Username = (string)item.Username,
            Password = (string)item.Password,
            Domain = (string)item.Domain,
            UseEncryption = Convert.ToBoolean((long)item.UseEncryption),
            CreatedTime = DateTime.Parse((string)item.CreatedTime),
            LastUsedTime = item.LastUsedTime != null ? DateTime.Parse((string)item.LastUsedTime) : null
        }).ToList();
    }

    public async Task SaveSmbConnectionAsync(SmbConnectionConfig config)
    {
        using var connection = _dbContext.GetConnection();

        await connection.ExecuteAsync(@"
            INSERT OR REPLACE INTO SmbConnections 
            (Id, Name, ServerAddress, ShareName, Username, Password, Domain, UseEncryption, CreatedTime, LastUsedTime)
            VALUES (@Id, @Name, @ServerAddress, @ShareName, @Username, @Password, @Domain, @UseEncryption, @CreatedTime, @LastUsedTime)
        ", new
        {
            Id = config.Id.ToString(),
            config.Name,
            config.ServerAddress,
            config.ShareName,
            config.Username,
            config.Password,
            config.Domain,
            UseEncryption = config.UseEncryption ? 1 : 0,
            CreatedTime = config.CreatedTime.ToString("O"),
            LastUsedTime = config.LastUsedTime?.ToString("O")
        });
    }

    public async Task DeleteSmbConnectionAsync(Guid id)
    {
        using var connection = _dbContext.GetConnection();

        await connection.ExecuteAsync(@"
            DELETE FROM SmbConnections WHERE Id = @Id
        ", new { Id = id.ToString() });
    }

    public async Task UpdateSmbLastUsedTimeAsync(Guid id)
    {
        using var connection = _dbContext.GetConnection();

        await connection.ExecuteAsync(@"
            UPDATE SmbConnections SET LastUsedTime = @LastUsedTime WHERE Id = @Id
        ", new
        {
            Id = id.ToString(),
            LastUsedTime = DateTime.Now.ToString("O")
        });
    }

    #endregion

    #region WebDAV连接管理

    public async Task<List<WebDavConnectionConfig>> GetAllWebDavConnectionsAsync()
    {
        using var connection = _dbContext.GetConnection();

        var items = await connection.QueryAsync<dynamic>(@"
            SELECT * FROM WebDavConnections ORDER BY CreatedTime DESC
        ");

        return items.Select(item => new WebDavConnectionConfig
        {
            Id = Guid.Parse((string)item.Id),
            Name = (string)item.Name,
            ServerUrl = (string)item.ServerUrl,
            Username = (string)item.Username,
            Password = (string)item.Password,
            UseSSL = Convert.ToBoolean((long)item.UseSSL),
            Port = (int)(long)item.Port,
            CreatedTime = DateTime.Parse((string)item.CreatedTime),
            LastUsedTime = item.LastUsedTime != null ? DateTime.Parse((string)item.LastUsedTime) : null
        }).ToList();
    }

    public async Task SaveWebDavConnectionAsync(WebDavConnectionConfig config)
    {
        using var connection = _dbContext.GetConnection();

        await connection.ExecuteAsync(@"
            INSERT OR REPLACE INTO WebDavConnections 
            (Id, Name, ServerUrl, Username, Password, UseSSL, Port, CreatedTime, LastUsedTime)
            VALUES (@Id, @Name, @ServerUrl, @Username, @Password, @UseSSL, @Port, @CreatedTime, @LastUsedTime)
        ", new
        {
            Id = config.Id.ToString(),
            config.Name,
            config.ServerUrl,
            config.Username,
            config.Password,
            UseSSL = config.UseSSL ? 1 : 0,
            config.Port,
            CreatedTime = config.CreatedTime.ToString("O"),
            LastUsedTime = config.LastUsedTime?.ToString("O")
        });
    }

    public async Task DeleteWebDavConnectionAsync(Guid id)
    {
        using var connection = _dbContext.GetConnection();

        await connection.ExecuteAsync(@"
            DELETE FROM WebDavConnections WHERE Id = @Id
        ", new { Id = id.ToString() });
    }

    public async Task UpdateWebDavLastUsedTimeAsync(Guid id)
    {
        using var connection = _dbContext.GetConnection();

        await connection.ExecuteAsync(@"
            UPDATE WebDavConnections SET LastUsedTime = @LastUsedTime WHERE Id = @Id
        ", new
        {
            Id = id.ToString(),
            LastUsedTime = DateTime.Now.ToString("O")
        });
    }

    #endregion
}
