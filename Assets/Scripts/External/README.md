# External package stubs

The files in this folder provide *minimal* compile-time stubs for third-party
packages that the project depends on but that are not (yet) installed in this
clone.

| Stub file | Real package |
|-----------|--------------|
| `SupabaseStubs.cs` | `supabase-csharp` (NuGet, via NuGetForUnity) |
| `PostgrestStubs.cs` | `postgrest-csharp` (NuGet, transitive of supabase) |
| `DOTweenStubs.cs` | DOTween (Unity Asset Store or `com.demigiant.dotween` mirror) |
| `GoogleMobileAdsStubs.cs` | Google Mobile Ads Unity Plugin (.unitypackage) |

When you install the real package, **delete the corresponding stub file** so the
real types take precedence. Each stub is gated on a `*_REAL` scripting define
symbol — adding the symbol in *Project Settings → Player → Scripting Define
Symbols* also disables the stub.

The stubs implement just enough surface area to satisfy the call sites already
present in `Assets/Scripts/`. They are no-ops at runtime; do not ship them to
production.
