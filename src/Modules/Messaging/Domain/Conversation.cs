namespace Bartrix.Modules.Messaging.Domain;

public sealed class Conversation
{
    private readonly List<ConversationMessage> _messages = new();

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid TradeProposalId { get; private set; }

    public Guid ParticipantAUserId { get; private set; }

    public Guid ParticipantBUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ConversationMessage> Messages => _messages;

    private Conversation()
    {
    }

    public Conversation(
        Guid tradeProposalId,
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

    public ConversationMessage AddMessage(Guid senderUserId, string body, DateTimeOffset createdAtUtc)
    {
        if (!HasParticipant(senderUserId))
        {
            throw new InvalidOperationException("Only conversation participants can send messages.");
        }

        var message = new ConversationMessage(Id, senderUserId, body, createdAtUtc);
        _messages.Add(message);
        UpdatedAtUtc = createdAtUtc;
        return message;
    }
}
