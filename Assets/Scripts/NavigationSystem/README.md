# Sistema de Enjambre de Luciérnagas con Código Morse

## Descripción
Sistema de navegación visual que utiliza un enjambre de luciérnagas que parpadean en código Morse para guiar al jugador hacia un objetivo específico.

## Características
- ✨ Luciérnagas que parpadean mensajes en código Morse
- 🧭 Dirección automática hacia el objetivo (ADELANTE, DERECHA, IZQUIERDA, ARRIBA, ABAJO)
- 🎯 Activación/desactivación automática según distancia
- 💫 Movimiento flotante orgánico de las luciérnagas
- 🔦 Sistema de iluminación 2D integrado

## Componentes

### 1. MorseCodeTranslator
Clase estática que traduce texto a código Morse.

**Ejemplo de uso:**
```csharp
string morse = MorseCodeTranslator.TextToMorse("ADELANTE");
// Resultado: ".- -.. . .-.. .- -. - ."
```

### 2. FireflyGuide
Componente individual de cada luciérnaga.

**Características:**
- Parpadeo en código Morse
- Movimiento flotante suave
- Control de intensidad de luz
- Colores personalizables

### 3. FireflySwarm
Gestor del enjambre completo.

**Parámetros principales:**
- `Target Destination`: Objetivo hacia donde guiar
- `Player`: Referencia al jugador
- `Activation Distance`: Distancia para activar el enjambre
- `Deactivation Distance`: Distancia para desactivar (cerca del objetivo)
- `Firefly Count`: Número de luciérnagas en el enjambre
- `Swarm Radius`: Radio del círculo de luciérnagas

## Instalación

### Opción 1: Configuración Manual en Unity

1. **Crear el GameObject del Enjambre:**
   - En la jerarquía: `GameObject > Create Empty`
   - Nombrar: "FireflySwarm"
   - Añadir componente: `FireflySwarm`

2. **Configurar el Enjambre:**
   - Asignar `Target Destination`: El transform del objetivo (círculo amarillo)
   - Asignar `Player`: El transform del jugador
   - Ajustar `Activation Distance`: 15 (se activa a 15 unidades del objetivo)
   - Ajustar `Deactivation Distance`: 3 (se desactiva a 3 unidades)
   - Ajustar `Firefly Count`: 8 (número de luciérnagas)

3. **Crear Prefab de Luciérnaga (Opcional):**
   Si quieres usar sprites personalizados:
   - Crear GameObject con SpriteRenderer
   - Añadir componente `Light2D` (Universal Render Pipeline)
   - Añadir componente `FireflyGuide`
   - Guardar como prefab
   - Asignar en `Firefly Prefab` del FireflySwarm

### Opción 2: Configuración Rápida

El sistema creará luciérnagas automáticamente si no se asigna un prefab.

## Uso en Código

### Cambiar el objetivo dinámicamente:
```csharp
FireflySwarm swarm = GetComponent<FireflySwarm>();
swarm.SetTarget(nuevoObjetivo.transform);
```

### Cambiar el jugador:
```csharp
swarm.SetPlayer(nuevoJugador.transform);
```

### Personalizar mensajes:
En el inspector, modifica el array `Direction Messages`:
- [0] = "ADELANTE"
- [1] = "DERECHA"
- [2] = "IZQUIERDA"
- [3] = "ARRIBA"
- [4] = "ABAJO"

## Código Morse Internacional

| Letra | Morse | Letra | Morse |
|-------|-------|-------|-------|
| A | .-    | N | -.    |
| B | -... | O | ---   |
| C | -.-. | P | .--. |
| D | -..  | Q | --.- |
| E | .    | R | .-.  |
| F | ..-. | S | ...  |
| G | --.  | T | -    |
| H | .... | U | ..-  |
| I | ..   | V | ...- |
| J | .--- | W | .--  |
| K | -.-  | X | -..- |
| L | .-.. | Y | -.-- |
| M | --   | Z | --.. |

**Ejemplo:** "ADELANTE" = `.- -.. . .-.. .- -. - .`

## Timing del Código Morse
- **Punto (.)**: 0.2 segundos
- **Raya (-)**: 0.6 segundos (3x punto)
- **Espacio entre símbolos**: 0.2 segundos
- **Espacio entre letras**: 0.6 segundos
- **Espacio entre palabras**: 1.4 segundos

## Troubleshooting

### Las luciérnagas no aparecen:
- Verifica que el componente `Light2D` esté disponible (requiere Universal Render Pipeline)
- Asegúrate de que la cámara tenga el componente `Universal Additional Camera Data`

### El enjambre no se activa:
- Verifica que `Player` y `Target Destination` estén asignados
- Comprueba que la distancia entre jugador y objetivo esté dentro del rango de activación
- Revisa en Scene view los gizmos (esferas amarilla y verde)

### Las luciérnagas no parpadean:
- Asegúrate de que el componente `FireflyGuide` esté presente
- Verifica que los mensajes en `Direction Messages` no estén vacíos

## Personalización Avanzada

### Cambiar velocidad del parpadeo:
Modifica en `MorseCodeTranslator.GetSymbolDuration()` el parámetro `dotDuration`.

### Ajustar movimiento flotante:
En `FireflyGuide`:
- `floatSpeed`: Velocidad del movimiento
- `floatAmplitude`: Amplitud del movimiento

### Cambiar colores:
En `FireflyGuide`:
- `glowColor`: Color de la luz y sprite
- `maxIntensity`: Intensidad máxima
- `minIntensity`: Intensidad mínima

## Notas
- El sistema usa el tag "Player" para encontrar automáticamente al jugador si no está asignado
- Las luciérnagas se crean automáticamente en Start()
- El mensaje Morse se actualiza cada 2 segundos (configurable en `updateInterval`)
