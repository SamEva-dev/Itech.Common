using DomainRelay.Mapping.Expressions.Dynamic;

namespace Itech.Querying;

public interface IDynamicQueryOptionsFactory<in TRequest>
{
    DynamicQueryOptions Create(TRequest request);
}
