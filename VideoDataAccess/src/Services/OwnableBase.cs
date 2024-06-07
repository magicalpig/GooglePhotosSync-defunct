namespace VideoDataAccess.Models;

public abstract class OwnableBase
{
    public string? Owner { get; internal set; }
}