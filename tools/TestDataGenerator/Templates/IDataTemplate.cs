// IDataTemplate.cs - Interface for test data templates
// Date: December 2, 2025

namespace TestDataGenerator.Templates;

public interface IDataTemplate
{
    /// <summary>
    /// Get the header/column names for the data
    /// </summary>
    string[] GetHeaders();

    /// <summary>
    /// Generate a single valid record
    /// </summary>
    Dictionary<string, object> GenerateValidRecord(int index);

    /// <summary>
    /// Generate a single invalid record with specified error type
    /// </summary>
    Dictionary<string, object> GenerateInvalidRecord(int index, ErrorType errorType);

    /// <summary>
    /// Get the schema name for this template
    /// </summary>
    string GetSchemaName();
}

public enum ErrorType
{
    MissingRequiredField,
    InvalidDataType,
    OutOfRangeValue,
    InvalidFormat,
    NullValue,
    EmptyString,
    InvalidDate,
    NegativeValue,
    TooLong,
    SpecialCharacters
}
