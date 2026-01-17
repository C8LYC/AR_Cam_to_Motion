using UnityEngine;

public static class SkeletonRootProvider
{
    public static Transform CurrentRoot { get; private set; }

    public static void SetCurrentRoot(Transform root)
    {
        CurrentRoot = root;
    }

    public static void ClearIfMatches(Transform root)
    {
        if (CurrentRoot == root)
            CurrentRoot = null;
    }
}
