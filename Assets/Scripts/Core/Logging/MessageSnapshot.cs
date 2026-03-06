using System;

[Serializable]
public class MessageSnapshot
{
    public string Role { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }

    public MessageSnapshot()
    {
        Timestamp = DateTime.Now;
    }

    public MessageSnapshot(string role, string content)
    {
        Role = role;
        Content = content;
        Timestamp = DateTime.Now;
    }

    public MessageSnapshot(string role, string content, DateTime timestamp)
    {
        Role = role;
        Content = content;
        Timestamp = timestamp;
    }
}
