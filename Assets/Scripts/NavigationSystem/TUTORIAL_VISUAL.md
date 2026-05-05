# 🎬 Tutorial Visual Paso a Paso

Esta guía te muestra exactamente qué debes ver en cada paso de la implementación.

---

## 📋 ANTES DE EMPEZAR

### Lo que necesitas tener listo:

1. ✅ Unity abierto con tu proyecto
2. ✅ Una escena cargada (la que tiene tu juego)
3. ✅ Un jugador en la escena
4. ✅ Un punto objetivo (o crearemos uno)

---

## 🎯 PASO 1: VERIFICAR UNIVERSAL RENDER PIPELINE

### ¿Qué hacer?

1. Ve al menú superior: `Edit > Project Settings`
2. En la ventana que se abre, busca en el panel izquierdo: `Graphics`
3. Haz clic en `Graphics`

### ¿Qué deberías ver?

En el panel derecho, busca el campo **"Scriptable Render Pipeline Settings"**

**✅ CORRECTO - Tienes URP:**
```
Scriptable Render Pipeline Settings
┌─────────────────────────────────────┐
│ UniversalRenderPipelineAsset       │  ← Algo con "Universal" o "URP"
│ (UniversalRenderPipelineAsset)     │
└─────────────────────────────────────┘
```

**❌ INCORRECTO - No tienes URP:**
```
Scriptable Render Pipeline Settings
┌─────────────────────────────────────┐
│ None (RenderPipelineAsset)         │  ← Dice "None"
└─────────────────────────────────────┘
```

### Si no tienes URP:

1. Cierra la ventana de Project Settings
2. Ve a: `Window > Package Manager`
3. En la esquina superior izquierda, cambia el dropdown de "Packages: In Project" a **"Unity Registry"**
4. En la lista, busca **"Universal RP"**
5. Haz clic en él
6. Haz clic en el botón **"Install"** (esquina inferior derecha)
7. Espera a que se instale (puede tardar 1-2 minutos)
8. Cierra el Package Manager
9. Ve a: `Assets > Create > Rendering > URP Asset (with 2D Renderer)`
10. Guárdalo con el nombre "GameURP"
11. Vuelve a `Edit > Project Settings > Graphics`
12. Arrastra "GameURP" al campo "Scriptable Render Pipeline Settings"

---

## 🎮 PASO 2: VERIFICAR EL JUGADOR

### ¿Qué hacer?

1. En la ventana **Hierarchy** (panel izquierdo), busca tu jugador
2. Puede llamarse: "Player", "PlayerChamaleon", "Character", etc.
3. Haz clic en él para seleccionarlo

### ¿Qué deberías ver en el Inspector?

En la parte superior del Inspector:

**✅ CORRECTO:**
```
┌─────────────────────────────────────┐
│ ☑ PlayerChamaleon          Static ▼ │
│ Tag: Player ▼        Layer: Default │  ← Tag dice "Player"
└─────────────────────────────────────┘
```

**❌ INCORRECTO:**
```
┌─────────────────────────────────────┐
│ ☑ PlayerChamaleon          Static ▼ │
│ Tag: Untagged ▼      Layer: Default │  ← Tag dice "Untagged"
└─────────────────────────────────────┘
```

### Si el tag no es "Player":

1. Haz clic en el dropdown donde dice "Untagged"
2. Selecciona **"Player"** de la lista
3. Si no aparece "Player" en la lista:
   - Selecciona "Add Tag..." al final de la lista
   - Haz clic en el botón **"+"**
   - Escribe: **Player**
   - Haz clic fuera del campo para guardar
   - Vuelve a tu jugador en la Hierarchy
   - Ahora sí selecciona "Player" en el dropdown de Tag

---

## 📍 PASO 3: CREAR EL OBJETIVO

### ¿Qué hacer?

1. En la ventana **Hierarchy**, haz clic derecho en un espacio vacío
2. Selecciona: `Create Empty`
3. Se creará un GameObject llamado "GameObject"

