---
id: investigation.plane-integration
title: Plane Integration Investigation
kind: investigation
status: draft
owners: [core-owner, content-editor, frontend-owner]
audience: [core-owner, content-editor, frontend-owner]
lane: planning
related:
  - source.planning-index
  - source.current-goals
  - plan.core-rolling-board
  - plan.content-rolling-board
  - plan.frontend-sadconsole-rolling-board
---

# Plane Integration Investigation

Status: High-level investigation of where Plane could support or mirror existing GameGameGame planning and documentation workflows.

## Assumptions

- Plane is installed and available for local experimentation.
- The repository remains the source of truth for code, content, stable behavior contracts, architecture, and source-controlled historical context unless a later decision explicitly changes that.
- Initial integrations should be low-risk, reversible, and one-way where possible.
- Plane should first prove value as an active coordination and triage layer before it is allowed to compete with repository documentation as a source of truth.

## Potential benefits for planning

### Live board visibility

Plane can make current planning state easier to inspect than hand-maintained Markdown rolling boards. The existing `docs/Plans/Core-Rolling-Board.md`, `docs/Plans/Content-Rolling-Board.md`, and `docs/Plans/Frontend-SadConsole-Rolling-Board.md` already map naturally to Plane projects, work items, states, cycles, modules, labels, and views.

Measurable outcomes:

- Time to answer "what is active now?" drops below 30 seconds without opening multiple Markdown files.
- Every active `Now` item has a visible Plane work item with owner, status, and link back to the repository planning file.
- At least 90% of selected sprint/cycle work can be represented in Plane without losing the user story, owner, dependency, and done-when criteria from the repository board.
- End-of-cycle review can list completed, carried, blocked, and deferred work without manually re-reading all three rolling boards.

### Faster triage for scenario and feedback observations

Plane can absorb high-churn observations from content experiments, scenario runs, feedback builds, and frontend playtesting before those observations deserve durable source-of-truth placement.

Measurable outcomes:

- New scenario observations can be captured as a Plane work item or comment in under two minutes.
- Each promoted capability gap has a link to the scenario, report, or feedback observation that justified promotion.
- Fewer uncategorized notes accumulate in source documents such as `Capability-Gap-Log.md` and `Design-Quirks-and-Gotchas.md`.
- A weekly or sprint wrap-up can identify the top recurring labels, such as `ecology`, `topology`, `authoring-friction`, or `frontend-polish`.

### Better coordination across ownership lanes

GameGameGame deliberately separates Core, Content, and Frontend ownership. Plane can expose cross-lane dependencies without flattening those boundaries.

Measurable outcomes:

- Cross-lane dependencies are visible as blocked/linked work items rather than prose scattered across plans.
- Work items touching multiple lanes retain a primary owner plus explicit collaborators.
- The number of planning updates needed to move one item through a sprint decreases compared with updating multiple Markdown sections.
- Retrospective notes identify fewer cases where selected work was blocked by an untracked dependency.

### Pull request and implementation linkage

Plane's GitHub integration can link work items to issues and pull requests, and can automate state movement from pull request lifecycle events.

Measurable outcomes:

- At least 80% of implementation PRs during a trial cycle reference a Plane work item.
- Plane state transitions caused by PR open/review/merge events match the intended workflow without manual correction.
- Completed cycle summaries can connect work items to merged commits or PRs.
- CI or review failures can be discussed on the associated work item rather than only in transient chat.

## Potential benefits for documentation

### Easier reading and discussion surface

Plane Pages can provide a more approachable reading and discussion surface for project summaries, sprint notes, investigation notes, and external-facing feedback documents.

Measurable outcomes:

- A collaborator can find the current sprint summary, current experiment notes, and feedback-build instructions from Plane without browsing the repository.
- Plane pages link back to the canonical repository files for source-of-truth content.
- Documentation comments or questions are captured next to the relevant page or linked work item.
- Stakeholder-facing summaries can be produced without exposing the full repository workflow structure.

### Work-linked documentation

Plane Pages can mention work items directly and convert selected text into work items. This is valuable for investigations and retrospectives where action items emerge from prose.

Measurable outcomes:

- Each investigation page that produces follow-up work contains linked Plane work items for those follow-ups.
- Retrospective action items are converted into tracked work instead of remaining as unchecked prose.
- Readers can navigate from a planning summary to the exact work items it created.
- Fewer follow-up tasks are manually copied between docs and boards.

### Versioned collaborative drafting outside the repository

Plane Pages include collaborative editing, page history, comments, and export options. This could reduce friction for draft summaries and non-code collaboration.

Measurable outcomes:

