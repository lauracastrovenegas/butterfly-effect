using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public abstract class CharacterContext : MonoBehaviour
{
    public abstract string get_prompt_context(string userInput, Dictionary<string, object> state);
    
    // Modified ParseResponse method to remove any leading bracketed marker
    public virtual (string marker, string response) ParseResponse(string fullResponse)
    {
        if (string.IsNullOrEmpty(fullResponse)) return ("NORMAL", fullResponse);
        
        string marker = "NORMAL";
        string pattern = @"^\s*\[(?<marker>[^\]]+)\]\s*";
        var match = Regex.Match(fullResponse, pattern);
        if (match.Success)
        {
            // Capture the marker in uppercase
            marker = match.Groups["marker"].Value.ToUpperInvariant();
            fullResponse = fullResponse.Substring(match.Length);
        }
        return (marker, fullResponse);
    }
}