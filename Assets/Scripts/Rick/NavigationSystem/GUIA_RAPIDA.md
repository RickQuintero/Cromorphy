# 🔥 Guía Rápida: Enjambre de Luciérnagas con Código Morse

## ⚡ Configuración en 3 Pasos

### Paso 1: Crear el Enjambre
1. En Unity, haz clic derecho en la Jerarquía
2. Selecciona `Create Empty`
3. Nómbralo "FireflySwarm_Guia"

### Paso 2: Añadir el Script
1. Selecciona el GameObject "FireflySwarm_Guia"
2. En el Inspector, haz clic en `Add Component`
3. Busca y añade: **FireflySwarmSetup**

### Paso 3: Configurar
1. En el Inspector, verás el componente `FireflySwarmSetup`
2. Arrastra tu **objetivo** (el círculo amarillo) al campo `Destination Transform`
3. Arrastra tu **jugador** al campo `Player Transform` (o déjalo vacío si tu jugador tiene el tag "Player")
4. ¡Listo! Dale Play y prueba

## 🎮 Cómo Funciona

### Activación Automática
- El enjambre aparece cuando el jugador está **entre 3 y 15 unidades** del objetivo
- Se desactiva cuando llegas muy cerca (menos de 3 unidades)
- Se desactiva si te alejas mucho (más de 15 unidades)

### Mensajes en Código Morse
Las luciérnagas parpadean mensajes según la dirección:
- **ADELANTE**: Cuando el objetivo está en la dirección general hacia adelante
- **DERECHA**: Cuando debes ir hacia la derecha
- **IZQUIERDA**: Cuando debes ir hacia la izquierda
- **ARRIBA**: Cuando el objetivo está arriba
- **ABAJO**: Cuando el objetivo está abajo

### Ejemplo de Código Morse
"ADELANTE" se traduce a: `.- -.. . .-.. .- -. - .`
- `.` = parpadeo corto (punto)
- `-` = parpadeo largo (raya)
- ` ` = pausa entre letras

## 🎨 Personalización

### Cambiar Número de Luciérnagas
En el Inspector, ajusta `Number Of Fireflies` (por defecto: 8)

### Cambiar Distancias
- `Activation Distance`: Cuándo aparece el enjambre (por defecto: 15)
- `Deactivation Distance`: Cuándo desaparece cerca del objetivo (por defecto: 3)

### Cambiar Mensajes
Puedes cambiar los mensajes en el Inspector:
- `Forward Message`: "ADELANTE"
- `Right Message`: "DERECHA"
- `Left Message`: "IZQUIERDA"
- `Up Message`: "ARRIBA"
- `Down Message`: "ABAJO"

Puedes poner cualquier texto, ¡incluso números!

## 🔧 Configuración Avanzada

### Usar tu Propio Sprite de Luciérnaga
1. Crea un prefab con:
   - SpriteRenderer (tu sprite)
   - Light2D (para el brillo)
   - FireflyGuide (el script)
2. En el componente `FireflySwarm`, asigna tu prefab en `Firefly Prefab`

### Cambiar Colores
1. Selecciona una luciérnaga individual en la jerarquía
2. En el componente `FireflyGuide`:
   - `Glow Color`: Color del brillo
   - `Max Intensity`: Brillo máximo
   - `Min Intensity`: Brillo mínimo

### Ajustar Movimiento
En el componente `FireflyGuide`:
- `Float Speed`: Velocidad del movimiento flotante
- `Float Amplitude`: Amplitud del movimiento

## 🐛 Solución de Problemas

### ❌ No veo las luciérnagas
**Solución:** 
- Asegúrate de estar usando Universal Render Pipeline (URP)
- Verifica que tu cámara tenga el componente `Universal Additional Camera Data`
- Comprueba que estés dentro del rango de activación (3-15 unidades del objetivo)

### ❌ Las luciérnagas no parpadean
**Solución:**
- Verifica que el componente `FireflyGuide` esté presente en cada luciérnaga
- Asegúrate de que los mensajes no estén vacíos

### ❌ El enjambre no sigue al jugador
**Solución:**
- Verifica que el campo `Player Transform` esté asignado
- Si usas el tag "Player", asegúrate de que tu jugador lo tenga

### ❌ Error de compilación con Light2D
**Solución:**
- Necesitas tener instalado el paquete "Universal RP" en Unity
- Ve a `Window > Package Manager` y busca "Universal RP"

## 💡 Consejos

1. **Prueba en Scene View**: Activa los Gizmos para ver los rangos de activación (esferas amarilla y verde)

2. **Múltiples Objetivos**: Puedes tener varios enjambres, uno para cada objetivo importante

3. **Cambiar Objetivo en Runtime**: 
   ```csharp
   GetComponent<FireflySwarmSetup>().ChangeDestination(nuevoObjetivo.transform);
   ```

4. **Desactivar Temporalmente**:
   ```csharp
   GetComponent<FireflySwarmSetup>().SetActive(false);
   ```

## 📝 Ejemplo de Uso en tu Juego

Imagina que tienes un checkpoint o punto de interés:

1. Crea un GameObject vacío en ese punto
2. Nómbralo "Checkpoint_01"
3. Crea el enjambre como se explicó arriba
4. Asigna "Checkpoint_01" como `Destination Transform`
5. Cuando el jugador se acerque, las luciérnagas lo guiarán automáticamente

## 🎯 Caso de Uso: Tu Imagen

Según tu imagen, donde tienes un círculo amarillo como objetivo:

1. Selecciona el GameObject del círculo amarillo (o crea un GameObject vacío en esa posición)
2. Crea el enjambre siguiendo los 3 pasos de arriba
3. Asigna ese GameObject como destino
4. El enjambre aparecerá cuando el jugador esté cerca y lo guiará con mensajes Morse

## 📚 Más Información

Para detalles técnicos completos, consulta el archivo `README.md` en la misma carpeta.

---

**¿Necesitas ayuda?** Revisa los mensajes de Debug en la consola de Unity. El sistema imprime información útil sobre su estado.
