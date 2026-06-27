namespace Bartrix.Modules.Messaging.Domain;

public sealed class Conversation
{
    private readonly List<ConversationMessage> _messages = new();

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Null for direct (non-trade) conversations.</summary>
    public Guid? TradeProposalId { get; private set; }

    public Guid ParticipantAUserId { get; private set; }

    public Guid ParticipantBUserId { get; private set; }

    public int UnreadCountA { get; private set; }

    public int UnreadCountB { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ConversationMessage> Messages => _messages;

    private Conversation()
    {
    }

    public Conversation(
        Guid? tradeProposalId,
        Guid participantAUserId,
        Guid participantBUserId,
        DateTimeOffset createdAtUtc)
    {
        TradeProposalId = tradeProposalId;
        ParticipantAUserId = participantAUserId;
        ParticipantBUserId = participantBUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public bool HasParticipant(Guid userId)
    {
        return userId == ParticipantAUserId || userId == ParticipantBUserId;
    }

    public Guid OtherParticipant(Guid userId)
    {
        return userId == ParticipantAUserId ? ParticipantBUserId : ParticipantAUserId;
    }

    public int UnreadFor(Guid userId)
    {
        return userId == ParticipantAUserId ? UnreadCountA : UnreadCountB;
    }

    public ConversationMessage AddMessage(Guid senderUserId, string? body, string? imageUrl, DateTimeOffset createdAtUtc)
    {
        if (!HasParticipant(senderUserId))
        {
            throw new InvalidOperationException("Only conversation participants can send messages.");
        }

        var message = new ConversationMessage(Id, senderUserId, body, imageUrl, createdAtUtc);
        _messages.Add(message);
        UpdatedAtUtc = createdAtUtc;

        // Increment the recipient's unread counter.
        if (senderUserId == ParticipantAUserId)
        {
            UnreadCountB += 1;
        }
        else
        {
            UnreadCountA += 1;
        }

        return message;
    }

    public void MarkRead(Guid userId)
    {
        if (userId == ParticipantAUserId)
        {
            UnreadCountA = 0;
        }
        else if (userId == ParticipantBUserId)
        {
            UnreadCountB = 0;
        }

        foreach (var message in _messages.Where(m => m.SenderUserId != userId && !m.IsRead))
        {
            message.MarkRead();
        }
    }
}
