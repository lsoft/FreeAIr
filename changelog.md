#FreeAir fork (by ish ) changes

## Unreleased

### Fixed
- Fixed PostBuildEvent for WhisperNet.Runtime.zip: added explicit .\ to script path.
- Build now succeeds in VS 2022 & VS 2026
- added changelog.md (this file)
- removed ANE throw on solutionPath = null in SelectedIdentifier.ctor, prevented to work with separate files.

### Added/Modified
- Added Title to ChatWrapper, ChatDescription, ChatListToolWindowControl
- FreeAir.Shared: added cNotifyBase class for easy INPC, cMyCommand
- wpfHelpers: added cInputDlg 
- chat rename in a chat list (via listbox context menu)
- start discussion , start chat here -  hold Ctrl to add to previous chat 


### TODO
- [x] Rename chat (context menu)
- [x] Chat selection (new/existing)
- [ ] Font settings
- [ ] Chat trim
- [ ] Code coloring