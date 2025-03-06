<h1 align="center">Jellyfin Smart Collections Plugin</h1>

<p align="center">
Jellyfin Smart Collections Plugin is a plugin that automatically creates Smart Collections based on selected Tags associated with your library

</p>

## What is it?

You can configure Jellyfin Smart Collections Plugin with a list of Tags on movies and series in your Jellyfin server.

For example, "The Long Kiss Goodnight" gets a "christmas" tag applied when you fetch metadata

![](https://files.jpc.io/d/Bha8r-Screenshot%202025-03-06%20at%203.50.04%E2%80%AFPM.png)

![](https://files.jpc.io/d/Bha8r-Screenshot%202025-03-06%20at%203.59.21%E2%80%AFPM.png)

Now that you know that your movie has been tagged with "christmas", you can set up a Smart Collection for the christmas tag in the plugin settings

![](https://files.jpc.io/d/Bha8r-Screenshot%202025-03-06%20at%205.29.44%E2%80%AFPM.png)

The result after running the task is that a Collection is created for each Tag you configured in the plugin setup. All movies and series matching the tag are included in the Smart Collection. The Smart Collection is kept up to date any time the task runs, removing items that were un-tagged and adding items that were tagged since the last time the plugin ran.

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

For now, the deploy process is manual. Manually update the VERSION variable in the code, then:

Run

```bash
# Make sure you've updated the VERSION variable
make build
make zip
md5sum ./smart-collections-<version>.zip
# Manually update manifest.json with a release for this hash
make create-gh-release
# git push whatever changes, including the new version in the manifest
```
