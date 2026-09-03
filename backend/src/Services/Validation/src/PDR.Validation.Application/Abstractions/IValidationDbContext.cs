using Microsoft.EntityFrameworkCore;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Application.Abstractions;

public interface IValidationDbContext
{
    DbSet<ValidationRun> Runs { get; }

    DbSet<AddressAssessment> Assessments { get; }

    DbSet<ValidationIssue> Issues { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
