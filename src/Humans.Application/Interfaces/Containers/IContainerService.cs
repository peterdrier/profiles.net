using NodaTime;

namespace Humans.Application.Interfaces.Containers;

public interface IContainerService
{
    Task<IReadOnlyList<ContainerDto>> GetBySeasonAsync(Guid campSeasonId, CancellationToken ct = default);
    Task<IReadOnlyList<ContainerDto>> GetOrgByYearAsync(int year, CancellationToken ct = default);
    Task<IReadOnlyList<ContainerDto>> GetAllByYearAsync(int year, CancellationToken ct = default);
    Task<ContainerDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ContainerDto> CreateAsync(ContainerData data, CancellationToken ct = default);
    Task<ContainerDto> UpdateAsync(Guid id, ContainerData data, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ContainerDto> SavePlacementAsync(Guid id, string geoJson, CancellationToken ct = default);
    Task ClearPlacementAsync(Guid id, CancellationToken ct = default);
}

public interface IContainerImageStorage
{
    Task<string> SaveImageAsync(Guid containerId, Stream stream, string contentType, ContainerImageKind kind, CancellationToken ct = default);
    void DeleteImage(string storagePath);
}

public enum ContainerImageKind { Main, Placement }

public record ContainerImageUpload(Stream Content, string ContentType, string FileName, long Length);

public record ContainerDto(
    Guid Id,
    Guid? CampSeasonId,
    int Year,
    string Name,
    string? Description,
    string? ImageStoragePath,
    string? ImageContentType,
    string? ImageFileName,
    string? LocationGeoJson,
    string? PlacementNotes,
    string? PlacementImageStoragePath,
    string? PlacementImageContentType,
    string? PlacementImageFileName,
    Instant CreatedAt,
    Instant UpdatedAt
);

public record ContainerData(
    Guid? CampSeasonId,
    int Year,
    string Name,
    string? Description,
    string? PlacementNotes = null,
    ContainerImageUpload? MainImage = null,
    ContainerImageUpload? PlacementImage = null,
    bool RemoveMainImage = false,
    bool RemovePlacementImage = false
);
