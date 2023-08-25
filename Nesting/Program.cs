using System;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using netDxf.Entities;
using netDxf.Header;
using System.Linq;
using System.IO;
using netDxf;




public class CNCRouterProperties
{
    public double ToolDiameter { get; set; }
    public double SheetWidth { get; set; }
    public double SheetHeight { get; set; }
    public double OffsetFromBoundary { get; set; } = 5;  // default value set to 1
}

public class Chromosome
{
    public List<DxfDocument> Genes { get; set; } = new List<DxfDocument>();
    public List<bool> Rotations { get; set; } = new List<bool>();
    public double Fitness { get; set; }
}
public class GeneticNesting
{
    private const int PopulationSize = 100;
    private const int Generations = 100;
    private const double MutationRate = 0.05;
    private const int ElitismCount = 1;
    private CNCRouterProperties RouterProperties;
    private Random rand = new Random();
    public GeneticNesting(CNCRouterProperties routerProperties)
    {
        RouterProperties = routerProperties;
    }
    private static (Vector3 Min, Vector3 Max) GetBounds(DxfDocument doc)
    {
        if (doc == null)
            return (new Vector3(), new Vector3());

        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (var line in doc.Entities.Lines)
        {
            UpdateMinMax(line.StartPoint);
            UpdateMinMax(line.EndPoint);
        }

        void UpdateMinMax(Vector3 point)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            minZ = Math.Min(minZ, point.Z);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
            maxZ = Math.Max(maxZ, point.Z);
        }

