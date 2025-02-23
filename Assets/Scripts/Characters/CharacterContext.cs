using UnityEngine;
using System.Collections.Generic;

public abstract class CharacterContext : MonoBehaviour
{
    public virtual string get_prompt_context(string userInput, Dictionary<string, object> state)
    {
        return string.Empty;
    }

    public virtual (string marker, string response) ParseResponse(string fullResponse)
    {
        if (string.IsNullOrEmpty(fullResponse)) return ("NORMAL", fullResponse);

        if (fullResponse.StartsWith("[") && fullResponse.Contains("]"))
        {
            int endBracket = fullResponse.IndexOf("]");
            string marker = fullResponse.Substring(1, endBracket - 1);
            string cleanResponse = fullResponse.Substring(endBracket + 1).TrimStart();
            return (marker, cleanResponse);
        }

        return ("NORMAL", fullResponse);
    }
}