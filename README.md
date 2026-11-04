# Hazelchat for Umbraco

The NuGet package [Hazelchat.Umbraco](https://www.nuget.org/packages/Hazelchat.Umbraco): adds your Hazelchat AI chat agent to every page Umbraco 13 to 18 renders, from one setting. Owner and developer documentation: [Hazelchat.Umbraco/README.md](Hazelchat.Umbraco/README.md) and the guide at [hazelchat.co/integrations/umbraco](https://hazelchat.co/integrations/umbraco/).

## Develop

- `Hazelchat.Umbraco/`: the package (.NET 8 for Umbraco 13–16, .NET 10 for Umbraco 17–18). `Key.cs` and `Page.cs` hold the rules; the middleware and health check use them.
- `tests/Hazelchat.Umbraco.Tests/`: unit tests for the key, the tag and which responses may change (`dotnet test tests/Hazelchat.Umbraco.Tests`).
- Releases: raise `<Version>` in the csproj, add the CHANGELOG entry, push a tag `vX.Y.Z`. The workflow tests, packs and publishes to nuget.org; the Umbraco Marketplace picks it up within a day.

Security: see [SECURITY.md](Hazelchat.Umbraco/SECURITY.md). MIT licence.
