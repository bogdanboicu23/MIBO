namespace MIBO.Storage.Mongo;

public sealed class MongoOptions
{
    public string ConnectionString { get; set; } = default!;
    public string Database { get; set; } = "mibo";

    public string ConversationsCollection { get; set; } = "conversations";
    public string MessagesCollection { get; set; } = "messages";
    public string UiInstancesCollection { get; set; } = "ui_instances";
    public string ExternalServiceAuditsCollection { get; set; } = "external_service_audits";
    public string ExternalServiceStatusesCollection { get; set; } = "external_service_statuses";

    // Context pentru planner
    public int PlannerContextMessages { get; set; } = 12;
}
