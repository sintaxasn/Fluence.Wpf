# Documentation rebuild design

## Purpose

Create a complete Markdown documentation set for developers using the WPF library, people using the PowerShell module, and contributors. The pages must read well on GitHub and be ready for a later static site generator without committing to one now.

## Structure

The root `README.md` introduces the project and directs readers to `docs/index.md`. The docs index separates the C# and PowerShell journeys. Each journey offers a tutorial, task focused how-to guides, reference material, and explanations. `docs/theming.md` remains the canonical resource-key guide because the developer handbook points to it. Stable paths such as `docs/getting-started.md` and `docs/controls.md` remain useful entry points to preserve incoming links.

GitHub community files remain at their required root paths. The changelog keeps historical version headings and release facts. The PowerShell command reference is generated from comment-based help in the exported functions; the guide pages link to those generated pages.

## Sources of truth

Verify C# examples against the public API baseline, control source, theme templates, and gallery examples. Verify PowerShell examples against exported functions, their comment-based help, the module manifest, and runnable examples. Check package and framework claims against project files. Treat existing Markdown as background context and rewrite prose for the new structure.

## Compatibility and links

Use standard Markdown, fenced code blocks, relative links, and local images. Avoid generator-specific front matter, macros, or navigation configuration. Preserve the root README path used by the NuGet package, the changelog path used by release tooling, and paths referenced by GitHub issue templates. Keep agent instructions, licenses, screenshots, and public API baselines intact.

## Verification

Check Markdown links, encoding and line endings, code examples against source, generated PowerShell reference drift, and the repository text-policy hook. Review the authored content in a separate pass. Build the library after XML comment corrections and run any documentation-specific gates required by the repository.
