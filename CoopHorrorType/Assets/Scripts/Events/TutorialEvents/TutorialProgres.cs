using UnityEngine;

public static class TutorialProgress
{
    public static bool hasSeenIntro = false;
    public static bool hasReachedCheckpoint = false;
    public static Vector3 checkpointPosition = Vector3.zero;

    public static void ResetProgress()
    {
        hasSeenIntro = false;
        hasReachedCheckpoint = false;
        checkpointPosition = Vector3.zero;
    }
}