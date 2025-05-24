using RazorEngineCore;
using System.Text.Json;
using System.Dynamic;
using System.Text;

public class RenderTextService
{
    public string Render(string textTemplate, JsonElement jData)
    {
        try
        {
            var engine = new RazorEngine();


            var template = engine.Compile(textTemplate, builder =>
                {
                    builder.AddUsing("System");
                });

            var model = ConvertJsonElement(jData);

            return template.Run(model);
        }
        catch (RazorEngineCompilationException rex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Errors in Razor template:");
            sb.AppendLine(rex.Message);

            if (rex.Errors != null && rex.Errors.Any())
            {
                sb.AppendLine("Details:");
                foreach (var err in rex.Errors)
                {
                    sb.AppendLine($" - {err}");
                }
            }

            return sb.ToString();
        }
        catch (JsonException jex)
        {
            return $"Error in JSON:\n{jex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error :\n{ex.Message}";
        }
    }

    private object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var expando = new ExpandoObject() as IDictionary<string, object?>;
                foreach (var prop in element.EnumerateObject())
                {
                    expando[prop.Name] = ConvertJsonElement(prop.Value);
                }
                return expando;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElement(item));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out long l))
                    return l;
                if (element.TryGetDouble(out double d))
                    return d;
                return null;

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            default:
                return null;
        }
    }
}