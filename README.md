# Command Line Interface

A Command Line Interface Manager

The console's **Levels** dropdown, to the left of the command input, lets you tick
the log levels to display. All levels are enabled initially. Hidden logs continue
to be recorded in memory, within the existing `_maxLogEntries` history limit
(100 by default), and reappear when their level is enabled. `clear` removes both
visible and hidden history. Echo output is filtered as `Info`.

The dropdown and `setConsoleFilter` command share the same selection:

```text
setConsoleFilter Warning,Error
setConsoleFilter Debug,Info,Success
setConsoleFilter all
setConsoleFilter none
```

Level names are case-insensitive. Available levels are Debug, Info, Analytics,
Success, Warning, and Error. Invalid levels leave the current selection unchanged.
