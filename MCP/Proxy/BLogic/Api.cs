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
        /// <summary>The GitHub API URL of this release resource.</summary>
        public string url
        {
            get; set;
        }
        /// <summary>The GitHub API URL to list this release's assets.</summary>
        public string assets_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL used to upload a new asset to this release.</summary>
        public string upload_url
        {
            get; set;
        }
        /// <summary>The page a human would open on github.com to read this release.</summary>
        public string html_url
        {
            get; set;
        }
        /// <summary>GitHub's numeric id for this release.</summary>
        public int id
        {
            get; set;
        }
        /// <summary>The account that published this release.</summary>
        public Author author
        {
            get; set;
        }
        /// <summary>GitHub's opaque global node id for this release.</summary>
        public string node_id
        {
            get; set;
        }
        /// <summary>The version tag, e.g. `v1.2.3` - what a caller compares against the running build.</summary>
        public string tag_name
        {
            get; set;
        }
        /// <summary>The branch or commit SHA the release's tag was created against.</summary>
        public string target_commitish
        {
            get; set;
        }
        /// <summary>The release's display title, as entered on github.com.</summary>
        public string name
        {
            get; set;
        }
        /// <summary>Whether the release is still an unpublished draft.</summary>
        public bool draft
        {
            get; set;
        }
        /// <summary>Whether the release is marked as a pre-release rather than a stable one.</summary>
        public bool prerelease
        {
            get; set;
        }
        /// <summary>When the underlying tag/commit was created.</summary>
        public DateTime created_at
        {
            get; set;
        }
        /// <summary>When the release itself was published on GitHub.</summary>
        public DateTime published_at
        {
            get; set;
        }
        /// <summary>The uploaded files, including the VSIX itself.</summary>
        public Asset[] assets
        {
            get; set;
        }
        /// <summary>The GitHub API URL to download the release's source as a tarball.</summary>
        public string tarball_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL to download the release's source as a zip archive.</summary>
        public string zipball_url
        {
            get; set;
        }
        /// <summary>The release notes, in Markdown, as written on github.com.</summary>
        public string body
        {
            get; set;
        }
        /// <summary>The emoji reaction counts attached to this release.</summary>
        public Reactions reactions
        {
            get; set;
        }
        /// <summary>How many times this release's release notes mention another user.</summary>
        public int mentions_count
        {
            get; set;
        }
    }

    /// <summary>The GitHub account that published a <see cref="Release"/>.</summary>
    public class Author
    {
        /// <summary>The account's GitHub username.</summary>
        public string login
        {
            get; set;
        }
        /// <summary>GitHub's numeric id for the account.</summary>
        public int id
        {
            get; set;
        }
        /// <summary>GitHub's opaque global node id for the account.</summary>
        public string node_id
        {
            get; set;
        }
        /// <summary>URL of the account's avatar image.</summary>
        public string avatar_url
        {
            get; set;
        }
        /// <summary>The account's Gravatar id, if it uses one instead of an uploaded avatar.</summary>
        public string gravatar_id
        {
            get; set;
        }
        /// <summary>The GitHub API URL of the account resource.</summary>
        public string url
        {
            get; set;
        }
        /// <summary>The account's profile page on github.com.</summary>
        public string html_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing the account's followers.</summary>
        public string followers_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing accounts this account follows.</summary>
        public string following_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing the account's gists.</summary>
        public string gists_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing repositories the account has starred.</summary>
        public string starred_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing the account's subscriptions.</summary>
        public string subscriptions_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing organizations the account belongs to.</summary>
        public string organizations_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing the account's repositories.</summary>
        public string repos_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL template for the account's events.</summary>
        public string events_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing events received by the account.</summary>
        public string received_events_url
        {
            get; set;
        }
        /// <summary>The account kind, e.g. `User` or `Organization`.</summary>
        public string type
        {
            get; set;
        }
        /// <summary>GitHub's finer-grained classification of the account's view type.</summary>
        public string user_view_type
        {
            get; set;
        }
        /// <summary>Whether the account is a GitHub site administrator.</summary>
        public bool site_admin
        {
            get; set;
        }
    }

    /// <summary>The emoji reaction counts GitHub attaches to a <see cref="Release"/>; unused by FreeAIr but present in the API response.</summary>
    public class Reactions
    {
        /// <summary>The GitHub API URL of this reactions resource.</summary>
        public string url
        {
            get; set;
        }
        /// <summary>The total number of reactions across all emoji.</summary>
        public int total_count
        {
            get; set;
        }
        /// <summary>Count of thumbs-up reactions.</summary>
        [JsonPropertyName("+1")]
        public int Plus1
        {
            get; set;
        }
        /// <summary>Count of thumbs-down reactions.</summary>
        [JsonPropertyName("-1")]
        public int Minus1
        {
            get; set;
        }
        /// <summary>Count of laugh reactions.</summary>
        public int laugh
        {
            get; set;
        }
        /// <summary>Count of hooray reactions.</summary>
        public int hooray
        {
            get; set;
        }
        /// <summary>Count of confused reactions.</summary>
        public int confused
        {
            get; set;
        }
        /// <summary>Count of heart reactions.</summary>
        public int heart
        {
            get; set;
        }
        /// <summary>Count of rocket reactions.</summary>
        public int rocket
        {
            get; set;
        }
        /// <summary>Count of eyes reactions.</summary>
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
        /// <summary>The GitHub API URL of this asset resource.</summary>
        public string url
        {
            get; set;
        }
        /// <summary>GitHub's numeric id for this asset.</summary>
        public int id
        {
            get; set;
        }
        /// <summary>GitHub's opaque global node id for this asset.</summary>
        public string node_id
        {
            get; set;
        }
        /// <summary>The file name as uploaded, e.g. `FreeAIr.vsix`.</summary>
        public string name
        {
            get; set;
        }
        /// <summary>An optional human-friendly label shown instead of <see cref="name"/> on github.com.</summary>
        public string label
        {
            get; set;
        }
        /// <summary>The account that uploaded this asset.</summary>
        public Uploader uploader
        {
            get; set;
        }
        /// <summary>The asset's MIME content type, e.g. `application/octet-stream`.</summary>
        public string content_type
        {
            get; set;
        }
        /// <summary>The asset's upload state, e.g. `uploaded`.</summary>
        public string state
        {
            get; set;
        }
        /// <summary>The asset's size in bytes.</summary>
        public int size
        {
            get; set;
        }
        /// <summary>How many times this asset has been downloaded.</summary>
        public int download_count
        {
            get; set;
        }
        /// <summary>When this asset was created.</summary>
        public DateTime created_at
        {
            get; set;
        }
        /// <summary>When this asset was last updated.</summary>
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
        /// <summary>The account's GitHub username.</summary>
        public string login
        {
            get; set;
        }
        /// <summary>GitHub's numeric id for the account.</summary>
        public int id
        {
            get; set;
        }
        /// <summary>GitHub's opaque global node id for the account.</summary>
        public string node_id
        {
            get; set;
        }
        /// <summary>URL of the account's avatar image.</summary>
        public string avatar_url
        {
            get; set;
        }
        /// <summary>The account's Gravatar id, if it uses one instead of an uploaded avatar.</summary>
        public string gravatar_id
        {
            get; set;
        }
        /// <summary>The GitHub API URL of the account resource.</summary>
        public string url
        {
            get; set;
        }
        /// <summary>The account's profile page on github.com.</summary>
        public string html_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing the account's followers.</summary>
        public string followers_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing accounts this account follows.</summary>
        public string following_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing the account's gists.</summary>
        public string gists_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing repositories the account has starred.</summary>
        public string starred_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing the account's subscriptions.</summary>
        public string subscriptions_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing organizations the account belongs to.</summary>
        public string organizations_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing the account's repositories.</summary>
        public string repos_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL template for the account's events.</summary>
        public string events_url
        {
            get; set;
        }
        /// <summary>The GitHub API URL listing events received by the account.</summary>
        public string received_events_url
        {
            get; set;
        }
        /// <summary>The account kind, e.g. `User` or `Organization`.</summary>
        public string type
        {
            get; set;
        }
        /// <summary>GitHub's finer-grained classification of the account's view type.</summary>
        public string user_view_type
        {
            get; set;
        }
        /// <summary>Whether the account is a GitHub site administrator.</summary>
        public bool site_admin
        {
            get; set;
        }
    }
}
