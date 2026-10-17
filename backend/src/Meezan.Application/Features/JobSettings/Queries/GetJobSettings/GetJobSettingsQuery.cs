using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.JobSettings.Queries.GetJobSettings;

public record GetJobSettingsQuery() : IRequest<Result<Dictionary<string, bool>>>;
