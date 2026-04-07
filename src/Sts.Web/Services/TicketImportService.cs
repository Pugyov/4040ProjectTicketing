using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Sts.Web.Data;
using Sts.Web.Models;

namespace Sts.Web.Services;

public class TicketImportService : ITicketImportService
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public TicketImportService(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<TicketImportResult> ImportAsync(TicketImportRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FileStream == Stream.Null)
        {
            return TicketImportResult.Failed("A JSON file is required.");
        }

        JsonNode schemaNode;
        try
        {
            schemaNode = await LoadSchemaAsync(cancellationToken);
        }
        catch (Exception)
        {
            return TicketImportResult.Failed("The ticket import schema could not be loaded.");
        }

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(request.FileStream, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return TicketImportResult.Failed("The uploaded file is not valid JSON.");
        }

        using (document)
        {
            var validationErrors = SimpleJsonSchemaValidator.Validate(document.RootElement, schemaNode, "$");
            if (validationErrors.Count > 0)
            {
                return TicketImportResult.Failed([
                    "The uploaded file does not match the required JSON schema.",
                    .. validationErrors
                ]);
            }

            var tickets = document.RootElement
                .GetProperty("tickets")
                .EnumerateArray()
                .Select(item => new Ticket
                {
                    Subject = item.GetProperty("subject").GetString() ?? string.Empty,
                    Description = item.TryGetProperty("description", out var descriptionElement)
                        ? descriptionElement.GetString()
                        : null,
                    Status = Enum.Parse<TicketStatus>(item.GetProperty("status").GetString()!, ignoreCase: false),
                    Team = request.Team,
                    CreatedByUserId = request.CreatedByUserId,
                    CreatedAtUtc = DateTime.UtcNow
                })
                .ToArray();

            _dbContext.Tickets.AddRange(tickets);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return TicketImportResult.Failed("The imported tickets could not be saved. Please try again.");
            }

            return TicketImportResult.Success(tickets.Length);
        }
    }

    private async Task<JsonNode> LoadSchemaAsync(CancellationToken cancellationToken)
    {
        var schemaPath = Path.Combine(_environment.ContentRootPath, "Schemas", "ticket-import-schema.json");
        var schemaContent = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        return JsonNode.Parse(schemaContent)
               ?? throw new InvalidOperationException("Schema content could not be parsed.");
    }
}

internal static class SimpleJsonSchemaValidator
{
    public static IReadOnlyList<string> Validate(JsonElement element, JsonNode schemaNode, string path)
    {
        var errors = new List<string>();
        if (schemaNode is not JsonObject schema)
        {
            errors.Add("The ticket import schema is invalid.");
            return errors;
        }

        var type = schema["type"]?.GetValue<string>();
        switch (type)
        {
            case "object":
                ValidateObject(element, schema, path, errors);
                break;
            case "array":
                ValidateArray(element, schema, path, errors);
                break;
            case "string":
                ValidateString(element, schema, path, errors);
                break;
            default:
                errors.Add($"{path} uses an unsupported schema type.");
                break;
        }

        return errors;
    }

    private static void ValidateObject(JsonElement element, JsonObject schema, string path, List<string> errors)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{path} must be an object.");
            return;
        }

        var properties = schema["properties"]?.AsObject() ?? [];
        var required = schema["required"]?.AsArray().Select(item => item?.GetValue<string>()).Where(item => item is not null).Cast<string>().ToHashSet() ?? [];
        var additionalPropertiesAllowed = schema["additionalProperties"]?.GetValue<bool>() ?? true;

        foreach (var requiredProperty in required)
        {
            if (!element.TryGetProperty(requiredProperty, out _))
            {
                errors.Add($"{path}.{requiredProperty} is required.");
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!properties.TryGetPropertyValue(property.Name, out var childSchemaNode))
            {
                if (!additionalPropertiesAllowed)
                {
                    errors.Add($"{path}.{property.Name} is not allowed.");
                }

                continue;
            }

            if (childSchemaNode is null)
            {
                continue;
            }

            errors.AddRange(Validate(property.Value, childSchemaNode, $"{path}.{property.Name}"));
        }
    }

    private static void ValidateArray(JsonElement element, JsonObject schema, string path, List<string> errors)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"{path} must be an array.");
            return;
        }

        var itemSchema = schema["items"];
        if (itemSchema is null)
        {
            return;
        }

        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            errors.AddRange(Validate(item, itemSchema, $"{path}[{index}]"));
            index++;
        }
    }

    private static void ValidateString(JsonElement element, JsonObject schema, string path, List<string> errors)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{path} must be a string.");
            return;
        }

        var value = element.GetString() ?? string.Empty;
        if (schema["minLength"] is JsonValue minLengthNode && value.Length < minLengthNode.GetValue<int>())
        {
            errors.Add($"{path} must be at least {minLengthNode.GetValue<int>()} characters long.");
        }

        if (schema["maxLength"] is JsonValue maxLengthNode && value.Length > maxLengthNode.GetValue<int>())
        {
            errors.Add($"{path} must be at most {maxLengthNode.GetValue<int>()} characters long.");
        }

        if (schema["enum"] is JsonArray enumValues)
        {
            var allowed = enumValues
                .Select(item => item?.GetValue<string>())
                .Where(item => item is not null)
                .Cast<string>()
                .ToArray();

            if (!allowed.Contains(value, StringComparer.Ordinal))
            {
                errors.Add($"{path} must be one of: {string.Join(", ", allowed)}.");
            }
        }
    }
}
