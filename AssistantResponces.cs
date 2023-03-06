using System.Collections.Generic;
using System;

namespace VoiceAssistant
{
    public sealed class AssistantResponces
    {
        public List<string> ActivationAssistantWord_Response;

        public List<string> Hello_Response;
        public List<string> HowAreYou_Response;
        public List<string> TimeQuery_Response;
        public List<string> StopTalking_Response;
        public List<string> StopListening_Response;
        public List<string> ThankYou_Response;
        public List<string> Cancell_Response;
        public List<string> ExceptionsLogo_Response;
        public List<string> OK_Response;

        public AssistantResponces()
        {
            ActivationAssistantWord_Response = new List<string>()
            {
                "I'm here", "I'm waiting for commands"
            };

            Hello_Response = new List<string>()
            {
                "Hi", "Hello", "glad to hear you"
            };

            HowAreYou_Response = new List<string>()
            {
                "I am working normally", "And how do I know"
            };

            TimeQuery_Response = new List<string>()
            {
                $"{DateTime.UtcNow: hh mm yyyy}"
            };

            StopTalking_Response = new List<string>()
            {
                "Ok", "Fine", "I will be quite", "No problems"
            };

            StopListening_Response = new List<string>()
            {
                "If you need me, just ask"
            };

            ThankYou_Response = new List<string>()
            {
                "No problems", "I am glad to help you!", "You will have to"
            };

            Cancell_Response = new List<string>()
            {
                "Cancellation successfull", "Cancelling the operation", "Cancelled successfully", "Operation was cancelled"
            };

            ExceptionsLogo_Response = new List<string>()
            {
                "Operation cannot bhe perfomed", "Unable to execute command"
            };

            OK_Response = new List<string>()
            {
                "Ok", "No problems", "I will do"
            };
        }
    }
}
