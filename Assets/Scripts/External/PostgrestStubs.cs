// Compile-time stubs for postgrest-csharp. Define POSTGREST_REAL to disable.
#if !POSTGREST_REAL
#pragma warning disable CS0067
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Postgrest.Models
{
    public abstract class BaseModel { }
}

namespace Postgrest
{
    public class ModeledResponse<T> where T : Models.BaseModel
    {
        public List<T> Models { get; } = new List<T>();
    }

    public class QueryBuilder<T> where T : Models.BaseModel, new()
    {
        public QueryBuilder<T> Select(string columns)                                       => this;
        public QueryBuilder<T> Filter(string column, Constants.Operator op, object value)   => this;
        public QueryBuilder<T> Order(string column, Constants.Ordering ordering)            => this;
        public QueryBuilder<T> Limit(int limit)                                             => this;

        public Task<ModeledResponse<T>> Get()                  => Task.FromResult(new ModeledResponse<T>());
        public Task<ModeledResponse<T>> Insert(T model)        => Task.FromResult(new ModeledResponse<T>());
        public Task<ModeledResponse<T>> Update(T model)        => Task.FromResult(new ModeledResponse<T>());
        public Task<T>                  Single()               => Task.FromResult<T>(null);
    }
}

namespace Postgrest.Constants
{
    public enum Operator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Like,
        ILike,
        In,
        Is,
    }

    public enum Ordering
    {
        Ascending,
        Descending,
    }
}
#pragma warning restore CS0067
#endif
