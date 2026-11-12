using System;
using System.Text;

namespace Umbral.Core
{
    [Serializable]
    public class SelectionPair
    {
        public string surface;
        public string option;
    }

    /// <summary>Lo que se manda a POST /reservations. Sin total: el servidor lo calcula.</summary>
    [Serializable]
    public class ReservationRequestDto
    {
        public string unitId;
        public SelectionPair[] selections;
    }

    /// <summary>Respuesta del servidor a una reserva creada.</summary>
    [Serializable]
    public class ReservationDto
    {
        public string id;
        public string unitId;
        public SelectionPair[] selections;
        public long total;
        public string createdAt;
    }

    /// <summary>Ficha mínima de reserva: unidad, acabados, total y código. Sin descuentos ni plan de pago (ADR #6).</summary>
    public static class Ficha
    {
        public static SelectionPair[] SelectionsOf(Configurator configurator)
        {
            var surfaces = configurator.Catalog.surfaces;
            var list = new SelectionPair[surfaces.Length];
            for (var i = 0; i < surfaces.Length; i++)
                list[i] = new SelectionPair { surface = surfaces[i].id, option = configurator.SelectedOptionId(surfaces[i].id) };
            return list;
        }

        public static string Text(Catalog catalog, ReservationDto r)
        {
            var sb = new StringBuilder();
            sb.Append("Ficha de reserva\n").Append("Unidad ").Append(r.unitId);
            foreach (var s in r.selections)
            {
                var surface = CatalogQueries.FindSurface(catalog, s.surface);
                sb.Append('\n').Append(surface.name).Append(": ").Append(CatalogQueries.FindOption(catalog, s.surface, s.option).name);
            }
            sb.Append("\nTotal: ").Append(PriceFormat.Bolivianos(r.total));
            sb.Append("\nCodigo ").Append(r.id.Length > 8 ? r.id.Substring(0, 8) : r.id);
            return sb.ToString();
        }
    }
}
