# Spike: presupuesto de escena (100 draw calls / 500k triángulos)

Objetivo: tener una forma repetible de medir draw calls y triángulos, y confirmar que el pipeline (URP) rinde esas cifras en el peor caso.

## Cómo se mide
- Prueba: `client/Assets/Umbral/App/Tests/BudgetTests.cs` (categoría `Spike`).
- Escena sintética: 100 objetos, cada uno con su propia malla (no se pueden agrupar), 10 materiales, ~490k triángulos, sin sombras.
- Métricas: `ProfilerRecorder` ("Draw Calls Count", "Triangles Count", "SetPass Calls Count"). Funciona en el editor, en batchmode con GPU y en builds de desarrollo, así que la misma medición se podrá repetir en el Quest.
- Requiere GPU: con `-nographics` la prueba se omite (así no rompe el CI).

Comando:

```bash
Unity -batchmode -projectPath client -runTests -testPlatform PlayMode -testFilter BudgetTests
```

## Resultado (Mac, Apple M1 Pro)
| Métrica | Valor | Presupuesto |
|---|---|---|
| Draw calls | 101 | 100 |
| Triángulos renderizados | 491,680 | 500,000 |
| SetPass calls | 2 | n/a |

El 101 incluye el cielo; los 100 objetos aportan 100.

## Qué NO demuestra
- **No es rendimiento del Quest.** Mide cuánto dibuja la escena, no cuántos fps da en un Quest 3. Eso solo se sabe con el visor.
- **Una sola vista.** El visor renderiza dos ojos; con Single Pass Instanced los draw calls no se duplican, pero no está verificado aquí.
- **Sin sombras, luces extra, postproceso ni interfaz.** Todo eso suma y debe caber dentro del presupuesto real.

## Regla práctica
El contenido real (departamento, mobiliario, acabados) debe medirse con la misma prueba, y cada escena nueva debe quedar bajo 100 draw calls y 500k triángulos. Un material compartido y mallas combinadas bajan los draw calls.
