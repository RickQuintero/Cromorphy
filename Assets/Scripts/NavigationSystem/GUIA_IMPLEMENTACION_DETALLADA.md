# 📘 Guía de Implementación Detallada - Sistema de Luciérnagas con Código Morse

Esta guía te explicará paso por paso, de forma detallada, cómo implementar el sistema de luciérnagas que te guiará hacia objetivos en tu juego.

---

## 📚 PARTE 1: ENTENDIENDO EL SISTEMA

### ¿Qué es este sistema?

Este sistema crea un **enjambre de luciérnagas** que:
1. Aparece automáticamente cuando el jugador está cerca de un objetivo
2. Se posiciona en la dirección hacia donde debe ir el jugador
3. Parpadea mensajes en código Morse indicando la dirección (ADELANTE, DERECHA, etc.)
4. Desaparece cuando llegas al objetivo o te alejas mucho

### ¿Cómo funciona técnicamente?

El sistema tiene **3 componentes principales**:

**1. MorseCodeTranslator** (Traductor)
- Es una clase que convierte texto normal a código Morse
- Ejemplo: "ADELANTE" → ".- -.. . .-.. .- -. - ."
- No necesitas hacer nada con esta clase, funciona automáticamente

**2. FireflyGuide** (Luciérnaga Individual)
- Controla UNA luciérnaga
- Hace que parpadee en código Morse
- Le da movimiento flotante suave
- Controla su luz y brillo

**3. FireflySwarm** (Enjambre Completo)
- Crea y gestiona TODAS las luciérnagas
- Calcula la dirección hacia el objetivo
- Decide qué mensaje mostrar (ADELANTE, DERECHA, etc.)
- Activa/desactiva el enjambre según la distancia

---

## 🎯 PARTE 2: REQUISITOS PREVIOS

Antes de empezar, necesitas verificar algunas cosas en tu proyecto de Unity.

### Requisito 1: Universal Render Pipeline (URP)

Este sistema usa luces 2D, que solo funcionan con URP.

**¿Cómo verificar si tienes URP?**
1. Ve al menú: `Edit > Project Settings`
2. En la ventana que se abre, busca la sección `Graphics`
3. Mira el campo `Scriptable Render Pipeline Settings`
4. Si dice algo como "UniversalRenderPipelineAsset" o "URP", ¡perfecto!
5. Si dice "None" o está vacío, necesitas configurar URP

**¿Cómo instalar URP si no lo tienes?**
1. Ve a `Window > Package Manager`
2. En la esquina superior izquierda, cambia el dropdown a `Unity Registry`
3. Busca "Universal RP" en la lista
4. Haz clic en "Universal RP" y luego en el botón `Install`
5. Espera a que se instale
6. Ve a `Assets > Create > Rendering > URP Asset (with 2D Renderer)`
7. Guarda el asset con un nombre como "MyURPSettings"
8. Ve a `Edit > Project Settings > Graphics`
9. Arrastra el asset que creaste al campo `Scriptable Render Pipeline Settings`

### Requisito 2: Tu jugador debe tener el tag "Player"

**¿Cómo verificar y configurar el tag?**
1. En la jerarquía de Unity, selecciona tu GameObject del jugador
2. En el Inspector, en la parte superior, verás un dropdown que dice "Tag"
3. Si dice "Player", ¡perfecto!
4. Si dice "Untagged" u otra cosa:
   - Haz clic en el dropdown "Tag"
   - Selecciona "Player"
   - Si no existe "Player", selecciona "Add Tag..."
   - Haz clic en el botón "+"
   - Escribe "Player" y guarda
   - Vuelve a tu jugador y asígnale el tag "Player"

### Requisito 3: Identificar tu objetivo

Necesitas saber cuál es el GameObject que representa tu objetivo (el círculo amarillo en tu imagen).

**¿Cómo encontrarlo?**
1. En la vista Scene de Unity, haz clic en el círculo amarillo
2. Mira en el Inspector, arriba verás el nombre del GameObject
3. Anota ese nombre (por ejemplo: "Checkpoint_01", "Target", "Goal", etc.)
4. Si no existe, tendrás que crearlo (te explicaré cómo más adelante)

