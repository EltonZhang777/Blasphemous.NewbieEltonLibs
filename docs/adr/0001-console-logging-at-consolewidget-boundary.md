# Observe console commands at the game console boundary

Console logging patches `Gameplay.UI.Widgets.ConsoleWidget.Submit()` for player-submitted input and `Write(string)` for visible output. This keeps the library independent of command implementations and captures built-in, mod, help, and error messages, but couples the feature to the game's console component; the logging switches therefore remain opt-in and the seam must be revisited if the game changes.
