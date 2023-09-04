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
using System.Threading;
using System.Windows.Forms;

public class CNCRouterProperties
{
    public double ToolDiameter { get; set; }
    public double SheetWidth { get; set; }
    public double SheetHeight { get; set; }
    public double OffsetFromBoundary { get; set; }  // default value set to 1
}
public class Chromosome
{
    public List<DxfDocument> Genes { get; set; } = new List<DxfDocument>();
    public List<bool> Rotations { get; set; } = new List<bool>();
    public double Fitness { get; set; }
}
public class GeneticNesting
{
    private static ThreadLocal<Random> threadLocalRand = new ThreadLocal<Random>(() => new Random()); //To make each thread access its own random instance
    private const int PopulationSize = 100;
    private const int Generations = 100;
    private const double MutationRate = 0.05;
    private const int ElitismCount = 1;
    private CNCRouterProperties RouterProperties;
    //private Random rand = new Random();
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
    //private double CalculateFitness(Chromosome chromosome)
    //{
    //    double wastedSpace = 0;
    //    double totalArea = RouterProperties.SheetWidth * RouterProperties.SheetHeight;
    //    foreach (var geneIndex in Enumerable.Range(0, chromosome.Genes.Count))
    //    {
    //        var dxfDoc = chromosome.Genes[geneIndex];
    //        var bounds = GetBounds(dxfDoc);
    //        double width = chromosome.Rotations[geneIndex] ? bounds.Max.Y - bounds.Min.Y : bounds.Max.X - bounds.Min.X;
    //        double height = chromosome.Rotations[geneIndex] ? bounds.Max.X - bounds.Min.X : bounds.Max.Y - bounds.Min.Y;
    //        wastedSpace += totalArea - (width * height);
    //    }
    //    return 1 / wastedSpace;
    //}
    //private Chromosome Crossover(Chromosome parent1, Chromosome parent2)
    //{
    //    Chromosome child = new Chromosome();
    //    int crossoverStart = threadLocalRand.Value.Next(parent1.Genes.Count);
    //    int crossoverEnd = crossoverStart + threadLocalRand.Value.Next(parent1.Genes.Count - crossoverStart);

    //    HashSet<string> genesInChild = new HashSet<string>();
    //    for (int i = crossoverStart; i < crossoverEnd; i++)
    //    {
    //        child.Genes.Add(parent1.Genes[i]);
    //        child.Rotations.Add(parent1.Rotations[i]);
    //        genesInChild.Add(parent1.Genes[i].Name);
    //    }

