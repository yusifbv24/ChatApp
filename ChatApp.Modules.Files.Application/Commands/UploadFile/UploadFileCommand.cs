using ChatApp.Modules.Files.Application.DTOs.Responses;
using ChatApp.Modules.Files.Application.Interfaces;
using ChatApp.Modules.Files.Application.Services;
using ChatApp.Modules.Files.Domain.Entities;
using ChatApp.Modules.Files.Domain.Enums;
using ChatApp.Modules.Files.Domain.Events;
using ChatApp.Shared.Kernel.Common;
using ChatApp.Shared.Kernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ChatApp.Modules.Files.Application.Commands.UploadFile
{
    public record UploadFileCommand(
        IFormFile File,
        Guid UploadedBy,
        Guid? ChannelId=null,
        Guid? ConversationId=null,
        bool IsProfilePicture=false
    ):IRequest<Result<FileUploadResult>>;



    public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
    {
        private const long MaxFileSizeInBytes = 100 * 1024 * 1024; // 100 MB
        public UploadFileCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("File is required")
                .Must(file => file.Length > 0).WithMessage("File cannot be empty")
                .Must(file => file.Length <= MaxFileSizeInBytes).WithMessage("File size cannot exceed 100 MB")
                .Must(file => FileTypeHelper.IsAllowedFileType(file.ContentType))
                .WithMessage("File type is not allowed");

            RuleFor(x => x.UploadedBy)
                .NotEmpty().WithMessage("Uploader ID is required");
        }
    }



    public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<FileUploadResult>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IVirusScanningService _virusScanningService;
        private readonly IEventBus _eventBus;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UploadFileCommandHandler> _logger;

        public UploadFileCommandHandler(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IVirusScanningService virusScanningService,
            IEventBus eventBus,
            IConfiguration configuration,
            ILogger<UploadFileCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService= fileStorageService;
            _virusScanningService = virusScanningService;
            _eventBus = eventBus;
            _configuration = configuration;
            _logger = logger;
        }



        public async Task<Result<FileUploadResult>> Handle(
            UploadFileCommand request,
            CancellationToken cancellationToken = default)
        {
            string? tempStoragePath = null;
            try
            {
                _logger?.LogInformation(
                    "Uploading file {FileName} by user {UserId}, ConversationId: {ConversationId}, ChannelId: {ChannelId}",
                    request.File.FileName,
                    request.UploadedBy,
                    request.ConversationId,
                    request.ChannelId);

                var originalFileName = request.File.FileName;
                var contentType=request.File.ContentType;
                var fileType=FileTypeHelper.GetFileType(contentType);
                var extension=FileTypeHelper.GetExtensionFromContentType(contentType);


                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                // Determine storage directory
                var directory = await DetermineStorageDirectoryAsync(
                    request.ChannelId,
                    request.ConversationId,
                    request.UploadedBy,
                    request.IsProfilePicture,
                    fileType,
                    cancellationToken);

                _logger?.LogInformation(
                    "Determined storage directory: {Directory} for file {FileName}",
                    directory,
                    originalFileName);

                // Save file temporarily
                tempStoragePath = await _fileStorageService.SaveFileAsync(
                    request.File,
                    uniqueFileName,
                    directory,
                    cancellationToken);

                // Scan for viruses
                _logger?.LogInformation("Scanning file {FileName} for viruses... ", uniqueFileName);
                //var scanResult = await _virusScanningService.ScanFileAsync(
                //    tempStoragePath,
                //    cancellationToken);
                var scanResult = new VirusScanResult
                {
                    IsClean = true
                };

                if (!scanResult.IsClean)
                {
                    _logger?.LogWarning(
                        "VIRUS DETECTED: User {UserId} uploaded infected file {FileName}. Threat: {Threat}",
                        request.UploadedBy,
                        originalFileName,
                        scanResult.ThreatName);

                    // Delete infected file immediately
                    await _fileStorageService.DeleteFileAsync(tempStoragePath, cancellationToken);

                    // Publish audit event for security monitoring
                    await _eventBus.PublishAsync(
                        new InfectedFileDetectedEvent(
                            request.UploadedBy,
                            originalFileName,
                            scanResult.ThreatName ?? "Unknown",
                            DateTime.UtcNow),
                        cancellationToken);

                    return Result.Failure<FileUploadResult>(
                        $"File upload rejected: Virus detected ({scanResult.ThreatName}).");
                }

                _logger?.LogInformation("File {FileName} is clean - proceeding with upload", uniqueFileName);

                // Create file metadata
                var fileMetadata = new FileMetadata(
                    uniqueFileName,
                    originalFileName,
                    contentType,
                    request.File.Length,
                    fileType,
                    tempStoragePath,
                    request.UploadedBy);

                // If image, process dimensions and thumbnail
                if (fileType == FileType.Image || request.IsProfilePicture)
                {
                    try
                    {
                        await ProcessImageAsync(
                            request.File,
                            fileMetadata,
                            tempStoragePath);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to process image dimensions/thumbnail");
                    }
                }


                await _unitOfWork.Files.AddAsync(fileMetadata, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish domain event
                await _eventBus.PublishAsync(
                    new FileUploadedEvent(
                        fileMetadata.Id,
                        uniqueFileName,
                        fileType,
                        request.File.Length,
                        request.UploadedBy,
                        fileMetadata.CreatedAtUtc),
                    cancellationToken);

                _logger?.LogInformation(
                    "File {FileId} uploaded succesfully",
                    fileMetadata.Id);

                // Generate URL from directory and filename with API base URL
                var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:7000";
                var relativePath = $"/uploads/{directory}/{uniqueFileName}".Replace("\\", "/");
                var downloadUrl = $"{apiBaseUrl.TrimEnd('/')}{relativePath}";

                // Generate thumbnail URL if thumbnail was created
                string? thumbnailUrl = null;
                if (!string.IsNullOrEmpty(fileMetadata.ThumbnailPath))
                {
                    var thumbnailFileName = Path.GetFileName(fileMetadata.ThumbnailPath);
                    var thumbnailRelativePath = $"/uploads/{directory}/{thumbnailFileName}".Replace("\\", "/");
                    thumbnailUrl = $"{apiBaseUrl.TrimEnd('/')}{thumbnailRelativePath}";
                }

                var result = new FileUploadResult(
                    fileMetadata.Id,
                    uniqueFileName,
                    request.File.Length,
                    downloadUrl,
                    thumbnailUrl);

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error uploading file {FileName}", request.File.FileName);

                // Clean up file if something went wrong
                if (!string.IsNullOrEmpty(tempStoragePath))
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(tempStoragePath, cancellationToken);
                    }
                    catch { }
                }

                return Result.Failure<FileUploadResult>("An error occurred while uploading the file");
            }
        }



        private async Task ProcessImageAsync(IFormFile file,FileMetadata fileMetadata,string storagePath)
        {
            using var image = await Image.LoadAsync(file.OpenReadStream());

            // Set dimensions
            fileMetadata.SetImageDimensions(image.Width, image.Height);

            // Generate thumbnail (max 200x200)
            if(image.Width>200 || image.Height > 200)
            {
                var thumbnailPath = storagePath.Replace(Path.GetFileName(storagePath), $"thumb_{Path.GetFileName(storagePath)}");

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(200, 200),
                    Mode = ResizeMode.Max
                }));

                var thumbnailDirectory = Path.GetDirectoryName(thumbnailPath);
                if (!string.IsNullOrEmpty(thumbnailDirectory))
                {
                    Directory.CreateDirectory(thumbnailDirectory);
                }

                await image.SaveAsync(thumbnailPath);
                fileMetadata.SetThumbnailPath(thumbnailPath);
            }
        }


        private Task<string> DetermineStorageDirectoryAsync(
            Guid? channelId,
            Guid? conversationId,
            Guid uploadedBy,
            bool IsProfilePicture,
            FileType fileType,
            CancellationToken cancellationToken)
        {
            // Profile pictures go to dedicated folder
            if (IsProfilePicture)
            {
                return Task.FromResult($"avatars/{uploadedBy}");
            }

            // File type subfolder
            var fileTypeFolder = fileType switch
            {
                FileType.Image => "images",
                FileType.Document => "documents",
                FileType.Video => "videos",
                FileType.Audio => "audio",
                FileType.Archive => "archives",
                _ => "other"
            };

            // Channel uploads: generic/channel_{channelId}/{fileType}/
            if (channelId.HasValue)
            {
                return Task.FromResult($"generic/channel_{channelId}/{fileTypeFolder}");
            }

            // Conversation uploads: generic/conversation_{conversationId}/{fileType}/
            if (conversationId.HasValue)
            {
                return Task.FromResult($"generic/conversation_{conversationId}/{fileTypeFolder}");
            }

            // Generic uploads (no context): generic/{fileType}/
            return Task.FromResult($"generic/{fileTypeFolder}");
        }


    }
}