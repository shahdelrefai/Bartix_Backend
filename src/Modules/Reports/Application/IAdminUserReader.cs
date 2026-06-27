namespace Bartrix.Modules.Reports.Application;

public interface IAdminUserReader
{
    Task<IReadOnlyList<Guid>> GetAdminUserIdsAsync(CancellationToken cancellationToken);
}
