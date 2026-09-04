using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Correlation;
using PDR.BuildingBlocks.Persistence;
using PDR.Validation.Application.Abstractions;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Infrastructure.Persistence;

public sealed class ValidationDbContext(
    DbContextOptions<ValidationDbContext> options,
    IAuditContext auditContext,
    ICorrelationContext correlationContext)
    : BaseDbContext(options, auditContext, correlationContext), IValidationDbContext
{
    public DbSet<ValidationRun> Runs => Set<ValidationRun>();

    public DbSet<AddressAssessment> Assessments => Set<AddressAssessment>();

    public DbSet<ValidationIssue> Issues => Set<ValidationIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ValidationDbContext).Assembly);
    }
}
