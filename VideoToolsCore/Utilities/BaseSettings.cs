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

    protected static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        // make all forward slashes into backslashes
        path = path.Replace('/', Path.DirectorySeparatorChar);
        // remove trailing slash if present. DB query will add its own
        path = path.TrimEnd(Path.DirectorySeparatorChar);
        return path;
    }
}