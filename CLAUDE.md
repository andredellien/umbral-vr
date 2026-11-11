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

Hecho y **sin ejecutar todavía** (se escribió sin red ni Unity): servidor `/health` y `/catalog` con 4 tests, `PriceCalculator`, `CatalogQueries`, `CatalogClient`, 10 tests EditMode, workflows de CI, docs.

Pendiente, en orden:
1. `cd server && npm install && npm test`; commitear `package-lock.json`. Corregir lo que falle.
2. Crear el proyecto Unity 6 (plantilla VR) en `client/` sin pisar `Assets/Umbral` ni `Assets/Resources`. Correr los 10 tests EditMode y corregir errores de compilación.
3. Escena `Boot` con `WristPrice` (Canvas World Space en la muñeca izquierda): carga el catálogo y muestra el precio de la unidad `A-301`. Debe mostrar lo mismo con el servidor apagado.
4. Ajustes Android para Quest: IL2CPP, ARM64, API 29+, ASTC, OpenXR + Meta Quest Support, "Allow downloads over HTTP" = Always allowed (para probar contra la IP local).
5. Build and Run en el Quest y medir fps.
6. Subir a GitHub, secrets de GameCI, desplegar el servidor a staging, canal de lanzamiento de Meta (verificar nombre y requisitos actuales).
7. Spikes de 1 día: reloj solar con 4 sets de lightmaps y escena de presupuesto (100 draw calls / 500k triángulos).

Criterio de salida de la Semana 1: alguien ajeno al equipo instala el build sin cable y ve el precio en la muñeca.
Después sigue `docs/PLAN.md` semana a semana.
