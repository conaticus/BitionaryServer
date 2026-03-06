namespace BitionaryServer.Exceptions;

public class UserAlreadyExistsException() 
    : ApiException($"User with username or email already exists.", statusCode: StatusCodes.Status409Conflict);