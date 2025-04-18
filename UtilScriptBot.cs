//MCCScript 1.0

// You can easily change the prefix here
MCC.LoadBot(new UtilBot()); // or any other prefix like ".", "/", "$", etc.

//MCCScript Extensions

/// <summary>
/// A utility bot for Minecraft server management and player entertainment.
/// Provides command handling for server administration, player teleportation, and music playback.
/// </summary>
class UtilBot : ChatBot
{
    private const string DEFAULT_PREFIX = "!";
    private readonly string commandPrefix;

    // Admin User name
    string AdminName = "FlameGrowl";
    private System.Timers.Timer adTimer;
    private Random random = new Random();
    private bool maintenanceMode = false; // Fixed the typo and modifier placement
    private readonly Dictionary<string, Command> commands;
    private bool debugMode = false;
    private readonly Dictionary<string, DateTime> userCooldowns = new Dictionary<string, DateTime>();
    private const int COOLDOWN_SECONDS = 5;
    private bool fakeNormalPlayer = true; // Add this field for fake mode
    private bool verboseMode = false; // Add this field for verbose mode
    private static readonly DateTime startupTime = DateTime.Now; // Store bot startup time

    public UtilBot(string prefix = DEFAULT_PREFIX)
    {
        commandPrefix = prefix;
        commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);
    }

    public override void Initialize()
    {
        SendText($"/msg {AdminName} Initializing UtilBot with prefix '{commandPrefix}'...");
        
        // Register basic commands
        RegisterCommand($"{commandPrefix}ping", "Check server latency", 
            (username, _) => SendText($"/msg {username} The current server ping is: {GetLatency()}"));
        
        RegisterCommand($"{commandPrefix}tps", "Check server TPS", 
            (username, _) => SendText($"/msg {username} The current server TPS is: {GetServerTPS()}"));
        
        RegisterCommand($"{commandPrefix}cl", "Clear items in loaded chunks", 
            (username, _) => {
                SendText("/kill @e[type=item]");
                SendText($"/msg {username} Done. Cleared all items in loaded Chunk. ;D");
            }, adminOnly: true, availableInMaintenance: true);
        
        RegisterCommand($"{commandPrefix}mt", "Toggle maintenance mode [-vb (verbose) -dg (debug)]", 
            (username, args) => {
                if (!IsAdmin(username))
                {
                    SendText($"/msg {username} This command is admin-only.");
                    return;
                }

                // Parse arguments
                bool setVerbose = args.Contains("-vb");
                bool setDebug = args.Contains("-dg");

                // Toggle maintenance mode
                maintenanceMode = !maintenanceMode;
                string status = maintenanceMode ? "enabled" : "disabled";
                
                // Set additional modes if flags are present
                if (maintenanceMode)
                {
                    if (setVerbose) verboseMode = true;
                    if (setDebug) debugMode = true;
                }
                else
                {
                    // Always reset modes when disabling maintenance mode
                    verboseMode = false;
                    debugMode = false;
                }

                // Send status messages
                SendText($"/msg {username} Maintenance mode {status}");
                SendText($"/say Bot is now in {(maintenanceMode ? "maintenance mode" : "normal mode")}");

                // Update status messages for debug/verbose modes
                if (setVerbose || verboseMode)
                    SendText($"/msg {username} Verbose mode {(verboseMode ? "enabled" : "disabled")}");
                if (setDebug || debugMode)
                    SendText($"/msg {username} Debug mode {(debugMode ? "enabled" : "disabled")}");
                    
            }, adminOnly: true, availableInMaintenance: true);

        // Add debug toggle command
        RegisterCommand($"{commandPrefix}debug", "Toggle debug mode", 
            (username, _) => {
                if (!IsAdmin(username))
                {
                    SendText($"/msg {username} This command is admin-only.");
                    return;
                }
                debugMode = !debugMode;
                SendText($"/msg {AdminName} Debug mode {(debugMode ? "enabled" : "disabled")}");
            }, adminOnly: true, availableInMaintenance: true);

        RegisterCommand($"{commandPrefix}verbose", "Toggle verbose mode (detailed output)", 
            (username, _) => {
                if (!IsAdmin(username))
                {
                    SendText($"/msg {username} This command is admin-only.");
                    return;
                }
                verboseMode = !verboseMode;
                SendText($"/msg {AdminName} Verbose mode {(verboseMode ? "enabled" : "disabled")}");
            }, adminOnly: true, availableInMaintenance: true);

        RegisterCommand($"{commandPrefix}help", "Shows available commands", 
            (username, args) => {
                var availableCommands = commands
                    .Where(c => (!c.Value.AdminOnly || IsAdmin(username)) && 
                              (!maintenanceMode || c.Value.AvailableInMaintenance || IsAdmin(username)))
                    .OrderBy(c => c.Key);

                SendText($"/msg {username} Available commands:");
                foreach (var cmd in availableCommands)
                {
                    SendText($"/msg {username} {cmd.Key}: {cmd.Value.Description}");
                }

                // Add radio commands help
                if (!maintenanceMode || IsAdmin(username))
                {
                    SendText($"/msg {username} {commandPrefix}play <song>: Play a song from the available list");
                    SendText($"/msg {username} Use {commandPrefix}songs to see available songs");
                }
            });

        RegisterCommand($"{commandPrefix}songs", "Shows available songs", 
            (username, args) => {
                if (maintenanceMode && !IsAdmin(username))
                {
                    SendText($"/msg {username} Bot is currently in maintenance mode.");
                    return;
                }
                SendText($"/msg {username} Available songs:");
                SendText($"/msg {username} - dead-inside-slowed");
                SendText($"/msg {username} - slay!");
                SendText($"/msg {username} - hensonn-sahara");
                SendText($"/msg {username} - hyperpop-x-rave-x-lida");
                SendText($"/msg {username} - kordhell-murder-in-my-mind");
                SendText($"/msg {username} - night-dancer");
                SendText($"/msg {username} - pharrell-williams-happy");
                SendText($"/msg {username} - playamne-x-nateki-midnight");
                SendText($"/msg {username} - x-slide");
                SendText($"/msg {username} - Zeldas-Lullaby");
            });

         RegisterCommand($"{commandPrefix}info", "Get detailed information about a command", 
            (username, args) => {
                if (string.IsNullOrWhiteSpace(args))
                {
                    SendText($"/msg {username} Usage: {commandPrefix}info <command_name>");
                    SendText($"/msg {username} Example: {commandPrefix}info ping");
                    return;
                }

                string commandName = $"{commandPrefix}{args.Trim()}";
                if (commands.TryGetValue(commandName, out Command cmd))
                {
                    SendText($"/msg {username} Command: {cmd.Name}");
                    SendText($"/msg {username} Description: {cmd.Description}");
                    SendText($"/msg {username} Admin only: {(cmd.AdminOnly ? "Yes" : "No")}");
                    SendText($"/msg {username} Available in maintenance: {(cmd.AvailableInMaintenance ? "Yes" : "No")}");
                    
                    // Additional command-specific information
                    switch (args.ToLower().Trim())
                    {
                        case "ping":
                            SendText($"/msg {username} Shows the current server latency in milliseconds.");
                            break;
                        case "tps":
                            SendText($"/msg {username} Shows the current server Ticks Per Second (TPS).");
                            break;
                        case "cl":
                            SendText($"/msg {username} Clears all dropped items in loaded chunks.");
                            SendText($"/msg {username} Note: This is an admin-only command.");
                            break;
                        case "mt":
                            SendText($"/msg {username} Toggles maintenance mode on/off.");
                            SendText($"/msg {username} When enabled, most commands become admin-only.");
                            break;
                        case "debug":
                            SendText($"/msg {username} Toggles debug mode for detailed logging.");
                            SendText($"/msg {username} Useful for troubleshooting bot issues.");
                            break;
                        case "verbose":
                            SendText($"/msg {username} Toggles verbose mode for detailed command execution info.");
                            break;
                        case "play":
                            SendText($"/msg {username} Plays music from the available song list.");
                            SendText($"/msg {username} Usage: !play <song_name>");
                            SendText($"/msg {username} Use !songs to see available songs.");
                            break;
                        case "doxx":
                            SendText($"/msg {username} Generates a random fake IP address for a player.");
                            SendText($"/msg {username} Usage: !doxx [player_name]");
                            SendText($"/msg {username} If no player is specified, uses your own name.");
                            SendText($"/msg {username} Note: This is just for entertainment, generates completely random numbers.");
                            break;
                        case "chance":
                            SendText($"/msg {username} Generates a random percentage (0.00% to 100.00%) for a player.");
                            SendText($"/msg {username} Usage: !gayrate [player_name]");
                            SendText($"/msg {username} If no player is specified, uses your own name.");
                            break;
                        case "stop":
                            SendText($"/msg {username} Stops any currently playing music.");
                            SendText($"/msg {username} Usage: !stop");
                            break;
                    }
                }
                else
                {
                    SendText($"/msg {username} Command '{commandName}' not found.");
                    SendText($"/msg {username} Use {commandPrefix}help to see available commands.");
                }
            });

        RegisterCommand($"{commandPrefix}sinfo", "Display server information", 
            (username, _) => {
                // Get TPS
                double currentTPS = GetServerTPS();
                string tpsColor = currentTPS >= 19.5 ? "green" : 
                                 currentTPS >= 15 ? "yellow" : "red";

                // Get player count
                var players = GetOnlinePlayers();
                int playerCount = players.Count(); // Add parentheses to call Count method
                int maxPlayers = GetMaxPlayers();

                // Get server uptime
                TimeSpan uptime = GetServerUptime();
                string uptimeStr = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";

                // Get memory usage (in MB)
                long usedMemory = GC.GetTotalMemory(false) / 1024 / 1024;
                long maxMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024;
                
                // Send formatted server info
                SendText($"/msg {username} === Server Information ===");
                SendText($"/msg {username} TPS: {currentTPS:F1} => {tpsColor}");
                SendText($"/msg {username} Players: {playerCount-1}/{maxPlayers}");
                SendText($"/msg {username} Memory: {usedMemory}MB/{maxMemory}MB");
                SendText($"/msg {username} Uptime: {uptimeStr}");
                
                if (verboseMode && IsAdmin(username))
                {
                    SendText($"/msg {username} Server Version: {GetServerVersion()}");
                }
            });

        SendText($"/msg {AdminName} UtilBot initialized successfully.");
        RegisterCommand($"{commandPrefix}ip", "Get server IP address", 
            (username, _) => {
                SendText($"/msg {username} Server IP: {GetServerIP()}");
            }, adminOnly: false, availableInMaintenance: true);

        RegisterCommand($"{commandPrefix}doxx", "Generate a fake IP address for a player", 
            (username, args) => {
                string targetPlayer = !string.IsNullOrWhiteSpace(args) ? args.Trim() : username;
                string fakeIP = GenerateFakeIP();
                SendText($"/say {targetPlayer}'s IP address is totally {fakeIP}");
            });

        RegisterCommand($"{commandPrefix}gayrate", "Generate a random percentage for a player", 
            (username, args) => {
                string targetPlayer = !string.IsNullOrWhiteSpace(args) ? args.Trim() : username;
                string percentage = GenerateRandomPercentage();
                SendText($"/say {targetPlayer} is {percentage} gay!");
            });

        RegisterCommand($"{commandPrefix}stop", "Stop currently playing music", 
            (username, _) => {
                SendText($"/stopsound {username}");
                SendText($"/title {username} actionbar [\"\",{{\"text\":\"Music stopped\",\"color\":\"red\"}}]");
            });

        RegisterCommand($"{commandPrefix}smt", "enable Sever Maintenance !!!Requiers Maintenance Plugin!!!",
        (username, args) => {
            if (!IsAdmin(username))
            {
                SendText($"/msg {username} This command is admin-only.");
                return;
            }
        bool smt = false;
        if (args.ToLower() == "on")
        {
            smt = true;
            SendText($"/msg {username} Server Maintenance is now enabled.");
            SendText($"/mt on");
        }
        else if (args.ToLower() == "off")
        {
            smt = false;
            SendText($"/msg {username} Server Maintenance is now disabled.");
            SendText($"/mt off");
        }
        else
        {
            SendText($"/msg {username} Usage: {commandPrefix}smt <on/off>");
            return;
        }
        
        }, adminOnly: true, availableInMaintenance: true);

        
        RegisterCommand($"{commandPrefix}vmt", "enable Server Maintenance on velocity server !!!Requires Maintenance Plugin!!! Usage: !vmt [server] <on|off>",
        (username, args) => {
            if (!IsAdmin(username))
            {
                SendText($"/msg {username} This command is admin-only.");
                return;
            }

            string[] arguments = args.Split(' ');
            string server = "";
            string action = "";

            // Parse arguments based on whether server is specified
            if (arguments.Length == 1)
            {
                // Only on/off provided
                action = arguments[0].ToLower();
            }
            else if (arguments.Length == 2)
            {
                // Both server and on/off provided
                server = arguments[0].ToLower();
                action = arguments[1].ToLower();
            }
            else
            {
                SendText($"/msg {username} Usage: {commandPrefix}vmt [server] <on/off>");
                return;
            }

            bool smt = false;
            string serverPrefix = !string.IsNullOrEmpty(server) ? $"{server}" : "";

            if (action == "on")
            {
                smt = true;
                SendText($"/msg {username} Server Maintenance is now enabled{(server != "" ? $" for {server}" : "")}.");
                SendText($"/mt on {serverPrefix}");
            }
            else if (action == "off")
            {
                smt = false;
                SendText($"/msg {username} Server Maintenance is now disabled{(server != "" ? $" for {server}" : "")}.");
                SendText($"/mt off {serverPrefix}");
            }
            else
            {
                SendText($"/msg {username} Usage: {commandPrefix}vmt [server] <on/off>");
                return;
            }
        }, adminOnly: true, availableInMaintenance: true);
    }
    
    /// <summary>
    /// Represents a bot command with its properties and execution logic
    /// </summary>
    public class Command
    {
        public string Name { get; }
        public string Description { get; }
        public bool AdminOnly { get; }
        public bool AvailableInMaintenance { get; }
        private readonly Action<string, string> ExecuteAction;

        public Command(string name, string description, Action<string, string> executeAction, bool adminOnly = false, bool availableInMaintenance = false)
        {
            Name = name;
            Description = description;
            ExecuteAction = executeAction;
            AdminOnly = adminOnly;
            AvailableInMaintenance = availableInMaintenance;
        }

        public void Execute(string username, string args)
        {
            ExecuteAction(username, args);
        }
    }

    /// <summary>
    /// Registers a new command to the bot.
    /// </summary>
    /// <param name="trigger">The command trigger (e.g., "!example")</param>
    /// <param name="description">Description of the command</param>
    /// <param name="action">Action to execute when the command is triggered</param>
    /// <param name="adminOnly">Whether the command is restricted to admins</param>
    /// <param name="availableInMaintenance">Whether the command is available in maintenance mode</param>
    public void RegisterCommand(string trigger, string description, Action<string, string> action, 
        bool adminOnly = false, bool availableInMaintenance = false)
    {
        // Store command with the ! prefix intact
        commands[trigger] = new Command(
            trigger,  // Remove TrimStart('!')
            description,
            action,
            adminOnly,
            availableInMaintenance
        );
    }

    /// <summary>
    /// Processes incoming chat messages and executes corresponding commands.
    /// </summary>
    /// <param name="text">The raw chat message text</param>
    /// <remarks>
    /// Handles both private messages and public chat messages.
    /// Messages are parsed to extract the username and command content.
    /// </remarks>
    public override void GetText(string text)
    {
        string message = string.Empty;
        string username = string.Empty;
        text = GetVerbatim(text);

        if (IsPrivateMessage(text, ref message, ref username) || IsChatMessage(text, ref message, ref username))
        {
            ProcessMessage(message, username);
        }
    }

    /// <summary>
    /// Processes user commands and executes corresponding actions.
    /// </summary>
    /// <param name="message">The chat message containing the command</param>
    /// <param name="username">The username of the command sender</param>
    private void ProcessMessage(string message, string username)
    {
        string command = message.Trim();

        if (!command.StartsWith(commandPrefix))
            return;

        // Check cooldown before processing command
        if (IsUserInCooldown(username))
            return;

        // Update last command time for the user
        userCooldowns[username] = DateTime.Now;

        // Debug output to check if messages are being received
        LogToConsole($"Received message: {command} from {username}");

        if (verboseMode && IsAdmin(username))
        {
            SendText($"/msg {AdminName} Command received: {command}");
            SendText($"/msg {AdminName} From user: {username}");
            SendText($"/msg {AdminName} Cooldown status: {(IsUserInCooldown(username) ? "Active" : "None")}");
            SendText($"/msg {AdminName} Admin status: {(IsAdmin(username) ? "Yes" : "No")}");
            SendText($"/msg {AdminName} Maintenance mode: {(maintenanceMode ? "Yes" : "No")}");
        }

        string[] parts = command.Split(' ');
        string cmdName = parts[0].ToLower();
        string args = string.Join(" ", parts.Skip(1));

        // Debug output to check command parsing
        LogToConsole($"Parsed command: {cmdName}, args: {args}");

        if (commands.TryGetValue(cmdName, out Command cmd))
        {
            // Debug output to check command lookup
            LogToConsole($"Found command: {cmd.Name}");

            if (cmd.AdminOnly && !IsAdmin(username))
            {
                SendText($"/msg {username} This command is admin-only.");
                return;
            }

            if (maintenanceMode && !cmd.AvailableInMaintenance && !IsAdmin(username))
            {
                SendText($"/msg {username} Bot is currently in maintenance mode.");
                return;
            }

            // Execute the command and send confirmation
            cmd.Execute(username, args);
            LogToConsole($"Executed command {cmdName} for {username}");
            return;
        }

        // If command not found in registered commands, try radio commands
        HandleRadioCommands(command, username);
    }

    private bool IsUserInCooldown(string username)
    {
        if (IsAdmin(username)) 
            return false; // Admins bypass cooldown
            
        if (userCooldowns.TryGetValue(username, out DateTime lastCommandTime))
        {
            TimeSpan timeSinceLastCommand = DateTime.Now - lastCommandTime;
            if (timeSinceLastCommand.TotalSeconds < COOLDOWN_SECONDS)
            {
                int remainingSeconds = COOLDOWN_SECONDS - (int)timeSinceLastCommand.TotalSeconds;
                SendText($"/msg {username} Please wait {remainingSeconds} seconds before using another command.");
                return true;
            }
        }
        return false;
    }

    private void HandleRadioCommands(string command, string username)
    {
        // Dictionary for radio system commands (case-insensitive)
        var radioCommands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { $"{commandPrefix}play dead-inside-slowed", "minecraft:ki10_dead_inside" },
            { $"{commandPrefix}play slay!", "minecraft:ki10_eternxlkz_slay" },
            { $"{commandPrefix}play hensonn-sahara", "minecraft:ki10_hensonn_sahara" },
            { $"{commandPrefix}play hyperpop-x-rave-x-lida", "minecraft:ki10_hyperpop_x_rave_x_lida" },
            { $"{commandPrefix}play kordhell-murder-in-my-mind", "minecraft:ki10_kordhell_murder_in_my_mind" },
            { $"{commandPrefix}play night-dancer", "minecraft:ki10_night_dancer" },
            { $"{commandPrefix}play pharrell-williams-happy", "minecraft:ki10_pharrell_williams_happy" },
            { $"{commandPrefix}play playamne-x-nateki-midnight", "minecraft:ki10_playamne_x_nateki_midnight" },
            { $"{commandPrefix}play x-slide", "minecraft:ki10_x_slide" },
            { $"{commandPrefix}play Zeldas-Lullaby", "minecraft:ki10_zeldas_lullaby_with_rain" }
        };

        // Extract the song name from the command
        string songName = command.Replace($"{commandPrefix}play ", "");

        if (radioCommands.TryGetValue(command, out string sound))
        {
            SendText($"/stopsound {username}");
            // Send the playsound command
            SendText($"/execute at {username} run playsound {sound} master {username}");
            // Display the currently playing song in the action bar
            SendText($"/title {username} actionbar [\"\",{{\"text\":\"Now Playing \",\"color\":\"blue\"}},{{\"text\":\"> \",\"color\":\"dark_green\"}},{{\"text\":\"{songName.Replace("\"", "\\\"")}\",\"bold\":true,\"color\":\"light_purple\"}}]");
        }
    }

    private void LogToConsole(string message)
    {
        if (debugMode || verboseMode)
        {
            string prefix = debugMode ? "DEBUG" : "VERBOSE";
            SendText($"/msg {AdminName} {prefix}: {message}");
        }
    }

    /// <summary>
    /// Retrieves the current server latency for the bot or highest player latency.
    /// </summary>
    /// <returns>Latency in milliseconds</returns>
    /// <remarks>
    /// Falls back to the highest player latency if bot latency is unavailable.
    /// Used by the !ping command to provide server connection information.
    /// </remarks>
    private int GetLatency()
    {
        var playerLatencies = GetPlayersLatency();
        var username = GetUsername();

        // If there is no bot in the list for some reason, take the highest latency among other players
        if (!playerLatencies.ContainsKey(username))
            return playerLatencies.Values.Max();

        return playerLatencies[username];
    }

    private bool IsAdmin(string username)
    {
        return !fakeNormalPlayer && username == AdminName;
    }

    private int GetMaxPlayers()
    {
        // This is a placeholder - replace with actual implementation
        // based on your server API capabilities
        return 20; // Default max players
    }

    private TimeSpan GetServerUptime()
    {
        // Use DateTime.Now instead of game ticks since we can't access them
        return DateTime.Now - startupTime;
    }

    private string GetServerVersion()
    {
        // This is a placeholder - replace with actual implementation
        // based on your server API capabilities
        return "1.20.4"; // Replace with actual version
    }
    private string GetServerIP()
    {
        // This is a placeholder - replace with actual implementation
        // based on your server API capabilities
        return "play.neonstudios.dev";
    }

    // Add this method to generate random IP addresses
    private string GenerateFakeIP()
    {
        return $"{random.Next(1, 255)}.{random.Next(0, 255)}.{random.Next(0, 255)}.{random.Next(1, 255)}";
    }

    // Add this helper method to generate random percentage
    private string GenerateRandomPercentage()
    {
        return $"{random.NextDouble() * 100:F2}%";
    }
}