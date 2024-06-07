namespace VideoDataAccess.Models;

// For google videos, this attribute tells us that the REST API
// does not return a particular property.
[AttributeUsage(AttributeTargets.Property)]
internal class NotFromGoogleRestAPIAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
internal class FromGoogleRestApiAttribute : Attribute
{
}
