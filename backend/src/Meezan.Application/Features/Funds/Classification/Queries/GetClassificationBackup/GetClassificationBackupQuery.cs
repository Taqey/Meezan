using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationBackup;

public sealed record GetClassificationBackupQuery : IRequest<Result<List<FundClassificationBackup>>>;
