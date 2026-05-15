namespace Bartrix.Modules.Messaging.Contracts;

public sealed record ConversationMessageResponse(
    Guid Id,
    Guid SenderUserId,
    string Body,
    DateTimeOffset CreatedAtUtc);
