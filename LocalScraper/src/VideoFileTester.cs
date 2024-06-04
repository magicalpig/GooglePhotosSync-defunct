using System.Text.RegularExpressions;
namespace LocalScraper;
public static partial class VideoFileTester
{
  [GeneratedRegex(@"^(\.mp4|\.m4v|\.mkv|\.avi|\.3gp|\.wmv|\.mov)$", RegexOptions.IgnoreCase, "en-US")]
  private static partial Regex VideoRegex();
  public static bool IsVideoFile(string f) => VideoRegex().IsMatch(f);
}