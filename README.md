[![](https://img.shields.io/nuget/v/soenneker.extensions.strings.templating.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.strings.templating/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.strings.templating/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.strings.templating/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.extensions.strings.templating.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.extensions.strings.templating/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.strings.templating/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.strings.templating/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.Strings.Templating
Render Scriban templates from a string with dictionary-backed global values and a short-lived parse cache.

## Installation

```bash
dotnet add package Soenneker.Extensions.Strings.Templating
```

## Render values

```csharp
using Soenneker.Extensions.Strings.Templating;

var values = new Dictionary<string, object?>
{
    ["customer_name"] = "Ada",
    ["items"] = new[] { "Keyboard", "Mouse" }
};

string template = "Hello {{ customer_name }}. {{ items | array.size }} items are ready.";
string result = await template.Render(values);
```

Dictionary keys become top-level Scriban variables. Values may be scalar values, collections, or objects that Scriban can expose through its normal member-access rules. Null or empty dictionaries are supported.

The optional `partials` dictionary also adds string-valued globals:

```csharp
var snippets = new Dictionary<string, string>
{
    ["signature"] = "Thanks,\nSupport"
};

string result = await "{{ message }}\n\n{{ signature }}".Render(
    new Dictionary<string, object?> { ["message"] = "Your export is ready." },
    snippets);
```

These values are not registered with a Scriban `ITemplateLoader`; they are strings, not independently parsed include files. Keep keys unique across both dictionaries because globals are read-only once added.

## Parsing and caching

Parsed templates are cached by exact template text for 30 seconds. Invalid syntax throws `InvalidOperationException` with Scriban's parse messages. Rendering errors propagate from Scriban. The method returns a `ValueTask<string>` because Scriban rendering can be asynchronous.

## Security

Treat the template as executable input. Scriban expressions can traverse the objects supplied in `replacements`; do not render templates from untrusted users against privileged application objects. Supply narrow DTOs or scalar values, and encode or sanitize the rendered result for its destination. Rendering does not automatically provide HTML, SQL, shell, or URL escaping.
