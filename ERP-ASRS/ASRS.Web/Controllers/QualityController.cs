using ASRS.Core.DTOs;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASRS.Web.Controllers;

[Authorize(Roles = "Yönetici,Kalite")]
public class QualityController : Controller
{
	private readonly IQualityInspectionService _qualityInspectionService;
	private readonly IQualityDefectService _qualityDefectService;

	public QualityController(
		IQualityInspectionService qualityInspectionService,
		IQualityDefectService qualityDefectService)
	{
		_qualityInspectionService = qualityInspectionService;
		_qualityDefectService = qualityDefectService;
	}

	[HttpGet]
	public async Task<IActionResult> Index(InspectionType? inspectionType, InspectionStatus? status)
	{
		var list = await _qualityInspectionService.GetAllAsync(inspectionType, status);
		ViewBag.InspectionType = inspectionType;
		ViewBag.Status = status;
		ViewBag.Types = Enum.GetValues<InspectionType>();
		ViewBag.Statuses = Enum.GetValues<InspectionStatus>();
		return View(list);
	}

	[HttpGet]
	public IActionResult Create()
	{
		ViewBag.Types = Enum.GetValues<InspectionType>();
		return View();
	}

	[HttpPost]
	public async Task<IActionResult> Create(CreateQualityInspectionDto dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrWhiteSpace(userId))
		{
			TempData["Error"] = "Kullanici bilgisi alinamadi.";
			return RedirectToAction(nameof(Index));
		}

		var ok = await _qualityInspectionService.CreateAsync(dto, userId);
		if (!ok)
		{
			TempData["Error"] = "Kalite kontrol kaydi olusturulamadi.";
			return RedirectToAction(nameof(Create));
		}

		TempData["Success"] = "Kalite kontrol kaydi olusturuldu.";
		return RedirectToAction(nameof(Index));
	}

	[HttpGet]
	public async Task<IActionResult> Details(int id)
	{
		var model = await _qualityInspectionService.GetByIdAsync(id);
		if (model == null)
			return NotFound();

		ViewBag.Statuses = Enum.GetValues<InspectionStatus>();
		ViewBag.Decisions = Enum.GetValues<UsageDecision>();
		ViewBag.Severities = Enum.GetValues<DefectSeverity>();
		return View(model);
	}

	[HttpPost]
	public async Task<IActionResult> UpdateStatus(int id, InspectionStatus status, string? notes)
	{
		var ok = await _qualityInspectionService.UpdateStatusAsync(id, status, notes);
		if (!ok)
		{
			TempData["Error"] = "Durum guncellenemedi. Gecis kurallarini kontrol edin.";
			return RedirectToAction(nameof(Details), new { id });
		}

		TempData["Success"] = "Durum guncellendi.";
		return RedirectToAction(nameof(Details), new { id });
	}

	[HttpPost]
	public async Task<IActionResult> Decide(int id, UsageDecision decision, string? notes)
	{
		var ok = await _qualityInspectionService.DecideAsync(id, decision, notes);
		if (!ok)
		{
			TempData["Error"] = "Kalite karari kaydedilemedi.";
			return RedirectToAction(nameof(Details), new { id });
		}

		TempData["Success"] = "Kalite karari kaydedildi.";
		return RedirectToAction(nameof(Details), new { id });
	}

	[HttpPost]
	public async Task<IActionResult> AddInspectionItem(int inspectionId, CreateQualityInspectionItemDto dto)
	{
		var ok = await _qualityInspectionService.AddInspectionItemAsync(inspectionId, dto);
		if (!ok)
		{
			TempData["Error"] = "Kontrol kriteri eklenemedi.";
			return RedirectToAction(nameof(Details), new { id = inspectionId });
		}

		TempData["Success"] = "Kontrol kriteri eklendi.";
		return RedirectToAction(nameof(Details), new { id = inspectionId });
	}

	[HttpPost]
	public async Task<IActionResult> ReportDefect(CreateQualityDefectDto dto)
	{
		var ok = await _qualityDefectService.CreateAsync(dto);
		if (!ok)
		{
			TempData["Error"] = "Uygunsuzluk kaydi olusturulamadi.";
			return RedirectToAction(nameof(Details), new { id = dto.QualityInspectionId });
		}

		TempData["Success"] = "Uygunsuzluk kaydi eklendi.";
		return RedirectToAction(nameof(Details), new { id = dto.QualityInspectionId });
	}
}
