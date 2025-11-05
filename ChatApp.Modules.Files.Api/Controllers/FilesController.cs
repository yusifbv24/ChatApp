using ChatApp.Modules.Files.Application.Commands.DeleteFile;
using ChatApp.Modules.Files.Application.Commands.UploadFile;
using ChatApp.Modules.Files.Application.DTOs.Requests;
using ChatApp.Modules.Files.Application.DTOs.Responses;
using ChatApp.Modules.Files.Application.Interfaces;
using ChatApp.Modules.Files.Application.Queries.GetFileById;
using ChatApp.Modules.Files.Application.Queries.GetUserFiles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatApp.Modules.Files.Api.Controllers
{
    /// <summary>
    /// Controller for file upload, download, and management
    /// </summary>
    [ApiController]
    [Route("api/files")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            IMediator mediator,
            IFileStorageService fileStorageService,
            IUnitOfWork unitOfWork,
            ILogger<FilesController> logger)
        {
            _mediator = mediator;
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }



        /// <summary>
        /// Upload a file
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
        [ProducesResponseType(typeof(FileUploadResult), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadFile(
            [FromForm] UploadFileRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new UploadFileCommand(request.File, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetFile),
                new { fileId = result.Value!.FileId },
                result.Value);
        }



        /// <summary>
        /// Upload profile picture (auto-resized to 400x400 thumbnail)
        /// </summary>
        [HttpPost("upload/profile-picture")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB for profile pictures
        [ProducesResponseType(typeof(FileUploadResult), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadProfilePicture(
            [FromForm] UploadFileRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            // Validate it's an image
            if (!request.File.ContentType.StartsWith("image/"))
            {
                return BadRequest(new { error = "Only image files are allowed for profile pictures" });
            }

            var result = await _mediator.Send(
                new UploadFileCommand(
                    request.File,
                    userId,
                    null,
                    null,
                    true),  // Special flag for profile pictures
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return CreatedAtAction(
                nameof(GetFile),
                new { fileId = result.Value!.FileId },
                result.Value);
        }




        /// <summary>
        /// Get file metadata by ID
        /// </summary>
        [HttpGet("{fileId:guid}")]
        [ProducesResponseType(typeof(FileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFile(
            [FromRoute] Guid fileId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetFileByIdQuery(fileId, userId),
                cancellationToken);

            if (result.IsFailure)
                return Forbid();

            if (result.Value == null)
                return NotFound(new { error = $"File with ID {fileId} not found" });

            return Ok(result.Value);
        }



        /// <summary>
        /// Download a file
        /// </summary>
        [HttpGet("{fileId:guid}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DownloadFile(
            [FromRoute] Guid fileId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var fileMetadata = await _unitOfWork.Files.GetByIdAsync(fileId, cancellationToken);

            if (fileMetadata == null)
                return NotFound(new { error = $"File with ID {fileId} not found" });

            // Permission check: Only uploader can download
            // In production, you'd check if file is shared in a channel/conversation
            if (fileMetadata.UploadedBy != userId)
            {
                return Forbid();
            }

            // Check if user has permission to download file
            var hasPermission = await CheckFileAccessPermissionAsync(fileId, userId, cancellationToken);

            if (!hasPermission)
            {
                _logger?.LogWarning(
                    "User {UserId} attempted to access file {FileId} without permission",
                    userId,
                    fileId);
                return Forbid();
            }

            try
            {
                var fileStream = await _fileStorageService.GetFileStreamAsync(
                    fileMetadata.StoragePath,
                    cancellationToken);

                return File(
                    fileStream,
                    fileMetadata.ContentType,
                    fileMetadata.OriginalFileName,
                    enableRangeProcessing: true);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { error = "File not found in storage" });
            }
        }


        /// <summary>
        /// Download thumbnail (for images only)
        /// </summary>
        [HttpGet("{fileId:guid}/thumbnail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DownloadThumbnail(
            [FromRoute] Guid fileId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var fileMetadata = await _unitOfWork.Files.GetByIdAsync(fileId, cancellationToken);

            if (fileMetadata == null)
                return NotFound(new { error = $"File with ID {fileId} not found" });

            if (string.IsNullOrEmpty(fileMetadata.ThumbnailPath))
                return NotFound(new { error = "Thumbnail not available for this file" });

            // Permission check
            if (fileMetadata.UploadedBy != userId)
            {
                return Forbid();
            }

            try
            {
                var fileStream = await _fileStorageService.GetFileStreamAsync(
                    fileMetadata.ThumbnailPath,
                    cancellationToken);

                return File(fileStream, fileMetadata.ContentType);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { error = "Thumbnail not found in storage" });
            }
        }


        /// <summary>
        /// Get all files uploaded by current user
        /// </summary>
        [HttpGet("my-files")]
        [ProducesResponseType(typeof(List<FileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyFiles(
            [FromQuery] int pageSize = 50,
            [FromQuery] int skip = 0,
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new GetUserFilesQuery(userId, pageSize, skip),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }


        /// <summary>
        /// Delete a file
        /// </summary>
        [HttpDelete("{fileId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFile(
            [FromRoute] Guid fileId,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _mediator.Send(
                new DeleteFileCommand(fileId, userId),
                cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(new { message = "File deleted successfully" });
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Guid.Empty;
            }

            return userId;
        }


        /// <summary>
        /// Check if user has permission to access a file
        /// User has access if:
        /// 1. They uploaded the file
        /// 2. File is used in a channel they're a member of
        /// 3. File is used in a conversation they're part of
        /// </summary>
        private async Task<bool> CheckFileAccessPermissionAsync(
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(fileId, cancellationToken);

            if (file == null) return false;

            // Check 1: User is the uploader
            if (file.UploadedBy == userId)
                return true;

            // Check 2: File is used in a channel where user is member
            var isInChannel = await _unitOfWork.Files.IsFileUsedInUserChannelsAsync(
                fileId,
                userId,
                cancellationToken);

            if(isInChannel)
                return true;

            // Check 3: File is used in a conversation where user is participant
            var isInConversation = await _unitOfWork.Files.IsFileUsedInUserConversationsAsync(
                fileId,
                userId,
                cancellationToken);

            return isInConversation;
        }
    }
}