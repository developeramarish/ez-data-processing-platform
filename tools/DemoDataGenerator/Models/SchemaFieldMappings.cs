namespace DemoDataGenerator.Models;

/// <summary>
/// Maps datasource schema indices to their actual available fields for metric generation
/// </summary>
public static class SchemaFieldMappings
{
    /// <summary>
    /// Get appropriate field paths for a given datasource schema index
    /// Returns tuple of (countField, sumField, avgField, rateField)
    /// </summary>
    public static (string count, string sum, string avg, string rate) GetFieldPaths(int schemaIndex)
    {
        return schemaIndex switch
        {
            0 => ("$.transactionId", "$.amount", "$.amount", "$.date"),  // SimpleTransaction
            1 => ("$.userId", "$.userId", "$.userId", "$.birthDate"),  // UserProfile
            2 => ("$.productId", "$.price", "$.price", "$.productId"),  // ProductCatalog
            3 => ("$.employeeId", "$.salary", "$.salary", "$.employeeId"),  // EmployeeRecord
            4 => ("$.reportId", "$.revenue", "$.profit", "$.reportId"),  // FinancialReport
            5 => ("$.responseId", "$.rating", "$.rating", "$.submittedAt"),  // SurveyResponse
            6 => ("$.orderId", "$.totalAmount", "$.totalAmount", "$.orderDate"),  // OrderManagement
            7 => ("$.sku", "$.quantity", "$.quantity", "$.lastStockUpdate"),  // InventoryItem
            8 => ("$.campaignId", "$.budget", "$.budget", "$.startDate"),  // MarketingCampaign
            9 => ("$.ticketId", "$.ticketId", "$.ticketId", "$.ticketId"),  // CustomerSupportTicket
            10 => ("$.trackingNumber", "$.weight", "$.weight", "$.estimatedDelivery"),  // ShipmentTracking
            11 => ("$.analysisId", "$.totalSales", "$.averageOrderValue", "$.analysisId"),  // SalesAnalytics
            12 => ("$.planId", "$.budget", "$.budget", "$.startDate"),  // OperationalPlan
            13 => ("$.researchId", "$.sampleSize", "$.confidenceLevel", "$.researchId"),  // MarketResearch
            14 => ("$.poNumber", "$.totalValue", "$.totalValue", "$.deliveryDate"),  // ProcurementOrder
            15 => ("$.projectId", "$.projectId", "$.projectId", "$.projectId"),  // ProjectManagement
            16 => ("$.inspectionId", "$.defectCount", "$.defectCount", "$.inspectionDate"),  // QualityControl
            17 => ("$.orderId", "$.amount", "$.amount", "$.orderId"),  // SupplierOrder
            18 => ("$.analysisId", "$.marketShare", "$.marketShare", "$.analysisId"),  // CompetitorAnalysis
            19 => ("$.targetId", "$.targetAmount", "$.actualAmount", "$.targetId"),  // SalesTarget
            _ => ("$.recordId", "$.amount", "$.amount", "$.date")  // Fallback
        };
    }

    /// <summary>
    /// Get metric formula descriptions based on schema
    /// </summary>
    public static string GetFormulaDescription(int schemaIndex, string metricType)
    {
        var fields = GetFieldPaths(schemaIndex);
        
        return metricType switch
        {
            "count" => $"count({fields.count})",
            "sum" => $"sum({fields.sum})",
            "avg" => $"avg({fields.avg})",
            "rate" => $"rate({fields.rate}[5m])",
            _ => "count($.recordId)"
        };
    }
}
