# Packaging change log

## 2026-09-16 — structure revision

- Moved the product beneath Assets/Animpic Studio.
- Organized product files by purpose and preserved existing asset GUIDs.
- Updated editor paths and kept supporting components local to the product.
- Added this offline entry guide.
- Excluded historical development sources from the distribution archive.

This is a packaging revision, not a claim of a new feature version or newly supported engine version.

## 2026-09-17 — catalog validation

- Checked asset references, shader compilation and demo scene startup in Unity 2022.3.3f1.
- Ran Asset Store Tools 12.0.1 general validation; remaining publication findings are tracked in the catalog audit.
- Consolidated 8 duplicate files and remapped references inside this product.
- Added HDRP 14 area-shadow keyword compatibility; water keeps its authored two-sided normal convention.
