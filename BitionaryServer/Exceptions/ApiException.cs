namespace BitionaryServer.Exceptions;

public class ApiException(string message, int statusCode = StatusCodes.Status500InternalServerError)
    : Exception(message)
{
    public int HttpStatusCode { get; } = statusCode;
}