### ¿Qué deberías ver?

En la Hierarchy:
```
Hierarchy
├─ Main Camera
├─ PlayerChamaleon
├─ GameObject  ← Este es el nuevo
└─ ...
```

### Renombrar el objetivo:

1. Con "GameObject" seleccionado, presiona **F2** (o haz clic lento dos veces)
2. Escribe: **Objetivo_Luciérnagas**
3. Presiona **Enter**

### Posicionar el objetivo:

1. Con "Objetivo_Luciérnagas" seleccionado
2. En el Inspector, busca el componente **Transform**
3. En **Position**, establece valores donde quieres que vaya el jugador

**Ejemplo:**
```
Transform
├─ Position
│  ├─ X: 10      ← 10 unidades a la derecha
│  ├─ Y: 5       ← 5 unidades arriba
│  └─ Z: 0       ← Siempre 0 en juegos 2D
├─ Rotation
│  └─ (dejar en 0, 0, 0)
└─ Scale
   └─ (dejar en 1, 1, 1)
```

### Hacer el objetivo visible (Opcional):

1. Con "Objetivo_Luciérnagas" seleccionado
2. En el Inspector, haz clic en **"Add Component"**
3. Escribe: **Sprite Renderer**
4. Presiona Enter
5. En el componente Sprite Renderer que aparece:
   - Haz clic en el círculo pequeño junto a "Sprite"
   - Selecciona cualquier sprite (puede ser "Circle" o "Square")
   - En "Color", haz clic en el cuadro de color
   - Selecciona amarillo
6. Ahora verás un círculo amarillo en la Scene view

---

## 🔥 PASO 4: CREAR EL ENJAMBRE

### ¿Qué hacer?

1. En la **Hierarchy**, haz clic derecho en un espacio vacío
2. Selecciona: `Create Empty`
3. Presiona **F2** para renombrar
4. Escribe: **FireflySwarm_Sistema**
5. Presiona **Enter**

### ¿Qué deberías ver?

En la Hierarchy:
```
Hierarchy
├─ Main Camera
├─ PlayerChamaleon
├─ Objetivo_Luciérnagas
├─ FireflySwarm_Sistema  ← Este es el nuevo
└─ ...
```

### Verificar posición:

1. Con "FireflySwarm_Sistema" seleccionado
2. En el Inspector, mira el componente **Transform**

**Debería estar así:**
```
Transform
├─ Position
│  ├─ X: 0
│  ├─ Y: 0
│  └─ Z: 0
├─ Rotation
│  ├─ X: 0
│  ├─ Y: 0
│  └─ Z: 0
└─ Scale
   ├─ X: 1
   ├─ Y: 1
   └─ Z: 1
```

**Si no está en (0,0,0):**
1. Haz clic derecho en "Transform"
2. Selecciona **"Reset"**

---

## 🔧 PASO 5: AÑADIR EL COMPONENTE

### ¿Qué hacer?

1. Asegúrate de tener "FireflySwarm_Sistema" seleccionado
2. En el Inspector, ve hasta abajo
3. Haz clic en el botón **"Add Component"**
4. Aparecerá un cuadro de búsqueda

### ¿Qué deberías ver?

```
┌─────────────────────────────────────┐
│ Search...                           │  ← Cuadro de búsqueda
└─────────────────────────────────────┘
```

### Buscar el componente:

1. Escribe: **FireflySwarmSetup**
2. Mientras escribes, aparecerá en la lista

**Deberías ver:**
```
┌─────────────────────────────────────┐
│ FireflySwarmSetup                   │  ← Este
│ Scripts                             │
└─────────────────────────────────────┘
```

3. Haz clic en **"FireflySwarmSetup"**

### ¿Qué deberías ver después?

En el Inspector, aparecerá un nuevo componente:

