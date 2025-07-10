using RestSharp;
using System.Threading.Tasks;
using static Program;

class Program
{
    public class PlannerLogic
    {
        // MIKE TODO: implement logic to implement 3 scenario options
        public static FormerenStation FindStationWithLowestMaxTimeBusy(List<FormerenStation> stations)
        {
            return stations
                .OrderBy(station => station.TimeBusy.Count > 0 ? station.TimeBusy.Max() : 0)
                .First();
        }

                Dictionary<int, double[]> scenarioWeights = new()
        {
            { 1, new double[] { 0.8, 0.15, 0.05 } },   // Scenario 1: 80%/15%/5%
            { 2, new double[] { 0.55, 0.30, 0.15 } }, // Scenario 2: 55%/30%/15%
            { 3, new double[] { 0.3, 0.35, 0.35 } } // Scenario 3: 33%/33%/34%
        };
        private int DrawGroupByScenario(int scenario, Random rng, Dictionary<int, double[]> scenarioWeights)
        {
            var weights = scenarioWeights[scenario];
            double roll = rng.NextDouble();
            if (roll < weights[0])
                return 1;
            else if (roll < weights[0] + weights[1])
                return 2;
            else
                return 3;
        }
        private CategoryCount PickOrderByScenario(
            List<CategoryCount> categories,
            int scenario,
            Random rng,
            Dictionary<int, double[]> scenarioWeights)
                {
                    while (true)
                    {
                        int group = DrawGroupByScenario(scenario, rng, scenarioWeights);

                        var groupCategories = categories
                            .Where(cat => cat.DurationGroup == group && cat.Count > 0)
                            .ToList();

                        if (groupCategories.Count > 0)
                        {
                            // Losowo z dostępnych
                            return groupCategories[rng.Next(groupCategories.Count)];
                        }
                        // Jeśli nie ma już orderów w żadnej grupie:
                        if (!categories.Any(cat => cat.Count > 0))
                            return null;
                        // Inaczej – losuj jeszcze raz
                    }
                }
        public void Plan(List<FormerenStation> formerenStations, List<CategoryCount> categories, int scenario, Random rng)
        {
            var bestStation = FindStationWithLowestMaxTimeBusy(formerenStations);
            var firstAvailableCategory = PickOrderByScenario(categories, scenario, rng, scenarioWeights);


            if (firstAvailableCategory != null)
            {
                // We take order for further processing
                Console.WriteLine($"Working with category: {firstAvailableCategory.Category}, We still have: {firstAvailableCategory.Count - 1}");
                int previousMax = bestStation.TimeBusy.Count > 0 ? bestStation.TimeBusy.Max() : 0;
                int duration = firstAvailableCategory.Duration;
                int prevY = 1; // We set up prevY to 1, just in case if order is 1st.

                // We assume that heigh of last order = Duration last category order
                // y parameter is quaite scetchy because it is calculated base on the middle of the schape (same as x, but x is constant per station)
                int prevHeight = 0;
                if (bestStation.OrdersAdded.Count > 0)
                {
                    var prevOrder = bestStation.OrdersAdded.Peek();
                    prevY = prevOrder.y;
                    prevHeight = categories.FirstOrDefault(cat => cat.Category == prevOrder.OrderCategory)?.Duration ?? 0;
                }
                int margin = 1;
                int newY = prevY + (prevHeight / 2) + (duration / 2) + margin;

                // end of y parameter calculation

                // Filling up TimeBusy for the station
                for (int i = 1; i <= duration; i++)
                {
                    bestStation.TimeBusy.Add(previousMax + i);
                }
                // We take 1 from Count property of the take category
                firstAvailableCategory.Count--;
                // We add Order to the station stack
                bestStation.OrdersAdded.Push((
                firstAvailableCategory.Category,
                bestStation.x,
                newY,
                firstAvailableCategory.Color
                ));
            }
            else
            {
                Console.WriteLine("Seems like we are done here.");
            }

        }
    }

    public static async Task SendMiroShapeAsync(string content, int x, int y, string fillColor, int height)
    {
        // Token have to be filled up ( temporary token for testing purposes)
        var options = new RestClientOptions("https://api.miro.com/v2/boards/uXjVIgiE9aY%3D/shapes");
        var client = new RestClient(options);
        var request = new RestRequest("");
        request.AddHeader("accept", "application/json");
        request.AddHeader("authorization", "Bearer eyJtaXJvLm9yaWdpbiI6ImV1MDEifQ_Rk85H1QZCIXNC7s_5KwI9z_L5F8");

        string body = $"{{\"data\":{{\"content\":\"{content}\",\"shape\":\"rectangle\"}},\"position\":{{\"x\":{x},\"y\":{y}}},\"geometry\":{{\"height\":{height},\"width\":100}},\"style\":{{\"fillColor\":\"{fillColor}\"}}}}";
        request.AddJsonBody(body, false);

        var response = await client.PostAsync(request);

        Console.WriteLine(response.Content);
    }

    public class FormerenStation
    // Define a class to represent a Formeren station with an ID and available time.
    // This class will be used to create a list of Formeren stations.
    // This class contains properties for the station ID and the time available for formeren.
    // Stack is beeing used to store orders added to the station, which can be later used for putting to RDY station.
    {
        public int FormerenStationID;
        public int TimeAvailableFormeren;
        public int x;
        public int y;
        public HashSet<int> TimeBusy { get; set; } = new HashSet<int>();
        public Stack<(string OrderCategory, int x, int y, string Color)> OrdersAdded { get; }
            = new Stack<(string, int, int, string)>();

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
                stations.Add(new FormerenStation { FormerenStationID = i, TimeAvailableFormeren = 1440, x = i*100, y=0 });
            }
            return stations;
        }
    }

