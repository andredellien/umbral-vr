# UMBRAL VR: contexto para Claude Code

Proyecto universitario (Univalle, Realidad Virtual y Aumentada). Sala de ventas inmersiva en VR para departamentos en preventa.
Diseño completo: `docs/GDD_UMBRAL_VR.pdf`. Plan: `docs/PLAN.md`. Decisiones: `docs/ADR-001-decisiones-iniciales.md`.
Equipo de 3 personas; el usuario lleva el desarrollo. Idioma de trabajo: español.

## Cómo quiero que trabajes

- Arquitecto senior pero práctico: **nada de sobreingeniería**. Lo mínimo que cumpla el GDD y funcione bien.
- Entrega continua: trunk-based, ramas cortas, `main` siempre desplegable, un build instalable cada viernes.
- Antes de decir que algo funciona, ejecútalo (tests, build, servidor). No afirmes lo que no verificaste.
- No añadas funciones fuera de la sección 14 del GDD ("alcance del prototipo").

## Stack

- `client/`: Unity 6 LTS, URP, OpenXR, XR Interaction Toolkit. Meta Quest 3/3S. Quest y tablet del asesor comparten código.
- `server/`: NestJS 11 + (luego) PostgreSQL. Hoy sirve `server/seed/catalog.json` en `GET /catalog`.
- CI/CD: GitHub Actions (`.github/workflows/server.yml`, `client.yml` con GameCI).

## Convenciones del cliente

