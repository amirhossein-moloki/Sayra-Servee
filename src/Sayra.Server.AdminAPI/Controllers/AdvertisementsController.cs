using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("api/advertisements")]
public class AdvertisementsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdvertisementItem>), 200)]
    public IActionResult Get()
    {
        var items = new List<AdvertisementItem>
        {
            new AdvertisementItem(
                "AD-101",
                "Monster Energy Summer Deal",
                "http://api.sayra-gamenet.lan/cdn/ads/monster.jpg",
                "http://api.sayra-gamenet.lan/promo/monster",
                10,
                15
            ),
            new AdvertisementItem(
                "AD-102",
                "Weekend Tournament Registration",
                "http://api.sayra-gamenet.lan/cdn/ads/tournament.jpg",
                "http://api.sayra-gamenet.lan/promo/tournament",
                5,
                10
            )
        };

        return Ok(items);
    }
}
