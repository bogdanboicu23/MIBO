using FluentAssertions;
using MIBO.Storage.Mongo.Store.Conversation;

namespace MIBO.Storage.Mongo.Tests.Unit.Store;

public class ConversationStoreModelsTests
{
    // ════════════════════════════════════════════
    //  ConversationSummary
    // ════════════════════════════════════════════

    [Fact]
    public void ConversationSummary_AllFieldsSetViaConstructor()
    {
        var now = DateTime.UtcNow;
        var summary = new ConversationSummary(
            "conv-1", "user-1", "My Chat",
            now, now, now, "Hello world", 5);

        summary.ConversationId.Should().Be("conv-1");
        summary.UserId.Should().Be("user-1");
        summary.Title.Should().Be("My Chat");
        summary.CreatedAt.Should().Be(now);
        summary.UpdatedAt.Should().Be(now);
        summary.LastMessageAt.Should().Be(now);
        summary.LastMessagePreview.Should().Be("Hello world");
        summary.MessageCount.Should().Be(5);
    }

    [Fact]
    public void ConversationSummary_NullableFieldsCanBeNull()
    {
        var now = DateTime.UtcNow;
        var summary = new ConversationSummary(
            "conv-1", "user-1", "Chat", now, now, null, null, 0);

        summary.LastMessageAt.Should().BeNull();
        summary.LastMessagePreview.Should().BeNull();
    }

    [Fact]
    public void ConversationSummary_EqualityByValue()
    {
        var now = DateTime.UtcNow;
        var a = new ConversationSummary("c1", "u1", "T", now, now, null, null, 0);
        var b = new ConversationSummary("c1", "u1", "T", now, now, null, null, 0);

        a.Should().Be(b);
    }

    // ════════════════════════════════════════════
    //  ConversationMessage
    // ════════════════════════════════════════════

    [Fact]
    public void ConversationMessage_AllFieldsSetViaConstructor()
    {
        var now = DateTime.UtcNow;
        var message = new ConversationMessage(
            "msg-1", "conv-1", "user-1", "user",
            "Hello", null, null, "corr-1", now);

        message.MessageId.Should().Be("msg-1");
        message.ConversationId.Should().Be("conv-1");
        message.UserId.Should().Be("user-1");
        message.Role.Should().Be("user");
        message.Text.Should().Be("Hello");
        message.UiV1.Should().BeNull();
        message.AssistantPayload.Should().BeNull();
        message.CorrelationId.Should().Be("corr-1");
        message.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void ConversationMessage_AssistantRoleWithPayload()
    {
        var now = DateTime.UtcNow;
        var payload = new { type = "chart", data = new[] { 1, 2, 3 } };

        var message = new ConversationMessage(
            "msg-2", "conv-1", "user-1", "assistant",
            "Here is the chart", null, payload, "corr-2", now);

        message.Role.Should().Be("assistant");
        message.AssistantPayload.Should().NotBeNull();
    }

    // ════════════════════════════════════════════
    //  ConversationDetails
    // ════════════════════════════════════════════

    [Fact]
    public void ConversationDetails_ContainsSummaryAndMessages()
    {
        var now = DateTime.UtcNow;
        var summary = new ConversationSummary("c1", "u1", "T", now, now, null, null, 1);
        var messages = new List<ConversationMessage>
        {
            new("m1", "c1", "u1", "user", "Hi", null, null, "cr1", now)
        };

        var details = new ConversationDetails(summary, messages);

        details.Conversation.Should().Be(summary);
        details.Messages.Should().HaveCount(1);
        details.Messages[0].Text.Should().Be("Hi");
    }

    [Fact]
    public void ConversationDetails_EmptyMessages()
    {
        var now = DateTime.UtcNow;
        var summary = new ConversationSummary("c1", "u1", "T", now, now, null, null, 0);
        var details = new ConversationDetails(summary, new List<ConversationMessage>());

        details.Messages.Should().BeEmpty();
    }
}
