# Dryv.HotChocolate 9.4.1 — Release Notes

## Highlights

This is a maintenance release that broadens target framework support and upgrades the HotChocolate dependency to the latest version. No public API changes.

---

## Changes

- **Multi-target framework support** — expanded from `net9.0` only to `net8.0;net9.0;net10.0`.
- **HotChocolate upgraded** from `16.1.3` → `16.6.9`.
- **`LangVersion`** set to `latest`.
- **NuGet packaging** — `GeneratePackageOnBuild` and `IsPackable` properties now explicitly configured.
- Redundant `Dryv` namespace references removed; unused dependencies cleaned up.

## Breaking Changes

None. This release is backwards-compatible.
