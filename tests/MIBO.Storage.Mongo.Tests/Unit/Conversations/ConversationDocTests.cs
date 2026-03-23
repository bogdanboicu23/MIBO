using System.Reflection;
using FluentAssertions;
using MIBO.Storage.Mongo.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MIBO.Storage.Mongo.Tests.Unit.Conversations;

public class ConversationDocTests
{
    // ════════════════════════════════════════════
    //  ConversationDoc — defaults
    // ════════════════════════════════════════════

    [Fact]
    public void ConversationDoc_DefaultTitle_IsNewChat()
    {
        var doc = new ConversationDoc();

        doc.Title.Should().Be("New chat");
    }

    [Fact]
    public void ConversationDoc_DefaultMessages_IsEmptyList()
    {
        var doc = new ConversationDoc();

        doc.Messages.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ConversationDoc_DefaultMeta_IsEmptyBsonDocument()
    {
        var doc = new ConversationDoc();

        doc.Meta.Should().NotBeNull();
        doc.Meta.ElementCount.Should().Be(0);
    }

    [Fact]
    public void ConversationDoc_DefaultTimestamps_AreIso8601()
    {
        var before = DateTime.UtcNow;
        var doc = new ConversationDoc();

        DateTime.TryParse(doc.CreatedAtUtc, out var created).Should().BeTrue();
        DateTime.TryParse(doc.UpdatedAtUtc, out var updated).Should().BeTrue();
        created.Should().BeOnOrAfter(before.AddSeconds(-1));
        updated.Should().BeOnOrAfter(before.AddSeconds(-1));
    }

    [Fact]
    public void ConversationDoc_NullableFields_AreNull()
    {
        var doc = new ConversationDoc();

        doc.LastMessageAtUtc.Should().BeNull();
        doc.LastMessagePreview.Should().BeNull();
        doc.Summary.Should().BeNull();
        doc.ExtraElements.Should().BeNull();
    }

    [Fact]
    public void ConversationDoc_DefaultMessageCount_IsZero()
    {
        var doc = new ConversationDoc();

        doc.MessageCount.Should().Be(0);
    }

    // ════════════════════════════════════════════
    //  ConversationDoc — BSON attributes
    // ════════════════════════════════════════════

    [Fact]
    public void ConversationDoc_HasBsonIgnoreExtraElementsAttribute()
    {
        typeof(ConversationDoc)
            .GetCustomAttribute<BsonIgnoreExtraElementsAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void ConversationDoc_Id_HasBsonIdAttribute()
    {
        typeof(ConversationDoc)
            .GetProperty(nameof(ConversationDoc.Id))!
            .GetCustomAttribute<BsonIdAttribute>()
            .Should().NotBeNull();
    }

    // ════════════════════════════════════════════
    //  StoredConversationMessageDoc — defaults
    // ════════════════════════════════════════════

    [Fact]
    public void StoredConversationMessageDoc_DefaultContent_IsEmpty()
    {
        var doc = new StoredConversationMessageDoc();

        doc.Content.Should().BeEmpty();
    }

    [Fact]
    public void StoredConversationMessageDoc_DefaultTokensApprox_IsZero()
    {
        var doc = new StoredConversationMessageDoc();

        doc.TokensApprox.Should().Be(0);
    }

    [Fact]
    public void StoredConversationMessageDoc_DefaultTimestamp_IsIso8601()
    {
        var doc = new StoredConversationMessageDoc();

        DateTime.TryParse(doc.TimestampUtc, out _).Should().BeTrue();
    }

    [Fact]
    public void StoredConversationMessageDoc_HasBsonIgnoreExtraElementsAttribute()
    {
        typeof(StoredConversationMessageDoc)
            .GetCustomAttribute<BsonIgnoreExtraElementsAttribute>()
            .Should().NotBeNull();
    }

    // ════════════════════════════════════════════
    //  MessageDoc — defaults
    // ════════════════════════════════════════════

    [Fact]
    public void MessageDoc_DefaultText_IsEmpty()
    {
        var doc = new MessageDoc();

        doc.Text.Should().BeEmpty();
    }

    [Fact]
    public void MessageDoc_DefaultCreatedAt_IsCloseToUtcNow()
    {
        var before = DateTime.UtcNow;

        var doc = new MessageDoc();

        doc.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        doc.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void MessageDoc_NullableFields_AreNull()
    {
        var doc = new MessageDoc();

        doc.UiV1.Should().BeNull();
        doc.AssistantPayload.Should().BeNull();
    }

    [Fact]
    public void MessageDoc_HasBsonIgnoreExtraElementsAttribute()
    {
        typeof(MessageDoc)
            .GetCustomAttribute<BsonIgnoreExtraElementsAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void MessageDoc_Id_HasBsonIdAttribute()
    {
        typeof(MessageDoc)
            .GetProperty(nameof(MessageDoc.Id))!
            .GetCustomAttribute<BsonIdAttribute>()
            .Should().NotBeNull();
    }

    // ════════════════════════════════════════════
    //  UiInstanceDoc — defaults
    // ════════════════════════════════════════════

    [Fact]
    public void UiInstanceDoc_DefaultSubscriptions_IsEmptyList()
    {
        var doc = new UiInstanceDoc();

        doc.Subscriptions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void UiInstanceDoc_DefaultTimestamps_AreCloseToUtcNow()
    {
        var before = DateTime.UtcNow;

        var doc = new UiInstanceDoc();

        doc.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        doc.UpdatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
    }

    [Fact]
    public void UiInstanceDoc_UiV1_IsNull()
    {
        var doc = new UiInstanceDoc();

        doc.UiV1.Should().BeNull();
    }

    [Fact]
    public void UiInstanceDoc_HasBsonIgnoreExtraElementsAttribute()
    {
        typeof(UiInstanceDoc)
            .GetCustomAttribute<BsonIgnoreExtraElementsAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void UiInstanceDoc_Id_HasBsonIdAttribute()
    {
        typeof(UiInstanceDoc)
            .GetProperty(nameof(UiInstanceDoc.Id))!
            .GetCustomAttribute<BsonIdAttribute>()
            .Should().NotBeNull();
    }
}
