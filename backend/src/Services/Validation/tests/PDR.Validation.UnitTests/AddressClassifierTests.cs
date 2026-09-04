using AwesomeAssertions;
using PDR.Validation.Application.Assess;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.UnitTests;

public class AddressClassifierTests
{
    [Fact]
    public void Classifies_discrete_elements_as_structured()
    {
        var result = AddressClassifier.Classify("DE", "Berlin", "10115", "Invalidenstrasse", "12", null);

        result.Should().Be(AddressClassification.Structured);
    }

    [Fact]
    public void Classifies_mixed_elements_and_free_text_as_hybrid()
    {
        var result = AddressClassifier.Classify("DE", "Berlin", "10115", null, null, "Invalidenstrasse 12");

        result.Should().Be(AddressClassification.Hybrid);
    }

    [Fact]
    public void Classifies_free_text_only_as_unstructured()
    {
        var result = AddressClassifier.Classify("DE", null, null, null, null, "Invalidenstrasse 12, Berlin");

        result.Should().Be(AddressClassification.Unstructured);
    }

    [Fact]
    public void Classifies_empty_address_as_absent()
    {
        var result = AddressClassifier.Classify(null, null, null, null, null, null);

        result.Should().Be(AddressClassification.Absent);
    }

    [Fact]
    public void Classifies_country_only_as_unrecognized()
    {
        var result = AddressClassifier.Classify("DE", null, null, null, null, null);

        result.Should().Be(AddressClassification.Unrecognized);
    }

    [Fact]
    public void Treats_whitespace_as_no_value()
    {
        var result = AddressClassifier.Classify("  ", "  ", null, null, null, "   ");

        result.Should().Be(AddressClassification.Absent);
    }
}
