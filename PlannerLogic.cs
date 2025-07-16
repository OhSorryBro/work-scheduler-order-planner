using System;

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


    public class LevelOfMatching
    {
        public Dictionary<int, double[]> ScenarioWeights { get; set; }
        public int ScenarioUsed { get; set; }
        public int TotalTries { get; set; }
        public int LevelOfSevernity { get; set; }
        public int OrdersMatched { get; set; }
        public int OrdersUnmatched { get; set; }

        public LevelOfMatching(int scenario)
        {
            ScenarioWeights = new Dictionary<int, double[]>
        {
            { 1, new double[] { 0.8, 0.15, 0.05 } },   // Scenario 1: 80%/15%/5%
            { 2, new double[] { 0.55, 0.30, 0.15 } },  // Scenario 2: 55%/30%/15%
            { 3, new double[] { 0.3, 0.35, 0.35 } }    // Scenario 3: 33%/33%/34%
        };
            ScenarioUsed = scenario;
            TotalTries = 0;
            LevelOfSevernity = 4;
            OrdersMatched = 0;
            OrdersUnmatched = 0;
        }

        public void IncrementTries()
        {
            TotalTries++;
        }

        public void UpdateSevernity(int newLevel)
        {
            LevelOfSevernity = newLevel;
        }

        // Przykładowa metoda oceny
        public void EvaluateMatching(/* params możesz dodać listę orderów, statusy itd. */)
        {
            // Tutaj zrób logikę oceny dopasowania, np. na podstawie OrdersMatched, OrdersUnmatched
            // i ustaw LevelOfSevernity odpowiednio (1/2/3)
        }
    }
    public static int GetLoadingTimeBySeverity(int levelOfSevernity)
    {
        switch (levelOfSevernity)
        {
            case 1:
                return 90;
            case 2:
                return 90;
            case 3:
                return 80;
            case 4:
                return 70;
            default:
                return 90;
        }
    }



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
    public class OrderInfo
    {
        public string Category { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Color { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }
    public List<OrderInfo> OrdersMovedFromTheStationList { get; } = new List<OrderInfo>();

    public void PlanToFormerenStation(List<FormerenStation> formerenStations,
                                        List<CategoryCount> categories,
                                        int scenario,
                                        Random rng,
                                        Dictionary<int, double[]> scenarioWeights)
    {
        var bestStation = FindStationWithLowestMaxTimeBusy(formerenStations);
        var matching = new LevelOfMatching(scenario);
        var firstAvailableCategory = PickOrderByScenario(categories, scenario, rng, scenarioWeights);


        if (firstAvailableCategory != null)
        {
            int duration = firstAvailableCategory.Duration;
            bool canPlace = formerenStations.Any(station =>
                                            IsSlotAvailable(station.TimeBusy,
                                            (station.TimeBusy.Count > 0 ? station.TimeBusy.Max() + 1 : 1),
                                            (station.TimeBusy.Count > 0 ? station.TimeBusy.Max() + 1 : 1) + duration - 1)
                                            );
            if (!canPlace)
            {
                throw new InvalidOperationException(
                $"No available worker slot for category {firstAvailableCategory.Category}, duration {duration} – try again!"
                );
            }
            // We take order for further processing
            Console.WriteLine($"Working with category: {firstAvailableCategory.Category}, We still have: {firstAvailableCategory.Count - 1}");
            int previousMax = bestStation.TimeBusy.Count > 0 ? bestStation.TimeBusy.Max() : 0;
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
            var orderStart = previousMax + 1;
            var orderEnd = orderStart + duration - 1;
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
            OrdersMovedFromTheStationList.Add(new OrderInfo
            {
                Category = firstAvailableCategory.Category,
                X = bestStation.x,
                Y = newY,
                Color = firstAvailableCategory.Color,
                Start = orderStart,
                End = orderEnd
            });
        }
        else
        {
            Console.WriteLine("No more orders to process in this scenario.");
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

    private static bool IsLoadingSlotAvailable(List<ReadyLocation> readyLocations, int loadingStart, int loadingEnd, int maxSimultaneousLoading)
    {
        int count = 0;
        foreach (var loc in readyLocations)
        {
            foreach (var order in loc.OrdersAdded)
            {
                if (order.OrderCategory == "Loading time")
                {
                    // Sprawdź czy się nakładają
                    if (!(order.orderEnd < loadingStart || order.orderStart > loadingEnd))
                    {
                        count++;
                        if (count >= maxSimultaneousLoading)
                            return false;
                    }
                }
            }
        }
        return true;
    }
    public static void TransferOrdersToReadyLocations(List<FormerenStation> formerenStations, List<ReadyLocation> readyLocations, List<CategoryCount> categories, int levelOfSevernity, int maxSimultaneousLoading)
    {
        var station = formerenStations.FirstOrDefault(s => s.OrdersAdded.Count > 0);
        if (station == null) return;

        var order = station.OrdersAdded.Pop();
        int duration = categories.First(cat => cat.Category == order.OrderCategory).Duration;
        int orderStart = order.orderStart;
        int orderEnd = order.orderEnd;
        int loadingDuration = GetLoadingTimeBySeverity(levelOfSevernity);
        int loadingStart = orderEnd + 1;
        int loadingEnd = loadingStart + loadingDuration - 1;

        ReadyLocation ready = null;

        foreach (var loc in readyLocations.OrderBy(_ => Guid.NewGuid()))
        {
            // Tu sprawdzamy cały zakres: orderStart ... loadingEnd
            if (IsSlotAvailable(loc.TimeBusy, orderStart, loadingEnd))
            {
                ready = loc;
                break;
            }
        }
        if (ready == null)
        {
            throw new InvalidOperationException("No empty place for this slot!");
        }


        // Calculate x
        int x = (20 + ready.ReadyLocationID) * 100;

        // Oznacz slot jako zajęty!
        MarkSlotBusy(ready.TimeBusy, orderStart, orderEnd);

        // We move order (with new x)
        ready.OrdersAdded.Push((
            order.OrderCategory,
            x,
            order.y,
            order.Color,
            orderStart,
            orderEnd
        ));
        int loadingY = order.y + duration / 2 + loadingDuration / 2 + 1; // That was a tricky one

        if (IsLoadingSlotAvailable(readyLocations, loadingStart, loadingEnd, maxSimultaneousLoading))
        {
            ready.OrdersAdded.Push((
                "Loading time",
                x,
                loadingY,
                "#000000",
                loadingStart,
                loadingEnd
            ));
            MarkSlotBusy(ready.TimeBusy, loadingStart, loadingEnd);
        }
        else
        {
            throw new InvalidOperationException(
                $"Loading slots exceed maxParallel={maxSimultaneousLoading} for time {loadingStart}-{loadingEnd}");
        }
    }

    public void CheckIfEnoughTimeAvailable(List<FormerenStation> formerenStations, List<CategoryCount> categories)
    {
        // This method checks if there is enough time available in the Formeren stations to process all orders.
        // If not, it will tell user that he made a mistake and there is not enough time to process all orders.
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