        return (new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
    }
    private double CalculateFitness(Chromosome chromosome)
    {
        double wastedSpace = 0;
        double totalArea = RouterProperties.SheetWidth * RouterProperties.SheetHeight;

        foreach (var geneIndex in Enumerable.Range(0, chromosome.Genes.Count))
        {
            var dxfDoc = chromosome.Genes[geneIndex];

            // Use the GetBounds method you provided to get the bounding box of the DXF document.
            var bounds = GetBounds(dxfDoc);

            double width, height;

            if (chromosome.Rotations[geneIndex])
            {
                // If rotated
                width = bounds.Max.Y - bounds.Min.Y;
                height = bounds.Max.X - bounds.Min.X;
            }
            else
            {
                // If not rotated
                width = bounds.Max.X - bounds.Min.X;
                height = bounds.Max.Y - bounds.Min.Y;
            }

            double partArea = width * height;
            wastedSpace += totalArea - partArea;
        }
        return 1 / wastedSpace;
    }
    private Chromosome Crossover(Chromosome parent1, Chromosome parent2)
    {
        Chromosome child = new Chromosome();

        int crossoverStart = rand.Next(parent1.Genes.Count);
        int crossoverEnd = crossoverStart + rand.Next(parent1.Genes.Count - crossoverStart);

        // Copying genes from parent1 to child
        for (int i = crossoverStart; i < crossoverEnd; i++)
        {
            child.Genes.Add(parent1.Genes[i]);
            child.Rotations.Add(parent1.Rotations[i]);
        }

        // Calculate occurrences
        Dictionary<string, int> childCounts = child.Genes.GroupBy(g => g.Name).ToDictionary(g => g.Key, g => g.Count());
        Dictionary<string, int> parent2Counts = parent2.Genes.GroupBy(g => g.Name).ToDictionary(g => g.Key, g => g.Count());

        // Taking genes from parent2 based on occurrences
        for (int i = 0; i < parent2.Genes.Count; i++)
        {
            DxfDocument currentGene = parent2.Genes[i];

            string geneKey = currentGene.Name;  // Using the filename as the unique identifier

            // If child doesn't have the gene or has fewer occurrences of the gene than parent2, add it
            if (!childCounts.ContainsKey(geneKey) || (childCounts[geneKey] < parent2Counts[geneKey]))
            {
                child.Genes.Add(currentGene);
                child.Rotations.Add(parent2.Rotations[i]);

                // Update the count in childCounts
                if (!childCounts.ContainsKey(geneKey))
                    childCounts[geneKey] = 1;
                else
                    childCounts[geneKey]++;
            }
        }

        return child;
    }
    private void Mutate(Chromosome chromosome)
    {
        for (int i = 0; i < chromosome.Rotations.Count; i++)
        {
            if (rand.NextDouble() < MutationRate)
            {
                chromosome.Rotations[i] = !chromosome.Rotations[i];
            }
        }
    }
    private Chromosome SelectParent(List<Chromosome> population)
    {
        double totalFitness = population.Sum(chromosome => chromosome.Fitness);
        double randomValue = rand.NextDouble() * totalFitness;
        double currentSum = 0;
        foreach (var chromosome in population)
        {
            currentSum += chromosome.Fitness;
            if (currentSum > randomValue)
            {
                return chromosome;
            }
        }
        return population.Last();
    }
    private void ParallelCalculateFitness(ConcurrentBag<Chromosome> population)
    {
        //Parallel.ForEach(population, chromosome =>
        //{
        //    chromosome.Fitness = CalculateFitness(chromosome);
        //});
        foreach (Chromosome chromosome in population)
        {
            chromosome.Fitness = CalculateFitness(chromosome);
        }
    }
    public Chromosome ExecuteNesting(List<DxfDocument> allParts)
    {
        Console.WriteLine("Starting nesting...");

        ConcurrentBag<Chromosome> population = new ConcurrentBag<Chromosome>();

        //Parallel.For(0, PopulationSize, i =>
        //{
        //    Chromosome chromosome = new Chromosome();
        //    foreach (DxfDocument partDoc in allParts)
        //    {
        //        // Assuming there's a way to determine if a DxfDocument represents a sheet metal part.
        //        // If there isn't, you might need additional logic or information to filter the parts.
        //        chromosome.Genes.Add(partDoc);
        //        chromosome.Rotations.Add(rand.Next(2) == 0);  // Randomly assign rotation status.
        //    }
        //    chromosome.Genes = chromosome.Genes.OrderBy(x => rand.Next()).ToList();
        //    chromosome.Fitness = CalculateFitness(chromosome);
        //    population.Add(chromosome);
        //});
        for (int i = 0; i < PopulationSize; i++)
        {
            Chromosome chromosome = new Chromosome();
            foreach (DxfDocument partDoc in allParts)
            {
                // Assuming there's a way to determine if a DxfDocument represents a sheet metal part.
                // If there isn't, you might need additional logic or information to filter the parts.
                chromosome.Genes.Add(partDoc);
                chromosome.Rotations.Add(rand.Next(2) == 0);  // Randomly assign rotation status.
            }
            chromosome.Genes = chromosome.Genes.OrderBy(x => rand.Next()).ToList();
            chromosome.Fitness = CalculateFitness(chromosome);
            population.Add(chromosome);
        }
        ParallelCalculateFitness(population);
        for (int generation = 0; generation < Generations; generation++)
        {
            Console.WriteLine($"Processing generation {generation + 1} of {Generations}...");
            ConcurrentBag<Chromosome> newPopulation = new ConcurrentBag<Chromosome>();

            // Add elites directly to the new population
            var sortedPopulation = population.OrderByDescending(chromo => chromo.Fitness).ToList();
            for (int i = 0; i < ElitismCount; i++)
            {
                newPopulation.Add(sortedPopulation[i]);
            }
            //Parallel.For(0, PopulationSize - ElitismCount, i =>  // Adjusted for elitism
            //{
            //    Chromosome parent1 = SelectParent(sortedPopulation);
            //    Chromosome parent2 = SelectParent(sortedPopulation);
            //    while (parent1 == parent2)
            //        parent2 = SelectParent(sortedPopulation);

            //    Chromosome child = Crossover(parent1, parent2);
            //    Mutate(child);

            //    child.Fitness = CalculateFitness(child);
            //    newPopulation.Add(child);
            //});
            for (int i = 0; i < PopulationSize - ElitismCount; i++)  // Adjusted for elitism
            {
                Chromosome parent1 = SelectParent(sortedPopulation);
                Chromosome parent2 = SelectParent(sortedPopulation);
                while (parent1 == parent2)
                    parent2 = SelectParent(sortedPopulation);

                Chromosome child = Crossover(parent1, parent2);
                Mutate(child);

                child.Fitness = CalculateFitness(child);
                newPopulation.Add(child);
            }

            population = newPopulation;
        }
        Console.WriteLine("Nesting complete!");
        return population.OrderByDescending(c => c.Fitness).FirstOrDefault();
    }
}
public class PlywoodNesting
{
    private List<DxfDocument> sheets = new List<DxfDocument>();
    public void NestParts()
    {
        // Select directories
        List<string> baseDirectories = new List<string>
    {
        @"C:\Users\Public\Documents\AutoCase\3D Case Design 2021\Projects\testcase1_2\Outputs",
        @"C:\Users\Public\Documents\AutoCase\3D Case Design 2021\Projects\testcase1\Outputs",       
    };

        // Store files by material
        Dictionary<string, List<string>> filesByMaterial = new Dictionary<string, List<string>>();

        // Get all DXF files from all directories
        foreach (var baseDirectory in baseDirectories)
        {
            string[] subdirectories = Directory.GetDirectories(baseDirectory);
            foreach (var subdir in subdirectories)
            {
                string materialName = System.IO.Path.GetFileName(subdir);  // Extracting the subdirectory name
                if (!filesByMaterial.ContainsKey(materialName))
                {
                    filesByMaterial[materialName] = new List<string>();
                }

                filesByMaterial[materialName].AddRange(Directory.GetFiles(subdir, "*.dxf"));
            }
        }

        // Process each material group
        foreach (var material in filesByMaterial.Keys)
        {
            List<DxfDocument> dxfDocuments = new List<DxfDocument>();
            foreach (string file in filesByMaterial[material])
            {
                DxfDocument document = DxfDocument.Load(file);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                string userInput = Interaction.InputBox($"Enter the multiplier for DXF '{fileName}':", "DXF Multiplier", "1");

                if (int.TryParse(userInput, out int multiplier))
                {
                    for (int i = 0; i < multiplier; i++)
                    {
                        dxfDocuments.Add(document);
                    }
                }
                else
                {
                    dxfDocuments.Add(document);
                }
            }

            // Reset the sheets for each material
            sheets = new List<DxfDocument>();
            ProcessPartsForNesting(dxfDocuments, material);
        }
    }

