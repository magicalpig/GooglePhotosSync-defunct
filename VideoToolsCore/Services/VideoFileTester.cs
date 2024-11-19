using System.Text.RegularExpressions;
namespace VideoToolsCore.Services;

public static partial class VideoFileTester
{
  [GeneratedRegex(@"^(\.mp4|\.m4v|\.mkv|\.avi|\.3gp|\.wmv|\.mov|\.mpg)$", RegexOptions.IgnoreCase, "en-US")]
  private static partial Regex VideoRegex();
  public static bool IsVideoFile(string f) => VideoRegex().IsMatch(f);
}