namespace SariSariPOS.API.DTOs;

public record CategoryDto(Guid Id, string Name, string? Description, int SortOrder);
public record CreateCategoryRequest(string Name, string? Description, int SortOrder = 0);
public record UpdateCategoryRequest(string Name, string? Description, int SortOrder);