---

## 🛠️ PARTE 3: IMPLEMENTACIÓN PASO A PASO

### PASO 1: Verificar que los scripts existen

Los scripts ya están creados en tu proyecto en la carpeta:
`Assets/Scripts/Rick/NavigationSystem/`

**Verifica que existen estos archivos:**
- FireflyGuide.cs
- FireflySwarm.cs
- MorseCodeTranslator.cs
- FireflySwarmSetup.cs

**¿Cómo verificar?**
1. En Unity, ve a la ventana Project (abajo)
2. Navega a: `Assets > Scripts > Rick > NavigationSystem`
3. Deberías ver los 4 archivos .cs listados arriba
4. Si no los ves, puede que Unity esté compilando. Espera unos segundos.
5. Si hay errores de compilación, revisa la ventana Console (abajo)

**Si hay errores de compilación:**
- Busca errores relacionados con "Light2D"
- Si ves errores de Light2D, asegúrate de tener URP instalado (ver Requisito 1)

### PASO 2: Crear el GameObject del objetivo (si no existe)

Si ya tienes el círculo amarillo u objetivo en tu escena, **salta al PASO 3**.

Si NO tienes un objetivo visible:

1. En la jerarquía, haz clic derecho
2. Selecciona `Create Empty`
3. Nómbralo "Objetivo_Luciérnagas"
4. En el Inspector, ajusta su posición (Transform > Position)
5. Colócalo donde quieres que el jugador vaya
   - Por ejemplo: X: 10, Y: 5, Z: 0

**Opcional: Hacer el objetivo visible**
1. Con "Objetivo_Luciérnagas" seleccionado
2. En el Inspector, haz clic en `Add Component`
3. Busca y añade "Sprite Renderer"
4. En Sprite Renderer, asigna cualquier sprite (puede ser un círculo)
5. Cambia el color a amarillo para que sea visible

### PASO 3: Crear el GameObject del Enjambre

Ahora vamos a crear el objeto que controlará todas las luciérnagas.

1. En la jerarquía de Unity, haz clic derecho en un espacio vacío
2. Selecciona `Create Empty`
3. Nómbralo exactamente: **"FireflySwarm_Sistema"**
4. Asegúrate de que esté en la posición (0, 0, 0)
   - En el Inspector, mira Transform
   - Position debe ser X: 0, Y: 0, Z: 0
   - Si no lo está, haz clic derecho en "Transform" y selecciona "Reset"

**¿Por qué en (0,0,0)?**
El sistema moverá automáticamente el enjambre a la posición correcta (delante del jugador). La posición inicial no importa mucho, pero (0,0,0) es un buen punto de partida.

### PASO 4: Añadir el componente FireflySwarmSetup

Este es el componente principal que configuraremos.

1. Asegúrate de tener seleccionado "FireflySwarm_Sistema" en la jerarquía
2. En el Inspector, ve hasta abajo
3. Haz clic en el botón `Add Component`
4. En el cuadro de búsqueda que aparece, escribe: **FireflySwarmSetup**
5. Haz clic en "FireflySwarmSetup" cuando aparezca en la lista
6. El componente se añadirá y verás varios campos en el Inspector

**¿Qué verás en el Inspector?**
Deberías ver algo como esto:

```
FireflySwarmSetup (Script)
├─ Referencias Automáticas
│  ├─ Player Transform: None (Transform)
│  └─ Destination Transform: None (Transform)
├─ Configuración Rápida
│  ├─ Activation Distance: 15
│  ├─ Deactivation Distance: 3
│  └─ Number Of Fireflies: 8
└─ Mensajes Personalizados
   ├─ Forward Message: ADELANTE
   ├─ Right Message: DERECHA
   ├─ Left Message: IZQUIERDA
   ├─ Up Message: ARRIBA
   └─ Down Message: ABAJO
```

### PASO 5: Configurar las referencias

