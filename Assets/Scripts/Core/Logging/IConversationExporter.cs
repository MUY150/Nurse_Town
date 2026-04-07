public interface IConversationExporter
{
    string FileExtension { get; }
    void Export(string filePath, ConversationSnapshot snapshot);
    void ExportAggregated(string filePath, AggregatedSession session);
}
