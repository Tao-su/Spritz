﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ToolWrapperLayer
{
    public class RSeQCWrapper
    {
        #region Public Properties

        public static string InnerDistanceRPlotSuffix { get; } = ".inner_distance_plot.r";

        public static string InnerDistanceFrequencyTableSuffix { get; } = ".inner_distance_freq.txt";

        public static string InnerDistanceDistanceTableSuffix { get; } = ".inner_distance.txt";

        #endregion Public Properties

        #region Public Methods

        public static int InferInnerDistance(string binDirectory, string bamPath, string geneModelPath, out string[] outputFiles)
        {
            if (Path.GetExtension(geneModelPath) != ".bed")
            {
                geneModelPath = BEDOPSWrapper.Gtf2Bed12(binDirectory, geneModelPath);
            }

            outputFiles = new string[]
            {
                Path.Combine(Path.GetDirectoryName(bamPath), Path.GetFileNameWithoutExtension(bamPath)) + InnerDistanceRPlotSuffix,
                Path.Combine(Path.GetDirectoryName(bamPath), Path.GetFileNameWithoutExtension(bamPath)) + InnerDistanceFrequencyTableSuffix,
                Path.Combine(Path.GetDirectoryName(bamPath), Path.GetFileNameWithoutExtension(bamPath)) + InnerDistanceDistanceTableSuffix
            };

            string script_path = Path.Combine(binDirectory, "scripts", "inferInnerDistance.bash");
            WrapperUtility.GenerateAndRunScript(script_path, new List<string>
            {
                "cd " + WrapperUtility.ConvertWindowsPath(binDirectory),
                "python RSeQC-2.6.4/scripts/inner_distance.py" +
                    " -i " + WrapperUtility.ConvertWindowsPath(bamPath) + // input
                    " -o " + WrapperUtility.ConvertWindowsPath(Path.Combine(Path.GetDirectoryName(bamPath), Path.GetFileNameWithoutExtension(bamPath))) + // out prefix
                    " -r " + WrapperUtility.ConvertWindowsPath(geneModelPath), // gene model in BED format
                WrapperUtility.EnsureClosedFileCommands(outputFiles[0]),
                WrapperUtility.EnsureClosedFileCommands(outputFiles[1]),
                WrapperUtility.EnsureClosedFileCommands(outputFiles[2]),
            }).WaitForExit();

            string[] distance_lines = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(bamPath), Path.GetFileNameWithoutExtension(bamPath)) + InnerDistanceDistanceTableSuffix);
            List<int> distances = new List<int>();
            foreach (string dline in distance_lines)
            {
                if (int.TryParse(dline.Split('\t')[1], out int distance) 
                    && distance < 250 && distance > -250) // default settings for infer_distance
                    distances.Add(distance);
            }
            int averageDistance = (int)Math.Round(distances.Average(), 0);
            return averageDistance;
        }

        public static bool CheckStrandSpecificity(string binDirectory, string bamPath, string geneModelPath, double minFractionStrandSpecific)
        {
            string outfile = Path.GetFileNameWithoutExtension(bamPath) + ".inferexpt";
            string outpath = Path.Combine(Path.GetDirectoryName(bamPath), outfile);

            // todo: rework this with Bio.IO.BAM
            // Can use the method IEnumerable<SAMAlignedSequence> Bio.IO.Bam.BamParser.Parse(Stream stream)
            // and then SAMAlignedSequence.Flag.HasFlag(SAMFlags.ASDFDSADF)

            InferExperiment(binDirectory, bamPath, geneModelPath, outpath);
            string[] lines = File.ReadAllLines(outpath);
            double fraction_aligned_in_same_direction = double.Parse(lines[lines.Length - 2].Split(':')[1].TrimStart());
            double fraction_aligned_in_other_direction = double.Parse(lines[lines.Length - 1].Split(':')[1].TrimStart());
            return fraction_aligned_in_same_direction / fraction_aligned_in_other_direction < 1 - minFractionStrandSpecific
                || fraction_aligned_in_same_direction / fraction_aligned_in_other_direction > minFractionStrandSpecific;
        }

        public static string WriteInstallScript(string binDirectory)
        {
            string scriptPath = Path.Combine(binDirectory, "scripts", "install_rseqc.bash");
            WrapperUtility.GenerateScript(scriptPath, new List<string>
            {
                "cd " + WrapperUtility.ConvertWindowsPath(binDirectory),
                "if [ ! -d RSeQC-2.6.4 ]; then",
                "  wget https://downloads.sourceforge.net/project/rseqc/RSeQC-2.6.4.tar.gz",
                "  tar -xvf RSeQC-2.6.4.tar.gz", // infer_experiment.py is in the scripts folder
                "  rm RSeQC-2.6.4.tar.gz",
                "fi"
            });
            return scriptPath;
        }

        #endregion Public Methods

        #region Private Methods

        private static void InferExperiment(string binDirectory, string bamFile, string geneModel, string outFile)
        {
            if (Path.GetExtension(geneModel) != ".bed")
            {
                geneModel = BEDOPSWrapper.Gtf2Bed12(binDirectory, geneModel);
            }
            string script_path = Path.Combine(binDirectory, "scripts", "infer_expt.bash");
            WrapperUtility.GenerateAndRunScript(script_path, new List<string>
            {
                "cd " + WrapperUtility.ConvertWindowsPath(binDirectory),
                "python RSeQC-2.6.4/scripts/infer_experiment.py" + 
                    " -r " + WrapperUtility.ConvertWindowsPath(geneModel) + 
                    " -i " + WrapperUtility.ConvertWindowsPath(bamFile) + 
                    " > " + WrapperUtility.ConvertWindowsPath(outFile),
                WrapperUtility.EnsureClosedFileCommands(outFile)
            }).WaitForExit();
        }

        #endregion Private Methods

    }
}
