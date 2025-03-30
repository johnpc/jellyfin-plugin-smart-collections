﻿using MediaBrowser.Model.Plugins;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.SmartCollections.Configuration
{
    public class TagTitlePair
    {
        public string Tag { get; set; }
        public string Title { get; set; }

        // Add parameterless constructor for XML serialization
        public TagTitlePair()
        {
            Tag = string.Empty;
            Title = "Smart Collection";
        }

        public TagTitlePair(string tag, string title = null)
        {
            Tag = tag;
            Title = title ?? GetDefaultTitle(tag);
        }

        private static string GetDefaultTitle(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return "Smart Collection";
                
            // If there are multiple tags, use the first one for the default title
            string firstTag = tag.Split(',')[0].Trim();
            return firstTag.Length > 0
                ? char.ToUpper(firstTag[0]) + firstTag[1..] + " Smart Collection"
                : "Smart Collection";
        }
        
        // Helper method to get individual tags as an array
        public string[] GetTagsArray()
        {
            if (string.IsNullOrEmpty(Tag))
                return new string[0];
                
            return Tag.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();
        }
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            // Initialize with empty lists - defaults will be added by Plugin.cs only on first run
            TagTitlePairs = new List<TagTitlePair>();
            Tags = new string[0];
        }

        public List<TagTitlePair> TagTitlePairs { get; set; }
        
        // Keep this for backward compatibility
        public string[] Tags { get; set; }
    }
}
