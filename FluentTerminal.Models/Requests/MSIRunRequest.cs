namespace FluentTerminal.Models.Requests
{
    public class MSIRunRequest : IMessage
    {
        public const byte Identifier = 20;

        byte IMessage.Identifier => Identifier;

        public string Path { get; set; }
    }
}