- Draft planning summaries can be collaboratively edited before any repository document changes are needed.
- External feedback or stakeholder review can happen through a Plane page or published page instead of requiring repository access.
- Finalized summaries can be exported or copied as Markdown and checked into the repository when they become durable.
- Plane page version history is sufficient to recover draft changes during the trial period.

### Documentation mirror and index

Plane could host a navigable mirror of selected repository documentation while keeping canonical content in Git. This would be most useful for high-level orientation and current planning, not for every source-of-truth detail.

Measurable outcomes:

- The mirrored index covers the current planning bridge, active rolling boards, feedback-build instructions, and current experiment summaries.
- Each mirrored page contains a repository-source link and a last-synced timestamp.
- Drift checks report whether Plane page content matches or intentionally summarizes the repository source.
- Users can reach the canonical repository document from the Plane mirror in one click.

## Candidates for initial experiments

### Experiment 1: Mirror active rolling boards into Plane projects

Existing repository workflow:

- `docs/Plans/Core-Rolling-Board.md`
- `docs/Plans/Content-Rolling-Board.md`
- `docs/Plans/Frontend-SadConsole-Rolling-Board.md`
- `docs/Source of Truth/Current-Goals.md`
- `docs/Source of Truth/planning-index.md`

Low-effort Plane transfer:

- Create one Plane workspace named `GameGameGame`.
- Create Plane projects for `Core`, `Content`, and `Frontend SadConsole`.
- Configure states equivalent to `Now`, `Next`, `Later`, and `Done`.
- Create one work item for each active rolling-board entry.
- Link each work item back to the relevant Markdown heading.
- Keep the repository board canonical during the trial.

Why this is low risk:

- It does not change code, content schema, tests, or engine behavior.
- The existing board structure already has clear user stories, owners, plans, and done-when criteria.
- Manual migration is feasible before automation is attempted.

Suggested measurement window:

- One sprint-selection and wrap-up cycle.

Relevant Plane documentation:

- Workspace overview: <https://docs.plane.so/core-concepts/workspaces/overview>
- Project management and project features: <https://docs.plane.so/core-concepts/projects/overview>
- API overview for later automation: <https://developers.plane.so/api-reference/introduction>

### Experiment 2: Use Plane as a triage queue for content-led ecology experiments

Existing repository workflow:

- `docs/Plans/Content-Rolling-Board.md`
- `docs/Source of Truth/Capability-Gap-Log.md`
- `docs/Source of Truth/Design-Quirks-and-Gotchas.md`
- `src/GameGameGame.Content/Beta/Ecology/EcologyVignettes.yaml`
- Other Beta scenarios under `src/GameGameGame.Content/Beta`

Low-effort Plane transfer:

- Create a `Content` project module or label set for ecology experiments.
- Create work items for scenario ideas, observed gaps, confusing authoring failures, and promoted follow-ups.
- Use labels such as `ecology`, `scenario`, `capability-gap`, `quirk`, `topology`, `authoring-friction`, and `promoted`.
- Link each item to the scenario YAML file, headless report, or source-of-truth log entry.

Why this is low risk:

- Content experiments already generate observations before they become implementation work.
- Plane can improve intake and triage without replacing the canonical gap log.
- Labels and filtered views can make recurring scenario pressures visible.

Suggested measurement window:

- One ecology experiment batch, including at least one scenario run/report review.

Relevant Plane documentation:

- Project features including modules, views, labels, and intake: <https://docs.plane.so/core-concepts/projects/overview>
- Webhooks for later event-driven automation: <https://developers.plane.so/dev-tools/intro-webhooks>
- API overview for posting run-report links or comments: <https://developers.plane.so/api-reference/introduction>

### Experiment 3: Host sprint summaries and investigation pages in Plane Pages

Existing repository workflow:

- `docs/Source of Truth/sprint-wrapup-process.md`
- `docs/Source of Truth/Current-Goals.md`
- Investigation documents such as this file
- Future short sprint summaries or wrap-up notes

Low-effort Plane transfer:

- Create Plane Pages for current sprint summary, ecology experiment notes, and Plane integration notes.
- Keep each page short and link to canonical repository docs.
- Use page mentions to connect summaries to active work items.
- Export or copy Markdown back into the repository only when a page becomes durable project history.

Why this is low risk:

- Pages can host summaries without claiming source-of-truth authority.
- Plane supports Markdown-style editing, work-item mentions, page version history, comments, and export.
- The repository still preserves durable architecture and planning records.

Suggested measurement window:

- One sprint wrap-up and next-sprint selection session.

Relevant Plane documentation:

- Pages overview: <https://docs.plane.so/core-concepts/pages/overview>
- Workspace overview for member access and settings: <https://docs.plane.so/core-concepts/workspaces/overview>

