using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SonqoAPI.Hubs
{
    [Authorize(Policy = "RequireStaff")]
    public class OrderHub : Hub
    {
    }
}