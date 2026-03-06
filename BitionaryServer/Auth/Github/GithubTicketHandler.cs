using System.Net.Http.Headers;
using System.Security.Claims;
using BitionaryServer.Models;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace BitionaryServer.Auth.Github;

public abstract class GithubTicketHandler
{
    public static async Task OnCreatingTicket(OAuthCreatingTicketContext context)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.github.com/user/emails");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await context.Backchannel.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var emails = await response.Content.ReadFromJsonAsync<List<GithubEmail>>();

        var primaryEmail = emails?
            .FirstOrDefault(e => e.Primary && e.Verified)?.Email;

        if (!string.IsNullOrEmpty(primaryEmail))
        {
            context.Identity!.AddClaim(new Claim(ClaimTypes.Email, primaryEmail));
        }
    }
}