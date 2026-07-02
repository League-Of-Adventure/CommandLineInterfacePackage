## [1.0.0] - 16/06/2025
### First Release
- Created Package

## [1.0.1] - 16/06/2025
## Minor Improvements
- Added Gizmos for MonoBehaviour classes

## [1.1.0] - 19/06/2026
## Console Added
- Added a ready-to-go Console using UI Toolkit
- Added a set of ready-to-go fonts for the Console
- Added a few new tests for the Tokenizer and Binder

## [1.1.1] - 20/02/2026
## Bug Fixes and removed debug messages
- Removed some debug messages from the console
- Fixed a bug in the Command Binder that was causing speech marks to be included in string tokens after binding
- Included a copy of UnityDefaultThemes in the package so that the console would render correctly once imported to another project

## [1.2.1] - 20/02/2026
## Console feature update
- Fixed some layout issues with the Console
- Added a scroll bar which appears when the console exceeds a maximum size
- Made it so the console correctly scrolls all the way to the bottom when a new message is added
- Added a command history to the Console. Using the up and down arrow keys allows scrolling through history to quickly copy commands

## [1.3.1] - 20/02/2026
## extOSC feature update
- Added support for commands over OSC using the extOSC plugin (https://assetstore.unity.com/packages/tools/input-management/extosc-open-sound-control-72005)
- Added support for VContainer Dependency Injection if package is installed

## [1.4.1] - 05/06/2026
- Added support for commands at launch time and commands on input triggers. These input triggers can be supplied to the Input handler via a new public IInputProvider interface
- Improved the IOutput interface so that rather than receiving a raw, preprocessed string, a Log object is provided instead. This allows custom loggers to add their own formatting if desired

## [1.5.1] - 08/06/2026
- Added prebuilt lifetime scope for easy setup in any project
- Improved editor tools for setting up components

## [1.5.3] - 08/06/2026
- Added InputBlocker so that the UI being open can prevent commands from being triggered
- Bug Fixes

## [1.6.0] - 08/06/2026
- Added first of many built in commands to add utility to command list

## [1.7.1] - 02/07/2026
- Split Logging into a separate Asmdef
- Created static LogDispatch class so that non-command operations can still log to the console