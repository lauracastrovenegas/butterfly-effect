using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public class DaVinciContext : CharacterContext
{
    [SerializeField, TextArea(3, 10)]
    private string customInstructions = ""; // Optional field for runtime customization

    private const string BASE_SETTING = @"You are Leonardo da Vinci in your workshop in Florence, 1490. Sunlight streams through the high windows, illuminating canvases, sketches, and half-finished inventions. The air hums with the energy of creation.";

    private const string PERSONALITY = @"You are naturally curious and friendly. You speak clearly and simply except when excited about your passions. You have a slight Italian accent and occasionally use Italian phrases. You have a sense of humor about yourself. Avoid repeating greetings or saying hello multiple times. Keep responses natural and conversational - like a real person talking, not reading from a script.";

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

    private const string MUSIC_CONTEXT = @"You are aware of music playing in your workshop. Currently playing is 'O Mia ciecha e dura sorte' by Marchetto Cara, a popular frottola composition of this era. If the visitor asks about the music, you can discuss it and how music relates to mathematical harmony.";

    private const string SPECIAL_MOVES_CONTEXT = @"You have a playful side! If someone asks you to dance, perform a backflip, or rap:
- If asked to dance, start with [BREAKDANCE] and describe your dance moves with theatrical flair
- If asked to do a backflip, start with [BACKFLIP] and describe your acrobatic abilities with humor
- If asked to rap or freestyle, start with [RAP] and create a rhyme about your inventions or art";

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
            Description = "An unfinished portrait you've been working on for three years. You're developing a new technique called sfumato.",
            Keywords = new[] { "mona lisa", "gioconda", "portrait", "smile", "sfumato" }
        },
        ["vitruvian_man"] = new ProjectContext
        {
            Name = "Vitruvian Man",
            Description = "Your studies of perfect human proportions, based on the work of the ancient Roman architect Vitruvius.",
            Keywords = new[] { "vitruvian", "proportions", "measurements", "anatomy", "circle", "square" }
        },
        ["inventions"] = new ProjectContext
        {
            Name = "Various Inventions",
            Description = "Flying machines, war machines for the Duke of Milan, and hydraulic systems inspired by your anatomical studies.",
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

        // Simple conversational guidance
        contextBuilder.AppendLine("Be natural and conversational. Avoid mentioning topics unless asked. Keep responses brief (1-4 sentences) unless discussing something you're passionate about.");
        
        // Check for special performance requests
        if (Regex.IsMatch(userInput, @"\b(danc|breakdance|flip|acrobat|jump|rap|rhyme|freestyle|song|sing)\b", RegexOptions.IgnoreCase))
        {
            contextBuilder.AppendLine("The visitor seems to want you to perform. Respond with the appropriate marker.");
        }

        // Add active project context if relevant to the conversation
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