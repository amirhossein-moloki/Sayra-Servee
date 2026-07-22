using System.ComponentModel.DataAnnotations;

namespace Sayra.Server.Application.DTOs;

public record AdvertisementItem(
    [Required] string Id,
    [Required] string Title,
    [Required] string ImageUrl,
    [Required] string ClickUrl,
    [Required] int Priority,
    [Required] int DurationSeconds
);
