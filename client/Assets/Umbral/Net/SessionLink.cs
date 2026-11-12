using System;
using System.Threading.Tasks;
using Umbral.Core;
using UnityEngine;

namespace Umbral.Net
{
    // Formato del relay: {"event": "...", "data": {...}}. JsonUtility no es polimórfico: se lee primero el evento y luego el mensaje concreto.
    [Serializable] internal class EventOnly { public string @event; }
    [Serializable] internal class CodeData { public string code; }
    [Serializable] internal class MessageData { public string message; }
    [Serializable] internal class CodeMessage { public string @event; public CodeData data; }
    [Serializable] internal class StateMessage { public string @event; public SessionSnapshot data; }
    [Serializable] internal class CommandMessage { public string @event; public SessionCommand data; }
    [Serializable] internal class ErrorMessage { public string @event; public MessageData data; }

    /// <summary>Un extremo de la sala (visor o asesor) sobre un transporte. Los eventos se disparan en el hilo que recibe el transporte.</summary>
    public class SessionLink
    {
        private readonly ISessionTransport transport;

        public SessionLink(ISessionTransport transport)
        {
            this.transport = transport;
            transport.Received += OnReceived;
            transport.Closed += () => Closed?.Invoke();
        }

        public bool IsOpen => transport.IsOpen;

        public event Action<string> Hosted;
        public event Action<string> Joined;
        public event Action<SessionSnapshot> StateReceived;
        public event Action<SessionCommand> CommandReceived;
        public event Action AdvisorJoined;
        public event Action AdvisorLeft;
        public event Action HostLeft;
        public event Action<string> ErrorReceived;
        public event Action Closed;

        public Task ConnectAsync(string url) => transport.ConnectAsync(url);

        public void Host(string code) => transport.Send(JsonUtility.ToJson(new CodeMessage { @event = "host", data = new CodeData { code = code } }));
        public void Join(string code) => transport.Send(JsonUtility.ToJson(new CodeMessage { @event = "join", data = new CodeData { code = code } }));
        public void PublishState(SessionSnapshot snapshot) => transport.Send(JsonUtility.ToJson(new StateMessage { @event = "state", data = snapshot }));
        public void SendCommand(SessionCommand command) => transport.Send(JsonUtility.ToJson(new CommandMessage { @event = "command", data = command }));
        public void Close() => transport.Close();

        private void OnReceived(string json)
        {
            string name;
            try { name = JsonUtility.FromJson<EventOnly>(json)?.@event; }
            catch (ArgumentException) { return; } // JSON roto: se ignora
            switch (name)
            {
                case "hosted": Hosted?.Invoke(JsonUtility.FromJson<CodeMessage>(json).data?.code); break;
                case "joined": Joined?.Invoke(JsonUtility.FromJson<CodeMessage>(json).data?.code); break;
                case "state": StateReceived?.Invoke(JsonUtility.FromJson<StateMessage>(json).data); break;
                case "command": CommandReceived?.Invoke(JsonUtility.FromJson<CommandMessage>(json).data); break;
                case "advisor-joined": AdvisorJoined?.Invoke(); break;
                case "advisor-left": AdvisorLeft?.Invoke(); break;
                case "host-left": HostLeft?.Invoke(); break;
                case "error": ErrorReceived?.Invoke(JsonUtility.FromJson<ErrorMessage>(json).data?.message); break;
            }
        }
    }

    public static class SessionUrl
    {
        /// <summary>De la URL HTTP del servidor (la misma del catálogo) a la del relay: http -> ws, https -> wss, ruta /session.</summary>
        public static string FromServer(string serverUrl)
        {
            var url = serverUrl.TrimEnd('/');
            if (url.StartsWith("https://")) url = "wss://" + url.Substring(8);
            else if (url.StartsWith("http://")) url = "ws://" + url.Substring(7);
            return url + "/session";
        }
    }
}
