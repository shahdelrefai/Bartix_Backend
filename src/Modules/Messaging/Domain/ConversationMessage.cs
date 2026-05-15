namespace Bartrix.Modules.Messaging.Domain;

public sealed class ConversationMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ConversationId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public string Body { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Conversation Conversation { get; private set; } = null!;

    private ConversationMessage()
    {
        Body = string.Empty;
    }

    public ConversationMessage(Guid conversationId, Guid senderUserId, string body, DateTimeOffset createdAtUtc)
    {
        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Body = body;
        CreatedAtUtc = createdAtUtc;
    }
}
