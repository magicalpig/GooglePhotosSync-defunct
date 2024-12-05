namespace VideoToolsCore.Utilities;

public abstract class BaseSettings
{
    public T GetRequiredSetting<T>(string name)
    {
        var property = GetType().GetProperty(name);
        if (property == null || property.GetValue(this) is not T value)
        {
            throw new InvalidOperationException($"Property {name} must be set in settings and be of type {typeof(T).Name}");
        }
        return value;
    }
}