Ahora vamos a decirle al sistema quién es el jugador y cuál es el objetivo.

**5.1 - Asignar el Jugador:**

1. En el Inspector, busca el campo **"Player Transform"**
2. Tienes dos opciones:

   **Opción A - Dejar vacío (Recomendado si tu jugador tiene tag "Player"):**
   - Simplemente déjalo en "None"
   - El sistema encontrará automáticamente al jugador por su tag
   
   **Opción B - Asignar manualmente:**
   - Haz clic en el círculo pequeño a la derecha del campo "Player Transform"
   - Se abrirá una ventana con todos los GameObjects de la escena
   - Busca y haz clic en tu jugador (puede llamarse "Player", "PlayerChamaleon", etc.)
   - O arrastra directamente tu jugador desde la jerarquía al campo

**5.2 - Asignar el Objetivo:**

1. En el Inspector, busca el campo **"Destination Transform"**
2. Haz clic en el círculo pequeño a la derecha del campo
3. Se abrirá una ventana con todos los GameObjects
4. Busca tu objetivo (el círculo amarillo o "Objetivo_Luciérnagas")
5. Haz clic en él
6. O arrastra el GameObject del objetivo desde la jerarquía al campo

**Verificación:**
- "Player Transform" debe mostrar el nombre de tu jugador (o estar vacío si usas el tag)
- "Destination Transform" debe mostrar el nombre de tu objetivo

### PASO 6: Ajustar la configuración (Opcional pero recomendado)

Ahora vamos a ajustar cómo se comporta el enjambre.

**6.1 - Activation Distance (Distancia de Activación):**
- **¿Qué es?** La distancia máxima desde el objetivo a la que el enjambre aparecerá
- **Valor por defecto:** 15
- **¿Cómo ajustarlo?**
  - Si tu nivel es pequeño, usa un valor menor (ej: 10)
  - Si tu nivel es grande, usa un valor mayor (ej: 20 o 30)
  - Piensa: "¿A qué distancia quiero que el jugador vea las luciérnagas?"

**6.2 - Deactivation Distance (Distancia de Desactivación):**
- **¿Qué es?** Qué tan cerca del objetivo debe estar el jugador para que el enjambre desaparezca
- **Valor por defecto:** 3
- **¿Cómo ajustarlo?**
  - Si quieres que desaparezca muy cerca: usa 1 o 2
  - Si quieres que se quede más tiempo: usa 4 o 5
  - Piensa: "¿A qué distancia el jugador ya no necesita guía?"

**6.3 - Number Of Fireflies (Número de Luciérnagas):**
- **¿Qué es?** Cuántas luciérnagas habrá en el enjambre
- **Valor por defecto:** 8
- **¿Cómo ajustarlo?**
  - Más luciérnagas = más vistoso pero más pesado (ej: 12-16)
  - Menos luciérnagas = más sutil y ligero (ej: 4-6)
  - Recomendado: 6-10 para un buen balance

**6.4 - Mensajes Personalizados:**
- Puedes cambiar los mensajes que parpadean las luciérnagas
- Por defecto están en español: ADELANTE, DERECHA, etc.
- Puedes cambiarlos a lo que quieras:
  - En inglés: FORWARD, RIGHT, LEFT, UP, DOWN
  - Más cortos: GO, R, L, U, D
  - Personalizados: SIGUE, VEN, AQUI, etc.

### PASO 7: Guardar y probar

1. Guarda tu escena: `File > Save` o `Ctrl+S`
2. Haz clic en el botón **Play** (triángulo arriba en el centro)
3. Mueve tu jugador en la escena

**¿Qué debería pasar?**

**Escenario 1 - Estás lejos del objetivo (más de 15 unidades):**
- No verás luciérnagas
- Esto es normal, están desactivadas

**Escenario 2 - Estás a distancia media (entre 3 y 15 unidades):**
- ¡Deberías ver aparecer las luciérnagas!
- Formarán un círculo flotante
- Estarán delante de ti, apuntando hacia el objetivo
- Empezarán a parpadear en código Morse

