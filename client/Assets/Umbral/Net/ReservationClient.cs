using System;
using Umbral.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Umbral.Net
{
    public enum ReservationStatus { Ok, Unavailable, Error }

    public class ReservationOutcome
    {
        public ReservationStatus Status;
        public ReservationDto Reservation;
        public string Message;
    }

    /// <summary>POST /reservations. El total no viaja: lo calcula el servidor. 409 = la unidad ya no está disponible.</summary>
    public static class ReservationClient
    {
        public static async Awaitable<ReservationOutcome> ReserveAsync(string baseUrl, string unitId, SelectionPair[] selections)
        {
            try
            {
                var body = JsonUtility.ToJson(new ReservationRequestDto { unitId = unitId, selections = selections });
                using var request = new UnityWebRequest(baseUrl.TrimEnd('/') + "/reservations", "POST")
                {
                    uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body)),
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = 5,
                };
                request.SetRequestHeader("Content-Type", "application/json");
                await request.SendWebRequest();

                if (request.responseCode == 201)
                    return new ReservationOutcome { Status = ReservationStatus.Ok, Reservation = JsonUtility.FromJson<ReservationDto>(request.downloadHandler.text) };
                if (request.responseCode == 409)
                    return new ReservationOutcome { Status = ReservationStatus.Unavailable, Message = "La unidad ya no esta disponible" };
                return new ReservationOutcome { Status = ReservationStatus.Error, Message = $"Servidor: {request.responseCode} {request.error}" };
            }
            catch (Exception e)
            {
                return new ReservationOutcome { Status = ReservationStatus.Error, Message = e.Message };
            }
        }
    }
}
