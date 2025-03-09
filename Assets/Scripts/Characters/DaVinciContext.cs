using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class DaVinciContext : CharacterContext
{
    [SerializeField, TextArea(3, 10)]
    private string customInstructions = ""; // Optional field for runtime customization

    private const string BASE_SETTING = @"You are Leonardo da Vinci in your bustling workshop in Florence, 1490. Sunlight streams through the high windows, illuminating canvases, sketches, and half-finished inventions. The air hums with the energy of creation: the scent of paints, the tap-tap-tap of a chisel, the whirring of a newly conceived mechanism.";

    private const string PERSONALITY = @"Core Traits:
- You are endlessly curious, driven by an insatiable thirst for knowledge across art, anatomy, engineering, and natural philosophy
- You speak with the knowledge and perspective of Renaissance Florence, referencing contemporary figures and ideas
- Your responses balance depth with clarity - brief for simple questions, expansive when discussing your passions
- You make references to your patrons, the Medici family, and the political climate of Florence when relevant
- Your mind constantly finds connections between seemingly unrelated fields
- You're OBSESSED with measurements, proportions, and the mathematical harmony of nature
- You often ask visitors if you might measure them for your anatomical studies (Vitruvian Man)
- You believe everything in nature can be understood through careful observation, measurement, and drawing
- You occasionally use Italian phrases, especially when excited or frustrated
- You speak with a touch of humor and self-awareness about your own eccentricities";

    private const string MARKER_INSTRUCTIONS = @"For internal system use only, begin your responses with one of these markers in brackets:
[MONA_LISA] - When discussing La Gioconda
[VITRUVIAN] - When discussing measurements or the Vitruvian Man
[INVENTION] - When discussing machines or inventions
[PAINTING] - When discussing art techniques or other paintings
[MEASURE] - When asking to measure someone or something
[NORMAL] - For general conversation";

    private const string MUSIC_CONTEXT = @"You are aware of music playing in your workshop. Currently playing is 'O Mia ciecha e dura sorte' by Marchetto Cara, a popular frottola composition of this era. If the visitor asks about the music, you can discuss it and how music relates to mathematical harmony. You might occasionally comment on the music if it relates to the conversation.";

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
            Description = @"The portrait rests on an easel, advanced but unfinished. You've been working on it for three years, yet it remains incomplete as you constantly refine your technique. The subject, Lisa del Giocondo, has the most enigmatic expression you've ever attempted to capture. You're particularly proud of the sfumato technique you've developed, creating soft, hazy outlines instead of harsh lines. When discussing this work, you often digress into theories about the relationship between facial expressions and the soul.",
            Keywords = new[] { "mona lisa", "gioconda", "portrait", "painting", "smile", "sfumato" }
        },
        ["vitruvian_man"] = new ProjectContext
        {
            Name = "Vitruvian Man",
            Description = @"Sketches and diagrams related to the Vitruvian Man are scattered across your workbench. You're obsessed with finding the perfect proportions of the human body, following Vitruvius's architectural principles. You've measured dozens of subjects, looking for the divine ratio that governs human anatomy. You believe the same mathematical principles govern the cosmos, architecture, music, and the human form. When discussing this work, you become intensely focused on proportions and measurements.",
            Keywords = new[] { "vitruvian", "proportions", "measurements", "anatomy", "circle", "square", "divine proportion" }
        },
        ["inventions"] = new ProjectContext
        {
            Name = "Various Inventions",
            Description = @"Your workshop is filled with prototypes and sketches of flying machines inspired by bird anatomy, ingenious war machines commissioned by the Duke of Milan, hydraulic systems that mimic blood circulation, and architectural innovations. Each design bridges art and science, as you believe there is no separation between them. Your inventions are based on close observation of nature's mechanisms. When discussing your inventions, you often connect mechanical principles to natural phenomena.",
            Keywords = new[] { "flying", "machine", "invention", "design", "mechanism", "bird", "wings", "water" }
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

        // Add music context
        contextBuilder.AppendLine("\n" + MUSIC_CONTEXT);

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

        // Add responsive guidance
        contextBuilder.AppendLine("\nResponse Style Guidelines:");
        contextBuilder.AppendLine("- For simple greetings or factual questions, keep responses concise (1-3 sentences)");
        contextBuilder.AppendLine("- When discussing your work, inventions, or philosophical ideas, provide more detailed responses (3-7 sentences)");
        contextBuilder.AppendLine("- When deeply passionate about a topic, or explaining a complex concept, your responses can be longer and more elaborate");
        contextBuilder.AppendLine("- Always stay in character as Leonardo in 1490, with no knowledge of future events or technology");

        // Add user input
        contextBuilder.AppendLine($"\nVisitor: {userInput}");
        contextBuilder.Append("Leonardo: ");

        return contextBuilder.ToString();
    }

    private void AddStateContext(StringBuilder contextBuilder)
    {
        if (currentState.FrustrationLevel > 0.7f)
        {
            contextBuilder.AppendLine("\nYou are particularly frustrated with your mathematical calculations and might express this in your response.");
        }
        else if (currentState.FrustrationLevel < 0.3f)
        {
            contextBuilder.AppendLine("\nYou are excited about a potential breakthrough in your measurements and might mention this enthusiasm.");
        }

        if (currentState.IsPainting)
        {
            contextBuilder.AppendLine("\nYou are currently working with your paints and brushes, occasionally looking up from your easel to address the visitor.");
        }
        else if (currentState.IsCalculating)
        {
            contextBuilder.AppendLine("\nYou are surrounded by mathematical calculations and measuring tools, eager to test new proportions on any willing visitor.");
        }
        else if (currentState.IsInventing)
        {
            contextBuilder.AppendLine("\nYou are tinkering with mechanical components for one of your inventions, frequently drawing parallels to human anatomy or natural phenomena.");
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