**Escenario 3 - Estás muy cerca del objetivo (menos de 3 unidades):**
- Las luciérnagas desaparecerán
- Ya no las necesitas porque estás en el objetivo

---

## 🔍 PARTE 4: VERIFICACIÓN Y DEBUGGING

### ¿Cómo saber si está funcionando?

**Método 1 - Visual:**
1. Pon el juego en Play
2. En la vista Scene (no Game), busca tu jugador
3. Muévete a una distancia media del objetivo
4. Deberías ver pequeños objetos flotando (las luciérnagas)

**Método 2 - Jerarquía:**
1. Con el juego en Play
2. En la jerarquía, expande "FireflySwarm_Sistema"
3. Deberías ver objetos hijos llamados "Firefly_0", "Firefly_1", etc.
4. Si los ves, el sistema está creando las luciérnagas correctamente

**Método 3 - Console:**
1. Abre la ventana Console: `Window > General > Console`
2. Cuando inicies el juego, deberías ver mensajes como:
   - "[FireflySwarm] Sistema de luciérnagas encontrado y listo"
3. Si ves errores en rojo, léelos para saber qué falta

### Problemas comunes y soluciones

**PROBLEMA 1: No veo ninguna luciérnaga**

**Posibles causas y soluciones:**

A) **No estás a la distancia correcta**
   - Solución: Mueve tu jugador más cerca o más lejos del objetivo
   - Usa la vista Scene para ver la distancia exacta

B) **El objetivo no está asignado**
   - Solución: Verifica que "Destination Transform" no esté en "None"
   - Reasigna el objetivo siguiendo el PASO 5.2

C) **El jugador no está asignado**
   - Solución: Asigna manualmente el jugador en "Player Transform"
   - O asegúrate de que tu jugador tenga el tag "Player"

D) **Las luciérnagas están pero no las ves**
   - Solución: Puede que sean muy pequeñas o transparentes
   - En la jerarquía, selecciona "Firefly_0" (con el juego en Play)
   - En el Inspector, mira el componente "Sprite Renderer"
   - Aumenta el tamaño en Transform > Scale (ej: X:2, Y:2, Z:1)

**PROBLEMA 2: Veo las luciérnagas pero no parpadean**

**Posibles causas y soluciones:**

A) **No tienen el componente FireflyGuide**
   - Solución: Con el juego en Play, selecciona una luciérnaga
   - Verifica que tenga el componente "FireflyGuide"
   - Si no lo tiene, detén el juego y verifica que los scripts estén bien

B) **Los mensajes están vacíos**
   - Solución: Verifica que los campos de mensajes no estén vacíos
   - Deben tener texto como "ADELANTE", "DERECHA", etc.

**PROBLEMA 3: Error de compilación con "Light2D"**

**Causa:** No tienes Universal Render Pipeline instalado

**Solución:**
1. Sigue las instrucciones del "Requisito 1" en la PARTE 2
2. Instala URP
3. Espera a que Unity recompile
4. Los errores deberían desaparecer

**PROBLEMA 4: Las luciérnagas aparecen en el lugar equivocado**

**Causa:** El sistema está calculando mal la dirección

**Solución:**
1. Verifica que tanto el jugador como el objetivo estén en el plano 2D correcto
2. Asegúrate de que ambos tengan Z = 0 (o el mismo valor de Z)
3. Si tu juego es 2D, todos los objetos deben estar en el mismo plano

---

## 🎨 PARTE 5: PERSONALIZACIÓN AVANZADA

Una vez que el sistema funciona, puedes personalizarlo.

### Personalización 1: Cambiar el aspecto de las luciérnagas

**Opción A - Cambiar colores (Fácil):**

1. Con el juego en Play, selecciona "FireflySwarm_Sistema" en la jerarquía
2. Expándelo y selecciona "Firefly_0"
3. En el Inspector, busca el componente "Firefly Guide (Script)"
4. Cambia estos valores:
   - **Glow Color**: El color de la luz (ej: verde, azul, rojo)
   - **Max Intensity**: Qué tan brillante (ej: 2.0 para más brillo)
   - **Min Intensity**: Brillo mínimo (ej: 0.05 para casi apagado)
