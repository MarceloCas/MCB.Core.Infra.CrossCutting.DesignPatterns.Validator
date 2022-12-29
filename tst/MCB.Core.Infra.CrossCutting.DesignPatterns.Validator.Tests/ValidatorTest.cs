using FluentAssertions;
using FluentValidation;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Validator;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Validator.Abstractions.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Tests.ValidatorTests;

public class ValidatorTest
{
    [Fact]
    public async Task Validator_Should_Validate()
    {
        // Arrange
        var customerValidator = new CustomerValidator();
        var invalidCustomer = new Customer()
        {
            Id = Guid.Empty,
            Name = string.Empty,
            BirthDate = default,
            IsActive = false
        };
        var underAgeCustomer = new Customer()
        {
            Id = Guid.NewGuid(),
            Name = "Customer A",
            BirthDate = DateTime.UtcNow.AddYears(-17),
            IsActive = true
        };
        var customer = new Customer()
        {
            Id = Guid.NewGuid(),
            Name = "Customer A",
            BirthDate = DateTime.UtcNow.AddYears(-19),
            IsActive = true
        };

        // Act
        var invalidCustomerValidationResult = customerValidator.Validate(invalidCustomer);
        var underAgeCustomerValidationResult = await customerValidator.ValidateAsync(underAgeCustomer, cancellationToken: default);
        var customerValidationResult = customerValidator.Validate(customer);

        // Assert
        invalidCustomerValidationResult.Should().NotBeNull();
        invalidCustomerValidationResult.HasErrorMessages.Should().BeTrue();
        invalidCustomerValidationResult.IsValid.Should().BeFalse();
        invalidCustomerValidationResult.HasValidationMessage.Should().BeTrue();
        invalidCustomerValidationResult.ValidationMessageCollection.Should().HaveCount(4);

        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[0].ValidationMessageType.Should().Be(ValidationMessageType.Error);
        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[0].Code.Should().Be("CustomerGuidIsRequired");
        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[0].Description.Should().Be("Customer Id is Required");

        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[1].ValidationMessageType.Should().Be(ValidationMessageType.Error);
        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[1].Code.Should().Be("CustomerNameIsRequired");
        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[1].Description.Should().Be("Customer Name is Required");

        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[2].ValidationMessageType.Should().Be(ValidationMessageType.Error);
        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[2].Code.Should().Be("CustomerBirthDateIsRequired");
        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[2].Description.Should().Be("Customer BirthDate is Required");

        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[3].ValidationMessageType.Should().Be(ValidationMessageType.Warning);
        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[3].Code.Should().Be("CustomerIsNotActive");
        invalidCustomerValidationResult.ValidationMessageCollection.ToArray()[3].Description.Should().Be("Customer is not active");

        underAgeCustomerValidationResult.Should().NotBeNull();
        underAgeCustomerValidationResult.HasErrorMessages.Should().BeFalse();
        underAgeCustomerValidationResult.IsValid.Should().BeTrue();
        underAgeCustomerValidationResult.HasValidationMessage.Should().BeTrue();
        underAgeCustomerValidationResult.ValidationMessageCollection.Should().HaveCount(1);

        underAgeCustomerValidationResult.ValidationMessageCollection.ToArray()[0].ValidationMessageType.Should().Be(ValidationMessageType.Information);
        underAgeCustomerValidationResult.ValidationMessageCollection.ToArray()[0].Code.Should().Be("CustomerIsUnderAge");
        underAgeCustomerValidationResult.ValidationMessageCollection.ToArray()[0].Description.Should().Be("Customer is under age");

        customerValidationResult.Should().NotBeNull();
        customerValidationResult.HasErrorMessages.Should().BeFalse();
        customerValidationResult.IsValid.Should().BeTrue();
        customerValidationResult.HasValidationMessage.Should().BeFalse();
        customerValidationResult.ValidationMessageCollection.Should().HaveCount(0);

    }

    [Fact]
    public void Validator_Should_CreateMessageCode()
    {
        // Arrange
        var expectedValue = "ERROR_CUSTOMER_IDISREQUIRED";
        var customerValidator = new CustomerValidator();

        // Act
        var messageCode = customerValidator.CreateMessageCode(ValidationMessageType.Error, "IdIsRequired");

        // Assert
        messageCode.Should().Be(expectedValue);
    }
}

public class CustomerValidator
    : ValidatorBase<Customer>
{
    protected override void ConfigureFluentValidationConcreteValidator(FluentValidationValidatorWrapper fluentValidationValidatorWrapper)
    {
        fluentValidationValidatorWrapper.RuleFor(customer => customer.Id)
            .Must(id => id != Guid.Empty)
            .WithErrorCode("CustomerGuidIsRequired")
            .WithMessage("Customer Id is Required")
            .WithSeverity(Severity.Error);

        fluentValidationValidatorWrapper.RuleFor(customer => customer.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithErrorCode("CustomerNameIsRequired")
            .WithMessage("Customer Name is Required")
            .WithSeverity(Severity.Error);

        fluentValidationValidatorWrapper.RuleFor(customer => customer.BirthDate)
            .GreaterThan(default(DateTime))
            .WithErrorCode("CustomerBirthDateIsRequired")
            .WithMessage("Customer BirthDate is Required")
            .WithSeverity(Severity.Error);

        fluentValidationValidatorWrapper.RuleFor(customer => customer.BirthDate)
            .Must(birthDate => {
                /*
                 * This age calc is wrong because not see the month and day of the current year, but is only a test
                 */
                var age = DateTime.UtcNow.Year - birthDate.Year;
                return age >= 18;
            })
            .When(customer => customer.BirthDate != default)
            .WithErrorCode("CustomerIsUnderAge")
            .WithMessage("Customer is under age")
            .WithSeverity(Severity.Info);

        fluentValidationValidatorWrapper.RuleFor(customer => customer.IsActive)
            .Must(isActive => isActive)
            .WithErrorCode("CustomerIsNotActive")
            .WithMessage("Customer is not active")
            .WithSeverity(Severity.Warning);
    }

    public string CreateMessageCode(ValidationMessageType validationMessageType, string codeBase)
        => CreateMessageCodeInternal(validationMessageType, codeBase);
}

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime BirthDate { get; set; }
    public bool IsActive { get; set; }

    public Customer()
    {
        Id = Guid.Empty;
        Name = string.Empty;
        BirthDate = DateTime.Now;
    }
}
