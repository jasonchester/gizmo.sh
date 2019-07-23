using System;

namespace Gizmo.Commands
{
    internal class ErrorResult : IOperationResult
    {
        private Exception ex;

        public ErrorResult(Exception ex)
        {
            this.ex = ex;
        }

        public string Message => ex.Message;

        public string Details => ex.ToString();
    }
}