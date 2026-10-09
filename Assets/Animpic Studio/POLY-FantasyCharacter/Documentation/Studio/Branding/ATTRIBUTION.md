# Brand asset attribution

Prepared for the authorized Animpic Character Studio integration on 2026-09-05. Public GET downloads only; no website content was changed.

## Official Animpic logos

These PNG files are unmodified downloads from the production Animpic Store markup. They are Animpic branding; the font OFL licenses below do not apply to these logos.

- Logos/animpic-wordmark-light.png: https://animpic.store/static/manager/animpic-wordmark-light.61980a3c3296.png
  Source size833×276, full-image aspect3.018116. Visible alpha bounds (threshold>8, right/bottom exclusive): x50,y46,right775,bottom205; visible extent725×159.
- Logos/animpic-mark.png: https://animpic.store/static/manager/animpic-mark.7ac5cb6c7797.png
  Source size184×184, full-image aspect1. Visible alpha bounds (threshold>8, right/bottom exclusive): x5,y13,right179,bottom161; visible extent174×148.

Preserve the downloaded bytes. Import as Sprite with alpha transparency and preserve aspect; account for the transparent margins in layout.

## Fonts downloaded from Animpic Store

The site's fonts.css declares Inter as the UI family and Manrope as the display family and identifies their source as the official Google Fonts repository.

- Raw/Inter-Variable.ttf: https://animpic.store/static/manager/fonts/Inter-Variable.bff0f6e3b9e2.ttf
- Raw/Manrope-Variable.ttf: https://animpic.store/static/manager/fonts/Manrope-Variable.c329d9ac3c7b.ttf
- Source declaration: https://animpic.store/static/manager/design-system/fonts.3c67e5c91924.css?v=1

Font authors and upstream:
- The Inter Project Authors: https://github.com/rsms/inter
- The Manrope Project Authors: https://github.com/sharanda/manrope
- Official Google Fonts distributions: https://github.com/google/fonts/tree/main/ofl/inter and https://github.com/google/fonts/tree/main/ofl/manrope

Complete font licenses are retained:
- Licenses/Inter-OFL.txt: https://raw.githubusercontent.com/google/fonts/main/ofl/inter/OFL.txt
- Licenses/Manrope-OFL.txt: https://raw.githubusercontent.com/google/fonts/main/ofl/manrope/OFL.txt

Downloaded font name-table copyright statements are also retained in both source and generated fonts:
- Inter: Copyright 2016 The Inter Project Authors (https://github.com/rsms/inter)
- Manrope: Copyright 2019 The Manrope Project Authors (https://github.com/sharanda/manrope)

The current Google Fonts license files additionally carry their distribution copyright notices (Inter2020 and Manrope2018). No Reserved Font Name is declared in either downloaded OFL copyright header.

## Static Unity font instances

These are static instances generated from the exact site-hosted variable fonts with fontTools4.60.1 varLib.instancer. Font family/author/copyright/license metadata is preserved; weight/style names are updated by the instancer.

- Fonts/Inter-Regular.ttf: wght400, opsz14.
- Fonts/Inter-Medium.ttf: wght500, opsz14.
- Fonts/Manrope-Bold.ttf: wght700.

The outputs contain no fvar/gvar tables. OS/2 weightClass and Latin/Cyrillic coverage were verified. The fonts remain under their SIL Open Font License1.1; distribute the licenses and copyright attribution with them. These font files are not a separately sold font product.

Reproduction: ../brand_research/build_static_fonts.py. The isolated task dependency is ../brand_research/python_deps; it must not be copied into Unity Assets. Only Fonts, Logos, Licenses and this attribution are needed in the Unity asset distribution. Raw files and research reports can remain outside Unity.

The generated per-font names, sizes, axes and SHA256 checksums are recorded in font-build-report.json. All downloaded and generated reusable asset checksums are recorded in asset-checksums.json.

