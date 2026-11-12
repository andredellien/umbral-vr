using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Umbral.Net
{
    /// <summary>
    /// ClientWebSocket sobre el relay del servidor. Las tareas se inician desde el hilo principal de Unity, así que
    /// Received y Closed también se disparan ahí (el contexto de sincronización de Unity captura las continuaciones).
    /// </summary>
    public class WebSocketTransport : ISessionTransport
    {
        private ClientWebSocket socket;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private Task sendChain = Task.CompletedTask; // ClientWebSocket admite un solo envío a la vez
        private int closedRaised;

        public bool IsOpen => socket != null && socket.State == WebSocketState.Open;
        public event Action<string> Received;
        public event Action Closed;

        public async Task ConnectAsync(string url)
        {
            socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(url), cts.Token);
            _ = ReceiveLoop();
        }

        public void Send(string json)
        {
            if (!IsOpen) return;
            var bytes = Encoding.UTF8.GetBytes(json);
            sendChain = sendChain.ContinueWith(_ => SendOne(bytes), TaskScheduler.Default).Unwrap();
        }

        private async Task SendOne(byte[] bytes)
        {
            try
            {
                if (IsOpen) await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
            }
            catch (Exception)
            {
                RaiseClosed(); // el envío falló: la conexión ya no sirve
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            var text = new StringBuilder();
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    text.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close) return;
                        text.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);
                    Received?.Invoke(text.ToString());
                }
            }
            catch (Exception)
            {
                // cierre abortado o cancelado: se informa abajo
            }
            finally
            {
                RaiseClosed();
            }
        }

        private void RaiseClosed()
        {
            if (Interlocked.Exchange(ref closedRaised, 1) == 0) Closed?.Invoke();
        }

        public void Close()
        {
            cts.Cancel();
            try { socket?.Dispose(); } catch (Exception) { /* ya cerrado */ }
            RaiseClosed();
        }
    }
}
