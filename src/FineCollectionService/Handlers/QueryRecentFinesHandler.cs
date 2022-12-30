namespace FineCollectionService.Handlers;

public class QueryRecentFinesHandler
{
    private readonly FineDbContext _fineDbContext;

    public QueryRecentFinesHandler(FineDbContext fineDbContext)
    {
        _fineDbContext = fineDbContext;
    }

    public async Task<IEnumerable<Fine>> HandleAsync() =>
        await _fineDbContext.Fines.OrderByDescending(fine => fine.Timestamp)
            .Take(15)
            .ToListAsync();
}
