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
            return Load("RepoCity/repo-data");
        }

        public static RepoData Load(string resourcePath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                throw new Exception(
                    "RepoCity: could not find Resources/" + resourcePath + ".json. " +
                    "Run scripts/generate-repo-data.js to generate it before building/playing the scene.");
            }

            RepoData data = JsonUtility.FromJson<RepoData>(asset.text);
            if (data.files == null) data.files = new List<FileEntry>();
            if (data.dirs == null) data.dirs = new List<string>();
            return data;
        }
    }
}
