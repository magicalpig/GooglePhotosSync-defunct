namespace VideoToolsCore.DataAccess.Models;

public abstract class OwnableEntityBase
{
    public string? Owner { get; internal set; }
}