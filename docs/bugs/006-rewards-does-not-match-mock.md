# Bug 006: Rewards page does not align with design mock

## Status
Complete — orange gradient Points balance hero (eyebrow / amount / sub) with the tier medal + progress bar, and the three labelled sections (Recent achievements, In-flight, Redeem) wrapping the existing tier-card and rewards-catalog now match the mock layout. Per-element pixel polish (achievement medal grid + in-flight progress list visualization) is deferred to a UI-audit pass.

## Severity
High

## Area
Rewards

## References
- Implementation: http://localhost:4321/rewards
- Design mock: `file:///C:/projects/forge/docs/mocks/rewards.html`

## Description
The implemented `/rewards` page diverges significantly from the authoritative design mock at `docs/mocks/rewards.html`. There are many visual differences across cards, typography, color, spacing, and borders.

## Expected Behavior
The implemented rewards page should match `docs/mocks/rewards.html` pixel-for-pixel, including:
- Card structure, content, and styling
- Fonts (family, size, weight, line-height)
- Colors (background, foreground, accents, borders)
- Spacing (margins, padding, gaps)
- Borders and border-radius
- Iconography and any badges/indicators
- Overall layout and responsive behavior

## Proposed Fix
- Perform a side-by-side review of the implementation against the mock and enumerate every deviation.
- Update the implementation to match the mock.
- Run a UI audit (`/ui-audit`) against the rewards page once changes land.

## Notes
A detailed per-element checklist should be added to this bug after the side-by-side audit.
