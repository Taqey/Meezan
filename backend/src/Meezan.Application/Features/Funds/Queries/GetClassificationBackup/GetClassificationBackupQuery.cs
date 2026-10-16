using MediatR;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Queries.GetClassificationBackup;

public sealed record GetClassificationBackupQuery : IRequest<List<FundClassificationBackup>>;