```
FireflySwarmSetup (Script)
├─ Script: FireflySwarmSetup
├─ Referencias Automáticas
│  ├─ Player Transform
│  │  └─ None (Transform)
│  └─ Destination Transform
│     └─ None (Transform)
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

---

## 🔗 PASO 6: ASIGNAR REFERENCIAS

### Asignar el Jugador:

**Opción A - Automático (Recomendado):**
1. Simplemente deja "Player Transform" en **"None"**
2. El sistema lo encontrará automáticamente si tiene el tag "Player"

**Opción B - Manual:**
1. Haz clic en el **círculo pequeño** a la derecha de "Player Transform"
2. Se abrirá una ventana: "Select Transform"

**Deberías ver:**
```
┌─────────────────────────────────────┐
│ Select Transform                    │
├─────────────────────────────────────┤
│ Search...                           │
├─────────────────────────────────────┤
│ Scene                               │
│ ├─ Main Camera                      │
│ ├─ PlayerChamaleon  ← Haz clic aquí│
│ ├─ Objetivo_Luciérnagas             │
│ └─ FireflySwarm_Sistema             │
└─────────────────────────────────────┘
```

3. Haz clic en tu jugador (ej: "PlayerChamaleon")
4. La ventana se cerrará

**Ahora deberías ver:**
```
Player Transform
└─ PlayerChamaleon (Transform)  ← Ya no dice "None"
```

### Asignar el Objetivo:

1. Haz clic en el **círculo pequeño** a la derecha de "Destination Transform"
2. Se abrirá la ventana "Select Transform"
3. Haz clic en **"Objetivo_Luciérnagas"**

**Ahora deberías ver:**
```
Destination Transform
└─ Objetivo_Luciérnagas (Transform)  ← Ya no dice "None"
```

---

## ⚙️ PASO 7: CONFIGURAR PARÁMETROS

### ¿Qué deberías ver?

En el Inspector, en la sección "Configuración Rápida":

```
Configuración Rápida
├─ Activation Distance: 15
├─ Deactivation Distance: 3
└─ Number Of Fireflies: 8
```

### ¿Qué significan?

**Activation Distance (15):**
- El enjambre aparece cuando estás a **menos de 15 unidades** del objetivo
- Si tu nivel es pequeño, cámbialo a **10**
- Si tu nivel es grande, cámbialo a **20 o 30**

**Deactivation Distance (3):**
- El enjambre desaparece cuando estás a **menos de 3 unidades** del objetivo
- Significa que ya llegaste
- Puedes dejarlo en **3** o cambiarlo a **2** si quieres que desaparezca más cerca

**Number Of Fireflies (8):**
- Cuántas luciérnagas habrá
- **8** es un buen número
- Puedes usar **6** para menos luciérnagas
- O **12** para más luciérnagas

### Cómo cambiar los valores:

1. Haz clic en el número
2. Escribe el nuevo valor
3. Presiona **Enter**

---

## 💾 PASO 8: GUARDAR

### ¿Qué hacer?

1. Ve al menú: `File > Save` (o presiona **Ctrl+S**)
2. Espera a que Unity guarde (verás un círculo girando abajo a la derecha)

---

## ▶️ PASO 9: PROBAR

### ¿Qué hacer?

1. Haz clic en el botón **Play** (triángulo grande arriba en el centro)
2. El juego empezará

### ¿Qué deberías ver?

**Escenario 1 - Estás lejos del objetivo:**

En la vista **Game**:
- No verás luciérnagas
- Esto es normal

En la **Hierarchy** (con el juego corriendo):
```
Hierarchy
├─ Main Camera
├─ PlayerChamaleon
├─ Objetivo_Luciérnagas
├─ FireflySwarm_Sistema
│  └─ (vacío o hijos desactivados)  ← No hay luciérnagas visibles
└─ ...
```

**Escenario 2 - Te acercas al objetivo (entre 3 y 15 unidades):**

En la vista **Game**:
- ¡Deberías ver aparecer las luciérnagas!
- Serán puntos de luz flotando
- Estarán delante de ti
- Empezarán a parpadear

En la **Hierarchy**:
```
Hierarchy
├─ Main Camera
├─ PlayerChamaleon
├─ Objetivo_Luciérnagas
├─ FireflySwarm_Sistema
│  ├─ Firefly_0  ← ¡Aparecieron!
│  ├─ Firefly_1
│  ├─ Firefly_2
│  ├─ Firefly_3
│  ├─ Firefly_4
│  ├─ Firefly_5
│  ├─ Firefly_6
│  └─ Firefly_7
└─ ...
```

**Escenario 3 - Llegas muy cerca del objetivo (menos de 3 unidades):**

En la vista **Game**:
- Las luciérnagas desaparecerán
- Ya no las necesitas

---

## 🔍 PASO 10: VERIFICAR EN SCENE VIEW

### ¿Qué hacer?

1. Con el juego en **Play**
2. Haz clic en la pestaña **Scene** (al lado de Game)
3. Busca tu jugador en la escena

### ¿Qué deberías ver?

Si estás en el rango correcto (3-15 unidades del objetivo):

```
Vista desde arriba:

        Objetivo
            ●
            |
            |
         🔥🔥🔥  ← Luciérnagas (círculo de puntos brillantes)
        🔥   🔥
       🔥  P  🔥  ← P = Jugador
        🔥   🔥
         🔥🔥🔥
