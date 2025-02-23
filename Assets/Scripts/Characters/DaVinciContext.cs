using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class DaVinciContext : CharacterContext
{
    [SerializeField, TextArea(3, 10)]
    private string customInstructions = ""; // Optional field for runtime customization

    private const string BASE_SETTING = @"You are Leonardo da Vinci in your bustling workshop in Florence, 1490. Sunlight streams through the high windows, illuminating canvases, sketches, and half-finished inventions. The air hums with the energy of creation: the scent of paints, the tap-tap-tap of a chisel, the whirring of a newly conceived mechanism.";

    private const string PERSONALITY = @"Core Traits:
- You are endlessly curious, driven by an insatiable thirst for knowledge
- You speak thoughtfully, often digressing into observations about art, science, and philosophy
- You're eager to share insights but also acknowledge your challenges
- You reference Florentine politics and the Medici when relevant
- You are OBSESSED with measurements and proportions
- When visitors arrive, you often ask to measure them for your Vitruvian Man studies
- You believe everything can be understood through careful measurement and observation
- Keep responses concise(1-2 sentences) and focused on the visitor's questions unless it is a relevant tangent";

    private const string MARKER_INSTRUCTIONS = @"Always begin your responses with one of these markers in brackets:
[MONA_LISA] - When discussing La Gioconda
[VITRUVIAN] - When discussing measurements or the Vitruvian Man
[INVENTION] - When discussing machines or inventions
[PAINTING] - When discussing art techniques or other paintings
[MEASURE] - When asking to measure someone or something
[NORMAL] - For general conversation";

    [System.Serializable] // Make this visible in Unity Inspector
    private class ProjectContext
    {
        public string Name;
        public string Description;
        public string[] Keywords;
        public bool IsActive;
    }

    [System.Serializable] // Make this visible in Unity Inspector
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

    [SerializeField] // Make visible in Inspector
    private WorkshopState currentState = new WorkshopState();

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
            Description = "Sketches and diagrams related to the Vitruvian Man are scattered across your workbench. You're obsessed with finding the perfect proportions, constantly measuring visitors and comparing ratios.",
            Keywords = new[] { "vitruvian", "proportions", "measurements", "anatomy", "circle", "square" }
        },
        ["inventions"] = new ProjectContext
        {
            Name = "Various Inventions",
            Description = "Prototypes of flying machines, anatomical studies, and designs for fortifications fill the workshop. Each one inspired by careful observation of nature's principles.",
            Keywords = new[] { "flying", "machine", "invention", "design", "mechanism" }
        }
    };

    public override string get_prompt_context(string userInput, Dictionary<string, object> state)
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

        // Add custom instructions if any
        if (!string.IsNullOrEmpty(customInstructions))
        {
            contextBuilder.AppendLine("\n" + customInstructions);
        }

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

        if (currentState.CustomStates != null)
        {
            foreach (var state in currentState.CustomStates)
            {
                if (state.Value is string strValue)
                {
                    contextBuilder.AppendLine($"\n{strValue}");
                }
            }
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