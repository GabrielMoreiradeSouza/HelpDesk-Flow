using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk_Flow.Services;

public sealed class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class ApiExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ApiException error => new ProblemDetails { Status = error.StatusCode, Title = error.Message },
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => new ProblemDetails
            {
                Status = 409, Title = "O registro foi alterado em outra sessão. Atualize e tente novamente."
            },
            _ => null
        };

        if (problem is null)
            return false;

        context.Response.StatusCode = problem.Status!.Value;
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}
