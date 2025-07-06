class Program
{
public class Plan
    // MIKE TODO: create an idea how we can create and export rdy plan to a file.
    {
    }

public class FormerenStation
    // Define a class to represent a Formeren station with an ID and available time.
    // This class will be used to create a list of Formeren stations.
    // This class contains properties for the station ID and the time available for formeren.
    {
        public int FormerenStationID;
        public int TimeAvailableFormeren;
    }

public class CreatorFormerenStation
    // Creates a list of formeren stations based on the user input. Every day in operation is slightly different,
    // so it is crucial to be able to create a list of stations dynamically.
    {
        public static List<FormerenStation> CreatorFormerenStations(int amount)
        {
            var stations = new List<FormerenStation>();
            for (int i = 1; i <= amount; i++)
            {
                stations.Add(new FormerenStation { FormerenStationID = i, TimeAvailableFormeren = 1440 });
            }
            return stations;
        }
    }

public class ReadyLocation
    // Define a class to represent a Formeren Ready Location with an ID (operation see it as a dock) and available time.
    // This class will be used to create a list of Formeren Ready locations.
    // This class contains properties for the location ID and the time available when order can be awaiting for pick-up.
    {
        public int ReadyLocationID;
        public int TimeAvailableReady;
    }

public class CreatorReadyLocation
    // Creates a list of ready locations based on the user input. Every day in operation is slightly different,
    // so it is crucial to be able to create a list of locations dynamically.
    {
        public static List<ReadyLocation> CreatorReadyLocations(int amount)
    {
        var locations = new List<ReadyLocation>();
        for (int i = 1; i <= amount; i++)
        {
            locations.Add(new ReadyLocation { ReadyLocationID = i, TimeAvailableReady = 1440 });
        }
        return locations;
    }
    }
    public class OrderOutboundSlotCategoryToMinutes
    {
        public string OrderOutboundSlotCategory;
        public int CategoryTimeNeeded()
        {
            switch (OrderOutboundSlotCategory)
            {
                case "VE(A)": return 60;
                case "VE(B)": return 90;
                case "E(A)": return 120;
                case "E(B)": return 150;
                case "M(A)": return 180;
                case "M(B)": return 210;
                case "D(A)": return 240;
                case "D(B)": return 270;
                case "VD(A)": return 300;
                case "VD(B)": return 330;
                case "Container": return 240;
                case "SE": return 270;
                case "BE(A)": return 180;
                case "BE(B)": return 270;
                case "NL": return 270;
                default: return 0;
            }
        }
    }
    public class CategoryCount
        // Used to create a list of categories with information how many order slots are requested by the user.
    {
        public string Category { get; set; }
        public int Count { get; set; }
    }
    static void Main(string[] args)
    {
        int FormerenStationAmmount = 0;
        int ReadyLocationAmmount = 0;
        int OrderSlotAmmount = 0;
        List<CategoryCount> Categories = new List<CategoryCount>
        {
            new CategoryCount{ Category = "VE(A)", Count =0},
            new CategoryCount{ Category = "VE(B)", Count =0},
            new CategoryCount{ Category = "E(A)", Count =0},
            new CategoryCount{ Category = "E(B)", Count =0},
            new CategoryCount{ Category = "M(A)", Count =0},
            new CategoryCount{ Category = "M(B)", Count =0},
            new CategoryCount{ Category = "D(A)", Count =0},
            new CategoryCount{ Category = "D(B)", Count =0},
            new CategoryCount{ Category = "VD(A)", Count =0},
            new CategoryCount{ Category = "VD(B)", Count =0},
            new CategoryCount{ Category = "Container", Count =0},
            new CategoryCount{ Category = "SE", Count =0},
            new CategoryCount{ Category = "BE(A)", Count =0},
            new CategoryCount{ Category = "BE(B)", Count =0},
            new CategoryCount{ Category = "NL", Count =0},
         };

        Console.WriteLine("Welcome to the Planner application!");
        Console.WriteLine("This application is designed to help you plan your work-load effectively at the Heijen department.");
        Console.WriteLine("It will assist you in determining the best order slots based on the available time at the Formeren station and Ready locations.");
        Console.WriteLine("Please type in ammount of Formeren stations available:");
        FormerenStationAmmount = Convert.ToInt16(Console.ReadLine());
        Console.WriteLine("Please type in ammount of Ready locations available:");
        ReadyLocationAmmount = Convert.ToInt16(Console.ReadLine());
        foreach(var category in Categories)
        {
            Console.WriteLine($"Please type in ammount of Order slots available for {category.Category}:");
            category.Count = Convert.ToInt16(Console.ReadLine());
        }


        var formerenStations = CreatorFormerenStation.CreatorFormerenStations(FormerenStationAmmount);
        var readyLocation = CreatorReadyLocation.CreatorReadyLocations(ReadyLocationAmmount);


        // Testing input section
        Console.WriteLine($"{FormerenStationAmmount}, {ReadyLocationAmmount},{OrderSlotAmmount} ");
        foreach (var category in Categories)
        {
            Console.WriteLine($"{category.Category}: {category.Count}");
        }
        foreach (var station in formerenStations)
        {
            Console.WriteLine($"FormerenStationID: {station.FormerenStationID}, TimeAvailableFormeren: {station.TimeAvailableFormeren}");
        }
        foreach (var location in readyLocation)
        {
            Console.WriteLine($"ReadylocationID: {location.ReadyLocationID}, TimeAvailableReady: {location.TimeAvailableReady}");
        }
        Console.ReadLine();
    }
}