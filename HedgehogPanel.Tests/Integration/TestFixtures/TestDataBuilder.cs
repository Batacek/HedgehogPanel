using System;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;

namespace HedgehogPanel.Tests.Integration.TestFixtures;

public static class TestDataBuilder
{
    public static Account CreateTestAccount(
        string? username = null,
        string? email = null,
        bool isActive = true,
        Guid? guid = null)
    {
        return new Account(
            guid ?? Guid.NewGuid(),
            username ?? $"testuser_{Guid.NewGuid():N}",
            email ?? $"test_{Guid.NewGuid():N}@hedgehog.batacek.eu",
            isActive
        );
    }

    public static Server CreateTestServer(
        string? name = null,
        string? hostname = null,
        int port = 22,
        Guid? guid = null)
    {
        return new Server(
            guid ?? Guid.NewGuid(),
            name ?? $"TestServer_{Guid.NewGuid():N}",
            hostname ?? "test.hedgehog.batacek.eu",
            port
        );
    }

    public static Node CreateTestNode(
        string? name = null,
        string? ipAddress = null,
        int port = 50051,
        Guid? guid = null)
    {
        return new Node(
            guid ?? Guid.NewGuid(),
            name ?? $"TestNode_{Guid.NewGuid():N}",
            ipAddress ?? "127.0.0.1",
            port
        );
    }

    public static Group CreateTestGroup(
        string? name = null,
        string? description = null,
        Guid? guid = null)
    {
        return new Group(
            guid ?? Guid.NewGuid(),
            name ?? $"TestGroup_{Guid.NewGuid():N}",
            description
        );
    }
}