5. Los cambios se perderán al detener el juego, así que anótalos
6. Detén el juego
7. Crea un prefab (explicado abajo)

**Opción B - Usar tu propio sprite (Avanzado):**

1. Detén el juego si está corriendo
2. En la jerarquía, haz clic derecho y crea: `Create Empty`
3. Nómbralo "MiLuciernaga_Prefab"
4. Añádele estos componentes:
   - `Add Component > Sprite Renderer`
   - `Add Component > Light 2D` (busca "Light 2D")
   - `Add Component > Firefly Guide` (busca "Firefly Guide")
5. En Sprite Renderer:
   - Asigna tu sprite personalizado
   - Ajusta el color si quieres
6. En Light 2D:
   - Light Type: Point
   - Color: El color que quieras
   - Intensity: 1.0
   - Outer Radius: 2.0
7. Arrastra "MiLuciernaga_Prefab" desde la jerarquía a la carpeta Project
8. Ahora tienes un prefab
9. Selecciona "FireflySwarm_Sistema"
10. En el componente "Firefly Swarm", busca "Firefly Prefab"
11. Arrastra tu prefab "MiLuciernaga_Prefab" a ese campo
12. Borra "MiLuciernaga_Prefab" de la jerarquía
13. Dale Play y verás tus luciérnagas personalizadas

### Personalización 2: Cambiar el comportamiento

**En el componente FireflySwarm:**

1. Selecciona "FireflySwarm_Sistema"
2. En el Inspector, busca "Firefly Swarm (Script)"
3. Puedes ajustar:
   - **Swarm Radius**: Qué tan grande es el círculo de luciérnagas (ej: 3.0)
   - **Distance From Player**: Qué tan lejos del jugador flotan (ej: 4.0)
   - **Update Interval**: Cada cuántos segundos cambia el mensaje (ej: 3.0)
   - **Show Debug Gizmos**: Actívalo para ver líneas de debug en Scene view

### Personalización 3: Múltiples objetivos

Si tienes varios puntos de interés:

1. Crea un enjambre para cada objetivo
2. Duplica "FireflySwarm_Sistema": Selecciónalo y presiona `Ctrl+D`
3. Renómbralo: "FireflySwarm_Objetivo2"
4. En el Inspector, cambia "Destination Transform" al nuevo objetivo
5. Ahora tienes dos enjambres independientes

**Tip:** Puedes desactivar un enjambre cuando el jugador complete un objetivo:
```csharp
GameObject.Find("FireflySwarm_Objetivo1").SetActive(false);
```

---

## 📊 PARTE 6: ENTENDIENDO EL CÓDIGO MORSE

No necesitas entender el código Morse para que funcione, pero puede ser interesante.

### ¿Cómo funciona?

El código Morse usa dos símbolos:
- **Punto (.)**: Parpadeo corto (0.2 segundos)
- **Raya (-)**: Parpadeo largo (0.6 segundos)

### Ejemplo: "ADELANTE"

```
A = .-     (punto-raya)
D = -..    (raya-punto-punto)
E = .      (punto)
L = .-..   (punto-raya-punto-punto)
A = .-     (punto-raya)
N = -.     (raya-punto)
T = -      (raya)
E = .      (punto)
```

Cuando las luciérnagas parpadean "ADELANTE", verás:
1. Parpadeo corto, parpadeo largo (A)
2. Pausa
3. Parpadeo largo, dos cortos (D)
4. Pausa
5. Un parpadeo corto (E)
6. Y así sucesivamente...

### ¿Puedo cambiar la velocidad del parpadeo?

Sí, pero requiere editar código:

1. Abre el archivo: `Assets/Scripts/Rick/NavigationSystem/MorseCodeTranslator.cs`
2. Busca la función `GetSymbolDuration`
3. Verás: `float dotDuration = 0.2f`
4. Cambia 0.2f a otro valor:
   - 0.1f = más rápido
   - 0.3f = más lento
