namespace Bartrix.Modules.Messaging.Contracts;

public sealed record SendMessageRequest(string? Body = null, string? ImageUrl = null);
