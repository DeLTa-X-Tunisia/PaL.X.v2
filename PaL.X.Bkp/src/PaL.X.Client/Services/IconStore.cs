using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace PaL.X.Client.Services
{
    public static class IconStore
    {
        private static readonly Lazy<Dictionary<string, Image>> Cache = new(() => new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase));

        public static Image? Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (Cache.Value.TryGetValue(name, out var img))
            {
                return img;
            }

            img = LoadFromResources(name);
            if (img != null)
            {
                Cache.Value[name] = img;
            }

            return img;
        }

        private static Image? LoadFromResources(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var candidates = new[]
            {
                $"PaL.X.Client.context.{name}",
                $"PaL.X.Client.gender.{name}",
                $"PaL.X.Client.message.{name}",
                $"PaL.X.Client.smiley.{name}",
                $"PaL.X.Client.smiley.138_Fire.{name}",
                $"PaL.X.Client.smiley.160_Two-hearts.{name}",
                $"PaL.X.Client.smiley.21_Slightly-happy.{name}",
                $"PaL.X.Client.smiley.221_Thumbs-up.{name}",
                $"PaL.X.Client.smiley.547_Rooster.{name}",
                $"PaL.X.Client.smiley.593_Tropical-drink.{name}",
                $"PaL.X.Client.smiley.666_Shovel.{name}",
                $"PaL.X.Client.smiley.Emoticonam.{name}"
            };

            foreach (var candidate in candidates)
            {
                using Stream? stream = assembly.GetManifestResourceStream(candidate);
                if (stream != null)
                {
                    return Image.FromStream(stream);
                }
            }

            return null;
        }
    }
}
