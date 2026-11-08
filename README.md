<h1 align="center">Jellyfin Smart Collections Plugin</h1>

<p align="center">
Jellyfin Smart Collections Plugin is a plugin that automatically creates Smart Collections based on selected Tags associated with your library

</p>

## What is it?

You can configure Jellyfin Smart Collections Plugin with a list of Tags on movies, series, and individual episodes in your Jellyfin server.

For example, "The Long Kiss Goodnight" gets a "christmas" tag applied when you fetch metadata

![](https://files.jpc.io/d/Bha8r-Screenshot%202025-03-06%20at%203.50.04%E2%80%AFPM.png)

![](https://files.jpc.io/d/Bha8r-Screenshot%202025-03-06%20at%203.59.21%E2%80%AFPM.png)

Now that you know that your movie has been tagged with "christmas", you can set up a Smart Collection for the christmas tag in the plugin settings

![](https://files.jpc.io/d/Bha8r-Screenshot%202025-03-06%20at%205.29.44%E2%80%AFPM.png)

The result after running the task is that a Collection is created for each Tag you configured in the plugin setup. All movies and series matching the tag are included in the Smart Collection. The Smart Collection is kept up to date any time the task runs, removing items that were un-tagged and adding items that were tagged since the last time the plugin ran.

Individual episodes can be tagged too. For example, tag a show's holiday special with "christmas" and that single episode joins the Christmas collection, without pulling in the rest of the series. Episodes match on tags only (not genres, studios, or people), so a series' genres never flood a collection with every episode.

![](https://files.jpc.io/d/Bha8r-Screenshot%202025-03-06%20at%205.31.29%E2%80%AFPM.png)

## Install Process

1. In Jellyfin, go to `Dashboard -> Plugins -> Catalog -> Gear Icon (upper left)` add and a repository.
1. Set the Repository name to @johnpc (Smart Collections)
1. Set the Repository URL to https://raw.githubusercontent.com/johnpc/jellyfin-plugin-smart-collections/refs/heads/main/manifest.json
1. Click "Save"
1. Go to Catalog and search for Smart Collections
1. Click on it and install
1. Restart Jellyfin

## User Guide

1. To set it up, visit `Dashboard -> Plugins -> My Plugins -> Smart Collections -> Settings`
1. Configure your tags that you want converted to Smart Collections as a comma-seperated list
1. For each collection, pick a matching mode:
    - **OR** (default): include items that match *any* of the tags (e.g. `christmas, holiday` → everything tagged christmas or holiday)
    - **AND**: include only items that match *all* of the tags (e.g. `documentary, sports` → only sports documentaries). A term can also be a person's name (e.g. `Tom Hanks, comedy` → Tom Hanks comedies)
1. A term matches an item's tags, genres, or studios, so no extra tagging is needed to collect by network or studio (e.g. `Netflix` → every movie and series whose Studios include Netflix; `Netflix, comedy` with **AND** → Netflix comedies). Episodes still match on tags only.
1. A term can also be a decade, matched against each movie's or series' production year. Write it as `1980s`, `80s` (`30s`-`90s` mean the 1900s, `00s`-`20s` the 2000s) or an explicit range like `1980-1989`. Episodes still match on tags only, so a decade never pulls in every episode of a series:
    - `80s` → every movie and series from 1980-1989
    - `2000s, 2010s` (OR) → everything from 2000-2019
    - `1980s, horror` (AND) → 80s horror
1. For more control, use the **+** button on a collection to add a filter line that narrows it down. Put the subject on the first line and the narrower type on the filter line. Items must match the first line **and** the filter line, and commas on either line mean OR:
    - `music, band, rock music` filtered by `documentary` = music documentaries
    - `sports, hockey, football` filtered by `documentary, biography` = sports documentaries and biographies
    - `1980s` filtered by `horror` = 80s horror movies and series
1. To keep items out of a collection, prefix a tag with `!`. Items carrying any excluded tag are removed, whatever the matching mode (works on the filter line too):
    - `adult animation, !anime` (AND) = adult animation that is not anime
    - `samurai, !family, !animation` = samurai titles that are neither family nor animation
1. Choose "Save"
1. Choose "Sync Smart Collections For Tags"
1. Viola! Your Smart Collections now exist.
1. Note: The Smart Collections Sync task is also available in your Scheduled Tasks section.

## Build Process

1. Clone or download this repository
2. Ensure you have .NET Core SDK setup and installed
3. Build plugin with following command.

```sh
make build
```

6. Manifest url is at https://raw.githubusercontent.com/johnpc/jellyfin-plugin-smart-collections/refs/heads/main/manifest.json

## Deploy Process

Releases are fully automated. Just merge to `main` and GitHub Actions will:
- Auto-bump the patch version from the latest tag
- Build the plugin with the correct version
- Create a GitHub release with the zip + manifest
- Update the manifest on `main` so users get the new version automatically
