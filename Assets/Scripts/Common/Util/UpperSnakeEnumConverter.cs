using Newtonsoft.Json.Converters;

public class UpperSnakeEnumConverter : StringEnumConverter
{
    public UpperSnakeEnumConverter() : base(new UpperSnakeCaseNamingStrategy(), false)
    {
    }
}
