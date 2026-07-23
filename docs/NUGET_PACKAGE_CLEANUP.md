# NuGet package cleanup

Andy.Tui now publishes one package ID: `Andy.Tui`. Earlier releases published
each implementation assembly independently. Published NuGet versions are
immutable, so their metadata and icons cannot be edited in place.

## Safe cleanup order

1. Publish and verify a new bundled `Andy.Tui` version.
2. On nuget.org, deprecate every version of the component package IDs below.
   Select `Andy.Tui` as the alternate package and explain that the component is
   now bundled.
3. Unlist every component-package version after verifying the replacement.
   Unlisting hides versions from search while retaining exact-version restore for
   existing consumers.

Component package IDs:

- `Andy.Tui.Animations`
- `Andy.Tui.Backend.Terminal`
- `Andy.Tui.CliWidgets`
- `Andy.Tui.Compose`
- `Andy.Tui.Compositor`
- `Andy.Tui.Core`
- `Andy.Tui.DisplayList`
- `Andy.Tui.Input`
- `Andy.Tui.Layout`
- `Andy.Tui.Observability`
- `Andy.Tui.Style`
- `Andy.Tui.Text`
- `Andy.Tui.Virtualization`
- `Andy.Tui.Widgets`

Nuget.org's **Delete** operation is an unlist, not a permanent deletion. This
repository's guarded **Retire legacy Andy.Tui component packages** Actions
workflow inventories and unlists only the IDs above. It runs only from `main`
and only when its confirmation input is exactly
`RETIRE_ANDY_TUI_COMPONENTS`.

The workflow uses a separate `NUGET_RETIRE_API_KEY` Actions secret. Create a
short-lived nuget.org API key with the **Unlist package versions** operation and
the `Andy.Tui.*` package glob, then save it under that secret name. The workflow
waits 15 seconds between requests to stay below nuget.org's 250 unlist requests
per API key per hour and verifies that no component versions remain listed. The
final assertion retries briefly because NuGet's public registration catalog can
lag successful unlist requests.

For a read-only local inventory, run:

```bash
scripts/retire-nuget-component-packages.sh
```

Individual versions can also be unlisted in the package-management UI or with:

```bash
dotnet nuget delete <PACKAGE_ID> <VERSION> \
  --source https://api.nuget.org/v3/index.json \
  --api-key <NUGET_API_KEY> \
  --non-interactive
```

Do not unlist `Andy.Tui`. The old component IDs will remain visible to the owner
under **Unlisted Packages**; nuget.org generally does not permanently delete
published packages.

References:

- [Deprecating packages](https://learn.microsoft.com/nuget/nuget-org/deprecate-packages)
- [`dotnet nuget delete`](https://learn.microsoft.com/dotnet/core/tools/dotnet-nuget-delete)
- [NuGet.org package deletion FAQ](https://learn.microsoft.com/nuget/nuget-org/nuget-org-faq#can-i-delete-a-package-published-to-nugetorg)
