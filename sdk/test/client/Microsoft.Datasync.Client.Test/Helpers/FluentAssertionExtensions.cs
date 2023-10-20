using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.Datasync.Client.Sync;

namespace Microsoft.Datasync.Client.Test;

[ExcludeFromCodeCoverage]
public static class FluentAssertionExtensions
{
    public static OfflineOperationsQueueEntityAssertions Should(this OfflineOperationsQueueEntity entity)
    {
        return new OfflineOperationsQueueEntityAssertions(entity);
    }
}

[ExcludeFromCodeCoverage]
public class OfflineOperationsQueueEntityAssertions : ObjectAssertions<OfflineOperationsQueueEntity, OfflineOperationsQueueEntityAssertions>
{
    public OfflineOperationsQueueEntityAssertions(OfflineOperationsQueueEntity instance) : base(instance)
    {
    }

    protected override string Identifier => "OfflineOperationsQueueEntity";

    public AndConstraint<OfflineOperationsQueueEntityAssertions> HaveValidMetadata(
        DateTimeOffset start, DateTimeOffset end, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.TransactionId != Guid.Empty)
            .FailWith("Expected a non-empty TransactionId, but found Guid.Empty.")
            .Then
            .ForCondition(Subject.CreatedAt != DateTimeOffset.MinValue && Subject.CreatedAt <= end)
            .FailWith("Expected CreatedAt to be valid, but found {0}", Subject.CreatedAt)
            .Then
            .ForCondition(Subject.UpdatedAt >= start && Subject.UpdatedAt <= end)
            .FailWith("Expected UpdatedAt to be between {0} and {1}, but found {2}.", start, end, Subject.UpdatedAt);

        return new AndConstraint<OfflineOperationsQueueEntityAssertions>(this);
    }
}
