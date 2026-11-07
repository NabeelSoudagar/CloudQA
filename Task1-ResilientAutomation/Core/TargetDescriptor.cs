using System.Collections.Generic;

namespace Task1_ResilientAutomation.Core;

public enum FieldType
{
    TextInput,
    RadioButtonGroup,
    DropdownSelect
}

public class TargetDescriptor
{
    public string FieldName { get; set; } = string.Empty;
    public FieldType Type { get; set; }
    
    // Primary keywords expected in label, aria-label, placeholder, or nearby text
    public List<string> PositiveKeywords { get; set; } = new();

    // Negative keywords to disambiguate similar fields (e.g. "last name", "middle name", "statement")
    public List<string> NegativeKeywords { get; set; } = new();

    // Specific attribute fragments that might exist (e.g. fname, first, gender, state)
    public List<string> AttributeClues { get; set; } = new();

    // Value to set or select
    public string TargetValue { get; set; } = string.Empty;

    public static TargetDescriptor ForFirstName(string value = "Alex") => new()
    {
        FieldName = "First Name",
        Type = FieldType.TextInput,
        PositiveKeywords = new() { "first name", "firstname", "first", "given name", "forename" },
        NegativeKeywords = new() { "last name", "lastname", "last", "surname", "middle name", "middle", "spouse", "father", "mother", "guardian" },
        AttributeClues = new() { "fname", "firstname", "first_name", "first-name", "givenname" },
        TargetValue = value
    };

    public static TargetDescriptor ForGender(string option = "Male") => new()
    {
        FieldName = "Gender",
        Type = FieldType.RadioButtonGroup,
        PositiveKeywords = new() { "gender", "sex" },
        NegativeKeywords = new() { "marital status", "genre", "state" },
        AttributeClues = new() { "gender", "sex" },
        TargetValue = option
    };

    public static TargetDescriptor ForState(string option = "India") => new()
    {
        FieldName = "State",
        Type = FieldType.DropdownSelect,
        PositiveKeywords = new() { "state", "province", "region", "state / province" },
        NegativeKeywords = new() { "statement", "status", "country code" },
        AttributeClues = new() { "state", "province", "region" },
        TargetValue = option
    };
}

