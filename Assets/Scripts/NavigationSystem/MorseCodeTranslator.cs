using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Traductor de texto a código Morse
/// </summary>
public static class MorseCodeTranslator {
    
    private static readonly Dictionary<char, string> morseCode = new Dictionary<char, string>() {
        {'A', ".-"}, {'B', "-..."}, {'C', "-.-."}, {'D', "-.."}, {'E', "."}, {'F', "..-."},
        {'G', "--."}, {'H', "...."}, {'I', ".."}, {'J', ".---"}, {'K', "-.-"}, {'L', ".-.."},
        {'M', "--"}, {'N', "-."}, {'O', "---"}, {'P', ".--."}, {'Q', "--.-"}, {'R', ".-."},
        {'S', "..."}, {'T', "-"}, {'U', "..-"}, {'V', "...-"}, {'W', ".--"}, {'X', "-..-"},
        {'Y', "-.--"}, {'Z', "--.."}, 
        {'0', "-----"}, {'1', ".----"}, {'2', "..---"}, {'3', "...--"}, {'4', "....-"},
        {'5', "....."}, {'6', "-...."}, {'7', "--..."}, {'8', "---.."}, {'9', "----."},
        {' ', "/"}
    };

    /// <summary>
    /// Convierte texto a código Morse
    /// </summary>
    public static string TextToMorse(string text) {
        text = text.ToUpper();
        string morse = "";
        
        foreach (char c in text) {
            if (morseCode.ContainsKey(c)) {
                morse += morseCode[c] + " ";
            }
        }
        
        return morse.Trim();
    }

    /// <summary>
    /// Obtiene la duración de un símbolo Morse en segundos
    /// </summary>
    public static float GetSymbolDuration(char symbol, float dotDuration = 0.2f) {
        switch (symbol) {
            case '.': return dotDuration;              // Punto
            case '-': return dotDuration * 3f;         // Raya (3 veces el punto)
            case ' ': return dotDuration * 3f;         // Espacio entre letras
            case '/': return dotDuration * 7f;         // Espacio entre palabras
            default: return dotDuration;
        }
    }
}
