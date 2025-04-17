# UtilBot Documentation

## Overview
UtilBot is a Minecraft chat bot for server management and player entertainment. It provides command handling, music playback, and administrative functions.
- Playtesting available on `play.neonstudios.dev` 
## Configuration
- Default command prefix: `!` (configurable)
- Admin username: `FlameGrowl`
- Command cooldown: 5 seconds (for non-admin users)
- Initialization: `MCC.LoadBot(new UtilBot());`

## Commands

### General Commands
| Command | Description | Admin Only | Maintenance Available |
|---------|-------------|------------|---------------------|
| `!help` | Shows available commands | No | Yes |
| `!ping` | Check server latency | No | No |
| `!tps` | Check server TPS | No | No |
| `!songs` | Shows available songs | No | No |

### Admin Commands
| Command | Description | Maintenance Available |
|---------|-------------|---------------------|
| `!mt` | Toggle maintenance mode | Yes |
| `!cl` | Clear items in loaded chunks | Yes |
| `!debug` | Toggle debug mode | Yes |
| `!verbose` | Toggle verbose output | Yes |

### Music System
Command format: `!play <songname>`

Available songs:
- dead-inside-slowed
- slay!
- hensonn-sahara
- hyperpop-x-rave-x-lida
- kordhell-murder-in-my-mind
- night-dancer
- pharrell-williams-happy
- playamne-x-nateki-midnight
- x-slide
- Zeldas-Lullaby

## Special Modes

### Maintenance Mode
- Activated with `!mt`
- Restricts command access to admin only
- Some commands marked as `availableInMaintenance` still work

### Debug Mode
- Activated with `!debug`
- Shows technical information about command processing
- Outputs messages in red
- Admin only

### Verbose Mode
- Activated with `!verbose`
- Shows command usage and user information
- Outputs messages in gold
- Admin only

### Fake Normal Player Mode
- Development feature
- Makes the bot treat admin as regular user
- Useful for testing restrictions
- Set via code only (`fakeNormalPlayer = true`)

## Cooldown System
- 5-second cooldown between commands
- Applies to non-admin users only
- Shows remaining cooldown time
- Admins bypass cooldown restriction

## Command Registration
Commands can be registered using:
```csharp
RegisterCommand(string trigger, string description, Action<string, string> action, 
    bool adminOnly = false, bool availableInMaintenance = false)
```

## Security Features
- Admin-only commands
- Command cooldowns
- Maintenance mode restrictions
- Case-insensitive command handling
- Prefix validation

## Error Handling
- Invalid command feedback
- Permission denial messages
- Cooldown notifications
- Maintenance mode warnings

For developers: The source code includes XML documentation comments for better IDE integration and code understanding.
