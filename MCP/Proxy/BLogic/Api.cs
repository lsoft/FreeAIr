#nullable disable
#pragma warning disable IDE1006
using System.Text.Json.Serialization;

namespace Proxy.BLogic
{
    /// <summary>
    /// One release as GitHub's Releases API returns it - the shape the update checker and the
    /// installer parse to find the latest FreeAIr VSIX and its download link.
    /// </summary>
    public class Release
    {
        public string url
        {
            get; set;
        }
        public string assets_url
        {
            get; set;
        }
        public string upload_url
        {
            get; set;
        }
        /// <summary>The page a human would open on github.com to read this release.</summary>
        public string html_url
        {
            get; set;
        }
        public int id
        {
            get; set;
        }
        public Author author
        {
            get; set;
        }
        public string node_id
        {
            get; set;
        }
        /// <summary>The version tag, e.g. `v1.2.3` - what a caller compares against the running build.</summary>
        public string tag_name
        {
            get; set;
        }
        public string target_commitish
        {
            get; set;
        }
        public string name
        {
            get; set;
        }
        public bool draft
        {
            get; set;
        }
        public bool prerelease
        {
            get; set;
        }
        public DateTime created_at
        {
            get; set;
        }
        public DateTime published_at
        {
            get; set;
        }
        /// <summary>The uploaded files, including the VSIX itself.</summary>
        public Asset[] assets
        {
            get; set;
        }
        public string tarball_url
        {
            get; set;
        }
        public string zipball_url
        {
            get; set;
        }
        /// <summary>The release notes, in Markdown, as written on github.com.</summary>
        public string body
        {
            get; set;
        }
        public Reactions reactions
        {
            get; set;
        }
        public int mentions_count
        {
            get; set;
        }
    }

    /// <summary>The GitHub account that published a <see cref="Release"/>.</summary>
    public class Author
    {
        public string login
        {
            get; set;
        }
        public int id
        {
            get; set;
        }
        public string node_id
        {
            get; set;
        }
        public string avatar_url
        {
            get; set;
        }
        public string gravatar_id
        {
            get; set;
        }
        public string url
        {
            get; set;
        }
        public string html_url
        {
            get; set;
        }
        public string followers_url
        {
            get; set;
        }
        public string following_url
        {
            get; set;
        }
        public string gists_url
        {
            get; set;
        }
        public string starred_url
        {
            get; set;
        }
        public string subscriptions_url
        {
            get; set;
        }
        public string organizations_url
        {
            get; set;
        }
        public string repos_url
        {
            get; set;
        }
        public string events_url
        {
            get; set;
        }
        public string received_events_url
        {
            get; set;
        }
        public string type
        {
            get; set;
        }
        public string user_view_type
        {
            get; set;
        }
        public bool site_admin
        {
            get; set;
        }
    }

    /// <summary>The emoji reaction counts GitHub attaches to a <see cref="Release"/>; unused by FreeAIr but present in the API response.</summary>
    public class Reactions
    {
        public string url
        {
            get; set;
        }
        public int total_count
        {
            get; set;
        }
        [JsonPropertyName("+1")]
        public int Plus1
        {
            get; set;
        }
        [JsonPropertyName("-1")]
        public int Minus1
        {
            get; set;
        }
        public int laugh
        {
            get; set;
        }
        public int hooray
        {
            get; set;
        }
        public int confused
        {
            get; set;
        }
        public int heart
        {
            get; set;
        }
        public int rocket
        {
            get; set;
        }
        public int eyes
        {
            get; set;
        }
    }

    /// <summary>
    /// One file attached to a <see cref="Release"/> - what the FreeAIr update flow downloads
    /// the VSIX from, via <see cref="browser_download_url"/>.
    /// </summary>
    public class Asset
    {
        public string url
        {
            get; set;
        }
        public int id
        {
            get; set;
        }
        public string node_id
        {
            get; set;
        }
        /// <summary>The file name as uploaded, e.g. `FreeAIr.vsix`.</summary>
        public string name
        {
            get; set;
        }
        public string label
        {
            get; set;
        }
        public Uploader uploader
        {
            get; set;
        }
        public string content_type
        {
            get; set;
        }
        public string state
        {
            get; set;
        }
        public int size
        {
            get; set;
        }
        public int download_count
        {
            get; set;
        }
        public DateTime created_at
        {
            get; set;
        }
        public DateTime updated_at
        {
            get; set;
        }
        /// <summary>The direct URL to fetch this asset's bytes from.</summary>
        public string browser_download_url
        {
            get; set;
        }
    }

    /// <summary>The GitHub account that uploaded an <see cref="Asset"/>.</summary>
    public class Uploader
    {
        public string login
        {
            get; set;
        }
        public int id
        {
            get; set;
        }
        public string node_id
        {
            get; set;
        }
        public string avatar_url
        {
            get; set;
        }
        public string gravatar_id
        {
            get; set;
        }
        public string url
        {
            get; set;
        }
        public string html_url
        {
            get; set;
        }
        public string followers_url
        {
            get; set;
        }
        public string following_url
        {
            get; set;
        }
        public string gists_url
        {
            get; set;
        }
        public string starred_url
        {
            get; set;
        }
        public string subscriptions_url
        {
            get; set;
        }
        public string organizations_url
        {
            get; set;
        }
        public string repos_url
        {
            get; set;
        }
        public string events_url
        {
            get; set;
        }
        public string received_events_url
        {
            get; set;
        }
        public string type
        {
            get; set;
        }
        public string user_view_type
        {
            get; set;
        }
        public bool site_admin
        {
            get; set;
        }
    }
}
