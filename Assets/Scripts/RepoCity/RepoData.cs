using System;
using System.Collections.Generic;
using UnityEngine;

namespace RepoCity
{
    [Serializable]
    public class FileEntry
    {
        public string path;
        public string dir;
        public string name;
        public string ext;
        public long size;
        public int commits;
        public string lastModified;
    }

    [Serializable]
    public class RepoData
    {
        public string repoName;
        public List<FileEntry> files;
        public List<string> dirs;
    }

    public static class RepoDataLoader
    {
        public static RepoData Load()
        {
            TextAsset asset = Resources.Load<TextAsset>("RepoCity/repo-data");
            if (asset == null)
            {
                throw new Exception(
                    "RepoCity: could not find Resources/RepoCity/repo-data.json. " +
                    "Run scripts/generate-repo-data.js to generate it before building/playing the scene.");
            }

            return JsonUtility.FromJson<RepoData>(asset.text);
        }
    }
}
