using MediatR;

namespace Meezan.Application.Features.JobSettings.Queries.GetJobSettings;

public record GetJobSettingsQuery() : IRequest<Dictionary<string, bool>>;
