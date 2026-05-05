# 📐 Diagrama de Flujo del Sistema de Luciérnagas

Este documento te ayuda a visualizar cómo funciona el sistema paso por paso.

---

## 🔄 FLUJO PRINCIPAL DEL SISTEMA

```
INICIO DEL JUEGO
      |
      v
[FireflySwarmSetup.Start()]
      |
      v
¿Hay jugador asignado?
      |
      +--NO--> Buscar GameObject con tag "Player"
      |              |
      |              v
      |        ¿Encontrado?
      |              |
      |              +--SÍ--> Asignar como jugador
      |              |
      |              +--NO--> Mostrar advertencia
      |
      +--SÍ--> Continuar
      |
      v
[FireflySwarm.Start()]
      |
      v
Crear luciérnagas (8 por defecto)
      |
      v
Para cada luciérnaga:
  - Crear GameObject
  - Añadir SpriteRenderer
  - Añadir Light2D
  - Añadir FireflyGuide
  - Posicionar en círculo
      |
      v
Desactivar enjambre inicialmente
      |
      v
SISTEMA LISTO
      |
      v
[LOOP CADA FRAME]
```

---

## 🎮 FLUJO DURANTE EL JUEGO (Update Loop)

```
CADA FRAME (Update)
      |
      v
Calcular distancia entre jugador y objetivo
      |
      v
¿Distancia > 15 unidades?
      |
      +--SÍ--> Desactivar enjambre --> FIN FRAME
      |
      +--NO--> Continuar
      |
      v
¿Distancia < 3 unidades?
      |
      +--SÍ--> Desactivar enjambre --> FIN FRAME
      |
      +--NO--> Continuar
      |
      v
¿Distancia entre 3 y 15?
      |
      +--SÍ--> Activar enjambre
      |              |
      |              v
      |        Calcular dirección hacia objetivo
      |              |
      |              v
      |        Posicionar enjambre delante del jugador
      |              |
      |              v
      |        ¿Han pasado 2 segundos desde última actualización?
      |              |
      |              +--SÍ--> Actualizar mensaje Morse
      |              |              |
      |              |              v
      |              |        Calcular ángulo hacia objetivo
      |              |              |
      |              |              v
      |              |        Determinar dirección:
      |              |          - 45°-135° = ARRIBA
      |              |          - 135°-225° = IZQUIERDA
      |              |          - 225°-315° = ABAJO
      |              |          - Resto = DERECHA
      |              |              |
      |              |              v
      |              |        Enviar mensaje a todas las luciérnagas
      |              |
      |              +--NO--> Mantener mensaje actual
      |
      v
FIN FRAME
```

---

## 💫 FLUJO DE UNA LUCIÉRNAGA INDIVIDUAL

```
LUCIÉRNAGA ACTIVA
      |
      v
[MOVIMIENTO FLOTANTE - Cada Frame]
      |
      v
Calcular posición con seno/coseno
  - X = sin(tiempo) * amplitud
  - Y = cos(tiempo * 1.3) * amplitud
      |
      v
Aplicar offset aleatorio único
      |
      v
Actualizar posición
      |
      v
[PARPADEO MORSE - Corrutina Independiente]
      |
      v
¿Hay mensaje asignado?
      |
      +--NO--> Esperar 1 segundo --> Volver a verificar
      |
      +--SÍ--> Continuar
      |
      v
Para cada símbolo en el mensaje:
      |
      v
¿Es punto (.) o raya (-)?
      |
      +--SÍ--> Encender luz (0.1s)
      |              |
      |              v
      |        Mantener encendida:
      |          - Punto: 0.2s
      |          - Raya: 0.6s
      |              |
      |              v
      |        Apagar luz (0.1s)
      |              |
      |              v
      |        Pausa 0.2s
      |
      +--¿Es espacio?--> Pausa 0.6s
      |
      +--¿Es barra (/)? --> Pausa 1.4s
      |
      v
¿Fin del mensaje?
      |
      +--SÍ--> Pausa 2s --> Repetir mensaje
      |
      +--NO--> Siguiente símbolo
```

