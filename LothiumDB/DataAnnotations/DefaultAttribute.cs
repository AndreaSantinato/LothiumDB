namespace LothiumDB.DataAnnotations;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class DefaultAttribute : Attribute
{
    public object DefaultValue { get; init; }

    public DefaultAttribute(object defaultValue)
    {
        this.DefaultValue = defaultValue;
    }
}