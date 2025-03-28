﻿using MediaBrowser.Model.Plugins;
using System.Collections.Generic;

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
            return tag.Length > 0
                ? char.ToUpper(tag[0]) + tag[1..] + " Smart Collection"
                : "Smart Collection";
        }
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
            public PluginConfiguration()
            {
                TagTitlePairs = new List<TagTitlePair>
                {
                    new TagTitlePair("christmas"),
                    new TagTitlePair("halloween"),
                    new TagTitlePair("japan"),
                    new TagTitlePair("based on novel or book"),
                    new TagTitlePair("revenge"),
                    new TagTitlePair("parody"),
                    new TagTitlePair("based on comic"),
                    new TagTitlePair("adult animation"),
                    new TagTitlePair("heist"),
                    new TagTitlePair("post-apocalyptic future"),
                    new TagTitlePair("reality"),
                    new TagTitlePair("mystery")
                };
                
                // For backward compatibility
                Tags = TagTitlePairs.ConvertAll(pair => pair.Tag).ToArray();
            }

            public List<TagTitlePair> TagTitlePairs { get; set; }
            
            // Keep this for backward compatibility
            public string[] Tags { get; set; }
    }
}
