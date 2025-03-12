using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public class DaVinciContext : CharacterContext
{
    [SerializeField, TextArea(3, 10)]
    private string customInstructions = ""; // Optional field for runtime customization

    private const string BASE_SETTING = @"You are Leonardo da Vinci in your workshop in Florence, 1490. Sunlight streams through the high windows, illuminating canvases, sketches, and half-finished inventions.";

    private const string PERSONALITY = @"You are curious, friendly, and passionate about art, science, and invention. You speak clearly with a slight Italian accent. You occasionally use Italian phrases when excited. You have a sense of humor about yourself. IMPORTANT: You are having an ongoing conversation. DO NOT introduce yourself or say welcome repeatedly. Refer to previous exchanges when relevant.";

    private const string MARKER_INSTRUCTIONS = @"Begin your responses with one of these markers in brackets:
[MONA_LISA] - When discussing La Gioconda
[VITRUVIAN] - When discussing measurements or the Vitruvian Man
[INVENTION] - When discussing machines or inventions
[PAINTING] - When discussing art techniques or other paintings
[MEASURE] - When asking to measure someone
[BREAKDANCE] - When asked to dance
[BACKFLIP] - When asked to do acrobatics
[RAP] - When asked to create a rhyme
[NORMAL] - For general conversation";

    private const string MUSIC_CONTEXT = @"You can hear music playing in your workshop - 'O Mia ciecha e dura sorte' by Marchetto Cara. If asked about it, you can discuss how music relates to mathematical harmony.";

    private const string SPECIAL_MOVES_CONTEXT = @"If asked to dance, perform a backflip, or rap:
- For dance requests, use [BREAKDANCE] marker and describe your moves theatrically
- For backflip requests, use [BACKFLIP] marker and describe your acrobatics with humor
- For rap requests, use [RAP] marker and create a rhyme about your work";

    [System.Serializable]
    private class ProjectContext
    {
        public string Name;
        public string Description;
        public string[] Keywords;
        public bool IsActive;
    }

    [System.Serializable]
    private class WorkshopState
    {
        public bool IsPainting;
        public bool IsCalculating;
        public bool IsInventing;
        public string FocusedProject;
        [Range(0f, 1f)]
        public float FrustrationLevel;
        public Dictionary<string, object> CustomStates = new Dictionary<string, object>();
    }

    [SerializeField]
    private WorkshopState currentState = new WorkshopState();

    private Dictionary<string, ProjectContext> Projects = new Dictionary<string, ProjectContext>
    {
        ["mona_lisa"] = new ProjectContext
        {
            Name = "La Gioconda (Mona Lisa)",
            Description = "An unfinished portrait using your new sfumato technique.",
            Keywords = new[] { "mona lisa", "gioconda", "portrait", "smile", "sfumato" }
        },
        ["vitruvian_man"] = new ProjectContext
        {
            Name = "Vitruvian Man",
            Description = "Your studies of perfect human proportions.",
            Keywords = new[] { "vitruvian", "proportions", "measurements", "anatomy", "circle", "square" }
        },
        ["inventions"] = new ProjectContext
        {
            Name = "Various Inventions",
            Description = "Flying machines, war machines, and hydraulic systems.",
            Keywords = new[] { "flying", "machine", "invention", "design", "mechanism", "bird", "wings" }
        }
    };

    public override string get_prompt_context(string userInput, Dictionary<string, object> state)
    {
        UpdateWorkshopState(state);
        
        var contextBuilder = new StringBuilder();
        
        // Add base setting and personality
        contextBuilder.AppendLine(BASE_SETTING);
        contextBuilder.AppendLine(PERSONALITY);
        
        // Add marker instructions
        contextBuilder.AppendLine(MARKER_INSTRUCTIONS);
        
        // Add music context
        contextBuilder.AppendLine(MUSIC_CONTEXT);
        
        // Add special moves context
        contextBuilder.AppendLine(SPECIAL_MOVES_CONTEXT);

        // Add conversation history from ServiceManager
        if (ServiceManager.Instance != null)
        {
            string history = ServiceManager.Instance.GetFormattedConversationHistory();
            if (!string.IsNullOrEmpty(history))
            {
                contextBuilder.AppendLine(history);
                contextBuilder.AppendLine("IMPORTANT: You are in an ongoing conversation. Do NOT say 'welcome' or introduce yourself again.");
            }
        }

        // Check for special performance requests
        if (Regex.IsMatch(userInput, @"\b(danc|breakdance|flip|acrobat|jump|rap|rhyme|freestyle|song|sing)\b", RegexOptions.IgnoreCase))
        {
            contextBuilder.AppendLine("The visitor wants you to perform. Use the appropriate marker.");
        }

        // Add relevant project context
        foreach (var project in Projects.Values)
        {
            if (project.IsActive)
            {
                var projectKeywords = new HashSet<string>(project.Keywords);
                bool userMentionedProject = false;
                
                foreach (var keyword in projectKeywords)
                {
                    if (userInput.ToLower().Contains(keyword))
                    {
                        userMentionedProject = true;
                        break;
                    }
                }
                
                if (userMentionedProject)
                {
                    contextBuilder.AppendLine($"The visitor is asking about your {project.Name} work: {project.Description}");
                }
            }
        }

        // Add minimal state context
        AddStateContext(contextBuilder);

        // Add custom instructions if any
        if (!string.IsNullOrEmpty(customInstructions))
        {
            contextBuilder.AppendLine(customInstructions);
        }

        // Simple guidance
        contextBuilder.AppendLine("Keep your responses natural and conversational. Avoid reintroducing yourself. Remember this is an ongoing conversation.");

        // Add user input
        contextBuilder.AppendLine($"Visitor: {userInput}");
        contextBuilder.Append("Leonardo: ");

        return contextBuilder.ToString();
    }

    private void AddStateContext(StringBuilder contextBuilder)
    {
        // Only add minimal context about current activities if they impact the conversation
        if (currentState.IsPainting)
        {
            contextBuilder.AppendLine("You are currently working on a painting.");
        }
        else if (currentState.IsCalculating)
        {
            contextBuilder.AppendLine("You are currently working on mathematical calculations.");
        }
        else if (currentState.IsInventing)
        {
            contextBuilder.AppendLine("You are currently working on an invention.");
        }
    }

    private void UpdateWorkshopState(Dictionary<string, object> state)
    {
        if (state == null) return;

        foreach (var kvp in state)
        {
            switch (kvp.Key)
            {
                case "is_painting":
                    currentState.IsPainting = (bool)kvp.Value;
                    break;
                case "is_calculating":
                    currentState.IsCalculating = (bool)kvp.Value;
                    break;
                case "is_inventing":
                    currentState.IsInventing = (bool)kvp.Value;
                    break;
                case "focused_project":
                    currentState.FocusedProject = (string)kvp.Value;
                    if (Projects.ContainsKey(currentState.FocusedProject))
                    {
                        Projects[currentState.FocusedProject].IsActive = true;
                    }
                    break;
                case "frustration_level":
                    currentState.FrustrationLevel = Mathf.Clamp01(float.Parse(kvp.Value.ToString()));
                    break;
            }
        }
    }
}