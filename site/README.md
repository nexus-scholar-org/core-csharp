# Nexus Scholar Core Pages

This directory is the GitHub Pages source for the Nexus Scholar Core project site.

## Structure

- `index.html` - project homepage and ecosystem entry point.
- `status/` - dated, evidence-linked protected-main implementation status.
- `roadmap/` - completed, next-candidate, and future feature sequence.
- `about/` - project vision and principles.
- `blog/` - public project narrative, motivation, positioning, and community posts.
- `developers/` - developer documentation.
- `tutorials/` - tutorial pages.
- `assets/` - shared styles, scripts, and generated visuals.
- `sitemap.xml` and `robots.txt` - current GitHub Pages discovery metadata.

The site is intentionally static and dependency-free. The `pages` workflow deploys it from `main` with the GitHub Pages artifact pipeline.

## Publish Source

Repository Pages build type is GitHub Actions. The historical `gh-pages` branch is retained for provenance but is no longer the deploy source.

## Custom Domain

The recommended public hostname is `nexus.mouadh.org`. Because this repository
publishes through a custom GitHub Actions workflow, GitHub stores the custom
domain in repository settings and ignores a checked-in `CNAME` file. Do not add
one to this directory. Follow `docs/ops/GITHUB-PAGES-CUSTOM-DOMAIN.md` after the
domain has been verified for the `nexus-scholar` GitHub organization.
