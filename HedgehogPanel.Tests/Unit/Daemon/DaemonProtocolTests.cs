using HedgehogPanel.Application.Services;
using HedgehogPanel.Domain.Enums;
using Xunit;

namespace HedgehogPanel.Tests.Unit.Daemon;

public class DaemonProtocolTests
{
    [Theory]
    [InlineData("2.0.0", "2.0.0")]
    [InlineData("2.0.0", "2.0.7")]
    [InlineData("1.4.5", "1.4.7")]
    public void Evaluate_SameMajorAndMinor_IsFull(string panel, string daemon)
    {
        Assert.Equal(ProtocolCompatibility.Full, DaemonProtocol.Evaluate(panel, daemon));
    }

    [Theory]
    [InlineData("2.0.0", "2.1.0")]
    [InlineData("2.3.0", "2.1.4")]
    public void Evaluate_SameMajorOnly_IsLimited(string panel, string daemon)
    {
        Assert.Equal(ProtocolCompatibility.Limited, DaemonProtocol.Evaluate(panel, daemon));
    }

    [Theory]
    [InlineData("0.4.0", "1.2.0")]
    [InlineData("1.2.0", "0.4.0")]
    public void Evaluate_PreReleaseAgainstFirstStableLine_IsLimitedWithAWarning(string panel, string daemon)
    {
        Assert.Equal(ProtocolCompatibility.LimitedPreRelease, DaemonProtocol.Evaluate(panel, daemon));
    }

    [Theory]
    [InlineData("2.0.0", "1.0.0")]
    [InlineData("2.0.0", "3.0.0")]
    [InlineData("0.4.0", "2.0.0")]
    public void Evaluate_DifferentMajor_IsIncompatible(string panel, string daemon)
    {
        Assert.Equal(ProtocolCompatibility.Incompatible, DaemonProtocol.Evaluate(panel, daemon));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2.0")]
    [InlineData("2.0.0.1")]
    [InlineData("two.0.0")]
    [InlineData("+2.0.0")]
    [InlineData(" 2.0.0")]
    [InlineData("2.-1.0")]
    public void Evaluate_UnreadableDaemonVersion_IsIncompatible(string? daemon)
    {
        // A daemon from before protocol 2.0.0 reports no version at all and lands here.
        Assert.Equal(ProtocolCompatibility.Incompatible, DaemonProtocol.Evaluate("2.0.0", daemon));
    }

    [Fact]
    public void Evaluate_WithoutAPanelVersion_UsesThisPanelsProtocol()
    {
        Assert.Equal(ProtocolCompatibility.Full, DaemonProtocol.Evaluate(DaemonProtocol.Version));
    }
}
