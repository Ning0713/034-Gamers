using UnityEngine;

public partial class PacJamDemoController
{
    private enum GhostType { Red, Pink, Blue, Orange }
    private enum GhostState { Scatter, Chase, Frightened, Eaten }

    private class Actor
    {
        public Transform Root;
        public MeshRenderer Body;
        public Transform Indicator;
        public MeshRenderer IndicatorRenderer;
        public bool BodyIsDirectional;
        public bool ShowIndicator;
        public Vector2Int Cell;
        public Vector2Int Spawn;
        public Vector2Int Dir;
        public Vector2Int WantDir;
        public Vector2Int LastDir = Vector2Int.left;
    }

    private sealed class Ghost : Actor
    {
        public GhostType Type;
        public Color NormalColor;
        public Vector2Int ScatterTarget;
        public bool Eaten;
    }

    private sealed class Pellet
    {
        public bool Power;
        public GameObject Go;
    }
}
