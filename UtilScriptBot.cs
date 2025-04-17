//MCCScript 1.0

// You can easily change the prefix here
MCC.LoadBot(new UtilBot("!")); // or any other prefix like ".", "/", "$", etc.

//MCCScript Extensions

/// <summary>
/// A utility bot for Minecraft server management and player entertainment.
/// Provides command handling for server administration, player teleportation, and music playback.
/// </summary>
class UtilBot : ChatBot
{
    private const string DEFAULT_PREFIX = ".";
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
    private bool fakeNormalPlayer = false; // Add this field for fake mode
    private bool verboseMode = false; // Add this field for verbose mode

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
        
        RegisterCommand($"{commandPrefix}mt", "Toggle maintenance mode", 
            (username, _) => {
                maintenanceMode = !maintenanceMode;
                string status = maintenanceMode ? "enabled" : "disabled";
                SendText($"/msg {username} Maintenance mode {status}");
                SendText($"/say Bot is now in {(maintenanceMode ? "maintenance mode" : "normal mode")}");
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

        SendText($"/msg {AdminName} UtilBot initialized successfully.");
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
}
