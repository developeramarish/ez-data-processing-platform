namespace DemoDataGenerator.Models;

public static class HebrewCategories
{
    public static readonly string[] All = new[]
    {
        "מכירות",
        "כספים", 
        "משאבי אנוש",
        "מלאי",
        "שירות לקוחות",
        "שיווק",
        "לוגיסטיקה",
        "תפעול",
        "מחקר ופיתוח",
        "רכש"
    };
    
    // ASCII-only equivalents for Prometheus metrics (Prometheus spec requires ASCII)
    public static readonly string[] AllAscii = new[]
    {
        "sales",
        "finance", 
        "human_resources",
        "inventory",
        "customer_service",
        "marketing",
        "logistics",
        "operations",
        "research_development",
        "procurement"
    };
    
    // Mapping dictionary for Hebrew to ASCII conversion
    private static readonly Dictionary<string, string> HebrewToAsciiMap = new()
    {
        ["מכירות"] = "sales",
        ["כספים"] = "finance",
        ["משאבי אנוש"] = "human_resources",
        ["מלאי"] = "inventory",
        ["שירות לקוחות"] = "customer_service",
        ["שיווק"] = "marketing",
        ["לוגיסטיקה"] = "logistics",
        ["תפעול"] = "operations",
        ["מחקר ופיתוח"] = "research_development",
        ["רכש"] = "procurement"
    };

    public static string GetRandom(Random random) => All[random.Next(All.Length)];
    
    public static string ToAscii(string hebrewCategory)
    {
        return HebrewToAsciiMap.TryGetValue(hebrewCategory, out var ascii) 
            ? ascii 
            : hebrewCategory.Replace(" ", "_").Replace(",", "");
    }
    
    public static string GetRandomAscii(Random random) => AllAscii[random.Next(AllAscii.Length)];
}

public static class DemoConfig
{
    public const int DataSourceCount = 20;
    public const int GlobalMetricCount = 20;
    public const int MetricsPerDataSource = 3; // 2-4 random
    public const int RandomSeed = 42; // For deterministic generation
    
    public static readonly string[] DataSourceNames = new[]
    {
        "מערכת מכירות מרכזית",
        "נתוני לקוחות CRM",
        "קטלוג מוצרים",
        "ניהול עובדים",
        "דוחות כספיים",
        "סקרי לקוחות",
        "מערכת הזמנות",
        "מלאי מחסן",
        "פעילות שיווק",
        "תמיכה טכנית",
        "מעקב משלוחים",
        "ניתוח מכירות",
        "תוכנית תפעול",
        "מחקר שוק",
        "רכש ציוד",
        "ניהול פרויקטים",
        "בקרת איכות",
        "הזמנות ספקים",
        "ניתוח תחרות",
        "יעדי מכירות"
    };

    public static readonly string[] SupplierNames = new[]
    {
        "מערכת BI ראשית",
        "ספק נתונים חיצוני",
        "מערכת ERP",
        "פלטפורמת ענן",
        "מערכת POS",
        "מערכת CRM",
        "שירות אנליטיקה",
        "מערכת WMS",
        "פלטפורמת שיווק",
        "מערכת תמיכה"
    };
}
