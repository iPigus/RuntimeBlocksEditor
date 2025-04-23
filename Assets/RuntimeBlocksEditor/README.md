# Runtime Blocks Editor - Asset Structure

## Folder Structure

```
RuntimeBlocksEditor/
├── Scripts/                 # Main components and logic
│   ├── Core/               # Core scripts including Gizmo tools
│   │   ├── GizmoScripts/  # Block editing components
│   ├── Editor/             # Unity editor scripts
│   └── Runtime/            # Runtime scripts
├── Shaders/                # Shaders
│   ├── Core/              # Core shaders
│   └── Variants/          # Shader variants
├── Materials/              # Materials
│   ├── Presets/           # Predefined materials
│   └── Examples/          # Example materials
├── Textures/              # Textures
│   ├── Default/           # Default textures
│   └── Examples/          # Example textures
├── Prefabs/                # Reusable prefabs
│   ├── Tools/             # Block editor tool prefabs
│   └── Blocks/            # Block prefabs
├── Examples/              # Usage examples
│   ├── Scenes/           # Demo scenes
│   └── Prefabs/          # Demo prefabs
└── Documentation/         # Documentation
    ├── UserGuide/        # User guide
    └── API/              # API documentation
```

## Namespace

The main namespace for the asset is `RuntimeBlocksEditor`, which contains the following sub-namespaces:

- `RuntimeBlocksEditor.Core` - Main asset logic and components
- `RuntimeBlocksEditor.Gizmo` - Block editing and manipulation tools
- `RuntimeBlocksEditor.Editor` - Editor tools and enhancements
- `RuntimeBlocksEditor.Shader` - Shaders and their variants
- `RuntimeBlocksEditor.Utility` - Helper tools and extensions

## Naming Conventions

1. **Scripts**
   - Class names: PascalCase (e.g., `TriplanarMaterialManager`)
   - Interface names: IPascalCase (e.g., `IBlockEditor`)
   - Method names: PascalCase (e.g., `InitializeMaterial`)
   - Variable names: camelCase (e.g., `blockMaterial`)
   - Private member variables: m_PascalCase (e.g., `m_Material`)

2. **Shaders**
   - Shader names: PascalCase (e.g., `TriplanarBlock`)
   - Property names: PascalCase with underscore prefix (e.g., `_BaseMap`)
   - Function names: camelCase (e.g., `calculateBlend`)

3. **Materials**
   - Material names: PascalCase (e.g., `BlockMaterial_Stone`)
   - Preset names: PascalCase (e.g., `BlockPreset_Basic`)

4. **Textures**
   - Texture names: snake_case (e.g., `stone_albedo`)
   - Normal maps: snake_case + _normal (e.g., `stone_normal`)

## Block Editor System

The block editor system (in the `RuntimeBlocksEditor.Gizmo` namespace) provides runtime block manipulation and consists of the following components:

1. **GizmoController.cs** - Main controller that manages block editing tools and coordinates other components. Acts as the central hub for the entire block editing system.

2. **GizmoTools.cs** - Implements block editing tools:
   - Move - For positioning blocks along axes or planes
   - Rotate - For rotating blocks around different axes
   - Select - For selecting blocks for manipulation
   - Delete - For removing blocks from the scene

3. **GizmoHistory.cs** - Manages operation history (undo/redo) to allow reverting or repeating block editing actions.

4. **GizmoVisuals.cs** - Manages visual elements of the block editor, such as arrows, planes, and rotation rings.

5. **GizmoUI.cs** - Handles user interface elements for the block editing tools.

6. **GizmoSelectable.cs** - Component added to blocks that can be selected with the editor tools.

7. **BlockToEdit.cs** - Base component for editable blocks, managing materials and selection state.

## Keyboard Shortcuts

- **1** - Switches to the select tool for block selection
- **2** - Switches to the move tool for block positioning
- **3** - Switches to the rotate tool for block rotation
- **4** - Switches to the scale tool for block scaling
- **T** - Toggles between local and global coordinate modes for block manipulation
- **Ctrl+Z** - Undoes the last block editing operation
- **Ctrl+Y** - Redoes the last undone block editing operation
- **Ctrl+click** - Multi-selection of blocks

## How to Use

1. Add the `EditorObject` component to any block GameObject you want to make editable
2. Ensure your scene has a GizmoController instance (you can use the provided prefab)
3. Press the keyboard shortcuts or use the UI buttons to switch between different block editing tools
4. Select blocks by clicking on them when the select tool is active
5. Use the move/rotate tools to manipulate the selected blocks
6. Use Ctrl+click to select multiple blocks for simultaneous editing

## Integration

To integrate the Runtime Blocks Editor into your project:

1. Import the package into your Unity project
2. Add the GizmoController prefab to your scene
3. Set the appropriate tag for selectable blocks in the GizmoController
4. Add the EditorObject component to any blocks you want to be editable
5. Customize materials for highlighting and error states as needed

## Requirements

- Unity 2022.3 or newer
- Universal Render Pipeline (URP)
- .NET Framework 4.7.1 or newer 