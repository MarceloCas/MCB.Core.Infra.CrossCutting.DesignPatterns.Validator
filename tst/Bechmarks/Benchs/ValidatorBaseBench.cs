using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using FluentValidation;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Validator;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Validator.Abstractions.Models;

namespace Bechmarks.Benchs;

[SimpleJob(RunStrategy.Throughput, launchCount: 1)]
[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
[MemoryDiagnoser]
[HtmlExporter]
public class ValidatorBaseBench
{
    [Params(1, 3, 5, 10, 20, 50, 100)]
    public int IterationCount { get; set; }

    [Benchmark()]
    public ValidationResult Validate()
    {
        var lastValidationResult = default(ValidationResult);
        var customerToValidate = new Customer();

        for (int i = 0; i < IterationCount; i++)
        {
            var dummyValidator = new DummyValidator();
            lastValidationResult = dummyValidator.Validate(customerToValidate);
        }

        return lastValidationResult;
    }
    [Benchmark(Baseline = true)]
    public ValidationResult Validate_Singleton()
    {
        var lastValidationResult = default(ValidationResult);
        var dummyValidator = new DummyValidator();
        var customerToValidate = new Customer();

        for (int i = 0; i < IterationCount; i++)
        {
            lastValidationResult = dummyValidator.Validate(customerToValidate);
        }

        return lastValidationResult;
    }
    [Benchmark()]
    public ValidationResult Validate_Parallel()
    {
        var lastValidationResult = default(ValidationResult);
        var customerToValidate = new Customer();

        Parallel.For(0, IterationCount, (i) =>
        {
            var dummyValidator = new DummyValidator();
            lastValidationResult = dummyValidator.Validate(customerToValidate);
        });

        return lastValidationResult;
    }
    [Benchmark()]
    public ValidationResult Validate_Parallel_Singleton()
    {
        var lastValidationResult = default(ValidationResult);
        var customerToValidate = new Customer();
        var dummyValidator = new DummyValidator();

        Parallel.For(0, IterationCount, (i) =>
        {
            lock (dummyValidator)
            {
                lastValidationResult = dummyValidator.Validate(customerToValidate);
            }
        });

        return lastValidationResult;
    }
}

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset BirthDate { get; set; }
}

public class DummyValidator
    : ValidatorBase<Customer>
{
    public static readonly string ID_IS_REQUIRED_CODE = "ID_IS_REQUIRED";
    public static readonly string NAME_IS_REQUIRED_CODE = "NAME_IS_REQUIRED_CODE";
    public static readonly string NAME_SOULD_HAVE_MAX_LENGTH_CODE = "NAME_SOULD_HAVE_MAX_LENGTH_CODE";
    public static readonly string BIRTH_DATE_IS_REQUIRED_CODE = "BIRTH_DATE_IS_REQUIRED_CODE";
    public static readonly string BIRTH_DATE_SOULD_HAVE_MAX_LENGTH_CODE = "BIRTH_DATE_SOULD_HAVE_MAX_LENGTH_CODE";

    protected override void ConfigureFluentValidationConcreteValidator(FluentValidationValidatorWrapper fluentValidationValidatorWrapper)
    {
        fluentValidationValidatorWrapper.RuleFor(q => q.Id)
            .Must(id => id != default)
            .WithErrorCode(ID_IS_REQUIRED_CODE)
            .WithMessage(ID_IS_REQUIRED_CODE)
            .WithSeverity(Severity.Error);

        fluentValidationValidatorWrapper.RuleFor(q => q.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithErrorCode(NAME_IS_REQUIRED_CODE)
            .WithMessage(NAME_IS_REQUIRED_CODE)
            .WithSeverity(Severity.Error)
            .Must(name => name.Length <= 150)
            .When(customer => !string.IsNullOrWhiteSpace(customer.Name))
            .WithErrorCode(NAME_SOULD_HAVE_MAX_LENGTH_CODE)
            .WithMessage(NAME_SOULD_HAVE_MAX_LENGTH_CODE)
            .WithSeverity(Severity.Error);

        fluentValidationValidatorWrapper.RuleFor(q => q.BirthDate)
            .Must(birthDate => birthDate > DateTimeOffset.MinValue)
            .WithErrorCode(BIRTH_DATE_IS_REQUIRED_CODE)
            .WithMessage(BIRTH_DATE_IS_REQUIRED_CODE)
            .WithSeverity(Severity.Error)
            .Must(birthDate => birthDate <= DateTimeOffset.UtcNow)
            .WithErrorCode(BIRTH_DATE_SOULD_HAVE_MAX_LENGTH_CODE)
            .WithMessage(BIRTH_DATE_SOULD_HAVE_MAX_LENGTH_CODE)
            .WithSeverity(Severity.Error);
    }
}