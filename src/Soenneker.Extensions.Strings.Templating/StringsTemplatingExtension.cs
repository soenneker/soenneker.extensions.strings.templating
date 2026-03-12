using Scriban;
using Scriban.Runtime;
using Soenneker.Dictionaries.Concurrent.AutoClearing;
using Soenneker.Extensions.String;
using Soenneker.Utils.PooledStringBuilders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Soenneker.Extensions.Strings.Templating;

/// <summary>
/// A .NET extension library for string replacement and template rendering
/// </summary>
public static class StringsTemplatingExtension
{
    private static readonly AutoClearingConcurrentDictionary<string, Template> _templateCache =
        new(TimeSpan.FromSeconds(30), comparer: StringComparer.Ordinal);

    /// <summary>
    /// Renders the specified template string using the provided replacement values and optional partial templates.
    /// </summary>
    /// <remarks>This method uses a cache to improve performance when rendering the same template multiple
    /// times. Placeholders in the template are replaced with values from the replacements dictionary. If partials are
    /// provided, they can be referenced within the template by name.</remarks>
    /// <param name="templateText">The template string to render. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="replacements">A dictionary containing key-value pairs to replace placeholders in the template. Keys represent placeholder
    /// names; values are the corresponding replacement values. Can be null if no replacements are needed.</param>
    /// <param name="partials">An optional dictionary of partial template names and their corresponding template strings. Used to resolve
    /// partials referenced within the main template. Can be null if no partials are required.</param>
    /// <returns>A task that represents the asynchronous rendering operation. The task result contains the rendered template as a
    /// string.</returns>
    /// <exception cref="ArgumentException">Thrown if templateText is null, empty, or consists only of white-space characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the template string contains parse errors.</exception>
    public static ValueTask<string> Render(
        this string templateText,
        Dictionary<string, object?>? replacements,
        Dictionary<string, string>? partials = null)
    {
        if (templateText.IsNullOrWhiteSpace())
            throw new ArgumentException("Template string is required", nameof(templateText));

        // Parse or get from cache
        Template parsedTemplate = _templateCache.GetOrAdd(templateText, static text =>
        {
            Template t = Template.Parse(text);

            if (!t.HasErrors)
                return t;

            // Build error message without intermediate List<string>
            var messages = t.Messages;
            if (messages is null || messages.Count == 0)
                throw new InvalidOperationException("Template parse errors");

            // Capacity guess: prefix + ~64 chars/message
            using var sb = new PooledStringBuilder(24 + (messages.Count * 64));
            sb.Append("Template parse errors: ");

            for (int i = 0; i < messages.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");

                sb.Append(messages[i].ToString());
            }

            throw new InvalidOperationException(sb.ToString());
        });

        int replacementCount = replacements?.Count ?? 0;
        int partialsCount = partials?.Count ?? 0;

        var scriptObject = new ScriptObject(replacementCount + partialsCount);

        if (replacementCount > 0)
        {
            foreach (KeyValuePair<string, object?> kvp in replacements!)
            {
                // Dictionary keys are non-null; cheap check
                if (kvp.Key.Length != 0)
                    scriptObject.SetValue(kvp.Key, kvp.Value, true);
            }
        }

        if (partialsCount > 0)
        {
            foreach (KeyValuePair<string, string> kvp in partials!)
            {
                if (kvp.Key.Length != 0)
                    scriptObject.SetValue(kvp.Key, kvp.Value, true);
            }
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return parsedTemplate.RenderAsync(context);
    }
}