### Experiment 4: Link GitHub PRs to Plane work items

Existing repository workflow:

- GitHub Actions restore/build/test sequence documented in `README.md`.
- Pull requests and code changes tied to selected planning work.
- Current source-controlled plans and tests remain the implementation authority.

Low-effort Plane transfer:

- Configure Plane GitHub integration for the repository if available in the installed edition.
- Start with PR-to-work-item references rather than broad bidirectional issue sync.
- Map PR lifecycle events to Plane states only after the manual board experiment works.
- Require PR titles or descriptions to reference the Plane work item identifier during the trial.

Why this is low risk:

- PR linkage can improve traceability without changing runtime code.
- Plane supports PR state automation and backlink comments.
- Bidirectional issue sync can be deferred until overwrite and drift behavior is understood.

Suggested measurement window:

- One implementation cycle with at least two merged PRs.

Relevant Plane documentation:

- GitHub integration: <https://docs.plane.so/integrations/github>
- Native integrations overview: <https://docs.plane.so/integrations/about>

### Experiment 5: Post scenario reports or feedback-build notes to Plane work items

Existing repository workflow:

- `src/GameGameGame.Headless` scenario recording/reporting support
- `tools/package-feedback-build.ps1`
- Agent/content tooling that can run persisted scenarios and produce diagnostic reports
- Feedback scenario catalog under `src/GameGameGame.Content/Beta/FeedbackManifest.yaml`

Low-effort Plane transfer:

- Manually attach or paste selected scenario reports to related Plane work items.
- If useful, add a small script later that posts report links or summaries to Plane through the REST API.
- Use Plane work item comments for run summaries, validation failures, runtime observations, and feedback-build package notes.
- Avoid uploading secrets, local-only paths, or excessively large artifacts.

Why this is low risk:

- The first pass can be manual.
- The existing tooling already produces useful diagnostic artifacts.
- Plane comments can collect evidence before a gap is promoted into source-of-truth docs.

Suggested measurement window:

- One feedback build or one focused scenario-reporting pass.

Relevant Plane documentation:

- API overview, authentication, pagination, rate limits, fields, and expand: <https://developers.plane.so/api-reference/introduction>
- Webhooks for later reverse integration from Plane events into local tooling: <https://developers.plane.so/dev-tools/intro-webhooks>
- Build a Plane app for future OAuth/bot integrations: <https://developers.plane.so/dev-tools/build-plane-app/overview>

### Experiment 6: Prototype a Plane-mentioned agent for planning or content triage

Existing repository workflow:

- Agent-mediated development, content authoring, and frontend ownership lanes.
- Existing in-process `AgentContentEditorApi` and content editor tools.
- Repository planning docs that agents read before implementation or content work.

Low-effort Plane transfer:

- Do not connect Plane directly to game/content mutation APIs at first.
- Prototype a Plane agent that only answers planning questions, summarizes linked docs, or classifies a work item as Core, Content, Frontend, gap, quirk, or feedback.
- Require all suggested repository changes to go through the normal code-agent workflow rather than direct Plane-side mutation.

Why this is low risk:

- The agent remains advisory.
- It tests whether Plane comments are a useful interaction surface for planning agents.
- It avoids creating a second path for content or code edits.

Suggested measurement window:

- Five to ten mention-driven planning or triage interactions.

Relevant Plane documentation:

- Plane agents overview: <https://developers.plane.so/dev-tools/agents/overview>
- Build a Plane app: <https://developers.plane.so/dev-tools/build-plane-app/overview>
- Webhooks: <https://developers.plane.so/dev-tools/intro-webhooks>

## Recommended trial order

1. Mirror the three active rolling boards manually into Plane.
2. Use Plane for content/ecology triage during one experiment batch.
3. Host sprint summaries and this investigation as Plane Pages linked back to repository docs.
4. Add GitHub PR references if the manual board workflow is useful.
5. Try API-based posting of scenario or feedback-build summaries.
6. Consider a Plane-mentioned advisory agent only after the basic work-item and page workflows prove valuable.

## Guardrails

- Keep repository documents canonical for durable truth until an explicit source-of-truth migration is selected.
- Prefer one-way sync from repository docs to Plane before any two-way sync.
- Put repository links and last-synced timestamps on mirrored Plane pages or work items.
- Do not store Plane API keys, OAuth secrets, webhook secrets, or exported secret CSV files in the repository.
- Treat Plane's GitHub bidirectional sync cautiously because it can overwrite issue/work-item content depending on configuration.
- Do not let Plane-hosted planning bypass the required reading order in `docs/Source of Truth/planning-index.md` for implementation or content work.
