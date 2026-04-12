#FreeAir fork (by ish ) changes

## Unreleased

### Fixed
- Fixed PostBuildEvent for WhisperNet.Runtime.zip: added explicit .\ to script path.
- Build now succeeds in VS 2022 & VS 2026
- added changelog.md (this file)

### Added/Modified
- Added Title to ChatWrapper, ChatDescription, ChatListToolWindowControl
- FreeAir.Shared: added cNotifyBase class for easy INPC, cMyCommand
- wpfHelpers: added cInputDlg 
- rename chat in a chat list (via listbox context menu)


### TODO
- [x] Rename chat (context menu)
- [ ] Chat selection (new/existing + model choice)
- [ ] Font settings
- [ ] Chat trim
- [ ] Code coloring