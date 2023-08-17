﻿using Inventor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic;



public class CNCRouterProperties
{
    public double ToolDiameter { get; set; }
    public double SheetWidth { get; set; }
    public double SheetHeight { get; set; }
    public double OffsetFromBoundary { get; set; } = 1;  // default value set to 1
}



public class Chromosome
{
    public List<PartDocument> Genes { get; set; } = new List<PartDocument>();
    public List<bool> Rotations { get; set; } = new List<bool>();
    public double Fitness { get; set; }
}



public class GeneticNesting
{
    private const int PopulationSize = 10;
    private const int Generations = 20;
    private const double MutationRate = 0.05;
    private const int ElitismCount = 1;
    private Inventor.Application InventorApplication;
    private CNCRouterProperties RouterProperties;
    private Random rand = new Random();


    public GeneticNesting(Inventor.Application inventorApp, CNCRouterProperties routerProperties)
    {
        InventorApplication = inventorApp;
        RouterProperties = routerProperties;
    }



    private double CalculateFitness(Chromosome chromosome)
    {
        double wastedSpace = 0;
        double totalArea = RouterProperties.SheetWidth * RouterProperties.SheetHeight;

        for (int i = 0; i < chromosome.Genes.Count; i++)
        {
            PartDocument part = chromosome.Genes[i];
            double width, height;

            if (chromosome.Rotations[i])
            {
                // If rotated
                width = part.ComponentDefinition.RangeBox.MaxPoint.Y - part.ComponentDefinition.RangeBox.MinPoint.Y;
                height = part.ComponentDefinition.RangeBox.MaxPoint.X - part.ComponentDefinition.RangeBox.MinPoint.X;
            }
            else
            {
                // If not rotated
                width = part.ComponentDefinition.RangeBox.MaxPoint.X - part.ComponentDefinition.RangeBox.MinPoint.X;
                height = part.ComponentDefinition.RangeBox.MaxPoint.Y - part.ComponentDefinition.RangeBox.MinPoint.Y;
            }

            double partArea = width * height;
            wastedSpace += totalArea - partArea;
        }

        return 1 / wastedSpace;
    }




    private Chromosome Crossover(Chromosome parent1, Chromosome parent2)
    {
        Chromosome child = new Chromosome
        {
            Genes = parent1.Genes.Take(parent1.Genes.Count / 2).Concat(parent2.Genes.Skip(parent2.Genes.Count / 2)).ToList(),
            Rotations = parent1.Rotations.Take(parent1.Rotations.Count / 2).Concat(parent2.Rotations.Skip(parent2.Rotations.Count / 2)).ToList()
        };
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
        Parallel.ForEach(population, chromosome =>
        {
            chromosome.Fitness = CalculateFitness(chromosome);
        });
    }



