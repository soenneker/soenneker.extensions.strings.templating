using Scriban;
using Scriban.Runtime;
using Soenneker.Extensions.String;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Soenneker.Dictionaries.Concurrent.AutoClearing;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Extensions.Strings.Templating;

/// <summary>
/// A .NET extension library for string replacement and template rendering
/// </summary>
public static class StringsTemplatingExtension
{
    private static readonly AutoClearingConcurrentDictionary<string, Template> _templateCache = new(TimeSpan.FromSeconds(30), comparer: StringComparer.Ordinal);

    /// <summary>
    /// Renders a Scriban template string with the specified replacements and optional partials.
    /// </summary>
    /// <param name="templateText">The template text to be rendered. Must be a valid Scriban template string.</param>
    /// <param name="replacements">
    /// A dictionary of key-value pairs where each key corresponds to a variable in the template,
    /// and the value is the object to inject into the template.
    /// </param>
    /// <param name="partials">
    /// An optional dictionary of named partial templates. Each entry represents a named template fragment
    /// that can be invoked from within the main template using Scriban's function syntax.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{String}"/> representing the asynchronous rendering operation,
    /// containing the final rendered template string.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="templateText"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the template cannot be parsed due to syntax errors.</exception>
    public static async ValueTask<string> Render(this string templateText, Dictionary<string, object?>? replacements,
        Dictionary<string, string>? partials = null)
    {
        if (templateText.IsNullOrWhiteSpace())
            throw new ArgumentException("Template string is required", nameof(templateText));

        // Parse or get from cache
        Template parsedTemplate = _templateCache.GetOrAdd(templateText, static text =>
        {
            Template? t = Template.Parse(text);

            if (t.HasErrors)
                throw new InvalidOperationException($"Template parse errors: {string.Join(", ", t.Messages)}");

            return t;
        });

        int replacementCount = replacements?.Count ?? 0;
        int partialsCount = partials?.Count ?? 0;

        var scriptObject = new ScriptObject(replacementCount + partialsCount);

        if (replacements is not null && replacementCount > 0)
        {
            foreach (KeyValuePair<string, object?> kvp in replacements)
            {
                // If you prefer strictness, throw on empty keys
                if (kvp.Key.HasContent())
                    scriptObject.SetValue(kvp.Key, kvp.Value, true);
            }
        }

        if (partials is not null && partialsCount > 0)
        {
            foreach (KeyValuePair<string, string> kvp in partials)
            {
                if (kvp.Key.HasContent())
                {
                    // Cheaper than a lambda per entry if these are static strings
                    scriptObject.SetValue(kvp.Key, kvp.Value, true);
                }
            }
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        // ConfigureAwait(false) for library code
        return await parsedTemplate.RenderAsync(context).NoSync();
    }
}