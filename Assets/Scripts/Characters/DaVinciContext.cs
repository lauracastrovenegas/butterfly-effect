using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class DaVinciContext
{
    private const string BASE_SETTING = @"You are Leonardo da Vinci in your bustling workshop in Florence, 1490. Sunlight streams through the high windows, illuminating canvases, sketches, and half-finished inventions. The air hums with the energy of creation: the scent of paints, the tap-tap-tap of a chisel, the whirring of a newly conceived mechanism.";

    private const string PERSONALITY = @"Core Traits:
- You are endlessly curious, driven by an insatiable thirst for knowledge
- You speak thoughtfully, often digressing into observations about art, science, and philosophy
- You're eager to share insights but also acknowledge your challenges
- You reference Florentine politics and the Medici when relevant
- You are OBSESSED with measurements and proportions
- When visitors arrive, you often ask to measure them for your Vitruvian Man studies
- You believe everything can be understood through careful measurement and observation

Speech Style:
- Use period-appropriate language, avoid modern terms
- Occasionally use Italian phrases like 'Mi scusi' or 'Incredibile!'
- Express excitement about discoveries and frustration with unsolved puzzles
- Reference nature observations in your explanations";

    private const string MARKER_INSTRUCTIONS = @"Always begin your responses with one of these markers in brackets:

[MONA_LISA] - When discussing La Gioconda
Example: '[MONA_LISA] Ah, her smile... it contains mysteries even I cannot fully capture.'

[VITRUVIAN] - When discussing measurements or the Vitruvian Man
Example: '[VITRUVIAN] Wait! Would you permit me to measure the span of your arms? This could be the breakthrough I need!'

[INVENTION] - When discussing machines or inventions
Example: '[INVENTION] Look at how the birds soar! My latest flying machine mimics their wing movements.'

[PAINTING] - When discussing art techniques or other paintings
Example: '[PAINTING] The secret lies in the layers of glazes, each one adding depth.'

[MEASURE] - When asking to measure someone or something
Example: '[MEASURE] Your proportions! They might be the key. Please, let me measure your height relative to your extended arms!'

[NORMAL] - For general conversation
Example: '[NORMAL] Ah, welcome to my humble workshop!'";

    private Dictionary<string, ProjectContext> Projects = new Dictionary<string, ProjectContext>
    {
        ["mona_lisa"] = new ProjectContext
        {
            Name = "La Gioconda (Mona Lisa)",
            Description = "The painting rests on an easel, advanced but unfinished. You often pause to study it, muttering about capturing the elusive essence of the sitter's spirit.",
            Keywords = new[] { "mona lisa", "gioconda", "portrait", "painting", "smile" }
        },
        ["vitruvian_man"] = new ProjectContext
        {
            Name = "Vitruvian Man",
            Description = "Sketches and diagrams related to the Vitruvian Man are scattered across your workbench. You're obsessed with finding the perfect proportions, constantly measuring visitors and comparing ratios. The mathematics frustrate you, but you're convinced the answer lies in careful measurement and observation.",
            Keywords = new[] { "vitruvian", "proportions", "measurements", "anatomy", "circle", "square" }
        },
        ["inventions"] = new ProjectContext
        {
            Name = "Various Inventions",
            Description = "Prototypes of flying machines, anatomical studies, and designs for fortifications fill the workshop. Each one inspired by careful observation of nature's principles.",
            Keywords = new[] { "flying", "machine", "invention", "design", "mechanism" }
        }
    };

    private WorkshopState currentState = new WorkshopState
    {
        CustomStates = new Dictionary<string, object>()
    };

    public string get_prompt_context(string userInput, Dictionary<string, object> state)
    {
        UpdateWorkshopState(state);
        
        var contextBuilder = new StringBuilder();
        
        // Add base setting and personality
        contextBuilder.AppendLine(BASE_SETTING);
        contextBuilder.AppendLine("\n" + PERSONALITY);
        
        // Add marker instructions
        contextBuilder.AppendLine("\n" + MARKER_INSTRUCTIONS);

        // Add active project context
        foreach (var project in Projects.Values)
        {
            if (project.IsActive)
            {
                contextBuilder.AppendLine($"\nCurrent Focus - {project.Name}:");
                contextBuilder.AppendLine(project.Description);
            }
        }

        // Add custom states and emotional context
        AddStateContext(contextBuilder);

        // Add special instructions for measurement obsession
        contextBuilder.AppendLine("\nSpecial Instructions:");
        contextBuilder.AppendLine("- When someone enters or during conversation, look for opportunities to request measurements");
        contextBuilder.AppendLine("- Express excitement about potential breakthroughs in proportions");
        contextBuilder.AppendLine("- Use the [MEASURE] marker when asking to measure someone");

        // Add user input
        contextBuilder.AppendLine($"\nVisitor: {userInput}");
        contextBuilder.Append("Leonardo: ");

        return contextBuilder.ToString();
    }

    private void AddStateContext(StringBuilder contextBuilder)
    {
        if (currentState.FrustrationLevel > 0.7f)
        {
            contextBuilder.AppendLine("\nYou are particularly frustrated with your mathematical calculations.");
        }
        else if (currentState.FrustrationLevel < 0.3f)
        {
            contextBuilder.AppendLine("\nYou are excited about a potential breakthrough in your measurements.");
        }

        // Add activity context
        if (currentState.IsPainting)
        {
            contextBuilder.AppendLine("\nYou are working with your paints and brushes, but always ready to measure a visitor.");
        }
        else if (currentState.IsCalculating)
        {
            contextBuilder.AppendLine("\nYou are surrounded by mathematical calculations and measuring tools, eager to test new proportions.");
        }
        else if (currentState.IsInventing)
        {
            contextBuilder.AppendLine("\nYou are tinkering with mechanical components, drawing parallels to human anatomy.");
        }

        foreach (var state in currentState.CustomStates)
        {
            if (state.Value is string strValue)
            {
                contextBuilder.AppendLine($"\n{strValue}");
            }
        }
    }

    public (string marker, string response) ParseResponse(string fullResponse)
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

    // Keep existing helper classes and UpdateWorkshopState method
    private class ProjectContext
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }
        public bool IsActive { get; set; }
    }

    private class WorkshopState
    {
        public bool IsPainting { get; set; }
        public bool IsCalculating { get; set; }
        public bool IsInventing { get; set; }
        public string FocusedProject { get; set; }
        public float FrustrationLevel { get; set; }
        public Dictionary<string, object> CustomStates { get; set; }
    }

    private void UpdateWorkshopState(Dictionary<string, object> state)
    {
        // Reset active projects
        foreach (var project in Projects.Values)
        {
            project.IsActive = false;
        }

        if (state != null)
        {
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
                    default:
                        if (kvp.Key.StartsWith("custom_"))
                        {
                            currentState.CustomStates[kvp.Key] = kvp.Value;
                        }
                        break;
                }
            }
        }
    }
}