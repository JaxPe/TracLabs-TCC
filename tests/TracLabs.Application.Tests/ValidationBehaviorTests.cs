using FluentAssertions;
using FluentValidation;
using MediatR;
using TracLabs.Application.Common.Behaviors;
using AppValidationException = TracLabs.Application.Common.Exceptions.ValidationException;
using Xunit;

namespace TracLabs.Application.Tests;

public class ValidationBehaviorTests
{
    public record SampleCommand(string Name) : IRequest<string>;

    public class SampleCommandValidator : AbstractValidator<SampleCommand>
    {
        public SampleCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nome não pode ser vazio.");
        }
    }

    [Fact]
    public async Task Handle_WhenRequestIsInvalid_ShouldThrowValidationException()
    {
        // Arrange
        var validators = new List<IValidator<SampleCommand>> { new SampleCommandValidator() };
        var behavior = new ValidationBehavior<SampleCommand, string>(validators);
        var invalidCommand = new SampleCommand("");

        // Act
        var act = () => behavior.Handle(
            invalidCommand,
            () => Task.FromResult("Sucesso"),
            CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
        ex.Which.Errors["Name"].Should().Contain("Nome não pode ser vazio.");
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_ShouldInvokeNextDelegate()
    {
        // Arrange
        var validators = new List<IValidator<SampleCommand>> { new SampleCommandValidator() };
        var behavior = new ValidationBehavior<SampleCommand, string>(validators);
        var validCommand = new SampleCommand("Laboratório RBC");

        // Act
        var result = await behavior.Handle(
            validCommand,
            () => Task.FromResult("Sucesso"),
            CancellationToken.None);

        // Assert
        result.Should().Be("Sucesso");
    }
}
