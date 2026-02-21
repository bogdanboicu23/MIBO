namespace MIBO.ConversationService.Models;

public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
}

public class SendMessageResponse
{
    public string Reply { get; set; } = string.Empty;
}
