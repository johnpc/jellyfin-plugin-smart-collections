<h1 align="center">Jellyfin Smart Collections Plugin</h1>

<p align="center">
Jellyfin Smart Collections plugin is a plugin that automatically creates Smart Collections based on selected Tags associated with your library;

</p>

## Install Process

## From Repository

1. In jellyfin, go to dashboard -> plugins -> Repositories -> add and paste this link https://raw.githubusercontent.com/johnpc/JellyfinPluginManifest/master/manifest.json
2. Go to Catalog and search for Smart Collections
3. Click on it and install
4. Restart Jellyfin

## From .zip file

1. Download the .zip file from release page
2. Extract it and place the .dll file in a folder called `plugins/Smart Collections` under the program data directory or inside the portable install directory
3. Restart Jellyfin

## User Guide

1. To create smart collections you can do it from Schedule task or directly from the configuration of the plugin.
2. You need to have enabled the option "Smart Collections" under display

## Build Process

1. Clone or download this repository
2. Ensure you have .NET Core SDK setup and installed
3. Build plugin with following command.

```sh
rm -rf bin
dotnet format
dotnet publish --configuration Release --output bin
cd bin && zip -r Jellyfin-Plugin-SmartCollections.zip ./Jellyfin.Plugin.SmartCollections.dll && cd -
md5sum bin/Jellyfin-Plugin-SmartCollections.zip
```

4. Place the resulting .dll file in a folder called `plugins/Smart Collections` under the program data directory or inside the portable install directory
5. Upload ./bin/Jellyfin-Plugin-SmartCollections.zip as GH release and update https://github.com/johnpc/JellyfinPluginManifest with the release (checksum from `md5sum bin/Jellyfin-Plugin-SmartCollections.zip`)
6. Manifest url is https://raw.githubusercontent.com/johnpc/JellyfinPluginManifest/<hash>/manifest.json

## Deploy Process

For now, the deploy process is manual. In github, go to the release or create the release (like https://github.com/johnpc/jellyfin-plugin-smart-collections/releases/edit/v0.0.0.1). From there you can upload the `Jellyfin-Plugin-SmartCollections.zip` generated from the build process.

## Install process

Install the plugin in jellyfin by visiting your Jellyfin Admin dashboard and choosing Plugins > Catelog > Gear Icon.

There you can add this repository as a plugin source:

```bash
Name: @johnpc (SmartCollections)
Repo url: https://raw.githubusercontent.com/johnpc/JellyfinPluginManifest/4b652a28476ab29dc441755df6f4a18f236e949b/manifest.json
```

Then find, go back to Plugins > Catelog and you'll see Smart Collections there. Click it, choose install, and restart your jellyfin server!