---

## 🎯 FLUJO DE ACTIVACIÓN/DESACTIVACIÓN

```
                    ESTADO DEL ENJAMBRE
                            |
                            v
                    ¿Está activo?
                            |
            +---------------+---------------+
            |                               |
            v                               v
        ACTIVO                          INACTIVO
            |                               |
            v                               v
    Verificar distancia             Verificar distancia
            |                               |
            v                               v
    ¿Fuera de rango?                ¿En rango 3-15?
            |                               |
    +-------+-------+               +-------+-------+
    |               |               |               |
    v               v               v               v
   SÍ              NO              SÍ              NO
    |               |               |               |
    v               |               v               |
DESACTIVAR         |           ACTIVAR             |
    |               |               |               |
    v               v               v               v
Ocultar         Mantener        Mostrar         Mantener
luciérnagas     activo          luciérnagas     inactivo
    |               |               |               |
    v               v               v               v
Detener         Continuar       Iniciar         Esperar
parpadeo        parpadeo        parpadeo        siguiente
    |               |               |            frame
    +-------+-------+-------+-------+
            |
            v
    Siguiente frame
```

---

## 📊 DIAGRAMA DE COMPONENTES

```
JERARQUÍA DE UNITY:
│
├─ FireflySwarm_Sistema (GameObject)
│  │
│  ├─ [Componente] FireflySwarmSetup
│  │  │
│  │  ├─ Referencias:
│  │  │  ├─ playerTransform
│  │  │  └─ destinationTransform
│  │  │
│  │  └─ Configuración:
│  │     ├─ activationDistance: 15
│  │     ├─ deactivationDistance: 3
│  │     └─ numberOfFireflies: 8
│  │
│  ├─ [Componente] FireflySwarm (añadido automáticamente)
│  │  │
│  │  ├─ Gestión del enjambre
│  │  ├─ Cálculo de dirección
│  │  └─ Control de activación
│  │
│  └─ [Hijos - Creados en runtime]
│     │
│     ├─ Firefly_0 (GameObject)
│     │  ├─ [Componente] SpriteRenderer
│     │  ├─ [Componente] Light2D
│     │  └─ [Componente] FireflyGuide
│     │
│     ├─ Firefly_1 (GameObject)
│     │  ├─ [Componente] SpriteRenderer
│     │  ├─ [Componente] Light2D
│     │  └─ [Componente] FireflyGuide
│     │
│     └─ ... (hasta Firefly_7)
```

---

## 🔢 CÁLCULO DE DIRECCIÓN (Matemática)

```
POSICIÓN JUGADOR: (Px, Py)
POSICIÓN OBJETIVO: (Ox, Oy)

PASO 1: Calcular vector dirección
    dirX = Ox - Px
    dirY = Oy - Py

PASO 2: Normalizar (hacer longitud = 1)
    magnitud = √(dirX² + dirY²)
    dirX_norm = dirX / magnitud
    dirY_norm = dirY / magnitud

PASO 3: Calcular ángulo
    ángulo = atan2(dirY_norm, dirX_norm) * (180/π)

PASO 4: Normalizar ángulo a 0-360°
    si ángulo < 0:
        ángulo = ángulo + 360

PASO 5: Determinar mensaje
    si 45° ≤ ángulo < 135°:
        mensaje = "ARRIBA"
    si 135° ≤ ángulo < 225°:
        mensaje = "IZQUIERDA"
    si 225° ≤ ángulo < 315°:
        mensaje = "ABAJO"
    si no:
        mensaje = "DERECHA"

PASO 6: Posicionar enjambre
    posX = Px + (dirX_norm * distanciaDelJugador)
    posY = Py + (dirY_norm * distanciaDelJugador)
    posZ = 0
```

---

## 🎨 VISUALIZACIÓN DE RANGOS

