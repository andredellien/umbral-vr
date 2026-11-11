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

## Estado actual (Semana 1)

Hecho y verificado: servidor (4 tests, CI verde), proyecto Unity 6000.6.8f1 en `client/` (URP, XRI 3.7.0, OpenXR 1.19.0, Meta OpenXR 2.7.0), 10 tests EditMode y 1 test PlayMode pasan en local, rig compartido `Assets/Umbral/Prefabs/UmbralRig.prefab` (solo teletransporte con fundido, giro 30° sin 180°, modo sentado vía tracking Device; menú `Umbral > Build Rig Prefab`), escena `Boot` y grey-box PROVISORIO `Apartment_Greybox` de 58 m² (el GDD no da plano; reemplazar medidas en `GreyboxBuilder.cs` cuando exista), `WristPrice` en el rig (muestra A-301 sin servidor), ajustes Android aplicados (menú `Umbral > Apply Android Quest Settings`), APK compila (`Umbral.EditorTools.BuildScript.Android`), repo público `andredellien/umbral-vr`, informe en `docs/AVANCE.md/.pdf`.

Pendiente, en orden:
1. Build and Run en el Quest y medir fps (falta el visor en modo desarrollador). Ajustar posición del panel en la muñeca. Confirmar ASTC.
2. CI del cliente en rojo: la licencia `.ulf` de la Mac está atada a la máquina (`Machine bindings don't match`) y el respaldo por email/contraseña falla. Investigar el flujo de activación de licencia personal de GameCI para Unity 6.
3. Desplegar el servidor a staging (secret `DEPLOY_HOOK_URL`; hoy el deploy se omite) y canal de lanzamiento de Meta (verificar nombre y requisitos actuales).
4. Spikes de 1 día: reloj solar con 4 sets de lightmaps y escena de presupuesto (100 draw calls / 500k triángulos).

Criterio de salida de la Semana 1: alguien ajeno al equipo instala el build sin cable y ve el precio en la muñeca.
Después sigue `docs/PLAN.md` semana a semana.
