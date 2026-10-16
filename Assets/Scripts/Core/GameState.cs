namespace LastStep.Core
{
    public enum GameState
    {
        Boot, MainMenu, Intro, Preparing, Countdown, Fire, Finisher, Resolving,
        Transition, Victory, Defeat, Paused
    }

    public enum GameMode { PC, VR }
    public enum DominantHand { Right, Left }
}