```

### Activar Gizmos para ver rangos:

1. Con "FireflySwarm_Sistema" seleccionado
2. En el Inspector, busca el componente "Firefly Swarm (Script)"
3. Marca la casilla **"Show Debug Gizmos"**

**Ahora en Scene view verás:**
```
        Objetivo
            ●
        ╱       ╲
      ╱           ╲
    ╱    ┌─────┐   ╲  ← Círculo amarillo (radio 15)
   │     │     │     │
   │     │  ●  │     │  ← Círculo verde (radio 3)
   │     └─────┘     │
    ╲               ╱
      ╲           ╱
        ╲       ╱
```

---

## 🎨 PASO 11: VER EL PARPADEO

### ¿Qué hacer?

1. Con el juego en Play
2. Acércate al objetivo (entre 3-15 unidades)
3. Observa las luciérnagas

### ¿Qué deberías ver?

Las luciérnagas parpadearán en un patrón:

```
Tiempo: 0s    0.5s   1s    1.5s   2s    2.5s   3s
        |      |     |      |     |      |     |
Luz:    ●━     ━●●   ●      ●━●●  ●━     ━●    ━
        ↑      ↑     ↑      ↑     ↑      ↑     ↑
        A      D     E      L     A      N     T

Leyenda:
● = Parpadeo corto (punto)
━ = Parpadeo largo (raya)
```

**Parpadeo corto:** La luz se enciende y apaga rápido (0.2s)
**Parpadeo largo:** La luz se enciende y apaga lento (0.6s)

---

## ✅ PASO 12: VERIFICACIÓN FINAL

### Checklist de verificación:

Marca cada punto que funcione:

- [ ] Al dar Play, no hay errores en la Console
- [ ] Cuando me acerco al objetivo, aparecen las luciérnagas
- [ ] Las luciérnagas forman un círculo
- [ ] Las luciérnagas están delante de mí, hacia el objetivo
- [ ] Las luciérnagas parpadean (se encienden y apagan)
- [ ] Cuando llego muy cerca del objetivo, desaparecen
- [ ] Cuando me alejo mucho, desaparecen
- [ ] En la Hierarchy veo "Firefly_0" hasta "Firefly_7" cuando están activas

### Si todos los puntos están marcados:

¡Felicidades! El sistema está funcionando correctamente. 🎉

---

## 🐛 TROUBLESHOOTING VISUAL

### Problema: No veo luciérnagas

**Verifica en la Console:**

1. Ve a: `Window > General > Console`
2. ¿Ves mensajes en rojo?

**Si ves:**
```
NullReferenceException: Object reference not set to an instance of an object
FireflySwarm.Update()
```

**Solución:** El objetivo o jugador no están asignados
- Vuelve al PASO 6 y asigna las referencias

**Si ves:**
```
The type or namespace name 'Light2D' could not be found
```

**Solución:** No tienes URP instalado
- Vuelve al PASO 1 e instala URP

### Problema: Las luciérnagas están pero no se mueven

**Verifica:**

1. Selecciona "Firefly_0" en la Hierarchy (con el juego en Play)
2. En el Inspector, busca "Firefly Guide (Script)"
3. Verifica que:
   - Float Speed: 1 (no 0)
   - Float Amplitude: 0.3 (no 0)

### Problema: Las luciérnagas están en el lugar equivocado

**Verifica en Scene view:**

1. ¿Las luciérnagas están muy lejos?
2. Selecciona "FireflySwarm_Sistema"
3. En el Inspector, busca "Firefly Swarm (Script)"
4. Cambia "Distance From Player" a un valor menor (ej: 2)

---

## 📸 RESUMEN VISUAL DE LO QUE DEBERÍAS VER

### En el Inspector (FireflySwarm_Sistema seleccionado):

```
┌─────────────────────────────────────────────────────┐
│ Inspector                                           │
├─────────────────────────────────────────────────────┤
│ ☑ FireflySwarm_Sistema                    Static ▼ │
│ Tag: Untagged ▼              Layer: Default ▼      │
├─────────────────────────────────────────────────────┤
│ Transform                                           │
│ Position    X: 0    Y: 0    Z: 0                   │
│ Rotation    X: 0    Y: 0    Z: 0                   │
│ Scale       X: 1    Y: 1    Z: 1                   │
├─────────────────────────────────────────────────────┤
│ FireflySwarmSetup (Script)                         │
│ Script: FireflySwarmSetup                          │
│                                                     │
│ Referencias Automáticas                             │
│ Player Transform                                    │
│ └─ PlayerChamaleon (Transform)                     │
│ Destination Transform                               │
│ └─ Objetivo_Luciérnagas (Transform)                │
│                                                     │
│ Configuración Rápida                                │
│ Activation Distance        15                       │
│ Deactivation Distance      3                        │
│ Number Of Fireflies        8                        │
│                                                     │
│ Mensajes Personalizados                             │
│ Forward Message           ADELANTE                  │
│ Right Message             DERECHA                   │
│ Left Message              IZQUIERDA                 │
│ Up Message                ARRIBA                    │
│ Down Message              ABAJO                     │
├─────────────────────────────────────────────────────┤
│ Firefly Swarm (Script)                             │
│ (Este se añade automáticamente)                     │
└─────────────────────────────────────────────────────┘
```

### En la Hierarchy (con el juego en Play y cerca del objetivo):

```
┌─────────────────────────────────────┐
│ Hierarchy                           │
├─────────────────────────────────────┤
│ ▼ SampleScene                       │
│   ├─ Main Camera                    │
│   ├─ PlayerChamaleon                │
│   ├─ Objetivo_Luciérnagas           │
│   ├─▼ FireflySwarm_Sistema          │
│   │  ├─ Firefly_0                   │
│   │  ├─ Firefly_1                   │
│   │  ├─ Firefly_2                   │
│   │  ├─ Firefly_3                   │
│   │  ├─ Firefly_4                   │
│   │  ├─ Firefly_5                   │
│   │  ├─ Firefly_6                   │
│   │  └─ Firefly_7                   │
│   └─ ...                            │
└─────────────────────────────────────┘
```

---

¡Con esta guía visual deberías poder implementar el sistema sin problemas! Si algo no se ve como se muestra aquí, revisa ese paso específico.
