using MCB.Core.Infra.CrossCutting.DesignPatterns.Validator.Abstractions;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Validator.Abstractions.Enums;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Validator.Abstractions.Models;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Validator;

public abstract class ValidatorBase
{
    // Protected Methods
    protected static ValidationMessageType CreateValidationMessageType(FluentValidation.Severity severity)
    {
        ValidationMessageType validationMessageType;

        if (severity == FluentValidation.Severity.Error)
            validationMessageType = ValidationMessageType.Error;
        else if (severity == FluentValidation.Severity.Warning)
            validationMessageType = ValidationMessageType.Warning;
        else
            validationMessageType = ValidationMessageType.Information;

        return validationMessageType;
    }
    protected static ValidationResult CreateValidationResult(FluentValidation.Results.ValidationResult fluentValidationValidationResult)
    {
        var validationMessageCollection = new List<ValidationMessage>();

        foreach (var validationFailure in fluentValidationValidationResult.Errors)
            validationMessageCollection.Add(
                new ValidationMessage(
                    ValidationMessageType: CreateValidationMessageType(validationFailure.Severity),
                    Code: validationFailure.ErrorCode,
                    Description: validationFailure.ErrorMessage
                )
            );

        return new ValidationResult(validationMessageCollection);
    }
}
public abstract class ValidatorBase<T>
    : ValidatorBase,
    IValidator<T>
{
    // Fields
    private bool _hasFluentValidationValidatorWrapperConfigured;

    // Properties
    public FluentValidationValidatorWrapper FluentValidationValidatorWrapperInstance { get; }

    // Constructors
    protected ValidatorBase()
    {
        FluentValidationValidatorWrapperInstance = new FluentValidationValidatorWrapper();
    }

    // Private Methods
    private void CheckAndConfigureFluentValidationConcreteValidator()
    {
        if (_hasFluentValidationValidatorWrapperConfigured)
            return;

        ConfigureFluentValidationConcreteValidator(FluentValidationValidatorWrapperInstance);

        _hasFluentValidationValidatorWrapperConfigured = true;
    }

    // Protected Methods
    protected abstract void ConfigureFluentValidationConcreteValidator(FluentValidationValidatorWrapper fluentValidationValidatorWrapper);
    protected string CreateMessageCodeInternal(ValidationMessageType validationMessageType, string codeBase)
    {
        return $"{validationMessageType}_{typeof(T).Name}_{codeBase}".ToUpperInvariant();
    }

    // Public Methods
    public ValidationResult Validate(T instance)
    {
        CheckAndConfigureFluentValidationConcreteValidator();

        return CreateValidationResult(FluentValidationValidatorWrapperInstance.Validate(instance));
    }
    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken)
    {
        CheckAndConfigureFluentValidationConcreteValidator();

        return CreateValidationResult(await FluentValidationValidatorWrapperInstance.ValidateAsync(instance, cancellationToken));
    }

    #region Fluent Validation Wrapper
    public class FluentValidationValidatorWrapper
        : FluentValidation.AbstractValidator<T>
    {

    }
    #endregion
}
