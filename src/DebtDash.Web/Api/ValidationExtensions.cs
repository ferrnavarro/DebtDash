using FluentValidation;

namespace DebtDash.Web.Api;

public static class ValidationExtensions
{
    public static IServiceCollection AddValidationPipeline(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();
        return services;
    }

    public static async Task<IResult> ValidateAndProcess<T>(
        T request,
        IValidator<T> validator,
        Func<Task<IResult>> process)
    {
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
            return Results.ValidationProblem(errors);
        }
        return await process();
    }
}
