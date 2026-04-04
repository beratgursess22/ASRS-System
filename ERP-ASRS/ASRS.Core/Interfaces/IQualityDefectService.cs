using ASRS.Core.DTOs;

namespace ASRS.Core.Interfaces;

public interface IQualityDefectService
{
	Task<IEnumerable<QualityDefectDto>> GetByInspectionIdAsync(int inspectionId);
	Task<bool> CreateAsync(CreateQualityDefectDto dto);
	Task<bool> ResolveAsync(int defectId);
}
