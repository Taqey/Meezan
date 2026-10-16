using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.MergeFundCategories;

public sealed record MergeFundCategoriesCommand(int SourceCategoryId, int TargetCategoryId) : IRequest<MergeFundCategoriesResultDto>, ITransactionalCommand;
