using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Identity.Application.Commands.Positions
{
    public record DeletePositionCommand(Guid PositionId) : IRequest<Result>;

    public class DeletePositionCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeletePositionCommandHandler> logger) : IRequestHandler<DeletePositionCommand, Result>
    {
        public async Task<Result> Handle(
            DeletePositionCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                var position = await unitOfWork.Positions
                    .Include(p => p.Users)
                    .FirstOrDefaultAsync(p => p.Id == command.PositionId, cancellationToken);

                if (position == null)
                    return Result.Failure("Position not found");

                // Check if any users are assigned to this position
                if (position.Users.Any())
                    return Result.Failure($"Cannot delete position. {position.Users.Count} user(s) are currently assigned to this position");

                unitOfWork.Positions.Remove(position);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Position {PositionId} deleted successfully", command.PositionId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting position {PositionId}", command.PositionId);
                return Result.Failure("An error occurred while deleting the position");
            }
        }
    }
}
