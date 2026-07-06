public enum GameSceneId
{
    MainMenu,
    OpeningStreaming,
    Countryside,
    Indoor,
    LiveStreamGameplay,
    PhoneGameplay
}

public enum GameFlowState
{
    None,
    MainMenu,
    OpeningLive,
    Cg1,
    OpeningChoice,
    TransformDialogue,
    Cg2,
    Countryside,
    Indoor,
    LiveStream,
    PhoneGameplay,
    GameOver
}

public enum OpeningChoice
{
    KeepStatus,
    TransformHome
}
