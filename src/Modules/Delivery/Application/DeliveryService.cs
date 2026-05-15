using Bartrix.Modules.Delivery.Contracts;
using Bartrix.Modules.Delivery.Domain;
using Bartrix.Modules.Delivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Delivery.Application;

public sealed class DeliveryService : IDeliveryService
{
    private readonly DeliveryDbContext _dbContext;
    private readonly ITradeDeliveryReader _tradeReader;
    private readonly TimeProvider _timeProvider;

    public DeliveryService(
        DeliveryDbContext dbContext,
        ITradeDeliveryReader tradeReader,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tradeReader = tradeReader;
        _timeProvider = timeProvider;
    }

    public async Task<DeliveryResponse> GetTradeDeliveryAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken)
    {
        var delivery = await GetOrCreateDeliveryAsync(userId, tradeProposalId, cancellationToken);
        return Map(delivery);
    }

    public async Task<DeliveryResponse> UpdateTradeDeliveryAsync(Guid userId, Guid tradeProposalId, UpdateDeliveryRequest request, CancellationToken cancellationToken)
    {
        var delivery = await GetOrCreateDeliveryAsync(userId, tradeProposalId, cancellationToken);
        var method = ParseMethod(request.Method);
        var location = NormalizeLocation(method, request.Location);
        var notes = NormalizeNotes(request.Notes);
        ValidateSchedule(method, request.ScheduledAtUtc);

        Execute(() => delivery.Schedule(userId, method, location, request.ScheduledAtUtc, notes, _timeProvider.GetUtcNow()));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(delivery);
    }

    public async Task<DeliveryResponse> MarkDeliveredAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken)
    {
        var delivery = await GetOrCreateDeliveryAsync(userId, tradeProposalId, cancellationToken);
        Execute(() => delivery.MarkDelivered(userId, _timeProvider.GetUtcNow()));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(delivery);
    }

    public async Task<DeliveryResponse> ConfirmAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken)
    {
        var delivery = await GetOrCreateDeliveryAsync(userId, tradeProposalId, cancellationToken);
        Execute(() => delivery.Confirm(userId, _timeProvider.GetUtcNow()));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(delivery);
    }

    public async Task<DeliveryResponse> CancelAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken)
    {
        var delivery = await GetOrCreateDeliveryAsync(userId, tradeProposalId, cancellationToken);
        Execute(() => delivery.Cancel(userId, _timeProvider.GetUtcNow()));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(delivery);
    }

    private async Task<TradeDelivery> GetOrCreateDeliveryAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Deliveries.SingleOrDefaultAsync(x => x.TradeProposalId == tradeProposalId, cancellationToken);

        if (existing is not null)
        {
            EnsureParticipant(userId, existing);
            return existing;
        }

        var trade = await _tradeReader.GetTradeAsync(tradeProposalId, cancellationToken);
        if (trade is null)
        {
            throw new DeliveryValidationException("Trade proposal was not found.");
        }

        if (trade.SenderUserId != userId && trade.ReceiverUserId != userId)
        {
            throw new DeliveryValidationException("You do not have access to this trade delivery.");
        }

        if (!string.Equals(trade.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
        {
            throw new DeliveryValidationException("Delivery can only be created for accepted trades.");
        }

        var delivery = new TradeDelivery(
            trade.TradeProposalId,
            trade.SenderUserId,
            trade.ReceiverUserId,
            _timeProvider.GetUtcNow());

        _dbContext.Deliveries.Add(delivery);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return delivery;
    }

    private static void EnsureParticipant(Guid userId, TradeDelivery delivery)
    {
        if (!delivery.HasParticipant(userId))
        {
            throw new DeliveryValidationException("You do not have access to this trade delivery.");
        }
    }

    private static DeliveryMethod ParseMethod(string method)
    {
        if (!Enum.TryParse<DeliveryMethod>(method, true, out var parsed))
        {
            throw new DeliveryValidationException("Method must be Meetup, Dropoff, or DigitalService.");
        }

        return parsed;
    }

    private static string? NormalizeLocation(DeliveryMethod method, string? location)
    {
        if (method is DeliveryMethod.Meetup or DeliveryMethod.Dropoff)
        {
            var normalized = location?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new DeliveryValidationException("Location is required for meetup and dropoff.");
            }

            if (normalized.Length > 500)
            {
                throw new DeliveryValidationException("Location cannot exceed 500 characters.");
            }

            return normalized;
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return null;
        }

        var value = location.Trim();
        if (value.Length > 500)
        {
            throw new DeliveryValidationException("Location cannot exceed 500 characters.");
        }

        return value;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var normalized = notes.Trim();
        if (normalized.Length > 1000)
        {
            throw new DeliveryValidationException("Notes cannot exceed 1000 characters.");
        }

        return normalized;
    }

    private static void ValidateSchedule(DeliveryMethod method, DateTimeOffset? scheduledAtUtc)
    {
        if (method is DeliveryMethod.Meetup or DeliveryMethod.Dropoff)
        {
            if (!scheduledAtUtc.HasValue)
            {
                throw new DeliveryValidationException("Scheduled time is required for meetup and dropoff.");
            }
        }
    }

    private static void Execute(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException exception)
        {
            throw new DeliveryValidationException(exception.Message);
        }
    }

    private static DeliveryResponse Map(TradeDelivery delivery)
    {
        return new DeliveryResponse(
            delivery.Id,
            delivery.TradeProposalId,
            delivery.ParticipantAUserId,
            delivery.ParticipantBUserId,
            delivery.Method?.ToString(),
            delivery.Status.ToString(),
            delivery.Location,
            delivery.ScheduledAtUtc,
            delivery.Notes,
            delivery.CreatedAtUtc,
            delivery.UpdatedAtUtc);
    }
}