    //Get bounds of parts
    private static (Vector3 Min, Vector3 Max) GetBounds(DxfDocument doc)
    {
        if (doc == null)
            return (new Vector3(), new Vector3());

        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (var line in doc.Entities.Lines)
        {
            UpdateMinMax(line.StartPoint);
            UpdateMinMax(line.EndPoint);
        }

        void UpdateMinMax(Vector3 point)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            minZ = Math.Min(minZ, point.Z);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
            maxZ = Math.Max(maxZ, point.Z);
        }

        return (new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
    }
    //Draw the sheet
    private static Polyline2D CreateRectanglePolyline(double width, double height)
    {
        Polyline2D rect = new Polyline2D();
        rect.Vertexes.Add(new Polyline2DVertex(0, 0));
        rect.Vertexes.Add(new Polyline2DVertex(width, 0));
        rect.Vertexes.Add(new Polyline2DVertex(width, height));
        rect.Vertexes.Add(new Polyline2DVertex(0, height));
        rect.IsClosed = true; // Close the polyline to complete the rectangle

        // Optionally set a width if you want the rectangle to have a "thickness"
        //rect.SetConstantWidth(1); // Adjust as needed

        return rect;
    }
    private void ProcessPartsForNesting(List<DxfDocument> dxfParts, string subdirName)
    {
        CNCRouterProperties properties = new CNCRouterProperties
        {
            ToolDiameter = 1,
            SheetWidth = 2000,
            SheetHeight = 2000
        };

        // Initialize the main sheet for the current directory
        DxfDocument mainDxf = new DxfDocument(DxfVersion.AutoCad2013);
        Polyline2D sheetBoundary = CreateRectanglePolyline(properties.SheetWidth, properties.SheetHeight);
        mainDxf.Entities.Add(sheetBoundary);
        sheets.Add(mainDxf);

        GeneticNesting nesting = new GeneticNesting(properties);
        var bestSolution = nesting.ExecuteNesting(dxfParts);

        Dictionary<DxfDocument, List<(double x, double y, double width, double height)>> placedPartsBySheet = new Dictionary<DxfDocument, List<(double, double, double, double)>> { { mainDxf, new List<(double, double, double, double)>() } };

        var orderedParts = bestSolution.Genes.ToList();

        foreach (var part in orderedParts)
        {
            var bounds = GetBounds(part);
            double partWidth = bounds.Max.X - bounds.Min.X;
            double partHeight = bounds.Max.Y - bounds.Min.Y;

            bool partPlaced = false;

            foreach (var sheet in sheets)
            {
                var position = FindBestPositionForPart(partWidth, partHeight, properties.SheetWidth, properties.SheetHeight, placedPartsBySheet[sheet], properties.OffsetFromBoundary);
                if (position.HasValue)
                {
                    placedPartsBySheet[sheet].Add((position.Value.x, position.Value.y, partWidth, partHeight));
                    PlacePartOnSheet(sheet, part, position.Value.x, position.Value.y, false);
                    partPlaced = true;
                    break;
                }
            }

            if (!partPlaced)
            {
                DxfDocument newSheet = CreateNewSheet(properties.SheetWidth, properties.SheetHeight);
                sheets.Add(newSheet);
                placedPartsBySheet[newSheet] = new List<(double, double, double, double)>();
                var position = FindBestPositionForPart(partWidth, partHeight, properties.SheetWidth, properties.SheetHeight, placedPartsBySheet[newSheet], properties.OffsetFromBoundary);
                if (position.HasValue)
                {
                    placedPartsBySheet[newSheet].Add((position.Value.x, position.Value.y, partWidth, partHeight));
                    PlacePartOnSheet(newSheet, part, position.Value.x, position.Value.y, false);
                }
            }
        }

        int sheetNumber = 1;
        foreach (DxfDocument sheet in sheets)
        {
            sheet.Save($"{subdirName}_{sheetNumber}.dxf");
            sheetNumber++;
        }
    }

