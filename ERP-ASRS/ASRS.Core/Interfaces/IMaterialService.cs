using ASRS.Core.DTOs;

namespace ASRS.Core.Interfaces;

public interface IMaterialService
{
    Task<IEnumerable<MaterialListDto>> GetAllMaterialsAsync(string? search);
    Task<MaterialListDto?> GetMaterialByIdAsync(int id);
    Task<bool> CreateMaterialAsync(CreateMaterialDto dto);
    Task<bool> UpdateMaterialAsync(int id, CreateMaterialDto dto);
    Task<bool> DeleteMaterialAsync(int id);
}