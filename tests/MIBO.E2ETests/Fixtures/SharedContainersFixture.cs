using MIBO.TestInfrastructure.Containers;

namespace MIBO.E2ETests.Fixtures;

public class SharedContainersFixture : IAsyncLifetime
{
    private readonly TestContainersHelper _containers = new();

    public string PostgresConnectionString => _containers.PostgresConnectionString;
    public string MongoConnectionString => _containers.MongoConnectionString;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _containers.StartPostgreSqlAsync(),
            _containers.StartMongoDbAsync());
    }

    public async Task DisposeAsync()
    {
        await _containers.DisposeAsync();
    }
}
