using System.Collections.Generic;
using UnityEngine;

namespace RepoCity {

    public struct BuildingCategory {
        public string label;
        public Color color;

        public BuildingCategory(string label, Color color) {
            this.label = label;
            this.color = color;
        }
    }

    public static class BuildingPalette {

        public static readonly BuildingCategory DefaultCategory = new BuildingCategory("Other", new Color(0x7a / 255f, 0x8a / 255f, 0x99 / 255f));

        static readonly Dictionary<string, BuildingCategory> LanguageColors = new Dictionary<string, BuildingCategory> {
            { "js", new BuildingCategory("JavaScript", new Color(0xf0 / 255f, 0xdb / 255f, 0x4f / 255f)) },
            { "mjs", new BuildingCategory("JavaScript", new Color(0xf0 / 255f, 0xdb / 255f, 0x4f / 255f)) },
            { "cjs", new BuildingCategory("JavaScript", new Color(0xf0 / 255f, 0xdb / 255f, 0x4f / 255f)) },
            { "jsx", new BuildingCategory("JavaScript (React)", new Color(0xf7 / 255f, 0xc9 / 255f, 0x48 / 255f)) },
            { "ts", new BuildingCategory("TypeScript", new Color(0x4f / 255f, 0x86 / 255f, 0xf7 / 255f)) },
            { "tsx", new BuildingCategory("TypeScript (React)", new Color(0x5a / 255f, 0xa0 / 255f, 0xff / 255f)) },
            { "html", new BuildingCategory("HTML", new Color(0xe8 / 255f, 0x70 / 255f, 0x3a / 255f)) },
            { "css", new BuildingCategory("CSS", new Color(0x5f / 255f, 0xb4 / 255f, 0xe8 / 255f)) },
            { "scss", new BuildingCategory("CSS", new Color(0x5f / 255f, 0xb4 / 255f, 0xe8 / 255f)) },
            { "json", new BuildingCategory("Config / Data", new Color(0x9a / 255f, 0x9a / 255f, 0x9a / 255f)) },
            { "md", new BuildingCategory("Docs", new Color(0xde / 255f, 0xde / 255f, 0xde / 255f)) },
            { "py", new BuildingCategory("Python", new Color(0x5f / 255f, 0xd0 / 255f, 0xa0 / 255f)) },
            { "go", new BuildingCategory("Go", new Color(0x5f / 255f, 0xd0 / 255f, 0xe0 / 255f)) },
            { "rs", new BuildingCategory("Rust", new Color(0xd9 / 255f, 0x8f / 255f, 0x5f / 255f)) },
            { "java", new BuildingCategory("Java", new Color(0xd9 / 255f, 0x6f / 255f, 0x5f / 255f)) },
            { "c", new BuildingCategory("C / C++", new Color(0x8f / 255f, 0x8f / 255f, 0xd9 / 255f)) },
            { "cpp", new BuildingCategory("C / C++", new Color(0x8f / 255f, 0x8f / 255f, 0xd9 / 255f)) },
            { "h", new BuildingCategory("C / C++", new Color(0x8f / 255f, 0x8f / 255f, 0xd9 / 255f)) },
            { "svg", new BuildingCategory("Image", new Color(0xc7 / 255f, 0x7f / 255f, 0xd9 / 255f)) },
            { "png", new BuildingCategory("Image", new Color(0xc7 / 255f, 0x7f / 255f, 0xd9 / 255f)) },
            { "jpg", new BuildingCategory("Image", new Color(0xc7 / 255f, 0x7f / 255f, 0xd9 / 255f)) },
            { "yml", new BuildingCategory("Config / Data", new Color(0x9a / 255f, 0x9a / 255f, 0x9a / 255f)) },
            { "yaml", new BuildingCategory("Config / Data", new Color(0x9a / 255f, 0x9a / 255f, 0x9a / 255f)) },
            { "cs", new BuildingCategory("C#", new Color(0x68 / 255f, 0xa0 / 255f, 0x63 / 255f)) },
        };

        public static BuildingCategory CategoryForExt(string ext) {
            if (string.IsNullOrEmpty(ext)) {
                return DefaultCategory;
            }
            string key = ext.ToLowerInvariant();
            BuildingCategory category;
            if (LanguageColors.TryGetValue(key, out category)) {
                return category;
            }
            return DefaultCategory;
        }

        public static List<BuildingCategory> CategoriesInUse(IEnumerable<FileEntry> files) {
            List<BuildingCategory> result = new List<BuildingCategory>();
            HashSet<string> seenLabels = new HashSet<string>();
            foreach (FileEntry file in files) {
                BuildingCategory category = CategoryForExt(file.ext);
                if (seenLabels.Add(category.label)) {
                    result.Add(category);
                }
            }
            return result;
        }
    }
}
