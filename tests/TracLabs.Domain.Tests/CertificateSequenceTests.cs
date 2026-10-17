using FluentAssertions;
using TracLabs.Domain.Entities;
using Xunit;

namespace TracLabs.Domain.Tests;

public class CertificateSequenceTests
{
    [Fact]
    public void Increment_ShouldAdvanceSequenceByOne()
    {
        // Arrange
        var sequence = new CertificateSequence(2026, 42);

        // Act
        var next = sequence.Increment();

        // Assert
        next.Should().Be(43);
        sequence.LastNumber.Should().Be(43);
        sequence.Year.Should().Be(2026);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Constructor_WithInvalidYear_ShouldThrowException(int invalidYear)
    {
        // Act
        var act = () => new CertificateSequence(invalidYear);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
