using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Domain.Entities;

namespace HedgehogPanel.Application.Services;

public class ServerService : IServerService
{
    private readonly IServerRepository _serverRepository;

    public ServerService(IServerRepository serverRepository)
    {
        _serverRepository = serverRepository;
    }

    public async Task<Server?> GetServerByIdAsync(Guid id)
    {
        return await _serverRepository.GetByGuidAsync(id);
    }

    public async Task<IReadOnlyList<Server>> ListServersAsync(int limit = 100, int offset = 0)
    {
        return await _serverRepository.ListAsync(limit, offset);
    }

    public async Task<Server> CreateServerAsync(string name, string hostname, int port = 22)
    {
        var server = new Server(Guid.NewGuid(), name, hostname, port);
        var success = await _serverRepository.CreateAsync(server);
        if (!success) throw new Exception("Failed to create server");
        return server;
    }

    public async Task<bool> UpdateServerAsync(Server server)
    {
        return await _serverRepository.UpdateAsync(server);
    }

    public async Task<bool> DeleteServerAsync(Guid id)
    {
        return await _serverRepository.DeleteAsync(id);
    }
}
