# GameGameGame Glossary

Status: Source of truth for shared project terminology.

Read when:

- a design or implementation discussion depends on spatial, action, content, or frontend vocabulary;
- adding terms whose ambiguity could cause engine/editor/frontend misalignment.

## Spatial terms

- **Cardinal directions**: the four orthogonal directions `North`, `South`, `East`, and `West`.
- **Intercardinal directions**: the four diagonal directions `NorthEast`, `SouthEast`, `SouthWest`, and `NorthWest`.
- **Adjacent spaces**: two spaces are adjacent when they are reciprocally touching north-south, east-west, northwest-southeast, or northeast-southwest. In other words, plain **adjacent** includes both cardinal adjacency and intercardinal adjacency. Intercardinal adjacency is blocked when both orthogonal corner spaces between the two spaces are occupied.
- **Adjacent entities**: two entities are adjacent when they occupy adjacent spaces.
- **Cardinal adjacency**: adjacency through one of the four cardinal directions.
- **Intercardinal adjacency**: adjacency through one of the four intercardinal directions.
