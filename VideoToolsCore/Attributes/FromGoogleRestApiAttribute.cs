namespace VideoToolsCore.Attributes;

// For google videos, this attribute tells us that the REST API does return a particular property
[AttributeUsage(AttributeTargets.Property)]
internal class FromGoogleRestApiAttribute : Attribute
{
}
