# Bug 028: Material Icons render as raw text labels when Google Fonts CDN is unavailable

## Status
Complete

## Severity
High

## Area
Global — all pages

## References
- Implementation: all pages (sign-in, dashboard, workouts, rewards, profile, …)
- Source: `frontend/projects/forge/src/index.html` (CDN link), `frontend/angular.json` (styles)
- Screenshots: `docs/screenshots/desktop/` (before fix: icons shown as text; after fix: glyphs render)

## Description
Every icon on every page is rendered via a `<span class="material-icons">` or `<mat-icon>` element.
Both rely on the Material Icons ligature font, which was previously loaded exclusively from the
Google Fonts CDN:

```html
<link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet">
```

When the CDN request fails — due to network restrictions, offline use, or a slow connection —
the browser has no `Material Icons` font face available, so every icon ligature
(`visibility`, `play_arrow`, `nights_stay`, `check_circle`, …) renders as plain text.

Additionally, a `Material Symbols Rounded` CDN link and companion `<style>` block were present
in `index.html` even though **no** Angular template in the project used the
`material-symbols-rounded` CSS class. This added an unnecessary network request.

## Expected Behavior
- Every icon span renders as its glyph (eye, play triangle, moon, check-circle, …) regardless
  of CDN availability.
- No "visibility", "play_arrow", or similar ligature strings are ever visible to the user.

## Actual Behavior (before fix)
- Password-visibility toggle on Sign-in shows "visibility" / "visibility_off" as text.
- Dashboard hero CTAs show "play_arrow" and "history" as text.
- Eating-window tile shows "nights_stay" and "check_circle" as text.
- Navigation icons (`home`, `fitness_center`, `redeem`, `person`) show as text when CDN is slow.

## Root Cause
The `material-icons` font was fetched at runtime from `https://fonts.googleapis.com`. When that
request is blocked or delayed, the browser has no `@font-face` fallback and renders the raw
ligature string instead of a glyph.

## Fix Applied
1. Installed the `material-icons` npm package (v1.13.14), which bundles WOFF2/WOFF font files.
2. Added `node_modules/material-icons/iconfont/material-icons.css` to the Angular build's
   `styles` array in `angular.json`. The Angular builder inlines the `@font-face` declarations
   and copies the referenced `.woff2` font files into the production bundle — no CDN required.
3. Removed the Google Fonts CDN `<link>` for Material Icons from `index.html`.
4. Removed the unused `Material Symbols Rounded` CDN `<link>` and its companion `<style>` block
   from `index.html` (no template used the `material-symbols-rounded` class).

## Verification
After the fix, rebuilding the app and navigating to any page in an environment without Google
Fonts access (e.g. `--disable-extensions --host-rules="MAP fonts.googleapis.com 127.0.0.1"`)
shows all icons rendered as glyphs. The desktop screenshot attached to the PR confirms the
"visibility" eye icon, navigation icons, and dashboard card icons all display correctly.
