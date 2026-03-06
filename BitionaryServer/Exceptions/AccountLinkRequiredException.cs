namespace BitionaryServer.Exceptions;

public class AccountLinkRequiredException() 
    : ApiException($"You must login with username and password and verify your email before linking your Github account.", statusCode: StatusCodes.Status403Forbidden);