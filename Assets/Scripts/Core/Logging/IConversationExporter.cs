public interface IConversationExporter
{
    string FileExtension { get; }
    void Export(string filePath, ConversationSnapshot snapshot);
}
