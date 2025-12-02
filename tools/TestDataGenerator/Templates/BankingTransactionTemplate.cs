// BankingTransactionTemplate.cs - Template for banking transaction test data
// Date: December 2, 2025

using Bogus;

namespace TestDataGenerator.Templates;

public class BankingTransactionTemplate : IDataTemplate
{
    private readonly Faker _faker;
    private readonly bool _allowErrors;

    private BankingTransactionTemplate(bool allowErrors = false)
    {
        _faker = new Faker();
        _allowErrors = allowErrors;
    }

    public static BankingTransactionTemplate CreateValid()
    {
        return new BankingTransactionTemplate(allowErrors: false);
    }

    public static BankingTransactionTemplate CreateWithErrors()
    {
        return new BankingTransactionTemplate(allowErrors: true);
    }

    public string[] GetHeaders()
    {
        return new[]
        {
            "AccountNumber",
            "TransactionId",
            "TransactionDate",
            "TransactionType",
            "Amount",
            "Balance",
            "Currency",
            "BranchCode",
            "TellerID",
            "ReferenceNumber",
            "Notes"
        };
    }

    public Dictionary<string, object> GenerateValidRecord(int index)
    {
        var amount = Math.Round(_faker.Random.Decimal(100.00M, 50000.00M), 2);
        var previousBalance = Math.Round(_faker.Random.Decimal(10000.00M, 100000.00M), 2);
        var transactionType = _faker.PickRandom(new[] { "Deposit", "Withdrawal", "Transfer", "Payment", "Interest" });

        var balance = transactionType == "Withdrawal" || transactionType == "Payment"
            ? previousBalance - amount
            : previousBalance + amount;

        return new Dictionary<string, object>
        {
            ["AccountNumber"] = $"{_faker.Random.Number(10000000, 99999999)}",
            ["TransactionId"] = $"BNK-{DateTime.Now:yyyyMMdd}-{index:D8}",
            ["TransactionDate"] = _faker.Date.Between(DateTime.Now.AddDays(-30), DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss"),
            ["TransactionType"] = transactionType,
            ["Amount"] = amount,
            ["Balance"] = Math.Round(balance, 2),
            ["Currency"] = "ILS",
            ["BranchCode"] = _faker.Random.Number(100, 999).ToString(),
            ["TellerID"] = $"T{_faker.Random.Number(1000, 9999)}",
            ["ReferenceNumber"] = $"REF{_faker.Random.Number(100000000, 999999999)}",
            ["Notes"] = _faker.Lorem.Sentence()
        };
    }

    public Dictionary<string, object> GenerateInvalidRecord(int index, ErrorType errorType)
    {
        var record = GenerateValidRecord(index);

        switch (errorType)
        {
            case ErrorType.MissingRequiredField:
                record.Remove("AccountNumber");
                break;

            case ErrorType.InvalidDataType:
                record["Amount"] = "NOT_A_NUMBER";
                break;

            case ErrorType.OutOfRangeValue:
                record["Amount"] = -99999.99M;
                break;

            case ErrorType.NegativeValue:
                record["Balance"] = -5000.00M;
                break;

            case ErrorType.InvalidFormat:
                record["TransactionDate"] = "02/12/2025";
                break;
        }

        return record;
    }

    public string GetSchemaName()
    {
        return "banking-transaction-v1";
    }
}
