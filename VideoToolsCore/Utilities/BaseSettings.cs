namespace VideoToolsCore.Utilities;

public abstract class BaseSettings
{
    public string GetRequiredSetting(string name)
    {
        var property = GetType().GetProperty(name);
        if (property == null || property.GetValue(this) is not string value)
        {
            throw new InvalidOperationException($"Property {name} must be set in settings");
        }
        return value;
    }
}