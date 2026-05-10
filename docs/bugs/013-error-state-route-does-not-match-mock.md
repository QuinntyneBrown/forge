# Bug 013: /error route does not match the error-state mock (missing illustration, banner, diagnostics)

## Status
Complete

## Severity
High

## Area
Error state

## References
- Implementation: http://localhost:4321/error
- Design mock: `file:///C:/projects/forge/docs/mocks/error-state.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/error-state.png`

## Description
The implemented `/error` route renders a single rounded card with the headline "Something went sideways", a one-paragraph blurb, a "Go to dashboard" button, and three rows showing "Backend / HealthKit ingest / Trace id" with status pills. The mock specifies a much richer screen modeled on an Apple Watch sync failure: a top error banner, a hero illustration with a circled-cross watch icon, a multi-paragraph copy block, an inline error-code chip, two CTAs (Retry sync + Go to dashboard), a four-row "Diagnostics" panel with colored status icons, and a troubleshooting / contact-support footer.

## Expected Behavior
Per `docs/mocks/error-state.html`:
- **Top banner** in error-container color: `cloud_off` icon + "Apple Watch sync paused — last successful sync 14 minutes ago." + a right-aligned "Retry now" link.
- **Hero illustration**: 180×180 radial-gradient circle containing a 120×120 white rounded square with a `watch` icon and a small red cross-circle overlay top-right.
- **Title** "Couldn't sync with your Apple Watch" + a longer 2-sentence sub explaining what was attempted and what is safe.
- **Error code chip** (`ERR_HEALTHKIT_OFFLINE · 0xA3`) below the sub.
- **Two CTAs**: filled "Retry sync" (teal) and outlined "Go to dashboard".
- **Diagnostics card** with four rows: Forge Fit servers (ok), Internet (ok), HealthKit authorization (warn), Apple Watch reachable (idle/clock). Each row has a colored round icon and a title + sub.
- **Footer**: "View troubleshooting guide ›" link and a meta line with trace ID + contact support.

## Actual Behavior
- No top banner.
- No hero illustration.
- No error code chip.
- Only one CTA ("Go to dashboard"); no "Retry sync".
- The diagnostics block is a 3-row plain key/value list, not the icon-prefixed Diagnostics card.
- No troubleshooting link, no trace ID footer.

## Proposed Fix
- Build an `ErrorStatePageComponent` that takes an error descriptor (code, title, sub, diagnostics list) and renders the layout above. The current implementation is closer to a generic "something failed" toast and should be replaced.
- Add the banner as part of the app shell when an error condition is active (or as a fixed top section of the error route).
- Provide a default descriptor that mirrors the mock for the `/error` showcase route, and let real callers pass their own.

## Resolution
The `/error` route now renders the full error-state mock layout. Earlier work
already shipped the banner, hero illustration, diagnostics card, retry CTA,
and dashboard CTA. This iteration closed the remaining gap by making the
error-code chip query-param-driven.

**File changes**
- `frontend/projects/forge/src/app/pages/error/error.page.ts` — convert
  `traceId` to a `computed` signal, add `errorCodeLabel` computed signal that
  reads `?code=` from the route (numeric values render as `Error {code}`,
  non-numeric values render verbatim, missing values fall back to
  `ERR_HEALTHKIT_OFFLINE · 0xA3`), and wire the retry button to a
  `window.location.reload()` handler.
- `frontend/projects/forge/src/app/pages/error/error.page.html` — bind the
  chip to `errorCodeLabel()`, the footer trace to `traceId()`, and the retry
  button click to `onRetry()`.
- `frontend/e2e/pages/error.page.ts` — new POM with locators for banner,
  hero, code chip, retry button, diagnostics card, and individual rows.
- `frontend/e2e/tests/error-state-content-and-styling.spec.ts` — new spec
  covering each Bug 013 sub-item as its own `test()`.
- `frontend/e2e/tests/error-state.spec.ts` — refreshed to assert the new
  testids (`error-page-*`) since the route no longer mounts
  `forge-sync-error-panel`; the underlying intent (traceId surfacing +
  dashboard CTA) is preserved.

**Approach**
- Reused existing Material design tokens (`--md-sys-color-error-container`,
  `--md-sys-color-primary`, etc.) — no hardcoded hex.
- The chip's "numeric → Error N" formatting keeps the test's
  `?code=500 → "Error 500"` contract while leaving the descriptive default
  intact for the showcase landing case.
