# Bug 012: Rewards "Recent achievements" is a single text card and "In-flight" is a placeholder

## Status
Complete

## Severity
High

## Area
Rewards

## References
- Implementation: http://localhost:4321/rewards
- Design mock: `file:///C:/projects/forge/docs/mocks/rewards.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/rewards.png`

## Description
Bug 006 marked the rewards page as "Complete" for the orange balance hero and the labeled section structure. However the actual contents of two of those sections — Recent achievements and In-flight — are not implemented. They render as a single text card ("Gold · 17965 pts available · 5585 pts to Platinum") and a generic placeholder paragraph respectively, instead of the medal grid and the four progress rows shown in the mock.

## Expected Behavior
Per `docs/mocks/rewards.html`:
- **Recent achievements**: a 2-column (mobile) / 3-column (tablet) / 4-column (desktop) grid of `.ach` tiles. Each tile is centered, with a 56×56 round medal in a tier color (gold / teal / blue / orange), an optional `×N` count badge bottom-right, the achievement title and a one-line sub. The mock shows six items: Morning Warrior, 1500-Cal Club, Night Resister, Iron Week, 300-Min Week, First 5 lb Down.
- **In-flight**: a vertical list of progress cards. Each card has an icon tile, title, sub, "X / Y" counter on the right, and a teal gradient progress bar below. Mock shows four: Morning Warrior x10, -20 lb May, Night Resister x10, 1500-Cal Club ×7.

## Actual Behavior
- **Recent achievements** renders one bordered card containing the user's tier and points-to-next-tier — duplicate of the hero content. There is no medal grid.
- **In-flight** renders the literal placeholder string "Track your in-flight goals here. Progress updates as you log workouts." There are no progress cards.
- The "Catalog" reward tiles render with empty round outlines instead of colored icon tiles (tv / restaurant / spa / headphones / gift card per the mock); equipment-specific tints are not applied.

## Proposed Fix
- Build an `AchievementMedalComponent` with `tone` (gold/teal/blue/orange) and optional `count` props; render the six mock items in `RewardsPageComponent`.
- Build an `InFlightProgressItemComponent` with icon tile + title + sub + counter + progress bar; render the four mock items.
- Update the catalog reward tile to render an icon inside a tinted rounded-square (12px radius) rather than an empty stroked circle.