    public Chromosome ExecuteNesting(List<PartDocument> allParts)
    {
        Console.WriteLine("Starting nesting...");

        ConcurrentBag<Chromosome> population = new ConcurrentBag<Chromosome>();
        Parallel.For(0, PopulationSize, i =>
        {
            Chromosome chromosome = new Chromosome();
            foreach (PartDocument partDoc in allParts)
            {
                if (partDoc.SubType == "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}")  // This is the subtype ID for sheet metal parts.
                {
                    chromosome.Genes.Add(partDoc);
                    chromosome.Rotations.Add(rand.Next(2) == 0);  // Randomly assign rotation status.
                }
            }
            chromosome.Genes = chromosome.Genes.OrderBy(x => rand.Next()).ToList();
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
            Parallel.For(0, PopulationSize - ElitismCount, i =>  // Adjusted for elitism
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
    private List<PartDocument> sheets = new List<PartDocument>();
    public void NestParts()
    {
        Inventor.Application inventorApp;
        try
        {
            inventorApp = (Inventor.Application)Marshal.GetActiveObject("Inventor.Application");
        }
        catch
        {
            Type inventorAppType = Type.GetTypeFromProgID("Inventor.Application");
            inventorApp = (Inventor.Application)Activator.CreateInstance(inventorAppType);
            inventorApp.Visible = true;
        }



        List<string> allSelectedFiles = new List<string>();



        using (OpenFileDialog openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = "IAM files (*.iam)|*.iam|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = true;
            DialogResult result;
            do
            {
                result = openFileDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    allSelectedFiles.AddRange(openFileDialog.SafeFileNames);
                    MessageBox.Show("Add more files or cancel to proceed with selected files.");
                }
            } while (result != DialogResult.Cancel);
        }



        List<PartDocument> allParts = new List<PartDocument>();
        foreach (string fileName in allSelectedFiles)
        {
            allParts.AddRange(GetPartsFromAssembly(inventorApp, fileName));
        }

        List<PartDocument> processedParts = new List<PartDocument>();
        foreach (var part in allParts)
        {
            string partName = part.DisplayName;
            string userInput = Interaction.InputBox($"Enter the multiplier for part '{partName}':", "Part Multiplier", "1");
            if (int.TryParse(userInput, out int multiplier))
            {
                for (int i = 0; i < multiplier; i++)
                {
                    processedParts.Add(part);
                }
            }
            else
            {
                processedParts.Add(part);
            }
        }
        // Group parts by both material and thickness
        var partsByMaterialAndThickness = processedParts.GroupBy(p => new
        {
            Material = GetMaterialName(p),
            Thickness = p.ComponentDefinition.Parameters["Thickness"].Value
        }).ToDictionary(group => group.Key, group => group.ToList());
        
        foreach (var materialAndThickness in partsByMaterialAndThickness.Keys)
        {
            var partsForMaterialAndThickness = partsByMaterialAndThickness[materialAndThickness];
            ProcessPartsForNesting(inventorApp, partsForMaterialAndThickness);
            
        }

    }
    private string GetMaterialName(PartDocument part)
    {
        // Code to retrieve material name from the part.
        // This is a placeholder, adjust according to your API.
        return part.ActiveMaterial.Name;
    }
    private List<PartDocument> GetPartsFromAssembly(Inventor.Application inventorApp, string fileName)
    {
        string file = fileName;
        file = file.Replace(".iam", "");
        string filePath = @"C:\Users\Public\Documents\AutoCase\3D Case Design 2021\Projects\" + file + @"\Model Files\" + file + ".iam";


        inventorApp.Documents.CloseAll();
        string ipjPath = @"C:\Users\Public\Documents\AutoCase\3D Case Design 2021\Projects\" + file + @"\Model Files\Model2.ipj";
        try
        {
            inventorApp.Documents.CloseAll();
            inventorApp.DesignProjectManager.DesignProjects.AddExisting(ipjPath);
            DesignProject designProject = inventorApp.DesignProjectManager.DesignProjects.ItemByName[ipjPath];
            designProject.Activate();
        }
        catch { }

        AssemblyDocument asmDoc = inventorApp.Documents.Open(filePath, true) as AssemblyDocument;

        if (asmDoc == null)
        {
            throw new Exception($"Failed to open assembly document: {filePath}");
        }
        AssemblyComponentDefinition assemblyDef = asmDoc.ComponentDefinition;
        ComponentOccurrences occurrences = assemblyDef.Occurrences;
        
        List<PartDocument> parts = new List<PartDocument>();



        foreach (ComponentOccurrence occurrence in occurrences)
        {
            if (occurrence.DefinitionDocumentType == DocumentTypeEnum.kPartDocumentObject)
            {
                PartDocument partDoc = occurrence.Definition.Document as PartDocument;
                if (partDoc.SubType == "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}")  // Sheet metal parts.
                {
                    parts.Add(partDoc);
                }
            }
        }



        return parts;
    }
    private void ProcessPartsForNesting(Inventor.Application inventorApp, List<PartDocument> parts)
    {
        CNCRouterProperties properties = new CNCRouterProperties
        {
            ToolDiameter = 1,
            SheetWidth = 240,
            SheetHeight = 120
        };

        GeneticNesting nesting = new GeneticNesting(inventorApp, properties);
        var bestSolution = nesting.ExecuteNesting(parts);

        List<PartDocument> sheets = new List<PartDocument>();
        List<List<(double x, double y, double width, double height)>> placementsForEachSheet = new List<List<(double, double, double, double)>>();

        foreach (var part in bestSolution.Genes)
        {
            Box partBox = part.ComponentDefinition.RangeBox;
            double partWidth = partBox.MaxPoint.X - partBox.MinPoint.X;
            double partHeight = partBox.MaxPoint.Y - partBox.MinPoint.Y;
            string partMaterial = part.ComponentDefinition.Material.Name;
            double partThickness = part.ComponentDefinition.Parameters["Thickness"].Value;
            bool partPlaced = false;

            for (int i = 0; i < sheets.Count; i++)
            {
                var sheet = sheets[i];
                //double sheetThickness = sheet.ComponentDefinition.Parameters["Thickness"].Value;
                //double panelThickness = part.ComponentDefinition.Parameters["Thickness"].Value;
                if (sheet.ComponentDefinition.Material.Name == partMaterial) // Check if the sheet material matches the part's material  // && sheet.ComponentDefinition.Parameters["Thickness"].Value == partThickness  
                {
                    //double thicknessValue = sheet.ComponentDefinition.Parameters["PanelThickness"].Value;
                    //MessageBox.Show(thicknessValue.ToString());
                    var position = FindBestPositionForPart(partWidth, partHeight, properties.SheetWidth, properties.SheetHeight, placementsForEachSheet[i], properties.OffsetFromBoundary);
                    if (position.HasValue)
                    {
                        placementsForEachSheet[i].Add((position.Value.x, position.Value.y, partWidth, partHeight));
                        PlacePartOnSheet(inventorApp, sheet, part, position.Value.x + partWidth / 2, position.Value.y + partHeight / 2, false);
                        partPlaced = true;
                        break;
                    }
                }
            }

            if (!partPlaced)
            {
                // Create a new sheet with the part's material
                PartDocument newSheet = CreateNewSheet(inventorApp, properties.SheetWidth, properties.SheetHeight, partMaterial, partThickness);
                //PartDocument newSheet = CreateNewSheet(inventorApp, properties.SheetWidth, properties.SheetHeight, partMaterial);
                sheets.Add(newSheet);
                var newPlacementsList = new List<(double, double, double, double)>();
                var position = FindBestPositionForPart(partWidth, partHeight, properties.SheetWidth, properties.SheetHeight, newPlacementsList, properties.OffsetFromBoundary);
                newPlacementsList.Add((position.Value.x, position.Value.y, partWidth, partHeight));
                placementsForEachSheet.Add(newPlacementsList);
                PlacePartOnSheet(inventorApp, newSheet, part, position.Value.x + partWidth / 2, position.Value.y + partHeight / 2, false);

            }
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

    private PartDocument CreateNewSheet(Inventor.Application inventorApp, double width, double height, string material, double thickness)
    {
        PartDocument sheet = inventorApp.Documents.Add(DocumentTypeEnum.kPartDocumentObject) as PartDocument;
        sheet.SubType = "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}";  // Set the subtype ID for sheet metal parts.

        //MessageBox.Show(thickness.ToString());

        // Continue with sketches and features
        PlanarSketch sketch = sheet.ComponentDefinition.Sketches.Add(sheet.ComponentDefinition.WorkPlanes[3]);
        SheetMetalFeatures oSheetMetalFeatures = (SheetMetalFeatures)sheet.ComponentDefinition.Features;
        sketch.SketchLines.AddAsTwoPointRectangle(
            inventorApp.TransientGeometry.CreatePoint2d(0, 0),
            inventorApp.TransientGeometry.CreatePoint2d(width, height)
        );

        Profile profile = sketch.Profiles.AddForSolid();

        // Use Face feature in +Z direction
        FaceFeatureDefinition faceFeatureDef = oSheetMetalFeatures.FaceFeatures.CreateFaceFeatureDefinition(profile);
        faceFeatureDef.Direction = PartFeatureExtentDirectionEnum.kPositiveExtentDirection;

        oSheetMetalFeatures.FaceFeatures.Add(faceFeatureDef);

        // Adapted from VBA
        SheetMetalComponentDefinition oSheetMetalCompDef = sheet.ComponentDefinition as SheetMetalComponentDefinition;
        oSheetMetalCompDef.UseSheetMetalStyleThickness = false;

        Parameter oThicknessParam = oSheetMetalCompDef.Thickness;
        oThicknessParam.Value = thickness; // We directly set the desired thickness here

        // Set Material
        if (sheet.Materials.Cast<Material>().Any(m => m.Name == material))
        {
            Material materialToAssign = sheet.Materials[material];
            if (materialToAssign != null)
            {
                sheet.ComponentDefinition.Material = materialToAssign;
            }
            else
            {
                // Handle the case where the material is not found. 
                Console.WriteLine($"Material {material} not found.");
            }
        }
        else
        {
            // Handle the situation where the material doesn't exist
            Console.WriteLine($"Material {material} not found.");
        }

        inventorApp.ActiveView.Fit(true);
        return sheet;
    }










    private void PlacePartOnSheet(Inventor.Application inventorApp, PartDocument sheet, PartDocument part, double midPosX, double midPosY, bool rotZ)
    {
        sheet.Activate();
        // Derive the part into the sheet.
        DerivedPartUniformScaleDef oDerivedPartDef = sheet.ComponentDefinition.ReferenceComponents.DerivedPartComponents.CreateUniformScaleDef(part.FullFileName);
        oDerivedPartDef.ScaleFactor = 1;
        DerivedPartComponent oDerivedPart = sheet.ComponentDefinition.ReferenceComponents.DerivedPartComponents.Add(oDerivedPartDef as DerivedPartDefinition);



        Box partBox = part.ComponentDefinition.RangeBox;
        double partWidth = partBox.MaxPoint.X - partBox.MinPoint.X;
        double partHeight = partBox.MaxPoint.Y - partBox.MinPoint.Y;
        double expectedMinX = midPosX - partWidth / 2;



        // Target the last surface body, which is assumed to be the latest added one.
        SurfaceBody latestBody = sheet.ComponentDefinition.SurfaceBodies[sheet.ComponentDefinition.SurfaceBodies.Count];



        // Create an ObjectCollection and add the latest body to it.
        ObjectCollection objCollection = inventorApp.TransientObjects.CreateObjectCollection();
        objCollection.Add(latestBody);



        // Create a MoveFeatureDefinition.
        MoveDefinition oMoveDef = sheet.ComponentDefinition.Features.MoveFeatures.CreateMoveDefinition(objCollection);



        if (rotZ)
        {
            // Rotate about the Z-axis by 90 degrees (around the origin point).
            WorkAxis zAxis = sheet.ComponentDefinition.WorkAxes[3];
            RotateAboutLineMoveOperation oRotateAboutAxis = oMoveDef.AddRotateAboutAxis(zAxis, true, Math.PI / 2);



            // Update the model and pause for 1 second.
            inventorApp.ActiveView.Update();
            Thread.Sleep(1000);
        }



        // Set the free drag to translate the body to the mid point.
        FreeDragMoveOperation oFreeDrag = oMoveDef.AddFreeDrag(midPosX, midPosY, 0); // Z offset = 0



        // Update the model and pause for 1 second.
        inventorApp.ActiveView.Update();
        Thread.Sleep(1000);



        // Create the move feature using the defined transformation.
        MoveFeature oMoveFeature = sheet.ComponentDefinition.Features.MoveFeatures.Add(oMoveDef);
        SubtractDerivedFromSheet(inventorApp, sheet);
    }
    private void SubtractDerivedFromSheet(Inventor.Application inventorApp, PartDocument sheet)
    {
        sheet.SubType = "{4D29B490-49B2-11D0-93C3-7E0706000000}";



        SurfaceBodies oBodies = sheet.ComponentDefinition.SurfaceBodies;



        // Assuming the first body is always the sheet and subsequent bodies are the derived parts.
        SurfaceBody mainBody = oBodies[1];



        for (int i = 2; i <= oBodies.Count; i++) // Starting from 2 because 1 is the main sheet body.
        {
            SurfaceBody derivedBody = oBodies[i];



            ObjectCollection objCollection = inventorApp.TransientObjects.CreateObjectCollection();
            objCollection.Add(derivedBody);



            sheet.ComponentDefinition.Features.CombineFeatures.Add(mainBody, objCollection, PartFeatureOperationEnum.kCutOperation);



        }
        sheet.SubType = "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}";
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