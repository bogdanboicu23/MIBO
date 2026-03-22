using FluentAssertions;

namespace MIBO.Storage.Mongo.Tests.Unit;

public class MongoOptionsTests
{
    [Fact]
    public void MongoOptions_DefaultValues()
    {
        var options = new MongoOptions();

        options.Database.Should().Be("mibo");
        options.ConversationsCollection.Should().Be("conversations");
        options.MessagesCollection.Should().Be("messages");
        options.UiInstancesCollection.Should().Be("ui_instances");
        options.ExternalServiceAuditsCollection.Should().Be("external_service_audits");
        options.ExternalServiceStatusesCollection.Should().Be("external_service_statuses");
        options.PlannerContextMessages.Should().Be(12);
    }

    [Fact]
    public void MongoOptions_CanSetCustomValues()
    {
        var options = new MongoOptions
        {
            ConnectionString = "mongodb://localhost:27017",
            Database = "custom_db",
            ConversationsCollection = "custom_conv",
            MessagesCollection = "custom_msg",
            UiInstancesCollection = "custom_ui",
            ExternalServiceAuditsCollection = "custom_audits",
            ExternalServiceStatusesCollection = "custom_statuses",
            PlannerContextMessages = 20
        };

        options.ConnectionString.Should().Be("mongodb://localhost:27017");
        options.Database.Should().Be("custom_db");
        options.ConversationsCollection.Should().Be("custom_conv");
        options.MessagesCollection.Should().Be("custom_msg");
        options.UiInstancesCollection.Should().Be("custom_ui");
        options.ExternalServiceAuditsCollection.Should().Be("custom_audits");
        options.ExternalServiceStatusesCollection.Should().Be("custom_statuses");
        options.PlannerContextMessages.Should().Be(20);
    }
}
