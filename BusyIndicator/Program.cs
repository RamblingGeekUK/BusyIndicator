using Microsoft.Extensions.Configuration;
using rpi_ws281x;
using System;
using System.Drawing;
using System.Threading;
using System.Timers;
using TeamsBusyLED.Authentication;
using TeamsBusyLED.Graph;

namespace TeamsBusyLED
{
    class Program
    {

        private static System.Timers.Timer aTimer;

        static void Main(string[] args)
        {
            Console.WriteLine("Busy Indicator v0.01\n");

            InitLED();

            var appConfig = LoadAppSettings();

            if (appConfig == null)
            {
                Console.WriteLine("Missing or invalid appsettings.json...exiting");
                return;
            }

            var appId = appConfig["appId"];
            var scopesString = appConfig["scopes"];
            var scopes = scopesString.Split(';');

            // Initialize the auth provider with values from appsettings.json
            var authProvider = new DeviceCodeAuthProvider(appId, scopes);

            // Request a token to sign in the user
            var accessToken = authProvider.GetAccessToken().Result;

            // Initialize Graph client
            GraphHelper.Initialize(authProvider);

            // Get signed in user
            var user = GraphHelper.GetMeAsync().Result;
            Console.WriteLine($"Welcome {user.DisplayName}!\n");

            SetTimer();
            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.ReadLine();
            aTimer.Stop();
            aTimer.Dispose();
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            var Presence = GraphHelper.GetMePresenceAsync().Result;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine("\rThe Elapsed event was raised at {0:HH:mm:ss.fff}", e.SignalTime);
            Console.WriteLine($"\rPresence : {Presence.Availability}");
        }

        private static void InitLED()
        {
            //The default settings uses a frequency of 800000 Hz and the DMA channel 10.
            var settings = Settings.CreateDefaultSettings();

            //Use 16 LEDs and GPIO Pin 18.
            //Set brightness to maximum (255)
            //Use Unknown as strip type. Then the type will be set in the native assembly.
            var controller = settings.AddController(16, Pin.Gpio18, StripType.WS2812_STRIP, ControllerType.PWM0, 255, false);

            using (var rpi = new WS281x(settings))
            {
                //Set the color of the first LED of controller 0 to blue
                controller.SetLED(0, Color.Blue);
                //Set the color of the second LED of controller 0 to red
                controller.SetLED(1, Color.Red);
                rpi.Render();
            }
        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }


        static IConfigurationRoot LoadAppSettings()
        {
            var appConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();

            // Check for required settings
            if (string.IsNullOrEmpty(appConfig["appId"]) ||
                string.IsNullOrEmpty(appConfig["scopes"]))
            {
                return null;
            }

            return appConfig;
        }

        static string FormatDateTimeTimeZone(Microsoft.Graph.DateTimeTimeZone value)
        {
            // Get the timezone specified in the Graph value
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(value.TimeZone);
            // Parse the date/time string from Graph into a DateTime
            var dateTime = DateTime.Parse(value.DateTime);

            // Create a DateTimeOffset in the specific timezone indicated by Graph
            var dateTimeWithTZ = new DateTimeOffset(dateTime, timeZone.BaseUtcOffset)
                .ToLocalTime();

            return dateTimeWithTZ.ToString("g");
        }

        static void ListCalendarEvents()
        {
            var events = GraphHelper.GetEventsAsync().Result;

            Console.WriteLine("Events:");

            foreach (var calendarEvent in events)
            {
                Console.WriteLine($"Subject: {calendarEvent.Subject}");
                Console.WriteLine($"  Organizer: {calendarEvent.Organizer.EmailAddress.Name}");
                Console.WriteLine($"  Start: {FormatDateTimeTimeZone(calendarEvent.Start)}");
                Console.WriteLine($"  End: {FormatDateTimeTimeZone(calendarEvent.End)}");
            }
        }

    

    }
}
