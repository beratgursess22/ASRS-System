using ASRS.Core.DTOs;
using ASRS.Core.Enums;

namespace ASRS.Core.Interfaces;

public interface IQualityInspectionService
{
	Task<IEnumerable<QualityInspectionListDto>> GetAllAsync(InspectionType? inspectionType, InspectionStatus? status);
	Task<QualityInspectionDetailDto?> GetByIdAsync(int id);
	Task<bool> CreateAsync(CreateQualityInspectionDto dto, string inspectedByUserId);
	Task<bool> UpdateStatusAsync(int id, InspectionStatus status, string? notes);
	Task<bool> DecideAsync(int id, UsageDecision decision, string? notes);
	Task<bool> AddInspectionItemAsync(int inspectionId, CreateQualityInspectionItemDto dto);
}