- La lógica vive en `Umbral.Core` (C# puro, **sin UnityEngine**), con tests EditMode en `Core/Tests`.
- `Umbral.Net` maneja red y caché. El catálogo carga: servidor → caché en disco → `Resources/catalog_seed.json`.
- `client/Assets/Resources/catalog_seed.json` debe ser copia de `server/seed/catalog.json`. Si cambias uno, cambia el otro.
- Precios en bolivianos enteros (`long`). Precio = base de la unidad + diferencias de acabados. **Sin descuentos ni plan de pago.**
- Comodidad VR: solo teletransporte con fundido, giro en pasos de 30°, 72 fps mínimo, interfaz anclada a la muñeca o al objeto, nunca fija frente a los ojos.
- Commitea siempre los `.meta`. Una persona por escena a la vez; lo compartido va en prefabs.

## Estado actual (Semana 7)

Hecho y verificado: servidor (19 tests: relay WebSocket, reservas y eventos; reservas y eventos en archivos bajo `server/data/` porque no hay PostgreSQL ni Docker, ADR #12), proyecto Unity 6000.6.8f1 en `client/` (URP, XRI 3.7.0, OpenXR 1.19.0, Meta OpenXR 2.7.0), 53 tests EditMode y 53 PlayMode (más las capturas, que solo corren con `UMBRAL_SHOTS`/`UMBRAL_SERVER`; la prueba de reserva real necesita `UMBRAL_SERVER` con el servidor corriendo y datos limpios) (sin `-nographics`; con él se omiten los de GPU) pasan en local, rig compartido `Assets/Umbral/Prefabs/UmbralRig.prefab` (solo teletransporte con fundido, giro 30° sin 180°, modo sentado vía tracking Device; menú `Umbral > Build Rig Prefab`), escena `Boot` y grey-box PROVISORIO `Apartment_Greybox` de 58 m² (el GDD no da plano; reemplazar medidas en `GreyboxBuilder.cs` cuando exista), `WristPrice` en el rig (precio en vivo; formato fijo "Bs 520,000" sin depender del idioma), acabados: `Core/Configurator` (estado + total) compartido por `ConfiguratorHost`, muestras 3D `FinishSwatch` (XRSimpleInteractable) en tres paletas del grey-box (piso, pared oeste, mesón), colores solo en cliente (`FinishPalette`; toda opción del catálogo debe tener color), reloj solar en el grey-box (4 paradas 7/12/16/19 h: lightmaps horneados + ambiente + tinte del cielo por parada, fundido compartido; sin luces en tiempo real; **tras regenerar la escena con `Build Greybox Scene` hay que correr `Umbral > Bake Greybox Solar`**, ~4 min, requiere GPU), ventana 360° con panorama PROVISORIO generado por código (`WindowView`; reemplazar por fotos reales de dron, una por piso), cinta métrica (`TapeMeasure`: gatillo derecho marca la posición del control, 2 puntos = cm) y botones 3D de la muñeca izquierda (`WristButton`: 4 paradas + interruptor de la cinta), ajustes Android aplicados (menú `Umbral > Apply Android Quest Settings`), APK compila (`Umbral.EditorTools.BuildScript.Android`), repo público `andredellien/umbral-vr`, informe en `docs/AVANCE.md/.pdf`. Flujo de escenas (semana 5): `Lobby` (tutorial de 3 pasos: apuntar, teletransportar, agarrar; botones de pie/sentado; el área segura es de Guardian y solo se indica) → `Maqueta` (torre sobre mesa, un bloque por unidad: disponible en verde, reservada ámbar, vendida gris; solo las disponibles se eligen) → `Apartment_Greybox` (llegada: `ArrivalPoint` coloca al visitante en la puerta; el precio de la muñeca sigue la unidad elegida). `SceneFlow.GoTo` oscurece, carga y la escena nueva aclara sola (sin vuelo de cámara); estado entre escenas en `Session` sobre `Core/SessionFlow` (salto por índice listo para la tablet de la semana 6). Catálogo v2: `status` por unidad (vacío = disponible) y el seed del build gana a una caché de versión menor. Escenas generadas con `Umbral > Build Lobby Scene` / `Build Maqueta Scene`; el build arranca en el Lobby (`SceneRegistry`). No existe aún cómo volver a la maqueta desde la llegada.

App del asesor (semana 6): el visor es anfitrión de una sala por código de 4 dígitos (`SessionHost`, vive entre escenas; el código se ve en el tablero del lobby y en la muñeca) y el servidor NestJS hace de relay (`/session`, `{"event","data"}`, `RoomsService`; estado visor→asesor, comandos asesor→visor, el asesor tardío recibe el último estado). El visor publica `Core/SessionSnapshot` cuando cambia (sondeo a 4 Hz) y ejecuta `SessionCommand` (pausa, reanudar, saltar de etapa; sin descuentos). Pausa: negro + interactores/locomoción apagados + cartel por encima del fundido; se levanta sola si se va el asesor o se cae el canal; los saltos se ignoran en pausa. Sin servidor el comprador usa la app igual (reintenta con espera; solo `LogWarning`). La URL del relay sale de `SessionHost.ServerUrl` (mismo servidor del catálogo, `http://localhost:3000`: en el Quest hay que cambiarla por la IP de la PC). Escena `Asesor` (`Umbral > Build Advisor Scene`, UGUI Screen Space - Camera, `AdvisorView`): se une con el código, refleja el estado y manda comandos; NO está en la lista de escenas del APK del Quest. Capturas de informe: `UMBRAL_SHOTS=docs/img` con los tests `Shots`; interop con el servidor real: `UMBRAL_SERVER=http://localhost:3000` (test `Interop`). `ClientWebSocket` no está verificado con IL2CPP en el visor (puede hacer falta `link.xml`).

Semana 7: telemetría con cola local JSONL (`Net/TelemetryQueue`, GUID por evento, `TelemetryHost` envía lotes a `POST /events` con espera creciente y el servidor deduplica por id; eventos: inicio/fin de sesión, etapa, acabado, herramienta, reserva); reservas con `POST /reservations` (el servidor recalcula el total, 409 si la unidad no está disponible, `GET /catalog` superpone las reservas sin tocar el seed) y botón `Reservar` con ficha mínima en el grey-box (`Umbral > Add Greybox Reserve Station` lo añade a la escena horneada sin re-hornear). Arreglos de la revisión de la semana 5 aplicados (caché ilegible, aire entre bloques de la maqueta, llegada compensando el visor). Sin verificar: `ClientWebSocket`/envío HTTP bajo IL2CPP en el Quest, URL del servidor en el visor (`SessionHost.ServerUrl` es localhost), botón de reserva y ficha en VR, y las unidades que un servidor agregue tras generar la maqueta no tienen bloque.

Pendiente, en orden:
1. Build and Run en el Quest y medir fps (falta el visor en modo desarrollador). Ajustar posición del panel en la muñeca. Confirmar ASTC.
2. CI del cliente en rojo: la licencia `.ulf` de la Mac está atada a la máquina (`Machine bindings don't match`) y el respaldo por email/contraseña falla. Investigar el flujo de activación de licencia personal de GameCI para Unity 6.
3. Desplegar el servidor a staging (secret `DEPLOY_HOOK_URL`; hoy el deploy se omite) y canal de lanzamiento de Meta (verificar nombre y requisitos actuales).
4. Spikes de 1 día: reloj solar con 4 sets de lightmaps y escena de presupuesto (100 draw calls / 500k triángulos).

Criterio de salida de la Semana 1: alguien ajeno al equipo instala el build sin cable y ve el precio en la muñeca.
Después sigue `docs/PLAN.md` semana a semana.
