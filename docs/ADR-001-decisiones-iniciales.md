# ADR-001: Decisiones iniciales

Estado: aceptado. Se revisa si una decisión nos frena.

| # | Decisión | Por qué |
|---|---|---|
| 1 | El "espejo" del asesor envía **estado** por WebSocket (posición, acabados, hora del sol) y la tablet dibuja un plano 2D | Transmitir video es caro y frágil en Wi-Fi de sala |
| 2 | Addressables desde el día 1, contenido local hasta el piloto | Estructura correcta sin montar hosting todavía |
| 3 | Sin panel web en el slice: el catálogo (acabados y precios) vive en `server/seed/catalog.json` | El panel no valida nada del GDD que podamos probar en el semestre |
| 4 | TV por casting nativo del Quest; ficha de reserva por correo; webhook al CRM después | Integraciones sin cliente real no aportan |
| 5 | Reloj solar con **4 paradas** y fundido, no movimiento continuo | Cabe en luz horneada, respeta la regla de confort |
| 6 | **Precio en vivo se mantiene**, en versión mínima: base + diferencias de acabados. **Sin descuento del asesor ni plan de pago** | Es la mecánica central del GDD y cuesta poco; el resto no aporta al slice |
| 7 | Precios en **bolivianos enteros** (`long`) | Evita errores de punto flotante |
| 8 | Código de lógica en `Umbral.Core` (C# puro, sin UnityEngine) | Se prueba sin abrir el editor y en CI |
| 9 | Catálogo: red → caché en disco → seed incluido en el build | La app funciona sin internet, como pide el GDD |
| 10 | La primera opción de cada superficie es la incluida en el precio base (delta 0) | Convención simple para calcular y mostrar "No incluido" |

## Efecto sobre el GDD

Quedan fuera del slice: el descuento desde la tablet (7.1) y el plan de pago estimado en "La llave".
Los precios de ejemplo son inventados: no hace falta validar nada comercial.

## Pendiente de validar

- Cómo implementar el reloj solar en Quest (spike semana 1): lightmaps por parada vs. alternativas.
- Nombre y requisitos actuales del canal de lanzamiento de Meta para instalar sin cable.
