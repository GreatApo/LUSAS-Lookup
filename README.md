# LUSAS-Lookup
The Lusas Lookup Module is a Windows Forms application providing a user interface to access and manage geometric and database objects within a Lusas model. It allows users to view, filter, and interact with database members such as volumes, surfaces, lines, points, attributes, analyses, loadsets, and groups through a tree-view structure, alongside a detailed properties and methods viewer.

![LUSAS Lookup Dialog Preview](https://github.com/GreatApo/LUSAS-Lookup/blob/main/preview.png?raw=true)

# Features
- Tree View Navigation: Visualize all volumes, surfaces, lines, points, attributes, analyses, loadsets, and groups in a structured tree view.
- Detailed Object Viewer: Select an object in the tree view to display its properties and available methods.
- Dynamic Search and Filtering: Filter displayed items in the properties and methods table based on user input.
- Model Selection and Highlighting: Select and highlight objects in the LUSAS model directly from the UI.

# Usage
1. Select a geometric object in the model
2. Launch the dialog from the menu `Modules` > `LusasLookup`

# Installation
1. Get `LusasLookup.dll` and `LusasLookup.LML` from Releases (or manually compile the project in Visual Studio 2019)
2. Move both files at: `%userprofile%\Documents\Lusas211\Modules`
