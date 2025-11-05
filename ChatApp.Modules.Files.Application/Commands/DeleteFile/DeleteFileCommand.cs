using ChatApp.Modules.Files.Application.Interfaces;
using ChatApp.Modules.Files.Domain.Events;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Exceptions;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Files.Application.Commands.DeleteFile
{
    public record DeleteFileCommand(
        Guid FileId,
        Guid RequestedBy
    ):IRequest<Result>;


    public class DeleteFileCommandAbstractor : AbstractValidator<DeleteFileCommand>
    {
        public DeleteFileCommandAbstractor()
        {
            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("File ID is required");

            RuleFor(x => x.RequestedBy)
                .NotEmpty().WithMessage("Requester ID is required");
        }
    }


    public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<DeleteFileCommandHandler> _logger;

        public DeleteFileCommandHandler(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IEventBus eventBus,
            ILogger<DeleteFileCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task<Result> Handle(
            DeleteFileCommand request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation("Deleting File {FileId} ", request.FileId);

                var file = await _unitOfWork.Files.GetByIdAsync(request.FileId, cancellationToken);

                if (file == null)
                    throw new NotFoundException($"File with ID {request.FileId} not found");

                // Only uploader can delete their own file
                if (file.UploadedBy != request.RequestedBy)
                {
                    return Result.Failure("You can only delete files you uploaded");
                }

                if (file.IsDeleted)
                {
                    return Result.Failure("File is already deleted");
                }

                // Soft delete in database
                file.Delete(request.RequestedBy.ToString());

                await _unitOfWork.Files.UpdateAsync(file, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Delete physical file from storage
                //try
                //{
                //    await _fileStorageService.DeleteFileAsync(file.StoragePath, cancellationToken);

                //    // Delete thumbnail if exists
                //    if (!string.IsNullOrEmpty(file.ThumbnailPath))
                //    {
                //        await _fileStorageService.DeleteFileAsync(file.ThumbnailPath,cancellationToken);
                //    }
                //}
                //catch (Exception ex)
                //{
                //    _logger?.LogWarning(ex, "Failed to delete physical file {StoragePath}", file.StoragePath);
                //}

                // Publish domain event
                await _eventBus.PublishAsync(
                    new FileDeletedEvent(
                        file.Id,
                        file.FileName,
                        request.RequestedBy,
                        DateTime.UtcNow),
                    cancellationToken);

                _logger?.LogInformation("File {FileId} deleted succesfully", request.FileId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting file {FileId}", request.FileId);
                return Result.Failure(ex.Message);
            }
        }
    }
}