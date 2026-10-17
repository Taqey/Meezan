using System.Net;
using FluentValidation;
using MediatR;
using Meezan.Application.Common.Behaviors;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Infrastructure.Persistence;
using Meezan.WebApi.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Meezan.Infrastructure.Tests.Testing;

public class ResultPatternFoundationTests
{
    // ── 1. Result and Result<T> unit tests ───────────────────────────────────

    [Fact]
    public void Result_Success_HasCorrectProperties()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Result_Failure_HasCorrectProperties()
    {
        var error = Error.NotFound("ItemNotFound", "العنصر غير موجود");
        Result result = error;

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void ResultT_Success_ReturnsValue()
    {
        Result<string> result = "test-val";

        Assert.True(result.IsSuccess);
        Assert.Equal("test-val", result.Value);
    }

    [Fact]
    public void ResultT_Failure_ThrowsOnValueAccess()
    {
        var error = Error.Validation("ValError", "خطأ في التحقق");
        Result<string> result = error;

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Null(result.ValueOrDefault);
    }

    // ── 2. HTTP Mapping Extension Tests ─────────────────────────────────────

    [Fact]
    public void ToActionResult_Success_ReturnsOkObjectResult()
    {
        var result = Result.Success(123);
        var actionResult = result.ToActionResult();

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(123, okResult.Value);
    }

    [Fact]
    public void ToActionResult_Validation_ReturnsBadRequest()
    {
        var error = Error.Validation("Invalid", "خطأ بيانات");
        var result = Result.Failure<string>(error);
        var actionResult = result.ToActionResult();

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public void ToActionResult_NotFound_Returns404WithExactShape()
    {
        var error = Error.NotFound("NotFound", "غير موجود");
        var result = Result.Failure<string>(error);
        var actionResult = result.ToActionResult();

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.NotNull(notFound.Value);
    }

    [Fact]
    public void ToActionResult_Conflict_Returns409()
    {
        var error = Error.Conflict("Duplicate", "مكرر");
        var result = Result.Failure<string>(error);
        var actionResult = result.ToActionResult();

        var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.NotNull(conflict.Value);
    }

    [Fact]
    public void ToActionResult_WithExtraPayload_PreservesPayload()
    {
        var payload = new { custom = "value", count = 5 };
        var error = Error.NotFound("Custom", "رسالة", payload);
        var result = Result.Failure<string>(error);
        var actionResult = result.ToActionResult();

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Same(payload, notFound.Value);
    }

    // ── 3. ValidationBehavior Tests ─────────────────────────────────────────

    public record TestValidationCommand(string Name) : IRequest<Result<string>>;

    public class TestValidationCommandValidator : AbstractValidator<TestValidationCommand>
    {
        public TestValidationCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("الاسم مطلوب");
        }
    }

    [Fact]
    public async Task ValidationBehavior_WhenValidationFails_ReturnsResultFailure()
    {
        var validators = new List<IValidator<TestValidationCommand>> { new TestValidationCommandValidator() };
        var behavior = new ValidationBehavior<TestValidationCommand, Result<string>>(validators);

        var response = await behavior.Handle(
            new TestValidationCommand(""),
            (cancellationToken) => Task.FromResult(Result.Success("ok")),
            CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal(ErrorType.Validation, response.Error.Type);
        Assert.Equal("الاسم مطلوب", response.Error.Message);
    }

    // ── 4. TransactionBehavior on LocalDB Relational Database ────────────────

    public record TestTxCommand(string Key, bool ShouldSucceed) : IRequest<Result<string>>, ITransactionalCommand;

    [Fact]
    public async Task TransactionBehavior_RollsBackOnResultFailure_OnLocalDb()
    {
        var dbName = "ResultPatternTxTest_" + Guid.NewGuid().ToString("N");
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={dbName};Integrated Security=true;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        try
        {
            var uow = new Meezan.Infrastructure.Persistence.Repositories.Common.UnitOfWork(dbContext);
            var behavior = new TransactionBehavior<TestTxCommand, Result<string>>(uow);

            var cmd = new TestTxCommand("test-key", ShouldSucceed: false);

            var response = await behavior.Handle(
                cmd,
                async (ct) =>
                {
                    dbContext.JobSettings.Add(new Meezan.Domain.Entities.JobSetting
                    {
                        JobKey = "test-key",
                        Enabled = true,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await dbContext.SaveChangesAsync(ct);
                    return Result.Failure<string>(Error.Failure("Failed", "حدث خطأ متعمد"));
                },
                CancellationToken.None);

            Assert.True(response.IsFailure);

            // Re-read with a clean DbContext to verify rollback on relational database
            using var verifyContext = new ApplicationDbContext(options);
            var setting = await verifyContext.JobSettings.FirstOrDefaultAsync(s => s.JobKey == "test-key");
            Assert.Null(setting); // Rolled back!
        }
        finally
        {
            await dbContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task TransactionBehavior_CommitsOnResultSuccess_OnLocalDb()
    {
        var dbName = "ResultPatternTxTest_" + Guid.NewGuid().ToString("N");
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={dbName};Integrated Security=true;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        try
        {
            var uow = new Meezan.Infrastructure.Persistence.Repositories.Common.UnitOfWork(dbContext);
            var behavior = new TransactionBehavior<TestTxCommand, Result<string>>(uow);

            var cmd = new TestTxCommand("test-key-success", ShouldSucceed: true);

            var response = await behavior.Handle(
                cmd,
                async (ct) =>
                {
                    dbContext.JobSettings.Add(new Meezan.Domain.Entities.JobSetting
                    {
                        JobKey = "test-key-success",
                        Enabled = true,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await dbContext.SaveChangesAsync(ct);
                    return Result.Success("ok");
                },
                CancellationToken.None);

            Assert.True(response.IsSuccess);

            // Re-read with a clean DbContext to verify commit on relational database
            using var verifyContext = new ApplicationDbContext(options);
            var setting = await verifyContext.JobSettings.FirstOrDefaultAsync(s => s.JobKey == "test-key-success");
            Assert.NotNull(setting);
            Assert.True(setting.Enabled);
        }
        finally
        {
            await dbContext.Database.EnsureDeletedAsync();
        }
    }
}

