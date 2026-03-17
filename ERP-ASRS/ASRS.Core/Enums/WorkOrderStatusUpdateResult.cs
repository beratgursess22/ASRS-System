namespace ASRS.Core.Enums;

public enum WorkOrderStatusUpdateResult
{
    Success = 0,
    WorkOrderNotFound = 1,
    InvalidTransition = 2,
    BomCycleDetected = 3,
    StockInsufficient = 4,
    RestoreFailed = 5
}