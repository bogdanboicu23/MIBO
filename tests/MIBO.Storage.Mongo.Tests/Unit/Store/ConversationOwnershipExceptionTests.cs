using FluentAssertions;
using MIBO.Storage.Mongo.Store.Conversation;

namespace MIBO.Storage.Mongo.Tests.Unit.Store;

public class ConversationOwnershipExceptionTests
{
    [Fact]
    public void Constructor_SetsConversationId()
    {
        var ex = new ConversationOwnershipException("conv-123");

        ex.ConversationId.Should().Be("conv-123");
    }

    [Fact]
    public void Constructor_SetsMessageWithConversationId()
    {
        var ex = new ConversationOwnershipException("conv-456");

        ex.Message.Should().Contain("conv-456");
        ex.Message.Should().Contain("owned by a different user");
    }

    [Fact]
    public void Exception_IsOfTypeException()
    {
        var ex = new ConversationOwnershipException("x");

        ex.Should().BeAssignableTo<Exception>();
    }
}
