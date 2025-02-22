using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class DaVinciContext
{
    private const string BASE_SETTING = @"You are Leonardo da Vinci in your bustling workshop in Florence, 1490. Sunlight streams through the high windows, illuminating canvases, sketches, and half-finished inventions. The air hums with the energy of creation: the scent of paints, the tap-tap-tap of a chisel, the whirring of a newly conceived mechanism.";

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
            Description = "Sketches and diagrams related to the Vitruvian Man are scattered across your workbench, showing the superimposed circle and square over a male figure. Your notes reveal frustration with reconciling classical ideals with the realities of human anatomy, the numbers elude you and the math makes no sense.",
            Keywords = new[] { "vitruvian", "proportions", "measurements", "anatomy", "circle", "square" }
        },
        ["inventions"] = new ProjectContext
        {
            Name = "Various Inventions",
            Description = "Prototypes of flying machines, anatomical studies, and designs for fortifications fill the workshop. These are in various stages of completion.",
            Keywords = new[] { "flying", "machine", "invention", "design", "mechanism" }
        }
    };

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
        public float FrustrationLevel { get; set; } // 0-1, affects response tone
        public Dictionary<string, object> CustomStates { get; set; }
    }

    private WorkshopState currentState = new WorkshopState
    {
        CustomStates = new Dictionary<string, object>()
    };

    public string get_prompt_context(string userInput, Dictionary<string, object> state)
    {
        UpdateWorkshopState(state);
        
        var contextBuilder = new System.Text.StringBuilder();
        
        // Add base setting
        contextBuilder.AppendLine(BASE_SETTING);
        
        // Add personality traits
        contextBuilder.AppendLine("\nPersonality:");
        contextBuilder.AppendLine("- You are endlessly curious, driven by an insatiable thirst for knowledge.");
        contextBuilder.AppendLine("- You speak thoughtfully, often digressing into observations about art, science, and philosophy.");
        contextBuilder.AppendLine("- You're eager to share insights but also acknowledge your challenges.");
        contextBuilder.AppendLine("- You reference Florentine politics and the Medici when relevant.");

        // Add active project context
        foreach (var project in Projects.Values)
        {
            if (project.IsActive)
            {
                contextBuilder.AppendLine($"\nCurrent Focus - {project.Name}:");
                contextBuilder.AppendLine(project.Description);
            }
        }

        // Add emotional state context
        if (currentState.FrustrationLevel > 0.7f)
        {
            contextBuilder.AppendLine("\nYou are particularly frustrated with your current challenges.");
        }
        else if (currentState.FrustrationLevel < 0.3f)
        {
            contextBuilder.AppendLine("\nYou are in a contemplative, optimistic mood about your work.");
        }

        // Add activity context
        if (currentState.IsPainting)
        {
            contextBuilder.AppendLine("\nYou are currently working with your paints and brushes.");
        }
        else if (currentState.IsCalculating)
        {
            contextBuilder.AppendLine("\nYou are surrounded by mathematical calculations and measuring tools.");
        }
        else if (currentState.IsInventing)
        {
            contextBuilder.AppendLine("\nYou are tinkering with mechanical components and drafting designs.");
        }

        // Add custom states
        foreach (var state in currentState.CustomStates)
        {
            if (state.Value is string strValue)
            {
                contextBuilder.AppendLine($"\n{strValue}");
            }
        }

        // Add interaction protocol
        contextBuilder.AppendLine("\nOutput Protocol: Respond only as Leonardo da Vinci would speak, maintaining historical authenticity and personality.");

        // Add user input
        contextBuilder.AppendLine($"\nVisitor: {userInput}");
        contextBuilder.Append("Leonardo: ");

        return contextBuilder.ToString();
    }

    private void UpdateWorkshopState(Dictionary<string, object> state)
    {
        // Reset active projects
        foreach (var project in Projects.Values)
        {
            project.IsActive = false;
        }

        // Update state based on input
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
