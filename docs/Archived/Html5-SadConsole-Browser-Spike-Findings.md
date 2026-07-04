# HTML5 SadConsole Browser Spike Findings

Status: Archived findings document for the completed `html5-spike` branch. The spike proved that the existing SadConsole frontend can run as an itch.io-hosted browser build, but also exposed packaging, content-loading, dependency, and performance risks that must be addressed before treating this route as production-ready.

Read when:

- deciding whether SadConsole-in-browser should become a supported shareable build target;
- planning itch.io, HTML5, Blazor WebAssembly, KNI, or SadConsole distribution work;
- comparing this route against a browser-native frontend or another engine;
- hardening content loading, publish packaging, or frontend performance diagnostics.

Related source of truth:

- `docs/Plans/SadConsole-Frontend-Roadmap.md` records the active SadConsole/frontend direction on `main`.
- `docs/Archived/SadConsole-Spike-Findings.md` records the earlier SadConsole prototype architecture findings.
- `docs/Archived/SadConsole-Prototype-Assessment.md` records the earlier SadConsole UX and frontend-engine assessment.
- `html5-spike` contains the browser build implementation and spike-specific build script for reference.

## Executive summary

The spike succeeded: the existing `SadConsoleShell` ran in-browser on itch.io through Blazor WebAssembly and KNI, reused the existing SadConsole UI path, loaded embedded Beta content, displayed the scenario catalog, accepted keyboard input, launched scenarios, and advanced the shared Core simulation.

The result is playable, but not production-ready. The success depended on source-building SadConsole's MonoGame host against KNI instead of using the published DesktopGL host, publishing a Debug Blazor WASM artifact, stripping compressed framework assets for itch.io, preserving a very specific zip shape, and changing content loading away from filesystem assumptions.

The largest unresolved risk is performance. Busy scenarios such as `beta-pocket-bazaar` show severe input/turn delay in the browser build. This does not prove the stack is untenable, because the working artifact was still a Debug browser build without a completed Release/optimized WASM path. It does mean the next decision should be based on profiling and optimized-build evidence, not only on the fact that the game can load.

## Outcome

Validated by the spike:

- The existing SadConsole frontend can render in browser through Blazor WebAssembly and KNI/WebGL.
- The existing keyboard/menu flow can be reused rather than replaced by a parallel web UI.
- Existing Core simulation semantics remain shared; browser play still advances the same engine path.
- The browser build can be packaged as an itch.io HTML game and loaded from itch.io's iframe/CDN environment.
- Beta content can be bundled into the browser build and loaded without filesystem access.
- The final itch.io upload showed the Beta scenario catalog and allowed entering scenarios.

Not validated:

- A Release or optimized publish path that works reliably.
- Acceptable performance on busier scenarios.
- A durable dependency setup that does not depend on a local patched SadConsole checkout.
- A hardened automated build/test/upload flow.
- A browser smoke test that catches packaging, loading, input, and content regressions.

## Implementation shape

The browser spike used these major pieces on `html5-spike`:

- a Blazor WebAssembly browser project for the SadConsole frontend;
- `net8.0` browser targeting for KNI compatibility;
- multi-targeting for Core/Content where needed;
- a source-built SadConsole host patched to use KNI Blazor/GL packages;
- embedded Beta YAML content resources in `GameGameGame.Content`;
- a browser catalog path that discovers embedded resources instead of scanning the filesystem;
- a browser launch path that loads scenarios from embedded resources;
- an itch.io packaging script that publishes, strips compressed assets, and zips the correct file tree.

This was a successful spike shape, not a final architecture. The local patched SadConsole source checkout is the clearest sign that the dependency story still needs hardening.

## Findings

### itch.io packaging was stricter than expected

The first itch.io upload did not load because the artifact shape and compressed framework assets were wrong for itch.io's hosting behavior.

Specific issues:

- Blazor publish generated `.br` and `.gz` framework assets.
- itch.io's CDN did not serve those files with the required `Content-Encoding` headers.
- The Blazor loader then failed while fetching framework assets.
- One zip attempt flattened the publish directory structure, so `_framework`, `_content`, and other paths were not where the browser expected them.
- The final script had to preserve relative paths explicitly and strip compressed assets after publish.

Finding: treat itch.io packaging as a first-class compatibility target, not a generic static-file upload.

### Content loading was more complicated than expected

Desktop content discovery assumes filesystem access. Browser builds do not have the same filesystem model, so the Beta content path had to be changed.

What worked:

- embed Beta YAML files as assembly resources;
- discover embedded resource names at runtime;
- parse each YAML resource as an `EditableContentDocument`;
- extract scenarios from each document's `Scenarios` dictionary;
- launch selected scenarios by loading the embedded resource instead of a file path.

