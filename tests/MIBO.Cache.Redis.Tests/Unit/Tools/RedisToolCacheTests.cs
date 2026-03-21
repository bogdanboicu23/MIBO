using FluentAssertions;
using MIBO.Cache.Redis.Tools;
using Moq;
using StackExchange.Redis;

namespace MIBO.Cache.Redis.Tests.Unit.Tools;

public class RedisToolCacheTests
{
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly RedisToolCache _sut;

    public RedisToolCacheTests()
    {
        var muxMock = new Mock<IConnectionMultiplexer>();
        muxMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_dbMock.Object);

        _sut = new RedisToolCache(muxMock.Object);
    }

    // ════════════════════════════════════════════
    //  GetAsync
    // ════════════════════════════════════════════

    #region GetAsync

    [Fact]
    public async Task GetAsync_KeyExists_ReturnsValue()
    {
        // Arrange
        _dbMock
            .Setup(x => x.StringGetAsync(It.Is<RedisKey>(k => k == "my-key"), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)"cached-value");

        // Act
        var result = await _sut.GetAsync("my-key", CancellationToken.None);

        // Assert
        result.Should().Be("cached-value");
    }

    [Fact]
    public async Task GetAsync_KeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        _dbMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _sut.GetAsync("missing-key", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // ════════════════════════════════════════════
    //  SetAsync
    // ════════════════════════════════════════════

    #region SetAsync

    [Fact]
    public async Task SetAsync_CallsStringSetWithTtl()
    {
        // Arrange
        var ttl = TimeSpan.FromMinutes(5);
        _dbMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _sut.SetAsync("key", "value", ttl, CancellationToken.None);

        // Assert
        _dbMock.Verify(
            x => x.StringSetAsync(
                It.Is<RedisKey>(k => k == "key"),
                It.Is<RedisValue>(v => v == "value"),
                ttl,
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_EmptyValue_StillSetsKey()
    {
        // Arrange
        _dbMock
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _sut.SetAsync("key", "", TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        _dbMock.Verify(
            x => x.StringSetAsync(
                It.Is<RedisKey>(k => k == "key"),
                It.Is<RedisValue>(v => v == ""),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    #endregion
}