    //    for (int i = 0; i < parent2.Genes.Count; i++)
    //    {
    //        if (!genesInChild.Contains(parent2.Genes[i].Name))
    //        {
    //            child.Genes.Add(parent2.Genes[i]);
    //            child.Rotations.Add(parent2.Rotations[i]);
    //            genesInChild.Add(parent2.Genes[i].Name);
    //        }
    //    }
    //    return child;
    //}
    private double CalculateFitness(Chromosome chromosome)
    {
        double wastedSpace = 0;
        double totalArea = RouterProperties.SheetWidth * RouterProperties.SheetHeight;
        foreach (var geneIndex in Enumerable.Range(0, chromosome.Genes.Count))
        {
            var dxfDoc = chromosome.Genes[geneIndex];
            var bounds = GetBounds(dxfDoc);
            double width, height;
            if (chromosome.Rotations[geneIndex])
            {
                // If rotated
                //Console.WriteLine("Rotated");
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
        int crossoverStart = threadLocalRand.Value.Next(parent1.Genes.Count);
        int crossoverEnd = crossoverStart + threadLocalRand.Value.Next(parent1.Genes.Count - crossoverStart);
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
            if (threadLocalRand.Value.NextDouble() < MutationRate)
            {
                chromosome.Rotations[i] = !chromosome.Rotations[i];
            }
        }
    }
    private Chromosome SelectParent(List<Chromosome> population)
    {
        double totalFitness = population.Sum(chromosome => chromosome.Fitness);
        double randomValue = threadLocalRand.Value.NextDouble() * totalFitness;
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
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        Parallel.ForEach(population, options, chromosome =>
        {
            chromosome.Fitness = CalculateFitness(chromosome);
        });
    }
    public Chromosome ExecuteNesting(List<DxfDocument> allParts)
    {
        Console.WriteLine("Starting nesting...");

        ConcurrentBag<Chromosome> population = new ConcurrentBag<Chromosome>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        Parallel.For(0, PopulationSize, options, i =>
        {
            Chromosome chromosome = new Chromosome();
            foreach (DxfDocument partDoc in allParts)
            {
                // Add the part without rotation
                chromosome.Genes.Add(partDoc);
                chromosome.Rotations.Add(false);  // No rotation
                double fitnessWithoutRotation = CalculateFitness(chromosome);

                // Add the part with rotation
                chromosome.Rotations.RemoveAt(chromosome.Rotations.Count - 1); // Remove the last false
                chromosome.Rotations.Add(true);  // 90-degree rotation
                double fitnessWithRotation = CalculateFitness(chromosome);

                // Choose the best orientation
                if (fitnessWithoutRotation > fitnessWithRotation)
                {
                    chromosome.Rotations.RemoveAt(chromosome.Rotations.Count - 1); // Remove the last true
                    chromosome.Rotations.Add(false); // Re-add the false
                }
            }
            chromosome.Genes = chromosome.Genes.OrderBy(x => threadLocalRand.Value.Next()).ToList();
            chromosome.Fitness = CalculateFitness(chromosome);
            population.Add(chromosome);
        });

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
            Parallel.For(0, PopulationSize - ElitismCount, options, i =>  // Adjusted for elitism
            {
                Chromosome parent1 = SelectParent(sortedPopulation);
                Chromosome parent2 = SelectParent(sortedPopulation);
                while (parent1 == parent2)
                    parent2 = SelectParent(sortedPopulation);

                Chromosome child = Crossover(parent1, parent2);
                Mutate(child);
                child.Fitness = CalculateFitness(child);
                newPopulation.Add(child);
            });

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

        string outputDir = (Path.Combine(System.Environment.CurrentDirectory, @"DXFOuts"));
        Console.WriteLine(System.Environment.CurrentDirectory);

        // Select directories
        List<string> baseDirectories = new List<string>
    {
            Path.Combine(System.Environment.CurrentDirectory, @"multiple_materials"),
            //Path.Combine(System.Environment.CurrentDirectory, @"one_material"),
            //Path.Combine(System.Environment.CurrentDirectory, @"vertical_case")
 
    };
        // Store files by material
        Dictionary<string, List<string>> filesByMaterial = new Dictionary<string, List<string>>();

        // Get all DXF files from all directories
        foreach (var baseDirectory in baseDirectories)
        {
            string[] subdirectories = Directory.GetDirectories(baseDirectory);
            foreach (var subdir in subdirectories)
            {
                string materialName = Path.GetFileName(subdir);  // Extracting subdirectory name
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
            var (sheetWidth, sheetHeight) = GetSheetDimensionsFromUser(material);
            List<DxfDocument> dxfDocuments = new List<DxfDocument>();
            foreach (string file in filesByMaterial[material])
            {
                DxfDocument document = DxfDocument.Load(file);
                string fileName = Path.GetFileNameWithoutExtension(file);
                string userInput = Interaction.InputBox($"Enter the multiplier for '{fileName}':", "Multiplier", "1");

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
            ProcessPartsForNesting(dxfDocuments, material, outputDir, sheetWidth, sheetHeight);
        }
    }
    //This will be replaced with database operations code
    private (double Width, double Height) GetSheetDimensionsFromUser(string materialName)
    {
        double sheetWidth, sheetHeight;
        // Prompt for width
        string widthInput = Interaction.InputBox($"Enter sheet width (in meters) for material {materialName} (e.g. 2.44):", "Sheet Width");
        while (!double.TryParse(widthInput, out sheetWidth))
        {
            MessageBox.Show("Invalid input! Please enter a valid sheet width.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            widthInput = Interaction.InputBox($"Enter sheet width for material {materialName} (e.g. 2.44):", "Sheet Width");
        }
        // Prompt for height
        string heightInput = Interaction.InputBox($"Enter sheet height (in meters) for material {materialName} (e.g. 1.22):", "Sheet Height");
        while (!double.TryParse(heightInput, out sheetHeight))
        {
            MessageBox.Show("Invalid input! Please enter a valid sheet height.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            heightInput = Interaction.InputBox($"Enter sheet height for material {materialName} (e.g. 1.22):", "Sheet Height");
        }

        //To correct the numbers
        double factor = widthInput.Contains('.') ? 100 : 1000;
        sheetWidth *= factor;

        factor = heightInput.Contains('.') ? 100 : 1000;
        sheetHeight *= factor;

        Console.WriteLine(sheetWidth);
        Console.WriteLine(sheetHeight);
        return (sheetWidth, sheetHeight);
    }

    //Get dimensions of the parts
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
        //rect.SetConstantWidth(1); //thickness of sheet border
        return rect;



    }
    private void ProcessPartsForNesting(List<DxfDocument> dxfParts, string subdirName, string outputDirectory, double sheetWidth, double sheetHeight)
    {
        //Specify offset from boundary and gaps between parts (TollDiameter) from here:
        CNCRouterProperties properties = new CNCRouterProperties
        {
            ToolDiameter = 1,
            SheetWidth = sheetWidth,
            SheetHeight = sheetHeight,
            OffsetFromBoundary = 5
        };



        Dictionary<DxfDocument, List<(double x, double y, double width, double height)>> placedPartsBySheet = new Dictionary<DxfDocument, List<(double, double, double, double)>>();



        // Add a dictionary to manage sheets by material
        Dictionary<string, List<DxfDocument>> sheetsByMaterial = new Dictionary<string, List<DxfDocument>>();

        GeneticNesting nesting = new GeneticNesting(properties);
        var bestSolution = nesting.ExecuteNesting(dxfParts);
        var orderedParts = bestSolution.Genes.ToList();

        List<DxfDocument> currentMaterialSheets;
        if (!sheetsByMaterial.ContainsKey(subdirName))
        {

            currentMaterialSheets = new List<DxfDocument>();
            sheetsByMaterial[subdirName] = currentMaterialSheets;
        }
        else
        {
            currentMaterialSheets = sheetsByMaterial[subdirName];
        }



        foreach (var part in orderedParts)
        {
            var bounds = GetBounds(part);
            double partWidth = bounds.Max.X - bounds.Min.X;
            double partHeight = bounds.Max.Y - bounds.Min.Y;
            bool partPlaced = false;

            // Attempt to fit on existing sheets.
            for (int i = 0; i < currentMaterialSheets.Count; i++)
            {
                var sheet = currentMaterialSheets[i];
                var position = FindBestPositionForPart(partWidth, partHeight, properties.SheetWidth, properties.SheetHeight, placedPartsBySheet[sheet], properties.ToolDiameter, properties.OffsetFromBoundary);
                if (position.HasValue)
                {
                    placedPartsBySheet[sheet].Add((position.Value.x, position.Value.y, partWidth, partHeight));
                    PlacePartOnSheet(sheet, part, position.Value.x, position.Value.y, false);
                    partPlaced = true;
                    break;
                }
            }
            // If part couldn't be placed on any existing sheet, create a new sheet.
            if (!partPlaced)
            {

                DxfDocument newSheet = CreateNewSheet(properties.SheetWidth, properties.SheetHeight);
                currentMaterialSheets.Add(newSheet);
                var newPlacementsList = new List<(double, double, double, double)>();
                var position = FindBestPositionForPart(partWidth, partHeight, properties.SheetWidth, properties.SheetHeight, newPlacementsList, properties.ToolDiameter, properties.OffsetFromBoundary);
                newPlacementsList.Add((position.Value.x, position.Value.y, partWidth, partHeight));
                placedPartsBySheet[newSheet] = newPlacementsList;
                PlacePartOnSheet(newSheet, part, position.Value.x, position.Value.y, false);
            }
        }



        int sheetNumber = 1;
        foreach (DxfDocument sheet in currentMaterialSheets)
        {
            sheet.Save(Path.Combine(outputDirectory, $"{subdirName}_{sheetNumber}.dxf"));
            sheetNumber++;
        }
    }
    private DxfDocument CreateNewSheet(double sheetWidth, double sheetHeight)
    {
        DxfDocument newSheet = new DxfDocument(DxfVersion.AutoCad2013);
        Polyline2D sheetBoundary = CreateRectanglePolyline(sheetWidth, sheetHeight);
        newSheet.Entities.Add(sheetBoundary);
        return newSheet;
    }
    private (double x, double y)? FindBestPositionForPart(double partWidth, double partHeight, double sheetWidth, double sheetHeight, List<(double x, double y, double width, double height)> placedParts, double offsetFromBoundary, double sheetBoundaryOffset)
    {
        if (partWidth + 2 * sheetBoundaryOffset > sheetWidth || partHeight + 2 * sheetBoundaryOffset > sheetHeight)
        {
            // If sheet is too small
            MessageBox.Show("A part is too large to fit on the sheet. Please choose a larger sheet.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        double currentY = sheetBoundaryOffset;  // Start with only boundaryOffset
        while (currentY + partHeight <= sheetHeight - sheetBoundaryOffset)
        {
            double bestX = sheetBoundaryOffset;  // Start with only boundaryOffset
            while (bestX + partWidth <= sheetWidth - sheetBoundaryOffset)
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
        return null; // No conflict found
    }



    private void PlacePartOnSheet(DxfDocument mainDxf, DxfDocument part, double midPosX, double midPosY, bool rotZ)
    {
        // Calculate the bounding box of the part.
        var bounds = GetBounds(part);
        Vector3 partCenter = new Vector3((bounds.Min.X + bounds.Max.X) / 2, (bounds.Min.Y + bounds.Max.Y) / 2, 0);



        // Clone all entities from the part.
        List<EntityObject> clonedEntities = new List<EntityObject>();
        foreach (var entity in part.Entities.All)
        {
            var clonedEntity = (EntityObject)entity.Clone();
            if (rotZ)
            {
                // Rotate the part 90 degrees around its center
                clonedEntity.TransformBy(Matrix3.RotationZ(Math.PI / 2), partCenter);
            }

            // Adjust the position based on the rotation and part's bounding box
            double offsetX = rotZ ? bounds.Max.X : bounds.Min.X;
            double offsetY = bounds.Min.Y;
            Vector3 moveVector = new Vector3(midPosX - offsetX, midPosY - offsetY, 0);
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
    static void Main(string[] args)
    {
        Console.WriteLine("Program started...");
        PlywoodNesting nesting = new PlywoodNesting();
        nesting.NestParts();
        Console.WriteLine("Program completed!");
    }
}