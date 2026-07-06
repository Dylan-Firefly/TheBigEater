public enum GameSceneId
{
    MainMenu,
    OpeningStreaming,
    Countryside,
    OtherWorld,
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
    OtherWorld,
    Indoor,
    LiveStream,
    PhoneGameplay,
    DemoEnd,
    GameOver
}

public enum OpeningChoice
{
    KeepStatus,
    TransformHome
}
