# Claude escalation gate

Default: **Cursor + Unity MCP** owns engineering, debugging and daily testing. Claude owns **Canva pixel-art and asset iteration only**.

Ask Claude for debugging ONLY when:
- Cursor reproduced a concrete blocker and attempted at least one focused diagnosis;
- user agreed to escalate or the blocker is explicitly a high-complexity unresolved issue;
- handoff is minimal: 1 paragraph context, minimal logs, only related code snippets/diff, expected vs actual and exact question.

Never send full Unity project, 30-video ZIP or long transcripts to Claude for a minor code bug. Do not repeatedly pass full R1/R2 history to every call. Keep an approved short current-state note updated locally.
