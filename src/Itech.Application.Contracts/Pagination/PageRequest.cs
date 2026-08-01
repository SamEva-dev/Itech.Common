namespace Itech.Application.Contracts.Pagination;

public readonly record struct PageRequest
{
    public PageRequest(int page = PaginationParameters.DefaultPage, int pageSize = PaginationParameters.DefaultPageSize)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

        if (pageSize is < 1 or > PaginationParameters.MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                $"Page size must be between 1 and {PaginationParameters.MaximumPageSize}.");
        }

        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }
    public int PageSize { get; }
    public int Skip => checked((Page - 1) * PageSize);
}
