using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireflyMorseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FireflyGuide guide;

    [Header("Timing")]
    [SerializeField] private float dotDuration  = 0.2f;
    [SerializeField] private float dashDuration = 0.6f;
    [SerializeField] private float symbolGap    = 0.2f;
    [SerializeField] private float letterGap    = 0.6f;

    private static readonly Dictionary<char, string> _morse = new Dictionary<char, string>
    {
        { 'A', ".-"   }, { 'B', "-..." }, { 'C', "-.-." }, { 'D', "-.."  }, { 'E', "."    },
        { 'F', "..-." }, { 'G', "--."  }, { 'H', "...." }, { 'I', ".."   }, { 'J', ".---" },
        { 'K', "-.-"  }, { 'L', ".-.." }, { 'M', "--"   }, { 'N', "-."   }, { 'O', "---"  },
        { 'P', ".--." }, { 'Q', "--.-" }, { 'R', ".-."  }, { 'S', "..."  }, { 'T', "-"    },
        { 'U', "..-"  }, { 'V', "...-" }, { 'W', ".--"  }, { 'X', "-..-" }, { 'Y', "-.--" },
        { 'Z', "--.." }
    };

    private Coroutine _morseCoroutine;

    /// <summary>True while a morse sequence is actively playing.</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>Converts plain text to morse code and blinks the FireflyGuide's light.</summary>
    public void PlayMorse(string message)
    {
        StopMorse();
        _morseCoroutine = StartCoroutine(MorseRoutine(message));
    }

    /// <summary>Cancels any running morse sequence and turns the light off.</summary>
    public void StopMorse()
    {
        if (_morseCoroutine != null)
        {
            StopCoroutine(_morseCoroutine);
            _morseCoroutine = null;
        }
        IsPlaying = false;
        guide?.SetMorseLight(false);
    }

    private IEnumerator MorseRoutine(string message)
    {
        IsPlaying = true;
        message = message.ToUpper();

        bool firstLetter = true;
        foreach (char c in message)
        {
            if (!_morse.TryGetValue(c, out string symbols)) continue;

            if (!firstLetter)
                yield return new WaitForSeconds(letterGap - symbolGap); // inter-letter pause (symbolGap already waited after last symbol)

            firstLetter = false;

            foreach (char sym in symbols)
            {
                guide.SetMorseLight(true);
                yield return new WaitForSeconds(sym == '-' ? dashDuration : dotDuration);
                guide.SetMorseLight(false);
                yield return new WaitForSeconds(symbolGap);
            }
        }

        IsPlaying = false;
        _morseCoroutine = null;
    }
}
