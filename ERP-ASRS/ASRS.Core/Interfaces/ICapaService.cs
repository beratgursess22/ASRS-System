using ASRS.Core.DTOs;
using ASRS.Core.Enums;

namespace ASRS.Core.Interfaces;

public interface ICapaService
{
	Task<IEnumerable<CapaActionDto>> GetByDefectIdAsync(int defectId);
	Task<bool> CreateAsync(CreateCapaActionDto dto);
	Task<bool> UpdateStatusAsync(int capaId, CapaStatus status, string? completionNote);
}
