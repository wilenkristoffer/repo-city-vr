namespace RepoCity {
    public class WorldIslandConfig { public string label; public string resourcePath; }
    public static class WorldConfig {
        public static readonly System.Collections.Generic.List<WorldIslandConfig> Islands = new System.Collections.Generic.List<WorldIslandConfig> {
            new WorldIslandConfig { label = "Repo City", resourcePath = "RepoCity/repo-data" },
            new WorldIslandConfig { label = "Repo City (Three.js)", resourcePath = "RepoCity/Worlds/threejs/repo-data" },
            new WorldIslandConfig { label = "Repo City (Unity staging)", resourcePath = "RepoCity/Worlds/unity-staging/repo-data" },
        };
    }
}
