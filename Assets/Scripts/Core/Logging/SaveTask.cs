public enum SaveTaskType
{
    SaveJson,
    SaveMarkdown,
    SaveBoth
}

public class SaveTask
{
    public ConversationSnapshot Snapshot { get; set; }
    public SaveTaskType Type { get; set; }

    public SaveTask()
    {
        Type = SaveTaskType.SaveBoth;
    }

    public SaveTask(ConversationSnapshot snapshot, SaveTaskType type = SaveTaskType.SaveBoth)
    {
        Snapshot = snapshot;
        Type = type;
    }
}