public class ReadyLocation
    // Define a class to represent a Formeren Ready Location with an ID (operation see it as a dock) and available time.
    // This class will be used to create a list of Formeren Ready locations.
    // This class contains properties for the location ID and the time available when order can be awaiting for pick-up.
    // Stack is beeing used to store orders added to the Location, gets populated by FormerenStation.
    {
        public int ReadyLocationID;
        public int TimeAvailableReady;
        public int x;
        public int y;
        HashSet<int> TimeBusy { get; set; } = new HashSet<int>();
                public Stack<(string OrderCategory, int x, int y, string Color)> OrdersAdded { get; }
            = new Stack<(string, int, int, string)>();
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
            locations.Add(new ReadyLocation { ReadyLocationID = i, TimeAvailableReady = 1440, x = i * 100, y = 0 });
        }
        return locations;
    }
    }
    public class CategoryCount
        // Used to create a list of categories with information how many order slots are requested by the user.
    {
        public string Category { get; set; }
        public int Duration { get; set; }
        public int Count { get; set; }
        public int DurationGroup { get; set; }
        // 1 = shortest time needed, 3 = longest time needed.
        public string Color { get; set; }
    }
    static async Task Main(string[] args)
    {
        int FormerenStationAmmount = 0;
        int ReadyLocationAmmount = 0;
        int OrderSlotAmmount = 0;
        List<CategoryCount> Categories = new List<CategoryCount>
        {
            new CategoryCount{ Category = "VE(A)",      Duration=60 ,   Count =0, DurationGroup= 1, Color = "#f5f6f8"},
            new CategoryCount{ Category = "VE(B)",      Duration=90 ,   Count =0, DurationGroup= 1, Color = "#d5f692"},
            new CategoryCount{ Category = "E(A)",       Duration=120 ,  Count =0, DurationGroup= 2, Color = "#d0e17a"},
            new CategoryCount{ Category = "E(B)",       Duration=150 ,  Count =0, DurationGroup= 2, Color = "#93d275"},
            new CategoryCount{ Category = "M(A)",       Duration=180 ,  Count =0, DurationGroup= 2, Color = "#67c6c0"},
            new CategoryCount{ Category = "M(B)",       Duration=210 ,  Count =0, DurationGroup= 2, Color = "#23bfe7"},
            new CategoryCount{ Category = "D(A)",       Duration=240 ,  Count =0, DurationGroup= 2, Color = "#a6ccf5"},
            new CategoryCount{ Category = "D(B)",       Duration=270 ,  Count =0, DurationGroup= 3, Color = "#7b92ff"},
            new CategoryCount{ Category = "VD(A)",      Duration=300 ,  Count =0, DurationGroup= 3, Color = "#fff9b1"},
            new CategoryCount{ Category = "VD(B)",      Duration=330 ,  Count =0, DurationGroup= 3, Color = "#f5d128"},
            new CategoryCount{ Category = "Container",  Duration=240 ,  Count =0, DurationGroup= 2, Color = "#ff9d48"},
            new CategoryCount{ Category = "SE",         Duration=270 ,  Count =0, DurationGroup= 3, Color = "#f16c7f"},
            new CategoryCount{ Category = "BE(A)",      Duration=180 ,  Count =0, DurationGroup= 2, Color = "#ea94bb"},
            new CategoryCount{ Category = "BE(B)",      Duration=270 ,  Count =0, DurationGroup= 3, Color = "#ffcee0"},
            new CategoryCount{ Category = "NL",         Duration=270 ,  Count =0, DurationGroup= 3, Color = "#b384bb"},
         };

        // Welcome message and input section
        Console.WriteLine("Welcome to the Planner application!");
        Console.WriteLine("This application is designed to help you plan your work-load effectively at the Heijen department.");
        Console.WriteLine("It will assist you in determining the best order slots based on the available time at the Formeren station and Ready locations.");
        Console.WriteLine("Please type in ammount of Formeren stations available:");
        FormerenStationAmmount = Convert.ToInt16(Console.ReadLine());
        Console.WriteLine("Please type in ammount of Ready locations available:");
        ReadyLocationAmmount = Convert.ToInt16(Console.ReadLine());
        Console.WriteLine("Choose the scenario (1, 2, 3):");
        int scenario = int.Parse(Console.ReadLine());
        Random rng = new Random();
        foreach (var category in Categories)
        {
            Console.WriteLine($"Please type in ammount of Order slots available for {category.Category}:");
            category.Count = Convert.ToInt16(Console.ReadLine());
        }


        var formerenStations = CreatorFormerenStation.CreatorFormerenStations(FormerenStationAmmount);
        var readyLocation = CreatorReadyLocation.CreatorReadyLocations(ReadyLocationAmmount);



        // We are going to use PlannerLogic class to plan the orders based on the user input.
        PlannerLogic planner = new PlannerLogic();

        while (Categories.Sum(cat => cat.Count) > 0)
        {
            planner.Plan(formerenStations, Categories, scenario, rng);
        }

        Console.WriteLine("All orders have been given!");

        // Using RestSharp to make a POST request to the Miro API to create a shape on a board(booking)
        foreach (var station in formerenStations)
        {
            foreach (var order in station.OrdersAdded)
            {
                // We unpack the order tuple to get the category, x, y and color
                string content = order.OrderCategory;
                int x = order.x;
                int y = order.y;
                string fillColor = order.Color;
                int duration = Categories.First(cat => cat.Category == order.OrderCategory).Duration;
                int height = duration;
                await SendMiroShapeAsync(content, x, y, fillColor, height);

            }
        }

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