What was brittle:

- manifest-based loading was initially expected to be enough;
- DTO deserialization/reflection under Blazor WASM was more fragile than on desktop;
- fallback behavior hid failures by dropping back to Alpha content instead of clearly surfacing the browser content error.

Finding: browser content should be treated as a bundled-content target with explicit diagnostics, not as a transparent copy of desktop filesystem discovery.

### SadConsole reuse was real

The spike did preserve the most important frontend constraint: no parallel browser UI was created.

The existing `SadConsoleShell` and scenario menu behavior carried over far enough to prove that the frontend work is reusable. Keyboard input reached the shell, menu selection worked, scenarios launched, and simulation turns advanced through the existing Core path.

Finding: if the project wants a browser demo while keeping the SadConsole frontend as the canonical debug/player surface, this route remains credible.

### Dependency wiring is not production-ready

The published SadConsole/MonoGame DesktopGL package path was not browser-safe. The working route required patching SadConsole's MonoGame host project to build against KNI Blazor/GL packages.

Risks:

- local source checkout dependency;
- manual patching steps;
- unclear upgrade path when SadConsole, KNI, or .NET versions move;
- `net10.0` browser attempts hit JavaScript/runtime incompatibilities during the spike;
- Release publish regressed with keyboard type-initialization failures.

Finding: this cannot become a routine build target until the host dependency is vendored, forked, packaged, or upstreamed in a repeatable way.

### Performance is the main unresolved product risk

The final itch.io version can load and play scenarios, but busier showcases such as `beta-pocket-bazaar` have a severe input/turn delay.

Likely contributing areas to investigate:

- Debug Blazor WASM execution overhead;
- missing optimized `wasm-tools` publish path;
- interpreter or trimming settings;
- expensive per-turn simulation work that was acceptable on desktop but not in browser;
- rendering or layout cost inside the SadConsole/KNI path;
- allocation pressure and garbage collection during dense turns;
- input buffering where turns take long enough that controls feel delayed.

Finding: performance, not basic feasibility, should decide whether this stack graduates beyond spike status.

## What to do differently next time

If repeating this spike, change the order of proof:

1. Prove itch.io packaging first with the smallest possible Blazor/KNI/SadConsole page.
2. Prove browser content bundling early instead of assuming desktop filesystem discovery can be adapted late.
3. Include one busy scenario in the first playable benchmark, not only simple movement/input checks.
4. Add visible browser diagnostics before debugging fallbacks: content source, catalog count, launch diagnostics, frame time, turn time, and input queue delay.
5. Treat Release/optimized publish as part of the feasibility question, not as cleanup after the spike.

## Viability assessment

This stack is conditionally viable.

It is viable for:

- a shareable browser demo that reuses the SadConsole frontend;
- itch.io feedback builds where some rough edges are acceptable;
- proving frontend work without creating a separate web UI;
- debug-browser distribution if performance is improved.

It is risky for:

- a polished primary HTML5 platform;
- large or busy scenarios without optimization evidence;
- frequent public builds without dependency/build hardening;
- long-term support if SadConsole/KNI/browser compatibility remains manually patched.

Recommendation: do one focused hardening/performance pass before deciding to pivot. If optimized browser builds still show severe delay in busy scenarios, keep Core/Content shared but consider a browser-native renderer or another frontend route for HTML5.

## Recommended next work

1. Add in-browser timing diagnostics for frame duration, turn duration, entity count, scenario ID, and input latency.
2. Fix or work around Release browser publish failures.
3. Install/use `wasm-tools` and compare Debug, Release, and optimized WASM performance on the same scenarios.
4. Profile `beta-pocket-bazaar` to separate simulation cost from rendering/layout/input cost.
5. Replace the local patched SadConsole source dependency with a repeatable fork, package, vendored host, or upstream contribution.
6. Keep the itch.io packaging script strict about root zip shape and compressed asset removal.
7. Add a Playwright or equivalent browser smoke test that loads the published artifact, verifies the catalog count, enters a scenario, sends one movement key, and fails on browser console errors.
8. Improve browser-visible launch diagnostics so content-loading failures are not hidden behind Alpha fallback behavior.

## Decision checkpoint

Continue the SadConsole browser route only if the next hardening pass proves that busy scenarios can become acceptably responsive in an optimized browser build.

If performance remains poor after optimized publish and profiling, the better route is probably a browser-native presentation layer over the existing shared Core/Content model. That would cost more frontend work, but it would reduce dependence on patched SadConsole/KNI hosting and give more direct control over browser performance.
