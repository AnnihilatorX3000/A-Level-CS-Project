# A-Level CS Project
 TITLE: Interactive, Visual Comparison of Single-Source, Single-Destination, Shortest Path Pathfinding Algorithms
 
 A project created in 2017 for my A-Levels in Computer Science.
 
 The user selects a source and destination tile on the tile map, and selects a pathfinding algorithm (BFS, Dijkstra, A* with either Euclidean, Manhattan, or Chebyshev heuristic). The user may also place obstacles of varying colour and weight between the source and destination tiles in a Microsoft Paint-like fashion. To compare the algorithms, the user may add additional copies of the tile map, each of which with different pathfinding algorithms attached to it.
 
 When pathfinding begins, the program will attempt to use the selected pathfinding algorithm to navigate from the source tile to reach the destination tile. If the attempt succeeds, the tiles checked and path taken will be displayed on screen, along with the time taken (ms) for the algorithm to complete. If the attempt fails, a message will appear informing the user of this.
 
 The user may also import and export custom tile maps for later use. There are two versions of the program - the original has a limit to the number of unique tiles a tile map may contain, whereas the second version has no such limit (do note however that importing images may be slow!).
 
 See "How to use pathfinding program.txt" for instructions on how to use the program, "Interactive, Visual Comparison of Single-Source, Single-Destination, Shortest Path Pathfinding Algorithms.pdf" for the full documentation, and "ORIGINAL FINAL SOURCE CODE.txt" for the source code.
 
 This project was created as a C# Console App using Visual Studio.
