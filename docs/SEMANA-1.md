# Semana 1: esqueleto andante

Objetivo: un APK instalable en el Quest, generado por CI, que muestre en la muñeca el precio de una unidad traído de staging.

## Checklist

**Repo**
- [ ] Crear el repo en GitHub, activar Git LFS (`git lfs install`) y subir este contenido
- [ ] Proteger `main`: PR obligatorio + CI en verde

**Servidor**
- [ ] `cd server && npm install && npm test` y commitear `package-lock.json`
- [ ] Desplegar en Render/Railway/Fly (build: `npm ci && npm run build`, start: `npm run start:prod`)
- [ ] Guardar la URL como secret `STAGING_URL` y el hook de deploy como `DEPLOY_HOOK_URL`

**Cliente**
- [ ] Crear proyecto Unity 6 LTS en `client/` y copiar `client/Assets/Umbral` y `client/Assets/Resources`
- [ ] Force Text + Visible Meta Files
- [ ] Build Profile Android: OpenXR + Meta Quest feature, XR Interaction Toolkit, texturas ASTC, IL2CPP + ARM64
- [ ] Escena `Boot`: rig XR, `CatalogClient.LoadAsync(STAGING_URL)` y un Canvas en la muñeca izquierda con el precio de la unidad `A-301` (`PriceCalculator.Total` con `CatalogQueries.DefaultSelection`)
- [ ] Probar sin internet: debe mostrar el precio del seed

**CI/CD**
- [ ] Secrets de GameCI (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`); ver https://game.ci/docs/github/activation
- [ ] El workflow `client` compila el APK y lo sube como artefacto
- [ ] Configurar el canal de lanzamiento de Meta (o sideload por `adb install` como plan B)

**Spikes (máx. 1 día cada uno)**
- [ ] Reloj solar: probar 2 sets de lightmaps con fundido en un cuarto de prueba y medir memoria y fps
- [ ] Escena de presupuesto: 100 draw calls / 500k triángulos a 72 fps en el visor

## Criterio de salida

Alguien ajeno al equipo instala el build desde el canal (o con `adb`) y ve el precio en la muñeca.
Anotar en el tablero: fps medidos, tamaño del APK, tiempo de CI.
