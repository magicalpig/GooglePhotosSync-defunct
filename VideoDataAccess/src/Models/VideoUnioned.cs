namespace VideoDataAccess.Models;

using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
public class VideoUnioned
{  
  public VideoFileWindows? WindowsVideo { get; set; }
  public VideoFileGoogle? GoogleVideo { get; set; }
}
