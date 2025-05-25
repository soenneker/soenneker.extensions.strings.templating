using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Scriban;
using Scriban.Runtime;
using Soenneker.Extensions.String;

namespace Soenneker.Extensions.Strings.Templating;

/// <summary>
/// A .NET extension library for string replacement and template rendering
/// </summary>
public static class StringsTemplatingExtension
{
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
    public static ValueTask<string> Render(this string templateText, Dictionary<string, object>? replacements, Dictionary<string, string>? partials = null)
    {
        if (templateText.IsNullOrWhiteSpace())
            throw new ArgumentException("Template string is required", nameof(templateText));

        Template? parsedTemplate = Template.Parse(templateText);

        if (parsedTemplate.HasErrors)
            throw new InvalidOperationException($"Template parse errors: {string.Join(", ", parsedTemplate.Messages)}");

        int replacementCount = replacements?.Count ?? 0;
        int partialsCount = partials?.Count ?? 0;
        var scriptObject = new ScriptObject(replacementCount + partialsCount);

        if (replacements != null)
        {
            foreach (KeyValuePair<string, object> kvp in replacements)
            {
                scriptObject.SetValue(kvp.Key, kvp.Value, true);
            }
        }

        if (partialsCount > 0)
        {
            foreach ((string key, string partialValue) in partials!)
            {
                // Avoid closure allocation by caching value
                scriptObject.SetValue(key, () => partialValue, true);
            }
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return parsedTemplate.RenderAsync(context);
    }
}