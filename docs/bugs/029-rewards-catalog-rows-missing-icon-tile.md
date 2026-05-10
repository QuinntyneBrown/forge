# Bug 029: Rewards catalog rows render an empty circle placeholder instead of the mock's colored icon tile

## Status
Open

## Severity
Medium

## Area
Rewards / Reward shop

## References
- Implementation: http://localhost:4321/rewards
- Design mock: `file:///C:/projects/forge/docs/mocks/rewards.html` (lines 80–88, 232–272)
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/rewards.png`

## Description
Each row in the "Catalog" / "Reward shop" list should lead with a 48×48 rounded-square icon tile (matching the mock's `.reward__icon` pattern — a colored container with a centered Material Symbols glyph appropriate for the reward, e.g. `tv`, `restaurant`, `spa`, `headphones`, `card_giftcard`). The implementation instead renders a small empty outlined circle in the leading slot for every catalog row, giving each row the appearance of a radio button or unchecked selection control rather than a graphical reward affordance. This is consistent across desktop, tablet, and mobile.

## Expected Behavior
- Each catalog row leads with a 48×48 (`var(--shape-md)` corner) icon tile.
- Tile background varies by reward category to match the mock palette: secondary container (peach) by default, tertiary container (light blue), `#FFE7B0` (gold), primary container (mint), surface-container-high (neutral, for locked items).
- Icon glyph inside is a Material Symbol matching the reward (`tv`, `restaurant`, `spa`, `headphones`, `card_giftcard`, etc.).
- Locked items keep the neutral tile but still show an icon, not an empty circle.

## Actual Behavior
- Every row leads with a small (~16px) empty circular outline in the icon slot.
- No glyph is rendered, no per-reward color treatment, no rounded-square tile.
- Visually reads as a list of unchecked options, not a shop.

## Proposed Fix
- Add an `icon` (Material Symbol name) and `tone` (palette key — `secondary` / `tertiary` / `gold` / `primary` / `neutral`) to the reward model surfaced from the catalog API or mapping table.
- Replace the empty-circle template in `RewardsCatalogRowComponent` (or equivalent) with the `.reward__icon` markup from the mock, applying the per-tone background/foreground via a CSS modifier class.
- Seed at least the five mock items (`30-min show after dinner` → `tv`, `Cheat meal token` → `restaurant`, `Recovery day pass` → `spa`, `New playlist drop` → `headphones`, `$25 gear credit` → `card_giftcard`) with the correct glyph + tone so the third-pass screenshot matches.