    private (double x, double y)? FindBestPositionForPart(double partWidth, double partHeight, double sheetWidth, double sheetHeight, List<(double x, double y, double width, double height)> placedParts, double offsetFromBoundary)
    {
        double currentY = offsetFromBoundary;
        while (currentY + partHeight <= sheetHeight)
        {
            double bestX = offsetFromBoundary;
            while (bestX + partWidth <= sheetWidth)
            {
                var conflictingPart = FindConflictingPart(bestX, currentY, partWidth, partHeight, placedParts);
                if (!conflictingPart.HasValue) return (bestX, currentY);
                bestX = conflictingPart.Value.x + conflictingPart.Value.width + offsetFromBoundary;
            }
            currentY += partHeight + offsetFromBoundary;
        }
        return null;  // Doesn't fit on the current sheet
    }

    private (double x, double y, double width, double height)? FindConflictingPart(double x, double y, double width, double height, List<(double x, double y, double width, double height)> placedParts)
    {
        foreach (var placedPart in placedParts)
        {
            if (x < placedPart.x + placedPart.width && x + width > placedPart.x && y < placedPart.y + placedPart.height && y + height > placedPart.y)
            {
                return placedPart;
            }
        }
        return null;  // No conflict found
    }

    private DxfDocument CreateNewSheet(double width, double height)
    {
        DxfDocument newSheet = new DxfDocument();
        var rect = CreateRectanglePolyline(width, height);
        newSheet.Entities.Add(rect);
        return newSheet;
    }
    private void PlacePartOnSheet(DxfDocument mainDxf, DxfDocument part, double midPosX, double midPosY, bool rotZ)
    {
        Vector3 midpoint = new Vector3(midPosX, midPosY, 0);

        // Clone all entities from the part to avoid any unwanted modifications to the original entities.
        List<EntityObject> clonedEntities = new List<EntityObject>();
        foreach (var entity in part.Entities.All)
        {
            var clonedEntity = (EntityObject)entity.Clone();

            if (rotZ)
            {
                Console.WriteLine("Rotated");
                // Rotate the entity 90 degrees around the Z-axis at the specified midpoint
                clonedEntity.TransformBy(Matrix3.RotationZ(Math.PI / 2), midpoint);
            }

            // Create the moveVector for the midpoint.
            Vector3 moveVector = new Vector3(midPosX, midPosY, 0);
            // Translate the entity to the midpoint.
            clonedEntity.TransformBy(Matrix3.Identity, moveVector);

            clonedEntities.Add(clonedEntity);
        }

        // Add the cloned entities to the main DXF
        foreach (var entity in clonedEntities)
        {
            mainDxf.Entities.Add(entity);
        }
    }
}
public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Console.WriteLine("Program started...");
        PlywoodNesting nesting = new PlywoodNesting();
        nesting.NestParts();
        Console.WriteLine("Program completed!");
    }
}