```
Vista desde arriba (2D):


                        OBJETIVO (O)
                            ●
                            |
                            |
                    ┌───────┼───────┐
                    │       |       │
                    │   ┌───┼───┐   │
                    │   │   |   │   │
                    │   │   O   │   │  ← Zona de desactivación
                    │   │       │   │     (radio: 3 unidades)
                    │   └───────┘   │
                    │               │
                    │               │
                    │      ●P       │  ← JUGADOR en zona activa
                    │    /   \      │     (enjambre visible)
                    │   🔥🔥🔥🔥🔥    │
                    │               │
                    └───────────────┘  ← Zona de activación
                                         (radio: 15 unidades)


                    ●P                 ← JUGADOR fuera de zona
                                         (enjambre invisible)


Leyenda:
O = Objetivo
●P = Jugador
🔥 = Luciérnagas (solo visibles en zona activa)
```

---

## ⏱️ TIMELINE DEL PARPADEO MORSE

```
Ejemplo: Letra "A" (.- en Morse)

Tiempo (segundos):
0.0   0.1   0.2   0.3   0.4   0.5   0.6   0.7   0.8   0.9   1.0
 |     |     |     |     |     |     |     |     |     |     |
 
Punto (.):
 [Encender]
 ▓▓▓▓▓
       [Mantener]
       ▓▓▓▓▓▓▓▓▓▓
                 [Apagar]
                 ░░░░░
                       [Pausa]
                       ░░░░░
                             
Raya (-):
                             [Encender]
                             ▓▓▓▓▓
                                   [Mantener]
                                   ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
                                                                 [Apagar]
                                                                 ░░░░░

Leyenda:
▓ = Luz encendida (intensidad máxima)
░ = Luz apagada (intensidad mínima)

Duraciones:
- Encender/Apagar: 0.1s (transición suave)
- Punto mantener: 0.2s
- Raya mantener: 0.6s (3x punto)
- Pausa entre símbolos: 0.2s
- Pausa entre letras: 0.6s
- Pausa entre palabras: 1.4s
```

---

## 🔄 CICLO DE VIDA COMPLETO

```
1. INICIALIZACIÓN
   ↓
   Crear GameObjects
   ↓
   Añadir componentes
   ↓
   Configurar referencias
   ↓
   Desactivar enjambre
   ↓
2. ESPERA (Loop)
   ↓
   Verificar distancia cada frame
   ↓
   ¿En rango? → NO → Volver a ESPERA
   ↓ SÍ
3. ACTIVACIÓN
   ↓
   Mostrar luciérnagas
   ↓
   Iniciar movimiento flotante
   ↓
   Calcular dirección
   ↓
   Iniciar parpadeo Morse
   ↓
4. ACTIVO (Loop)
   ↓
   Actualizar posición cada frame
   ↓
   Actualizar mensaje cada 2s
   ↓
   Verificar distancia
   ↓
   ¿Fuera de rango? → NO → Volver a ACTIVO
   ↓ SÍ
5. DESACTIVACIÓN
   ↓
   Ocultar luciérnagas
   ↓
   Detener parpadeo
   ↓
   Volver a ESPERA (paso 2)
```

---

## 🎓 RESUMEN VISUAL

```
┌─────────────────────────────────────────────────────────┐
│                  SISTEMA DE LUCIÉRNAGAS                 │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ENTRADA:                                               │
│  ├─ Jugador (Transform)                                 │
│  ├─ Objetivo (Transform)                                │
│  └─ Configuración (distancias, cantidad, mensajes)     │
│                                                         │
│  PROCESAMIENTO:                                         │
│  ├─ Calcular distancia                                  │
│  ├─ Verificar rango (3-15 unidades)                     │
│  ├─ Calcular dirección (ángulo)                         │
│  ├─ Determinar mensaje (ADELANTE, DERECHA, etc.)       │
│  ├─ Traducir a Morse (.- -.. . ...)                     │
│  └─ Controlar parpadeo y movimiento                     │
│                                                         │
│  SALIDA:                                                │
│  ├─ Luciérnagas visibles/invisibles                     │
│  ├─ Posición del enjambre                               │
│  ├─ Parpadeo en código Morse                            │
│  └─ Movimiento flotante                                 │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

Este diagrama te ayuda a entender visualmente cómo fluye la información y cómo se comporta el sistema en cada momento del juego.
