# GitHub Pages Sitemap Implementation

## Overview
This document describes the implementation of automatic sitemap generation for the Minotaur GitHub Pages site.

## Problem
The GitHub Pages site at `https://develapp-ai.github.io/Minotaur/` was not generating a sitemap.xml file, which is important for:
- Search engine optimization (SEO)
- Helping search engines discover all pages on the site
- Improving site indexing and visibility

## Solution
Added the `jekyll-sitemap` plugin to the Jekyll configuration, which is an officially supported GitHub Pages plugin.

## Changes Made

### Modified `_config.yml`
Added three key configurations:

1. **URL Configuration**
   ```yaml
   url: https://develapp-ai.github.io
   baseurl: /Minotaur
   ```
   These settings are required for the sitemap plugin to generate correct absolute URLs.

2. **Plugin Configuration**
   ```yaml
   plugins:
     - jekyll-sitemap
   ```
   This enables the automatic sitemap generation.

## How It Works

1. **Automatic Generation**: When GitHub Pages builds the Jekyll site, the jekyll-sitemap plugin automatically:
   - Scans all pages and posts in the site
   - Generates a `sitemap.xml` file
   - Includes proper URLs using the `url` and `baseurl` settings
   - Updates the sitemap with each deployment

2. **No Additional Files Needed**: The plugin doesn't require:
   - A Gemfile (GitHub Pages includes it by default)
   - Any additional template files
   - Manual sitemap creation or maintenance

3. **Supported by GitHub Pages**: The jekyll-sitemap plugin is one of the officially supported plugins, meaning:
   - No special configuration needed
   - Fully compatible with GitHub Pages infrastructure
   - Maintained and updated by GitHub

## Verification

After the next GitHub Pages deployment (triggered by merging to main branch), you can verify the sitemap:

1. **Access the Sitemap**
   ```
   https://develapp-ai.github.io/Minotaur/sitemap.xml
   ```

2. **Expected Content**
   The sitemap should include entries for:
   - The main README.md page (as index)
   - All markdown files in the root directory that aren't excluded
   - All documentation files in the docs/ directory
   - Any other pages processed by Jekyll

3. **XML Format**
   The sitemap will be in standard XML sitemap format:
   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
     <url>
       <loc>https://develapp-ai.github.io/Minotaur/</loc>
       <lastmod>YYYY-MM-DD</lastmod>
     </url>
     <!-- More URLs... -->
   </urlset>
   ```

## Testing Locally (Optional)

If you want to test Jekyll locally before deploying:

```bash
# Install Jekyll and dependencies
gem install jekyll bundler

# Create a Gemfile with:
# source "https://rubygems.org"
# gem "github-pages", group: :jekyll_plugins

# Install dependencies
bundle install

# Serve the site locally
bundle exec jekyll serve

# Access the sitemap at:
# http://localhost:4000/Minotaur/sitemap.xml
```

## Search Engine Submission

Once the sitemap is live, you can submit it to search engines:

1. **Google Search Console**
   - Add the sitemap URL: `https://develapp-ai.github.io/Minotaur/sitemap.xml`

2. **Bing Webmaster Tools**
   - Submit the same sitemap URL

3. **robots.txt (Optional)**
   You could also add a robots.txt file in the root with:
   ```
   Sitemap: https://develapp-ai.github.io/Minotaur/sitemap.xml
   ```

## References

- [Jekyll Sitemap Plugin](https://github.com/jekyll/jekyll-sitemap)
- [GitHub Pages Dependency Versions](https://pages.github.com/versions/)
- [Sitemaps.org Protocol](https://www.sitemaps.org/protocol.html)
