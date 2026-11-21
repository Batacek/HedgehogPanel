using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HedgehogPanel.Application.Exceptions;
using HedgehogPanel.Application.Repositories;
using HedgehogPanel.Application.Services;
using HedgehogPanel.Domain.Entities;
using HedgehogPanel.Domain.Enums;
using HedgehogPanel.Domain.Exceptions;
using HedgehogPanel.Infrastructure.Configuration;
using Moq;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Services;

public class NodeHealthPollerTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private readonly Mock<INodeRepository> _nodes = new();
    private readonly Mock<INodeConnectionService> _connections = new();
    private readonly HedgehogConfig _config = new();

    private NodeHealthPoller Poller() => new(_nodes.Object, _connections.Object, _config);

    private static Node PairedNode(string name = "node") => new(Guid.NewGuid(), name, "10.0.0.1", 50051,
        status: NodeStatus.Online, daemonUuid: Guid.NewGuid(), daemonVersion: "0.1.0",
        protocolVersion: "2.0.0", daemonToken: "token");

    private void Paired(params Node[] nodes) =>
        _nodes.Setup(n => n.ListPairedAsync()).ReturnsAsync(nodes);

    private static NodeConnection Reached(Guid id) =>
        new(new Node(id, "node", "10.0.0.1", 50051), ProtocolCompatibility.Full);

    private void PingsSucceed() =>
        _connections.Setup(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Reached(id));

    // ---------- one round ----------

    [Fact]
    public async Task PollOnceAsync_PingsEveryPairedNode()
    {
        var first = PairedNode("first");
        var second = PairedNode("second");
        Paired(first, second);
        PingsSucceed();

        await Poller().PollOnceAsync(CancellationToken.None);

        _connections.Verify(c => c.PingAsync(first.Guid, It.IsAny<CancellationToken>()), Times.Once);
        _connections.Verify(c => c.PingAsync(second.Guid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PollOnceAsync_AsksOnlyForPairedNodes()
    {
        // Unpaired nodes have no token to present, so they must never reach the connection service.
        Paired(PairedNode());
        PingsSucceed();

        await Poller().PollOnceAsync(CancellationToken.None);

        _nodes.Verify(n => n.ListPairedAsync(), Times.Once);
        _nodes.Verify(n => n.ListAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _connections.Verify(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PollOnceAsync_WithNoPairedNodes_PingsNothing()
    {
        Paired();

        await Poller().PollOnceAsync(CancellationToken.None);

        _connections.Verify(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- failures ----------

    [Theory]
    [InlineData(DaemonFailure.Unreachable)]
    [InlineData(DaemonFailure.Timeout)]
    [InlineData(DaemonFailure.IdentityMismatch)]
    public async Task PollOnceAsync_ADaemonFailure_DoesNotStopTheOtherNodes(DaemonFailure failure)
    {
        var failing = PairedNode("failing");
        var healthy = PairedNode("healthy");
        Paired(failing, healthy);
        PingsSucceed();
        _connections.Setup(c => c.PingAsync(failing.Guid, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DaemonException(failure, "no answer"));
        _config.Daemon.PollMaxParallelism = 1;

        await Poller().PollOnceAsync(CancellationToken.None);

        _connections.Verify(c => c.PingAsync(healthy.Guid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PollOnceAsync_AnUnexpectedFailure_DoesNotStopTheOtherNodes()
    {
        var failing = PairedNode("failing");
        var healthy = PairedNode("healthy");
        Paired(failing, healthy);
        PingsSucceed();
        _connections.Setup(c => c.PingAsync(failing.Guid, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _config.Daemon.PollMaxParallelism = 1;

        await Poller().PollOnceAsync(CancellationToken.None);

        _connections.Verify(c => c.PingAsync(healthy.Guid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PollOnceAsync_ANodeDeletedDuringTheRound_IsSkipped()
    {
        var gone = PairedNode("gone");
        var healthy = PairedNode("healthy");
        Paired(gone, healthy);
        PingsSucceed();
        _connections.Setup(c => c.PingAsync(gone.Guid, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("Node", gone.Guid));
        _config.Daemon.PollMaxParallelism = 1;

        await Poller().PollOnceAsync(CancellationToken.None);

        _connections.Verify(c => c.PingAsync(healthy.Guid, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- concurrency ----------

    [Fact]
    public async Task PollOnceAsync_NeverChecksMoreNodesAtOnceThanConfigured()
    {
        _config.Daemon.PollMaxParallelism = 2;
        Paired(Enumerable.Range(0, 6).Select(i => PairedNode($"node{i}")).ToArray());

        var running = 0;
        var peak = 0;
        _connections.Setup(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid id, CancellationToken _) =>
            {
                var now = Interlocked.Increment(ref running);
                int seen;
                while (now > (seen = Volatile.Read(ref peak)) && Interlocked.CompareExchange(ref peak, now, seen) != seen) { }
                await Task.Delay(50);
                Interlocked.Decrement(ref running);
                return Reached(id);
            });

        await Poller().PollOnceAsync(CancellationToken.None);

        Assert.Equal(2, peak);
    }

    [Fact]
    public async Task PollOnceAsync_ANodeThatNeverAnswers_DoesNotHoldUpTheOthers()
    {
        var stuck = PairedNode("stuck");
        var others = new[] { PairedNode("a"), PairedNode("b"), PairedNode("c") };
        Paired(new[] { stuck }.Concat(others).ToArray());
        _config.Daemon.PollMaxParallelism = 2;

        var release = new TaskCompletionSource();
        var answered = new ConcurrentBag<Guid>();
        var allOthersAnswered = new TaskCompletionSource();
        _connections.Setup(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid id, CancellationToken _) =>
            {
                if (id == stuck.Guid)
                {
                    await release.Task;
                }
                else
                {
                    answered.Add(id);
                    if (answered.Count == others.Length) allOthersAnswered.TrySetResult();
                }

                return Reached(id);
            });

        var round = Poller().PollOnceAsync(CancellationToken.None);

        // The round is still waiting on the stuck node, yet the others were checked.
        await allOthersAnswered.Task.WaitAsync(Patience);
        Assert.False(round.IsCompleted);

        release.SetResult();
        await round.WaitAsync(Patience);
    }

    [Fact]
    public async Task PollOnceAsync_WhenCancelled_StopsAndThrows()
    {
        Paired(PairedNode());
        using var source = new CancellationTokenSource();
        var started = new TaskCompletionSource();
        _connections.Setup(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid id, CancellationToken ct) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, ct);
                return Reached(id);
            });

        var round = Poller().PollOnceAsync(source.Token);
        await started.Task.WaitAsync(Patience);
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => round.WaitAsync(Patience));
    }

    // ---------- the loop ----------

    [Fact]
    public async Task Loop_ChecksAgainAfterTheInterval()
    {
        _config.Daemon.PollIntervalSeconds = 1;
        var node = PairedNode();
        Paired(node);

        var stamps = new ConcurrentQueue<long>();
        var twice = new TaskCompletionSource();
        _connections.Setup(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                stamps.Enqueue(Stopwatch.GetTimestamp());
                if (stamps.Count >= 2) twice.TrySetResult();
                return Reached(id);
            });

        var poller = Poller();
        await poller.StartAsync(CancellationToken.None);
        try
        {
            await twice.Task.WaitAsync(Patience);
        }
        finally
        {
            await poller.StopAsync(CancellationToken.None);
        }

        var gaps = stamps.ToArray();
        var gap = Stopwatch.GetElapsedTime(gaps[0], gaps[1]);
        Assert.True(gap >= TimeSpan.FromMilliseconds(900), $"Rounds were only {gap.TotalMilliseconds:F0} ms apart.");
    }

    [Fact]
    public async Task Loop_ChecksRightAwayWithoutWaitingForTheFirstInterval()
    {
        _config.Daemon.PollIntervalSeconds = 3600;
        Paired(PairedNode());
        var pinged = new TaskCompletionSource();
        _connections.Setup(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                pinged.TrySetResult();
                return Reached(id);
            });

        var poller = Poller();
        await poller.StartAsync(CancellationToken.None);
        try
        {
            await pinged.Task.WaitAsync(Patience);
        }
        finally
        {
            await poller.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Loop_WhenARoundFails_TriesAgainAfterTheInterval()
    {
        _config.Daemon.PollIntervalSeconds = 1;
        var node = PairedNode();
        var listed = 0;
        _nodes.Setup(n => n.ListPairedAsync()).Returns(() =>
            Interlocked.Increment(ref listed) == 1
                ? throw new InvalidOperationException("database is down")
                : Task.FromResult<IReadOnlyList<Node>>(new[] { node }));
        var pinged = new TaskCompletionSource();
        _connections.Setup(c => c.PingAsync(node.Guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                pinged.TrySetResult();
                return Reached(id);
            });

        var poller = Poller();
        await poller.StartAsync(CancellationToken.None);
        try
        {
            await pinged.Task.WaitAsync(Patience);
        }
        finally
        {
            await poller.StopAsync(CancellationToken.None);
        }

        Assert.True(listed >= 2);
    }

    [Fact]
    public async Task Loop_WhenStoppedDuringARound_EndsCleanly()
    {
        Paired(PairedNode());
        var started = new TaskCompletionSource();
        _connections.Setup(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid id, CancellationToken ct) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, ct);
                return Reached(id);
            });

        var poller = Poller();
        await poller.StartAsync(CancellationToken.None);
        await started.Task.WaitAsync(Patience);

        await poller.StopAsync(CancellationToken.None).WaitAsync(Patience);

        Assert.True(poller.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Loop_WhenPollingIsSwitchedOff_ChecksNothing()
    {
        _config.Daemon.PollingEnabled = false;
        Paired(PairedNode());

        var poller = Poller();
        await poller.StartAsync(CancellationToken.None);
        await poller.ExecuteTask!.WaitAsync(Patience);
        await poller.StopAsync(CancellationToken.None);

        _nodes.Verify(n => n.ListPairedAsync(), Times.Never);
        _connections.Verify(c => c.PingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
