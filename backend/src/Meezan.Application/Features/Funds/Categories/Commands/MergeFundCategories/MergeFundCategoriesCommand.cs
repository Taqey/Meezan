using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Categories.Commands.MergeFundCategories;

public sealed record MergeFundCategoriesCommand(int SourceCategoryId, int TargetCategoryId) : IRequest<Result<MergeFundCategoriesResultDto>>, ITransactionalCommand;
