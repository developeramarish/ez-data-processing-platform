// CustomerTransactionTemplate.cs - Template for customer transaction test data
// Date: December 2, 2025

using Bogus;

namespace TestDataGenerator.Templates;

public class CustomerTransactionTemplate : IDataTemplate
{
    private readonly Faker _faker;
    private readonly bool _allowErrors;
    private readonly Random _random;

    private CustomerTransactionTemplate(bool allowErrors = false)
    {
        _faker = new Faker();
        _allowErrors = allowErrors;
        _random = new Random(12345); // Fixed seed for reproducibility
    }

    public static CustomerTransactionTemplate CreateValid()
    {
        return new CustomerTransactionTemplate(allowErrors: false);
    }

    public static CustomerTransactionTemplate CreateWithErrors()
    {
        return new CustomerTransactionTemplate(allowErrors: true);
    }

    public string[] GetHeaders()
    {
        return new[]
        {
            "TransactionId",
            "CustomerId",
            "CustomerName",
            "TransactionDate",
            "Amount",
            "Currency",
            "TransactionType",
            "Status",
            "Description"
        };
    }

    public Dictionary<string, object> GenerateValidRecord(int index)
    {
        var transactionDate = _faker.Date.Between(DateTime.Now.AddDays(-30), DateTime.Now);

        return new Dictionary<string, object>
        {
            ["TransactionId"] = $"TXN-{DateTime.Now:yyyyMMdd}-{index:D6}",
            ["CustomerId"] = $"CUST-{_faker.Random.Number(1000, 9999)}",
            ["CustomerName"] = _faker.Name.FullName(),
            ["TransactionDate"] = transactionDate.ToString("yyyy-MM-dd HH:mm:ss"),
            ["Amount"] = Math.Round(_faker.Random.Decimal(10.00M, 10000.00M), 2),
            ["Currency"] = _faker.PickRandom(new[] { "USD", "EUR", "GBP", "ILS" }),
            ["TransactionType"] = _faker.PickRandom(new[] { "Purchase", "Refund", "Transfer", "Withdrawal", "Deposit" }),
            ["Status"] = _faker.PickRandom(new[] { "Completed", "Pending", "Processing" }),
            ["Description"] = _faker.Commerce.ProductDescription()
        };
    }

    public Dictionary<string, object> GenerateInvalidRecord(int index, ErrorType errorType)
    {
        var record = GenerateValidRecord(index);

        // Apply error based on type
        switch (errorType)
        {
            case ErrorType.MissingRequiredField:
                record.Remove("TransactionId"); // Required field missing
                break;

            case ErrorType.InvalidDataType:
                record["Amount"] = "INVALID_NUMBER"; // Should be decimal
                break;

            case ErrorType.OutOfRangeValue:
                record["Amount"] = -1000.00M; // Negative amount not allowed
                break;

            case ErrorType.InvalidFormat:
                record["TransactionDate"] = "2025/12/02"; // Wrong format (should be yyyy-MM-dd)
                break;

            case ErrorType.NullValue:
                record["CustomerName"] = null!; // Null not allowed
                break;

            case ErrorType.EmptyString:
                record["Currency"] = ""; // Empty string not allowed
                break;

            case ErrorType.InvalidDate:
                record["TransactionDate"] = "2025-13-45 25:99:99"; // Invalid date
                break;

            case ErrorType.TooLong:
                record["Description"] = new string('A', 1001); // Max 1000 chars
                break;

            case ErrorType.SpecialCharacters:
                record["TransactionId"] = "TXN-<script>alert('XSS')</script>"; // Invalid characters
                break;
        }

        return record;
    }

    public string GetSchemaName()
    {
        return "customer-transaction-v1";
    }
}
