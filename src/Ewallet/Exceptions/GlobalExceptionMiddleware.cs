using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Exceptions;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
	public async Task Invoke(HttpContext context)
	{
		try
		{
			await next(context);
		}
		catch (Exception ex) when (context.Request.Path.StartsWithSegments("/api") && !context.Response.HasStarted)
		{
			await HandleApiExceptionAsync(context, ex);
		}
	}

	private Task HandleApiExceptionAsync(HttpContext context, Exception ex)
	{
		var statusCode = ex switch
		{
			WalletNotFoundException or UserNotFoundException or PaymentMethodNotFoundException or TopUpNotFoundException
				=> HttpStatusCode.NotFound,
			ConflictException or DbUpdateConcurrencyException => HttpStatusCode.Conflict,
			WalletException => HttpStatusCode.BadRequest,
			DbUpdateException => HttpStatusCode.Conflict,              // constraint / unique violation
			_ => HttpStatusCode.InternalServerError
		};

		var message = ex switch
		{
			WalletException or SimulatedFailureException => ex.Message,
			DbUpdateConcurrencyException => "Data wallet telah diubah oleh proses lain. Tidak ada perubahan yang disimpan, silakan coba lagi.",
			DbUpdateException => "Perubahan ditolak oleh constraint database. Tidak ada perubahan yang disimpan.",
			_ => "Terjadi kesalahan pada server."
		};

		if ((int)statusCode >= 500) logger.LogError(ex, "Unhandled exception");
		else logger.LogInformation("Request rejected ({Status}): {Message}", (int)statusCode, message);

		context.Response.ContentType = "application/json";
		context.Response.StatusCode = (int)statusCode;
		var response = new { status = context.Response.StatusCode, error = message, correlationId = context.TraceIdentifier };
		return context.Response.WriteAsync(JsonSerializer.Serialize(response));
	}
}
