namespace Bartrix.Modules.Messaging.Domain;

public sealed class ConversationMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ConversationId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public string? Body { get; private set; }

    public string? ImageUrl { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Conversation Conversation { get; private set; } = null!;

    private ConversationMessage()
    {
    }

    public ConversationMessage(Guid conversationId, Guid senderUserId, string? body, string? imageUrl, DateTimeOffset createdAtUtc)
    {
        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Body = body;
        ImageUrl = imageUrl;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkRead()
    {
        IsRead = true;
    }
}
