using Xunit;

namespace HedgehogPanel.Tests.Integration.TestFixtures;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<PostgreSqlFixture>
{
}
