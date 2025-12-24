using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using PaL.X.Client.Properties;

namespace PaL.X.Client.Services
{
    internal static class ResourceImageStore
    {
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> _smileyIndex = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>>(BuildSmileyIndex);
    private static readonly Lazy<IDictionary<string, string>> _smileyFileLookup = new Lazy<IDictionary<string, string>>(BuildSmileyFileLookup);

        internal static Image? LoadImage(string resourceKey)
        {
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                return null;
            }

            // 1. Try loading from Resources FIRST (Priority to embedded resources)
            try
            {
                // Try exact match
                var resource = Properties.Resources.GetObject(resourceKey);
                
                // Try with 'icon/' prefix (for imported icons)
                if (resource == null)
                {
                    string prefixedKey = "icon/" + resourceKey;
                    resource = Properties.Resources.GetObject(prefixedKey);
                }

                // Try replacing slashes with underscores (common resx convention)
                if (resource == null)
                {
                    string normalizedKey = resourceKey.Replace("/", "_").Replace("\\", "_").Replace(".", "_");
                    resource = Properties.Resources.GetObject(normalizedKey);
                }

                if (resource != null)
                {
                    switch (resource)
                    {
                        case Bitmap bmp:
                            return (Image)bmp.Clone();
                        case Image image:
                            return (Image)image.Clone();
                        case Icon icon:
                            return icon.ToBitmap();
                        case byte[] buffer:
                            using (var ms = new MemoryStream(buffer, writable: false))
                            {
                                return Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: true);
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ResourceImageStore] Failed to load from resources {resourceKey}: {ex.Message}");
            }

            // 2. Fallback to file system if it looks like a path
            if (resourceKey.Contains("/") || resourceKey.Contains("\\") || resourceKey.Contains("."))
            {
                try
                {
                    string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourceKey);
                    if (File.Exists(localPath))
                    {
                        using var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        using var temp = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: true);
                        return new Bitmap(temp);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ResourceImageStore] Failed to load from file {resourceKey}: {ex.Message}");
                }
            }

            return null;
        }

        internal static Image? LoadImage(string resourceKey, Size targetSize)
        {
            var image = LoadImage(resourceKey);
            if (image == null)
            {
                return null;
            }

            if (image.Width == targetSize.Width && image.Height == targetSize.Height)
            {
                return image;
            }

            if (ImageAnimator.CanAnimate(image))
            {
                return image;
            }

            try
            {
                var resized = new Bitmap(targetSize.Width, targetSize.Height);
                using var graphics = Graphics.FromImage(resized);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, new Rectangle(Point.Empty, targetSize));
                return resized;
            }
            finally
            {
                image.Dispose();
            }
        }

        internal static Image? LoadStaticImage(string resourceKey, Size targetSize)
        {
            var image = LoadImage(resourceKey);
            if (image == null)
            {
                return null;
            }

            try
            {
                var resized = new Bitmap(targetSize.Width, targetSize.Height);
                using var graphics = Graphics.FromImage(resized);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, new Rectangle(Point.Empty, targetSize));
                return resized;
            }
            finally
            {
                image.Dispose();
            }
        }

        internal static IReadOnlyDictionary<string, IReadOnlyList<string>> GetSmileyCategories(bool isAdmin = false)
        {
            var allCategories = _smileyIndex.Value;
            
            if (isAdmin)
            {
                return allCategories; // Admin voit tout
            }

            // User normal voit seulement Basic_Smiley, Basic_square, Basic_Bleu
            var basicCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Basic_Smiley",
                "Basic_square",
                "Basic_Bleu"
            };

            return allCategories
                .Where(kvp => basicCategories.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        }

        internal static IReadOnlyList<string> GetSmileysForCategory(string category)
        {
            if (_smileyIndex.Value.TryGetValue(category, out var values))
            {
                return values;
            }

            return Array.Empty<string>();
        }

        internal static bool TryGetSmileyResource(string fileName, out string resourceKey)
        {
            resourceKey = string.Empty;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            return _smileyFileLookup.Value.TryGetValue(fileName, out resourceKey);
        }

        internal static IEnumerable<string> EnumerateResources(string prefix)
        {
            var resourceSet = Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
            foreach (DictionaryEntry entry in resourceSet)
            {
                if (entry.Key is not string key)
                {
                    continue;
                }

                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    yield return key;
                }
            }
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildSmileyIndex()
        {
            var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var resourceSet = Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);

            foreach (DictionaryEntry entry in resourceSet)
            {
                if (entry.Key is not string key)
                {
                    continue;
                }

                if (!key.StartsWith("smiley/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var segments = key.Split('/');
                if (segments.Length < 3)
                {
                    continue;
                }

                var category = segments[1];
                if (!groups.TryGetValue(category, out var list))
                {
                    list = new List<string>();
                    groups[category] = list;
                }

                list.Add(key);
            }

            foreach (var list in groups.Values)
            {
                list.Sort(StringComparer.OrdinalIgnoreCase);
            }

            return groups.ToDictionary(static kvp => kvp.Key, static kvp => (IReadOnlyList<string>)kvp.Value, StringComparer.OrdinalIgnoreCase);
        }

        private static IDictionary<string, string> BuildSmileyFileLookup()
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var resourceSet = Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);

            foreach (DictionaryEntry entry in resourceSet)
            {
                if (entry.Key is not string key)
                {
                    continue;
                }

                if (!key.StartsWith("smiley/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = key.Split('/').LastOrDefault();
                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }

                if (!lookup.ContainsKey(fileName))
                {
                    lookup[fileName] = key;
                }
            }

            return lookup;
        }

        internal static string GetCategoryDisplayName(string categoryKey)
        {
            return categoryKey switch
            {
                "Basic_Smiley" => "😊 Smileys",
                "Basic_square" => "🟦 Carrés",
                "Basic_Bleu" => "💙 Bleus",
                "Prem_Activities" => "⚽ Activités",
                "Prem_Birds" => "🐦 Oiseaux",
                "Prem_Black" => "⚫ Noirs",
                "Prem_Blue" => "🔵 Bleu Premium",
                "Prem_Food" => "🍕 Nourriture",
                "Prem_Love" => "💖 Amour",
                "Gold" => "👑 Gold",
                _ => categoryKey
            };
        }
    }
}

