# 3D Die Viewer

A .NET 9 WPF application for visualizing and interacting with 3D polyhedral dice.

![3D Die Viewer Screenshot](Resources/screenshot.png)

## Overview

3D Die Viewer is an interactive application that allows users to view and rotate different types of polyhedral dice in three-dimensional space. The application provides realistic renderings of standard polyhedral dice (d4, d6, d8, d10, d12, d20) with customizable colors and intuitive rotation controls.

## Features

- **Multiple Die Types**: Visualize all standard RPG dice:
  - Tetrahedron (d4)
  - Cube (d6)
  - Octahedron (d8)
  - Decahedron/Pentagonal Trapezohedron (d10)
  - Dodecahedron (d12)
  - Icosahedron (d20)

- **Interactive 3D Rotation**: Naturally manipulate dice in 3D space:
  - Left-click + drag to rotate around X and Y axes
  - Right-click + drag to spin around the Z-axis (view direction)
  - Smart movement detection to determine rotation intent

- **Customizable Appearance**: Choose from a wide variety of colors for your dice

- **Realistic Rendering**: Dice include:
  - Properly positioned numbers
  - Orientation indicators (underlines) for 6 and 9 on d10, d12 and d20
  - Surface texture effects with contrast-optimized number visibility

- **Mathematically Accurate**: Proper geometric construction of all polyhedra with correct face numbering and orientation

## Technical Details

The application is built using:
- .NET 9
- WPF (Windows Presentation Foundation)
- 3D geometry rendering with WPF's Viewport3D
- Quaternion-based rotations for smooth interaction
- Vector math for proper 3D construction

### Architecture

- **Models**: Define the geometric structure of different dice types
- **Services**: Create textures and manage visual aspects of the dice
- **UI**: Handle user interaction and 3D visualization
- **Geometry**: Construct accurate polyhedra using precise mathematical formulations

## An Experiment in AI-Assisted Development

This project was created entirely through collaboration with GitHub Copilot, with the human developer providing only instructions and guidance. It serves two purposes:

1. **Skill Development**: To help the developer improve at writing effective GitHub Copilot prompts and understanding the collaboration process

2. **Technical Exploration**: To explore the boundaries of GitHub Copilot's capabilities in understanding and implementing 3D mathematics, geometry, and rendering techniques

> **Note**: Even this documentation was written by GitHub Copilot based on human instructions.

### Mathematical Challenges Solved

The project successfully tackled several complex mathematical problems:

- Correct generation of Platonic solid geometries
- Implementation of the d10 die (pentagonal trapezohedron), which is not a Platonic solid but is included as a standard RPG die
- Proper face-to-face relationships in polyhedra
- Quaternion-based rotation systems
- Smart movement detection algorithms
- Proper UV mapping for texture coordinates on irregular polygons

### Challenges with the d10 Implementation

The implementation of the d10 (decahedron/pentagonal trapezohedron) highlighted interesting limitations and learning opportunities:

- While GitHub Copilot excelled at creating Platonic solids, it initially struggled with understanding kite-shaped faces and how they mesh together in a pentagonal trapezohedron
- The human developer had to explicitly teach Copilot what a kite is geometrically (a quadrilateral with two pairs of adjacent equal sides)
- Multiple iterations were required to correctly position the vertices so that:
  - The left and right vertices were positioned just above the equator
  - The bottom vertex extended just below the equator
  - The faces formed proper kites rather than triangles
  - The two halves interlocked correctly with a 36-degree offset
- Special attention was needed to ensure the numbers were properly oriented and positioned on each face
- The d10 required a custom numbering scheme where opposite faces sum to 11 (treating 0 as 10)

This demonstrated that while AI assistants like Copilot have impressive capabilities with standard geometric structures, more specialized or complex geometries may require explicit human guidance and teaching.

## How to Use

1. Select a die type from the dropdown menu
2. Choose a color for your die from the color selector
3. Interact with the die using mouse controls:
   - Left-click + drag to rotate
   - Right-click + drag for z-axis rotation

## Requirements

- Windows operating system
- .NET 9 runtime
- Graphics card with DirectX 10 or later support

## Future Possibilities

- Additional dice types (non-standard polyhedra)
- Dice rolling physics simulation
- Multiple dice visualization
- Export/import customized dice
- Texture customization options
