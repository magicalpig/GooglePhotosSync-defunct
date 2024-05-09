using CasCap.Models;

namespace VidProbe;
public static class MediaItemExtensions
{
  public static string ToDetailedString(this MediaItem m)
  {
    return m.ToString() + $" {m.id}";
  }
}