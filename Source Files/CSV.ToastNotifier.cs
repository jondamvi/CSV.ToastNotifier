/* 
 * Toast Notification Tool (C# 5 Compatible Version)
 * Fixed string interpolation and modern language features
 * All $"" replaced with String.Format()
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using SXML = System.Xml;
using WXML = Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CSV.ToastNotifier
{
    enum NotificationType
    {
        Default,
        Alarm,
        Reminder,
        IncomingCall,
        Urgent
    }

    enum FileType
    {
        Wav, 
        Png
    }

    class Program
    {
        // Security constants
        private const int MAX_MESSAGE_LENGTH = 256;
        private const int MAX_TITLE_LENGTH = 32;
        private const int MAX_SOURCE_LENGTH = 32;
        private const int MAX_PATH_LENGTH = 259;
        private const int MAX_COMMAND_LINE = 1965;

        private static readonly Regex WavPathRegex = new Regex(
            @"^(?i)[A-Z]:\\(([A-Z0-9_\(\)\[\]!+-]|[A-Z0-9_\(\)\[\]!+-][A-Z0-9_.\(\)\[\]! +-]+[A-Z0-9_\(\)\[\]!+-])\\)*?([A-Z0-9_\(\)\[\]!+-]|[A-Z0-9_\(\)\[\]!+-][A-Z0-9_.\(\)\[\]! +-]+)\.WAV$", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PngPathRegex = new Regex(
            @"^(?i)[A-Z]:\\(([A-Z0-9_\(\)\[\]!+-]|[A-Z0-9_\(\)\[\]!+-][A-Z0-9_.\(\)\[\]! +-]+[A-Z0-9_\(\)\[\]!+-])\\)*?([A-Z0-9_\(\)\[\]!+-]|[A-Z0-9_\(\)\[\]!+-][A-Z0-9_.\(\)\[\]! +-]+)\.PNG$", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex patterns (fixed string interpolation)
        private static readonly Regex XmlInvalidCharsRegex = new Regex(@"[^\x09\x0A\x0D\x20-\xD7FF\xE000-\xFFFD]", RegexOptions.Compiled);
        private static readonly Regex PathInvalidCharsRegex = new Regex(
            String.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidPathChars()))), 
            RegexOptions.Compiled);

        static void Main(string[] args)
        {
            try
            {
                int totalLength = args.Sum(arg => arg.Length + 1);
                if (totalLength > MAX_COMMAND_LINE)
                {
                    throw new ArgumentException(String.Format("Command line too long. Total arguments exceed {0} characters.", MAX_COMMAND_LINE));
                }

                var parameters = ParseArguments(args);
                
                if (parameters.ShowHelp)
                {
                    ShowHelp();
                    return;
                }

                ShowNotification(parameters);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(String.Format("Error: {0}", ex.Message));
                Console.WriteLine("\nUse --help for usage information.");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(String.Format("Unexpected error: {0}", ex.Message));
                Environment.Exit(2);
            }
        }

        static NotificationParameters ParseArguments(string[] args)
        {
            var parameters = new NotificationParameters();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--help":
                    case "-h":
                    case "/?":
                        parameters.ShowHelp = true;
                        return parameters;
                        
                    case "--message":
                    case "-m":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("--message requires a value");
                        parameters.Message = ValidateAndSanitizeText(args[++i], MAX_MESSAGE_LENGTH, "message");
                        break;
                        
                    case "--source":
                    case "-s":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("--source requires a value");
                        parameters.SourceName = ValidateAndSanitizeText(args[++i], MAX_SOURCE_LENGTH, "source");
                        break;

                    case "--title":
                    case "-t":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("--title requires a value");
                        parameters.Title = ValidateAndSanitizeText(args[++i], MAX_TITLE_LENGTH, "title");
                        break;

                    case "--muted":
                        parameters.Muted = true;
                        break;

                    case "--icon":
                    case "-i":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("--icon requires a value");
                        parameters.IconPath = ValidateAndSanitizePath(args[++i], "icon", FileType.Png);
                        break;
                        
                    case "--audio":
                    case "-a":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("--audio requires a value");
                        parameters.AudioPath = ValidateAndSanitizePath(args[++i], "audio", FileType.Wav);
                        break;

                    case "--type":
                        if (i + 1 >= args.Length)
                            throw new ArgumentException("--type requires a value");
                        string typeValue = args[++i];
                        NotificationType type;
                        if (!Enum.TryParse(typeValue, true, out type))
                        {
                            throw new ArgumentException(String.Format(
                                "Invalid notification type: {0}. Valid values are: default, alarm, incomingCall, urgent",
                                typeValue));
                        }
                        parameters.Type = type;
                        break;
                        
                    default:
                        throw new ArgumentException(String.Format("Unknown argument: {0}", args[i]));
                }
            }
            
            if (!parameters.ShowHelp)
            {
                if (string.IsNullOrEmpty(parameters.Message))
                    throw new ArgumentException("--message is required");
                if (string.IsNullOrEmpty(parameters.SourceName))
                    throw new ArgumentException("--source is required");
            }
            
            return parameters;
        }

        static string ValidateAndSanitizeText(string input, int maxLength, string parameterName)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException(String.Format("{0} cannot be empty", parameterName));

            if (input.Contains('\0'))
                throw new ArgumentException(String.Format("{0} contains invalid null characters", parameterName));

            if (input.Length > maxLength)
                throw new ArgumentException(String.Format("{0} exceeds maximum length of {1} characters", parameterName, maxLength));

            string sanitized = XmlInvalidCharsRegex.Replace(input, "");
            
            if (string.IsNullOrWhiteSpace(sanitized))
                throw new ArgumentException(String.Format("{0} contains only invalid characters", parameterName));

            return sanitized;
        }

        static string ValidateAndSanitizePath(string path, string parameterName, FileType expectedFileType)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (path.Contains('\0'))
                throw new ArgumentException(String.Format("{0} path contains invalid null characters", parameterName));

            if (path.Length > MAX_PATH_LENGTH)
                throw new ArgumentException(String.Format("{0} path exceeds maximum length of {1} characters", parameterName, MAX_PATH_LENGTH));

            if (PathInvalidCharsRegex.IsMatch(path))
                throw new ArgumentException(String.Format("{0} path contains invalid characters", parameterName));

            var regex = expectedFileType == FileType.Wav ? WavPathRegex : PngPathRegex;
            var fileExtension = expectedFileType == FileType.Wav ? "wav" : "png";
            if (!regex.IsMatch(path))
                throw new ArgumentException(
                    String.Format("{0} path does not match required pattern. Must be a valid Windows path ending with '.{1}'.", parameterName, fileExtension));

            if (path.Contains("..") || path.Contains("~"))
                throw new ArgumentException(String.Format("{0} path contains potentially dangerous sequences", parameterName));

            try
            {
                path = Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(String.Format("{0} path is invalid: {1}", parameterName, ex.Message));
            }

            if (path.Contains("..") || path.Contains("~"))
                throw new ArgumentException(String.Format("{0} path contains potentially dangerous sequences", parameterName));

            string[] restrictedPaths = { 
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"C:\Windows\System32",
                @"C:\Windows\SysWOW64"
            };

            foreach (var restrictedPath in restrictedPaths)
            {
                if (path.StartsWith(restrictedPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(String.Format("{0} path points to a restricted system directory", parameterName));
                }
            }

            return path;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Toast Notification Tool v1.0.0");
            Console.WriteLine("Shows Windows Toast Notifications with custom text, title, icon, and sound.\n");
            Console.WriteLine("Usage: ToastNotification.exe --message <text> --source <app_name> [options]\n");
            Console.WriteLine("Required arguments:");
            Console.WriteLine("  --message, -m <text>     Notification message to display");
            Console.WriteLine("  --source, -s <name>      Application name (source of notification)\n");
            Console.WriteLine("Optional arguments:");
            Console.WriteLine("  --title, -t <text>       Notification title");
            Console.WriteLine("  --icon, -i <path>        Path to PNG icon file");
            Console.WriteLine("  --audio, -a <path>       Path to WAV audio file");
            Console.WriteLine("  --muted                  Disable notification sound");
            Console.WriteLine("  --type <type>            Notification type: default, alarm,");
            Console.WriteLine("                           incomingCall, urgent (default: default)");
            Console.WriteLine("  --help, -h, /?           Show this help message\n");
            Console.WriteLine("Examples:");
            Console.WriteLine("  ToastNotification.exe -m \"Hello World\" -s \"MyApp\"");
            Console.WriteLine("  ToastNotification.exe -m \"Meeting in 5 minutes\" -s \"Calendar\" -t \"Reminder\" --type reminder");
            Console.WriteLine("  ToastNotification.exe -m \"Custom notification\" -s \"MyApp\" -i \"C:\\icon.png\" -a \"C:\\sound.wav\"");
        }

        static void ShowNotification(NotificationParameters parameters)
        {
            try
            {
                Windows.Data.Xml.Dom.XmlDocument template = null;
                
                bool audioPathValid = !string.IsNullOrEmpty(parameters.AudioPath) && 
                                    File.Exists(parameters.AudioPath) && 
                                    ConfirmWavFileType(parameters.AudioPath);
                                    
                bool iconPathValid = !string.IsNullOrEmpty(parameters.IconPath) && 
                                    File.Exists(parameters.IconPath) && 
                                    ConfirmPngFileType(parameters.IconPath);

                if (iconPathValid)
                {
                    if (!string.IsNullOrEmpty(parameters.Title))
                    {
                        template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
                    }
                    else
                    {
                        template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(parameters.Title))
                    {
                        template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                    }
                    else
                    {
                        template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
                    }
                }

                var xml = template.GetXml();
                var toastXml = new System.Xml.XmlDocument();
                toastXml.LoadXml(xml);

                // Get text elements
                var textElements = toastXml.GetElementsByTagName("text");
                
                // Set notification type attributes
                if (parameters.Type != NotificationType.Default)
                {
                    var toastElement = toastXml.DocumentElement;
                    toastElement.SetAttribute("scenario", parameters.Type.ToString().ToLowerInvariant());
                    
                    if (parameters.Type == NotificationType.Reminder)
                    {
                        toastElement.SetAttribute("duration", "long");
                    }
                }

                if (!string.IsNullOrEmpty(parameters.Title))
                {
                    // Find text elements by id attribute
                    foreach (System.Xml.XmlElement textElement in textElements)
                    {
                        if (textElement.GetAttribute("id") == "1")
                        {
                            textElement.AppendChild(toastXml.CreateTextNode(parameters.Title));
                        }
                        else if (textElement.GetAttribute("id") == "2")
                        {
                            textElement.AppendChild(toastXml.CreateTextNode(parameters.Message));
                        }
                    }
                }
                else
                {
                    // Only message, no title
                    foreach (System.Xml.XmlElement textElement in textElements)
                    {
                        if (textElement.GetAttribute("id") == "1")
                        {
                            textElement.AppendChild(toastXml.CreateTextNode(parameters.Message));
                        }
                    }
                }

                if (iconPathValid)
                {
                    var imageElements = toastXml.GetElementsByTagName("image");

                    if (imageElements.Count > 0)
                    {
                        // Use Uri to properly format the file path
                        try
                        {
                            var uri = new Uri(parameters.IconPath, UriKind.Absolute);
                            imageElements[0].Attributes["src"].Value = uri.ToString();
                        }
                        catch
                        {
                            // If URI creation fails, skip the icon
                            iconPathValid = false;
                        }
                    }
                }

                var audioElement = toastXml.CreateElement("audio");
                if (!parameters.Muted && string.IsNullOrEmpty(parameters.AudioPath))
                {
                    // Default Toast Notification Audio will be played - don't set any attributes
                }
                else if (parameters.Muted || (!string.IsNullOrEmpty(parameters.AudioPath) && !audioPathValid))
                {
                    audioElement.SetAttribute("silent", "true");
                }
                else if (!parameters.Muted && audioPathValid)
                {
                    try
                    {
                        var uri = new Uri(parameters.AudioPath, UriKind.Absolute);
                        audioElement.SetAttribute("src", uri.ToString());
                    }
                    catch
                    {
                        // If URI creation fails, use default sound
                        audioElement.SetAttribute("silent", "true");
                    }
                }
                
                toastXml.DocumentElement.AppendChild(audioElement);

                // Create toast notification and show it
                var xmlDoc = new Windows.Data.Xml.Dom.XmlDocument();
                xmlDoc.LoadXml(toastXml.OuterXml);
                
                var toast = new ToastNotification(xmlDoc)
                {
                    Tag = "CSV_ToastNotifier",
                    Group = "CSV_ToastNotifier"
                };

                var notifier = ToastNotificationManager.CreateToastNotifier(parameters.SourceName);
                notifier.Show(toast);

                Console.WriteLine("Toast notification displayed successfully!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);
                throw;
            }
        }

       static bool ConfirmWavFileType(string filePath)
        {
            try
            {
                // Additional safety check
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length < 12)
                    return false;
                
                // Prevent reading extremely large files
                if (fileInfo.Length > 10 * 1024 * 1024) // 10MB limit
                    return false;

                using (var fs = File.OpenRead(filePath))
                {
                    var buffer = new byte[12];
                    int bytesRead = fs.Read(buffer, 0, 12);
                    if (bytesRead < 12)
                        return false;

                    string riff = Encoding.ASCII.GetString(buffer, 0, 4);
                    string wave = Encoding.ASCII.GetString(buffer, 8, 4);

                    return riff == "RIFF" && wave == "WAVE";
                }
            }
            catch
            {
                return false;
            }
        }

        static bool ConfirmPngFileType(string filePath)
        {
            try
            {
                // Additional safety check
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length < 8)
                    return false;
                
                // Prevent reading extremely large files
                if (fileInfo.Length > 1 * 1024 * 1024) // 1MB limit
                    return false;

                using (var fs = File.OpenRead(filePath))
                {
                    var buffer = new byte[8];
                    int bytesRead = fs.Read(buffer, 0, 8);
                    if (bytesRead < 8)
                        return false;

                    byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                    return buffer.SequenceEqual(pngSignature);
                }
            }
            catch
            {
                return false;
            }
        }
    }

    class NotificationParameters
    {
        public bool ShowHelp { get; set; }
        public string Message { get; set; }
        public string Title { get; set; }
        public string SourceName { get; set; }
        public bool Muted { get; set; }
        public string IconPath { get; set; }
        public string AudioPath { get; set; }
        public NotificationType Type { get; set; }
    }
}
