# GitHub Pages Custom Domain Plan

Status: prepared, not activated.

Recommended hostname:

```text
nexus.mouadh.org
```

## Why This Hostname

- `.org` matches an open research-infrastructure and community project better
  than `.shop`.
- A `nexus` subdomain preserves `mouadh.org` for the owner's broader public
  identity.
- A subdomain uses one stable DNS `CNAME` record instead of binding the apex
  domain to GitHub Pages IP addresses.

`mouadh.info` remains a good personal-profile or documentation domain.
`mouadh.shop` should be reserved for a commercial storefront if one is needed.

## Required Activation Order

The repository is owned by the `nexus-scholar` GitHub organization. Domain
verification therefore belongs at the organization level.

1. In the `nexus-scholar` organization settings, start Pages-domain
   verification for `mouadh.org`.
2. Add the exact TXT verification record supplied by GitHub to the DNS zone.
3. Wait for DNS propagation, verify the domain in GitHub, and retain the TXT
   record to prevent takeover.
4. In this repository's **Settings → Pages**, set the custom domain to
   `nexus.mouadh.org`.
5. At the DNS provider, create:

   ```text
   Type:   CNAME
   Name:   nexus
   Target: nexus-scholar.github.io
   ```

   The target must not include `/core-csharp`.
6. Verify with PowerShell:

   ```powershell
   Resolve-DnsName nexus.mouadh.org -Type CNAME
   ```

7. Wait for GitHub to provision TLS, then enable **Enforce HTTPS**.
8. Confirm both the custom hostname and the existing project URL resolve as
   GitHub documents, then update canonical, Open Graph, and sitemap URLs in
   `site/`.

## Actions-Workflow Detail

This site deploys with `.github/workflows/pages.yml` and
`actions/deploy-pages`. GitHub's custom-workflow Pages mode does not require a
checked-in `site/CNAME`; GitHub ignores that file in this mode. The custom
domain must be configured in repository settings.

## Safety Rules

- Do not configure DNS before adding and verifying the domain in GitHub.
- Do not use wildcard DNS records for GitHub Pages.
- Do not remove the organization-verification TXT record after verification.
- Do not claim the custom domain is active until DNS, TLS, redirects, and the
  Pages deployment have been checked from outside the local network.

## Current State

Until activation is explicitly approved and completed, the canonical public
site remains:

```text
https://nexus-scholar.github.io/core-csharp/
```

## GitHub References

- [Manage a custom domain for a GitHub Pages site](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site/managing-a-custom-domain-for-your-github-pages-site)
- [Verify a custom domain for GitHub Pages](https://docs.github.com/en/enterprise-cloud@latest/pages/configuring-a-custom-domain-for-your-github-pages-site/verifying-your-custom-domain-for-github-pages)
- [Secure a GitHub Pages site with HTTPS](https://docs.github.com/en/pages/getting-started-with-github-pages/securing-your-github-pages-site-with-https)
