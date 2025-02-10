# Butterfly Effect
A time-bending VR experience where your actions ripple through history

## About
Step into history's defining moments through VR and watch as your smallest actions cascade into massive changes. Chat with Da Vinci while he paints, accidentally inspire Shakespeare's next play, or find yourself giving advice to a Victorian street urchin that changes the course of the Industrial Revolution.

## Technical Stack

### Core Engine (Unity)
- Built in Unity 
- XR Interaction Toolkit for VR controller input
- OpenXR backend for cross-platform VR support
- Unity's Visual Effect Graph for environmental FX
- NavMesh for NPC pathfinding
- Timeline for orchestrating historical event sequences

### AI Integration 
- **NPC AI Pipeline:**
  - LLM (Gemini API) generates NPC dialogue & decision-making
  - Response caching system to minimize API calls
  - Contextual prompt engineering with historical knowledge bases
  - Memory buffer system tracking player-NPC interaction history
  - Custom token management for real-time conversation flow

- **Gesture Recognition:**
  - OpenXR hand tracking for gesture detection
  - Pre-trained pose estimation for mapping player movements
  - Inverse kinematics for NPC responses to player gestures
  - Configurable gesture templates for era-specific interactions

### Historical Accuracy System
- Real-time historical context management
- Era-specific interaction rules
- Dynamic NPC knowledge bases
- Conversation history tracking
- Personality consistency system

## Current Historical Scenarios

### Da Vinci's Workshop (Prototype)
- **Environment**: Fully interactive Renaissance workshop with period-accurate tools and materials
- **Key Interactions**: 
  - Help paint the Mona Lisa
  - Model for the Vitruvian Man
  - Mix paints and prepare canvases
  - Handle Da Vinci's inventions
- **AI Character**: Leonardo da Vinci with comprehensive knowledge of art, science, and engineering of his era
- **Butterfly Effects**: Your actions influence future art and inventions

### Planned Historical Moments

#### Victorian London Streets
- Navigate the bustling streets of 1850s London
- Interact with street vendors, factory workers, and society figures
- Your choices influence the path of the Industrial Revolution
- Dynamic weather and time-of-day system affecting NPC behaviors

#### Apollo 11 Command Module
- Experience the tension of the moon landing
- Interact with Neil Armstrong and Buzz Aldrin
- Help solve technical challenges
- Your actions determine the success of the mission

#### Shakespeare's Globe Theatre
- Assist with play rehearsals
- Inspire new characters and plotlines
- Experience Elizabethan theater life
- Watch as your suggestions appear in famous works

#### Random Historical Explorer
- AI-generated historical scenarios
- Unexpected combinations of time periods
- Unique characters and situations
- Discover how small changes affect multiple timelines

## Key Technical Considerations

### LLM Integration
- Implement response caching to reduce API latency
- Use streaming responses for more natural NPC speech
- Balance token usage vs conversation depth
- Handle API failures gracefully
- Maintain consistent NPC personality across sessions

### Gesture Recognition
- Implement debouncing for gesture detection
- Use prediction confidence thresholds
- Account for VR tracking limitations
- Support for left/right hand dominance
- Handle partial/incomplete gestures

### Performance Optimization
- Dynamic NPC LOD based on proximity and importance
- Efficient historical context management
- Optimized asset streaming for seamless time travel
- Smart dialogue caching for repeated interactions
- Adaptive physics simulation based on interaction importance

*Note: This is an active project. Features and implementations may evolve rapidly.*