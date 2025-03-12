using System.Collections.Generic;
using UnityEngine;
using System.Text;

// Add this to ServiceManager to maintain conversation history
public class ConversationHistory : MonoBehaviour
{
    [Header("Conversation Settings")]
    [SerializeField] private int maxExchangesToRemember = 3;
    [SerializeField] private bool enableConversationMemory = true;
    
    // List of past exchanges (each entry is a user question and AI response)
    private List<(string userInput, string aiResponse)> conversationHistory = new List<(string userInput, string aiResponse)>();
    
    // Add a new exchange to the history
    public void AddExchange(string userInput, string aiResponse)
    {
        if (!enableConversationMemory) return;
        
        // Add the new exchange
        conversationHistory.Add((userInput, aiResponse));
        
        // Trim the history if it's too long
        while (conversationHistory.Count > maxExchangesToRemember)
        {
            conversationHistory.RemoveAt(0);
        }
        
        Debug.Log($"Added conversation exchange. History contains {conversationHistory.Count} exchanges.");
    }
    
    // Get the conversation history formatted for inclusion in the context
    public string GetFormattedHistory()
    {
        if (!enableConversationMemory || conversationHistory.Count == 0)
        {
            return string.Empty;
        }
        
        var historyBuilder = new StringBuilder();
        historyBuilder.AppendLine("\nRecent conversation history (remember this context):");
        
        foreach (var exchange in conversationHistory)
        {
            historyBuilder.AppendLine($"Visitor: {exchange.userInput}");
            historyBuilder.AppendLine($"Leonardo: {exchange.aiResponse}\n");
        }
        
        return historyBuilder.ToString();
    }
    
    // Clear the conversation history
    public void ClearHistory()
    {
        conversationHistory.Clear();
        Debug.Log("Conversation history cleared");
    }
}