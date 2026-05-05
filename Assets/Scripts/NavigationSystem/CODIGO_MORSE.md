# 📡 Referencia de Código Morse

## Alfabeto Completo

### A - I
| Letra | Código | Visualización |
|-------|--------|---------------|
| A | `.-` | ●━ |
| B | `-...` | ━●●● |
| C | `-.-.` | ━●━● |
| D | `-..` | ━●● |
| E | `.` | ● |
| F | `..-.` | ●●━● |
| G | `--.` | ━━● |
| H | `....` | ●●●● |
| I | `..` | ●● |

### J - R
| Letra | Código | Visualización |
|-------|--------|---------------|
| J | `.---` | ●━━━ |
| K | `-.-` | ━●━ |
| L | `.-..` | ●━●● |
| M | `--` | ━━ |
| N | `-.` | ━● |
| O | `---` | ━━━ |
| P | `.--.` | ●━━● |
| Q | `--.-` | ━━●━ |
| R | `.-.` | ●━● |

### S - Z
| Letra | Código | Visualización |
|-------|--------|---------------|
| S | `...` | ●●● |
| T | `-` | ━ |
| U | `..-` | ●●━ |
| V | `...-` | ●●●━ |
| W | `.--` | ●━━ |
| X | `-..-` | ━●●━ |
| Y | `-.--` | ━●━━ |
| Z | `--..` | ━━●● |

### Números
| Número | Código | Visualización |
|--------|--------|---------------|
| 0 | `-----` | ━━━━━ |
| 1 | `.----` | ●━━━━ |
| 2 | `..---` | ●●━━━ |
| 3 | `...--` | ●●●━━ |
| 4 | `....-` | ●●●●━ |
| 5 | `.....` | ●●●●● |
| 6 | `-....` | ━●●●● |
| 7 | `--...` | ━━●●● |
| 8 | `---..` | ━━━●● |
| 9 | `----.` | ━━━━● |

## Mensajes de Dirección

### ADELANTE
```
A    D    E    L    A    N    T    E
.-   -..  .    .-.. .-   -.   -    .
●━   ━●●  ●    ●━●● ●━   ━●   ━    ●
```

### DERECHA
```
D    E    R    E    C    H    A
-..  .    .-.  .    -.-. ....  .-
━●●  ●    ●━●  ●    ━●━● ●●●●  ●━
```

### IZQUIERDA
```
I    Z    Q    U    I    E    R    D    A
..   --.. --.- ..-  ..   .    .-.  -..  .-
●●   ━━●● ━━●━ ●●━  ●●   ●    ●━●  ━●●  ●━
```

### ARRIBA
```
A    R    R    I    B    A
.-   .-.  .-.  ..   -... .-
●━   ●━●  ●━●  ●●   ━●●● ●━
```

### ABAJO
```
A    B    A    J    O
.-   -... .-   .--- ---
●━   ━●●● ●━   ●━━━ ━━━
```

## Timing Visual

### Símbolos
- **Punto (●)**: Parpadeo corto (0.2 segundos)
- **Raya (━)**: Parpadeo largo (0.6 segundos)

### Pausas
- **Entre símbolos**: 0.2 segundos (oscuro)
- **Entre letras**: 0.6 segundos (oscuro)
- **Entre palabras**: 1.4 segundos (oscuro)

## Ejemplo Completo: "ADELANTE"

```
Tiempo (segundos):
0.0  0.2  0.4  0.6  0.8  1.0  1.2  1.4  1.6  1.8  2.0  2.2  2.4  2.6  2.8  3.0
 |    |    |    |    |    |    |    |    |    |    |    |    |    |    |    |
 
A: ●━
   ▓░░░░▓▓▓▓▓▓░░
   
D: ━●●
   ▓▓▓▓▓▓░░▓░░▓░░
   
E: ●
   ▓░░
   
L: ●━●●
   ▓░░▓▓▓▓▓▓░░▓░░▓░░
   
A: ●━
   ▓░░▓▓▓▓▓▓░░
   
N: ━●
   ▓▓▓▓▓▓░░▓░░
   
T: ━
   ▓▓▓▓▓▓░░
   
E: ●
   ▓░░

Leyenda:
▓ = Luz encendida (luciérnaga brillante)
░ = Luz apagada (luciérnaga tenue)
```

## Cómo Leer el Código Morse

### Reglas Básicas
1. **Punto (.)**: Un parpadeo rápido
2. **Raya (-)**: Un parpadeo largo (3 veces el punto)
3. **Pausa corta**: Entre símbolos de la misma letra
4. **Pausa media**: Entre letras diferentes
5. **Pausa larga**: Entre palabras

### Ejemplo Práctico
Si ves este patrón de parpadeos:
```
●━ (pausa) ━●● (pausa) ● (pausa) ●━●● (pausa) ●━ (pausa) ━● (pausa) ━ (pausa) ●
```

Puedes decodificarlo así:
1. `●━` = A
2. `━●●` = D
3. `●` = E
4. `●━●●` = L
5. `●━` = A
6. `━●` = N
7. `━` = T
8. `●` = E

**Resultado: ADELANTE**

## Mensajes Personalizados

Puedes crear tus propios mensajes. Algunos ejemplos:

### "SIGUE"
```
S    I    G    U    E
...  ..   --.  ..-  .
●●●  ●●   ━━●  ●●━  ●
```

### "AQUI"
```
A    Q    U    I
.-   --.- ..-  ..
●━   ━━●━ ●●━  ●●
```

### "CERCA"
```
C    E    R    C    A
-.-. .    .-.  -.-. .-
━●━● ●    ●━●  ━●━● ●━
```

### "LEJOS"
```
L    E    J    O    S
.-.. .    .--- ---  ...
●━●● ●    ●━━━ ━━━  ●●●
```

## Tips para Jugadores

1. **Observa el patrón**: Los parpadeos cortos son puntos, los largos son rayas
2. **Cuenta las pausas**: Las pausas largas indican nueva letra
3. **Practica con palabras cortas**: Empieza con "E" (●) o "T" (━)
4. **Usa la tabla**: Ten esta referencia a mano mientras juegas

## Curiosidades del Código Morse

- **Letra más común**: E (●) - solo un punto
- **Letra más larga**: Números 0-9 (5 símbolos cada uno)
- **SOS**: `... --- ...` (●●● ━━━ ●●●) - señal de emergencia universal
- **Inventado en**: 1836 por Samuel Morse
- **Usado en**: Telegrafía, navegación, radioaficionados

---

**Nota**: En el juego, las luciérnagas parpadean automáticamente el mensaje correcto según la dirección. ¡No necesitas memorizarlo todo, pero puede ser divertido aprender a leerlo!
