using RestSharp;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Program;

class Program
{
    public class PlannerLogic
    {
        public static FormerenStation FindStationWithLowestMaxTimeBusy(List<FormerenStation> stations)
        {
            return stations
                .OrderBy(station => station.TimeBusy.Count > 0 ? station.TimeBusy.Max() : 0)
                .First();
        }
        public static ReadyLocation FindReadyLocationWithLowestMaxTimeBusy(List<ReadyLocation> readyLocations)
        {
            return readyLocations
                .OrderBy(location => location.TimeBusy.Count > 0 ? location.TimeBusy.Max() : 0)
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
                            return groupCategories[rng.Next(groupCategories.Count)];
                        }
                        if (!categories.Any(cat => cat.Count > 0))
                            return null;
                    }
                }
        public void PlanToFormerenStation(List<FormerenStation> formerenStations, List<CategoryCount> categories, int scenario, Random rng)
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
                var orderStart = previousMax +1;
                var orderEnd = orderStart + duration -1;
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
                firstAvailableCategory.Color,
                orderStart,
                orderEnd
                ));
            }
            else
            {
                Console.WriteLine("Seems like we are done here.");
            }
        
        }

        private static bool IsSlotAvailable(HashSet<int> timeBusy, int orderStart, int orderEnd)
        {
            for (int minute = orderStart; minute < orderEnd; minute++)
                if (timeBusy.Contains(minute))
                    return false;
            return true;
        }

        private static void MarkSlotBusy(HashSet<int> timeBusy, int orderStart, int orderEnd)
        {
            for (int minute = orderStart; minute < orderEnd; minute++)
                timeBusy.Add(minute);
        }
        public static void TransferOrdersToReadyLocations(List<FormerenStation> formerenStations, List<ReadyLocation> readyLocations, List<CategoryCount> categories)
        {
            var station = formerenStations.FirstOrDefault(s => s.OrdersAdded.Count > 0);
            if (station == null) return; // Wszystko przeniesione

            // Zdejmujemy JEDEN order z góry stacka (ostatnio dodany)
            var order = station.OrdersAdded.Pop();
            int duration = categories.First(cat => cat.Category == order.OrderCategory).Duration;

            // Szukamy docka, który ma cały slot wolny
            ReadyLocation ready = null;
            int orderStart = order.orderStart;
            int orderEnd = order.orderEnd;
            foreach (var loc in readyLocations.OrderBy(_ => Guid.NewGuid()))
            {
                if (IsSlotAvailable(loc.TimeBusy, orderStart, orderEnd))
                {
                    ready = loc;
                    break;
                }
            }
            if (ready == null)
            {
                // Brak wolnego docka – możesz tu wywołać exception, logikę restartu itd.
                Console.WriteLine("Brak wolnego docka na ten slot! (TODO: obsługa tego przypadku)");
                return;
            }

            // Wylicz x
            int x = (20 + ready.ReadyLocationID) * 100;

            // Oznacz slot jako zajęty!
            MarkSlotBusy(ready.TimeBusy, orderStart, orderEnd);

            // Przenosimy order (z nowym x)
            ready.OrdersAdded.Push((
                order.OrderCategory,
                x,
                order.y,
                order.Color,
                orderStart,
                orderEnd
            ));
        }

        public void CheckIfEnoughTimeAvailable(List<FormerenStation> formerenStations, List<CategoryCount> categories)
        {
            // This method checks if there is enough time available in the Formeren stations to process all orders.
            // If not, it will tell user that he fu*ked up and there is not enough time to process all orders.
            int totalTimeNeeded = categories.Sum(cat => cat.Count * cat.Duration);
            int totalTimeAvailable = formerenStations.Sum(station => station.TimeAvailableFormeren);
            if (totalTimeNeeded > totalTimeAvailable)
            {
                throw new InvalidOperationException($"Not enough time available in Formeren stations to process all orders. Orders need {totalTimeNeeded} minutes and it is greater than available: {totalTimeAvailable} minutes")
                {

                };
            }
        }
    }

    public static int ReadIntFromConsole(string prompt)
    {
        int result;
        while (true)
        {
            Console.WriteLine(prompt);
            string input = Console.ReadLine();
            if (int.TryParse(input, out result))
            {
                if (result > -1)
                    return result; // Sukces – zwracamy wynik
                else
                    Console.WriteLine("Please type in a valid number greater than 0.");
            }
            else
            {
                Console.WriteLine("Invalid format, only integer can be accepted.");
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
        request.AddHeader("authorization", "Bearer eyJtaXJvLm9yaWdpbiI6ImV1MDEifQ_9FnwMHJnrZXzGHHUF2f6cggr4Gk");

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
        public int orderStart;
        public int orderEnd;
        public HashSet<int> TimeBusy { get; set; } = new HashSet<int>();
        public Stack<(string OrderCategory, int x, int y, string Color, int orderStart, int orderEnd)> OrdersAdded { get; }
            = new Stack<(string, int, int, string, int orderStart, int orderEnd)>();

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
        public int orderStart;
        public int orderEnd;
        public HashSet<int> TimeBusy { get; set; } = new HashSet<int>();
                public Stack<(string OrderCategory, int x, int y, string Color,int orderStart, int orderEnd)> OrdersAdded { get; }
            = new Stack<(string, int, int, string, int orderStart, int orderEnd)>();
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


        int FormerenStationAmmount = ReadIntFromConsole("Please type in amount of Formeren stations available:");
        int ReadyLocationAmmount = ReadIntFromConsole("Please type in amount of Ready locations available:");
        int scenario = ReadIntFromConsole("Choose the scenario (1, 2, 3):");


        Random rng = new Random();
        foreach (var category in Categories)
        {
            category.Count = ReadIntFromConsole($"Please type in ammount of Order slots available for {category.Category}:");
        }


        var formerenStations = CreatorFormerenStation.CreatorFormerenStations(FormerenStationAmmount);
        var readyLocation = CreatorReadyLocation.CreatorReadyLocations(ReadyLocationAmmount);



        // We are going to use PlannerLogic class to plan the orders based on the user input.
        PlannerLogic planner = new PlannerLogic();
        //try
        //{
        //    // We check if user is not retarded and try to plan too many orders

        //    planner.CheckIfEnoughTimeAvailable(formerenStations, Categories);
        //    while (Categories.Sum(cat => cat.Count) > 0)
        //    {
        //        planner.PlanToFormerenStation(formerenStations, Categories, scenario, rng);
        //    }
        //        Console.WriteLine("All orders have been given!");
        //    foreach (var station in formerenStations)
        //    {
        //        foreach (var order in station.OrdersAdded)
        //        {
        //            // We unpack the order tuple to get the category, x, y and color
        //            string content = order.OrderCategory;
        //            int x = order.x;
        //            int y = order.y;
        //            string fillColor = order.Color;
        //            int duration = Categories.First(cat => cat.Category == order.OrderCategory).Duration;
        //            int height = duration;
        //            await SendMiroShapeAsync(content, x, y, fillColor, height);

        //        }
        //    }


        //    // Using RestSharp to make a POST request to the Miro API to create a shape on a board(booking)
        //    foreach (var station in formerenStations)
        //        {
        //            foreach (var order in station.OrdersAdded)
        //            {
        //                // We unpack the order tuple to get the category, x, y and color
        //                string content = order.OrderCategory;
        //                int x = order.x;
        //                int y = order.y;
        //                string fillColor = order.Color;
        //                int duration = Categories.First(cat => cat.Category == order.OrderCategory).Duration;
        //                int height = duration;
        //                await SendMiroShapeAsync(content, x, y, fillColor, height);

        //            }
        //        }

        //        // Testing input section
        //        Console.WriteLine($"{FormerenStationAmmount}, {ReadyLocationAmmount},{OrderSlotAmmount} ");
        //        foreach (var category in Categories)
        //        {
        //            Console.WriteLine($"{category.Category}: {category.Count}");
        //        }
        //        foreach (var station in formerenStations)
        //        {
        //            Console.WriteLine($"FormerenStationID: {station.FormerenStationID}, TimeAvailableFormeren: {station.TimeAvailableFormeren}");
        //        }
        //        foreach (var location in readyLocation)
        //        {
        //            Console.WriteLine($"ReadylocationID: {location.ReadyLocationID}, TimeAvailableReady: {location.TimeAvailableReady}");
        //        }
        //        Console.ReadLine();
            
        //}
        //catch (InvalidOperationException ex)
        //{
        //    {
        //        Console.WriteLine("Error: " + ex.Message);
        //        Console.ReadLine();
        //    }

        //}


        try
        {
            // We check if user is not retarded and try to plan too many orders
            planner.CheckIfEnoughTimeAvailable(formerenStations, Categories);
            while (Categories.Sum(cat => cat.Count) > 0)
            {
                planner.PlanToFormerenStation(formerenStations, Categories, scenario, rng);
            }
            Console.WriteLine("All orders have been given!");

            while (!formerenStations.All(station => station.OrdersAdded.Count == 0))
            {
                PlannerLogic.TransferOrdersToReadyLocations(formerenStations, readyLocation, Categories);
            }
            Console.WriteLine("All orders have been moved!");

            foreach (var location in readyLocation)
            {
                foreach (var order in location.OrdersAdded)
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



            // Using RestSharp to make a POST request to the Miro API to create a shape on a board(booking)
            //foreach (var location in readyLocation)
            //{
            //    foreach (var order in location.OrdersAdded)
            //    {
            //        // We unpack the order tuple to get the category, x, y and color
            //        string content = order.OrderCategory;
            //        int x = order.x;
            //        int y = order.y;
            //        string fillColor = order.Color;
            //        int duration = Categories.First(cat => cat.Category == order.OrderCategory).Duration;
            //        int height = duration;
            //        await SendMiroShapeAsync(content, x, y, fillColor, height);

            //    }
            //}

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
        catch (InvalidOperationException ex)
        {
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.ReadLine();
            }

        }



    }
}






