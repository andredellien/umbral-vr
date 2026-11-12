using System;
using System.Threading.Tasks;

namespace Umbral.Net
{
    /// <summary>Canal de texto con el relay. Abstracción mínima para poder probar la sesión sin red.</summary>
    public interface ISessionTransport
    {
        bool IsOpen { get; }
        event Action<string> Received;
        event Action Closed;
        Task ConnectAsync(string url);
        void Send(string json);
        void Close();
    }
}
