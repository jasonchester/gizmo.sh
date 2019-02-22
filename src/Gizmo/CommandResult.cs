namespace Gizmo
{
    public class CommandResult : IOperationResult
    {

        public static CommandResult Empty => new CommandResult("" , "");

        public CommandResult(string message, string details = "")
        {
            Message = message;
            Details = details;
        }
        public string Message {get;}

        public string Details {get;}
    }
}