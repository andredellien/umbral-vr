# UMBRAL VR

Sala de ventas inmersiva para departamentos en preventa (Meta Quest + tablet del asesor + backend).
Referencia de diseño: `docs/GDD_UMBRAL_VR.pdf`. Plan de entregas: `docs/PLAN.md`.

## Estructura

```
client/    Proyecto Unity 6 LTS (Quest y tablet del asesor comparten código)
server/    API NestJS (catálogo, telemetría, reservas)
content/   Fuentes de Blender y materiales (Git LFS)
docs/      GDD, plan, decisiones (ADR) y checklists semanales
.github/   CI/CD
```

## Cómo trabajamos

- Trunk-based: ramas cortas (< 2 días), PR con CI en verde, `main` siempre desplegable.
- Cada viernes sale un build instalable en el Quest y se prueba con alguien ajeno al equipo.
- Definition of Done: corre en el Quest a 72 fps, pasan los tests y se instala sin cable.

## Arranque rápido

Servidor:

```bash
cd server
npm install        # genera package-lock.json: commitearlo
npm run start:dev  # http://localhost:3000/catalog
npm test
```

Cliente:

1. Crea el proyecto en `client/` con Unity Hub (Unity 6 LTS, plantilla "Universal 3D" o "VR").
2. Copia encima las carpetas de `client/Assets/` de este repo (`Umbral/` y `Resources/`).
3. Sigue `docs/SEMANA-1.md`.

## Dos reglas para que Unity no duela en equipo

- Settings → Editor → Asset Serialization = **Force Text**; Version Control = **Visible Meta Files**.
- Una persona por escena a la vez; el trabajo compartido va en prefabs.