5. Guarda el archivo

---

## 🎮 PARTE 7: INTEGRACIÓN CON TU JUEGO

### Activar/Desactivar el enjambre desde código

Si quieres controlar cuándo aparece el enjambre desde tus propios scripts:

```csharp
// Obtener referencia al enjambre
GameObject swarm = GameObject.Find("FireflySwarm_Sistema");

// Desactivar
swarm.SetActive(false);

// Activar
swarm.SetActive(true);
```

### Cambiar el objetivo dinámicamente

Si el objetivo cambia durante el juego:

```csharp
// Obtener el componente
FireflySwarmSetup setup = GameObject.Find("FireflySwarm_Sistema").GetComponent<FireflySwarmSetup>();

// Cambiar objetivo
Transform nuevoObjetivo = GameObject.Find("NuevoCheckpoint").transform;
setup.ChangeDestination(nuevoObjetivo);
```

### Detectar cuando el jugador llega al objetivo

Puedes usar un Trigger en el objetivo:

1. Selecciona tu objetivo en la jerarquía
2. Añade componente: `Box Collider 2D` (o `Circle Collider 2D`)
3. Marca la casilla "Is Trigger"
4. Ajusta el tamaño del collider
5. Crea un script en el objetivo:

```csharp
using UnityEngine;

public class ObjetivoDetector : MonoBehaviour {
    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            Debug.Log("¡Jugador llegó al objetivo!");
            // Aquí puedes desactivar el enjambre
            GameObject.Find("FireflySwarm_Sistema").SetActive(false);
        }
    }
}
```

---

## ✅ PARTE 8: CHECKLIST FINAL

Antes de dar por terminada la implementación, verifica:

- [ ] URP está instalado y configurado
- [ ] El jugador tiene el tag "Player"
- [ ] Existe un GameObject de objetivo en la escena
- [ ] "FireflySwarm_Sistema" está creado
- [ ] El componente "FireflySwarmSetup" está añadido
- [ ] "Player Transform" está asignado (o vacío con tag correcto)
- [ ] "Destination Transform" está asignado
- [ ] Las distancias están configuradas (15 y 3 por defecto)
- [ ] El número de luciérnagas está configurado (8 por defecto)
- [ ] Al dar Play y acercarte al objetivo, ves las luciérnagas
- [ ] Las luciérnagas parpadean
- [ ] Las luciérnagas desaparecen cuando llegas muy cerca

---

## 🆘 PARTE 9: SOPORTE Y AYUDA

### Si algo no funciona:

1. **Revisa la Console** (`Window > General > Console`)
   - Los mensajes en rojo son errores
   - Los mensajes en amarillo son advertencias
   - Lee los mensajes, suelen indicar qué falta

2. **Verifica la jerarquía en Play mode**
   - Dale Play
   - Expande "FireflySwarm_Sistema"
   - ¿Ves objetos hijos? Bien
   - ¿No ves nada? Hay un problema en la creación

3. **Usa la vista Scene**
   - Con el juego en Play
   - Mira la vista Scene (no Game)
   - Busca visualmente las luciérnagas
   - Pueden estar en una posición inesperada

4. **Revisa las distancias**
   - Selecciona "FireflySwarm_Sistema"
   - Activa "Show Debug Gizmos"
   - En Scene view verás esferas de colores
   - Amarilla = rango de activación
   - Verde = rango de desactivación

### Archivos de referencia:

- **GUIA_RAPIDA.md**: Versión resumida de esta guía
- **README.md**: Documentación técnica completa
- **CODIGO_MORSE.md**: Tabla completa del código Morse

---

## 🎓 CONCLUSIÓN

Has implementado un sistema completo de navegación con luciérnagas que:
- Se activa automáticamente según la distancia
- Guía al jugador con código Morse
- Es completamente personalizable
- Funciona de forma independiente

El sistema está diseñado para ser "plug and play": una vez configurado, funciona solo sin necesidad de código adicional.

¡Disfruta de tus luciérnagas guía! 🔥✨
