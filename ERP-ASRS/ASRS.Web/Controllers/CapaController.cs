using ASRS.Core.DTOs;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASRS.Web.Controllers;

[Authorize(Roles = "Yönetici,Kalite")]
public class CapaController : Controller
{
	private readonly ICapaService _capaService;

	public CapaController(ICapaService capaService)
	{
		_capaService = capaService;
	}

	[HttpGet]
	public async Task<IActionResult> Index(int defectId)
	{
		var list = await _capaService.GetByDefectIdAsync(defectId);
		ViewBag.DefectId = defectId;
		ViewBag.Statuses = Enum.GetValues<CapaStatus>();
		return View(list);
	}

	[HttpPost]
	public async Task<IActionResult> Create(CreateCapaActionDto dto)
	{
		var ok = await _capaService.CreateAsync(dto);
		if (!ok)
		{
			TempData["Error"] = "CAPA kaydi olusturulamadi.";
			return RedirectToAction(nameof(Index), new { defectId = dto.QualityDefectId });
		}

		TempData["Success"] = "CAPA kaydi olusturuldu.";
		return RedirectToAction(nameof(Index), new { defectId = dto.QualityDefectId });
	}

	[HttpPost]
	public async Task<IActionResult> UpdateStatus(int capaId, int defectId, CapaStatus status, string? completionNote)
	{
		var ok = await _capaService.UpdateStatusAsync(capaId, status, completionNote);
		if (!ok)
		{
			TempData["Error"] = "CAPA durumu guncellenemedi.";
			return RedirectToAction(nameof(Index), new { defectId });
		}

		TempData["Success"] = "CAPA durumu guncellendi.";
		return RedirectToAction(nameof(Index), new { defectId });
	}
}
