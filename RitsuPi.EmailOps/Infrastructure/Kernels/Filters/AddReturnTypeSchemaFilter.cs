using Microsoft.SemanticKernel;

namespace RitsuPi.EmailOps.Infrastructure.Kernels.Filters;

// https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/adding-native-plugins?pivots=programming-language-csharp
public sealed class AddReturnTypeSchemaFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        await next(context);

        FunctionResultWithSchema resultWithSchema = new()
        {
            Value = context.Result.GetValue<object>(), 
            Schema = context.Function.Metadata.ReturnParameter?.Schema
        };

        context.Result = new FunctionResult(context.Result, resultWithSchema);
    }

    private sealed class FunctionResultWithSchema
    {
        public object? Value { get; set; }
        public KernelJsonSchema? Schema { get; set; }
    }
}
