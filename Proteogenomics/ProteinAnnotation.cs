﻿using Bio;
using Bio.Extensions;
using Bio.Algorithms.Translation;
using Proteomics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proteogenomics
{
    public static class ProteinAnnotation
    {

        #region Variant Annotation Labels

        public static string SynonymousVariantLabel = "variant:synonymous";
        public static string SingleAminoAcidVariantLabel = "variant:sav";
        public static string FrameShiftInsertionLabel = "variant:frameShiftInsertion";
        public static string FrameShiftDeletionLabel = "variant:frameShiftDeletion";
        public static string InFrameInsertionLabel = "variant:inFrameInsertion";
        public static string InFrameDeletionLabel = "variant:inFrameDeletion";

        #endregion Variant Annotation Labels

        #region Public Methods

        public static Protein OneFrameTranslationWithAnnotation(TranscriptPossiblyWithVariants transcript)
        {
            return OneFrameTranslationWithAnnotation(new List<TranscriptPossiblyWithVariants> { transcript }).FirstOrDefault();
        }

        // get the SAV notation X#X
        // or for indels use X#XXX or XXX#X where # is the start
        // get the SNV location 1:100000
        // get the codon change Gcc/Acc
        static HashSet<string> accessions = new HashSet<string>();
        public static List<Protein> OneFrameTranslationWithAnnotation(List<TranscriptPossiblyWithVariants> transcripts)
        {
            Dictionary<string, Protein> proteinDictionary = new Dictionary<string, Protein>();
            foreach (TranscriptPossiblyWithVariants t in transcripts)
            {
                ISequence referenceTranscriptSequence = new Sequence(
                    t.GetExonsUsedInDerivation().OrderByDescending(x => x.Sequence.Alphabet.Count).First().Sequence.Alphabet,
                    t.GetExonsUsedInDerivation().SelectMany(x => x.Sequence).ToArray());
                ISequence variantTranscriptSequence = t.Transcript.Strand == "+" ? t.Sequence : t.Sequence.GetReverseComplementedSequence();
                referenceTranscriptSequence = t.Transcript.Strand == "+" ? referenceTranscriptSequence : referenceTranscriptSequence.GetReverseComplementedSequence();
                int[] indices = t.GetExonsUsedInDerivation().SelectMany(x => Enumerable.Range((int)x.OneBasedStart, (int)(x.OneBasedEnd - x.OneBasedStart + 1))).ToArray();
                indices = t.Transcript.Strand == "+" ? indices : indices.Reverse().ToArray();
                Variant[] variants = new List<Variant>(t.Variants).ToArray();
                variants = t.Transcript.Strand == "+" ? variants : variants.Reverse().ToArray();

                string variantAminoAcidSequence = "";
                int variantIndex = 0;
                long refSeqIdx = t.ZeroBasedCodingStart - 3;
                long oneBasedTranscriptStart = t.Transcript.Exons.Min(x => x.OneBasedStart);
                for (long varSeqIdx = 0; varSeqIdx + 2 < variantTranscriptSequence.Count; varSeqIdx += 3)
                {
                    refSeqIdx += 3;
                    Codons.TryLookup(Transcription.GetRnaComplement(referenceTranscriptSequence[refSeqIdx]),
                            Transcription.GetRnaComplement(referenceTranscriptSequence[refSeqIdx + 1]),
                            Transcription.GetRnaComplement(referenceTranscriptSequence[refSeqIdx + 2]),
                            out byte originalAminoAcid);
                    Codons.TryLookup(Transcription.GetRnaComplement(variantTranscriptSequence[varSeqIdx]),
                        Transcription.GetRnaComplement(variantTranscriptSequence[varSeqIdx + 1]),
                        Transcription.GetRnaComplement(variantTranscriptSequence[varSeqIdx + 2]),
                        out byte newAminoAcid);
                    variantAminoAcidSequence += char.ToUpperInvariant((char)newAminoAcid);

                    long[] referenceCodonIndices = new long[] { indices[refSeqIdx], indices[refSeqIdx + 1], indices[refSeqIdx + 2] };
                    if (variantIndex < variants.Length && referenceCodonIndices.Contains(variants[variantIndex].OneBasedStart))
                    {
                        Variant v = variants[variantIndex++];
                        long aminoAcidPosition = refSeqIdx / 3 + 1;
                        if (v.ReferenceAllele.Length == v.AlternateAllele.Length) // single amino acid variation
                        {
                            v.Synonymous = originalAminoAcid == newAminoAcid;
                            v.Annotation = (v.Synonymous ? SynonymousVariantLabel : SingleAminoAcidVariantLabel) + " " +
                                char.ToUpperInvariant((char)originalAminoAcid) + aminoAcidPosition.ToString() + char.ToUpperInvariant((char)newAminoAcid) + " " +
                                v.Chr + ":" + v.OneBasedStart + " " +
                                String.Join("",
                                    char.ToUpperInvariant((char)referenceTranscriptSequence[refSeqIdx]),
                                    char.ToUpperInvariant((char)referenceTranscriptSequence[refSeqIdx + 1]),
                                    char.ToUpperInvariant((char)referenceTranscriptSequence[refSeqIdx + 2]))
                                + "/" +
                                String.Join("",
                                    char.ToUpperInvariant((char)variantTranscriptSequence[varSeqIdx]),
                                    char.ToUpperInvariant((char)variantTranscriptSequence[varSeqIdx + 1]),
                                    char.ToUpperInvariant((char)variantTranscriptSequence[varSeqIdx + 2]));
                        }

                        // TODO: adjust indices to keep stepping across the right portion of the reference transcript
                        // TODO: take into account when frameshifts make it miss a stop codon, or if a stop loss causes a runon -- could continue to take from the genome and look up variants...
                        else
                        {
                            v.Synonymous = originalAminoAcid == newAminoAcid && char.ToUpperInvariant((char)originalAminoAcid) == '*';
                            bool insertion = v.ReferenceAllele.Length < v.AlternateAllele.Length;
                            bool deletion = !insertion;
                            string longerAllele = insertion ? v.AlternateAllele : v.ReferenceAllele;
                            string shorterAllele = insertion ? v.ReferenceAllele : v.AlternateAllele;
                            bool frameshift = (longerAllele.Length - shorterAllele.Length) % 3 != 0;
                            if (frameshift)
                            {
                                v.Annotation = (insertion ? FrameShiftInsertionLabel : FrameShiftDeletionLabel) + " " +
                                    originalAminoAcid + aminoAcidPosition.ToString() +
                                    (insertion ? FrameShiftInsertionLabel : FrameShiftDeletionLabel) + " " +
                                    v.Chr + ":" + v.OneBasedStart;
                            }
                            else
                            {
                                string originalSequence = new string(new char[] { char.ToUpperInvariant((char)originalAminoAcid) });
                                string variantSequence = new string(new char[] { char.ToUpperInvariant((char)newAminoAcid) });
                                for (long ii = refSeqIdx + 3; ii + 2 < referenceTranscriptSequence.Count && (ii - refSeqIdx) / 3 < ((longerAllele.Length - shorterAllele.Length) / 3) + 1; ii += 3)
                                {
                                    long jj = ii - t.ZeroBasedCodingStart;
                                    if (insertion && Codons.TryLookup(
                                            Transcription.GetRnaComplement(variantTranscriptSequence[jj]),
                                            Transcription.GetRnaComplement(variantTranscriptSequence[jj + 1]),
                                            Transcription.GetRnaComplement(variantTranscriptSequence[jj + 2]),
                                            out byte newAminoAcid2))
                                        variantSequence += newAminoAcid2;
                                    if (deletion && Codons.TryLookup(Transcription.GetRnaComplement(referenceTranscriptSequence[ii]),
                                            Transcription.GetRnaComplement(referenceTranscriptSequence[ii + 1]),
                                            Transcription.GetRnaComplement(referenceTranscriptSequence[ii + 2]),
                                            out byte originalAminoAcid2))
                                        originalSequence += originalAminoAcid2;
                                }
                                v.Annotation = (insertion ? InFrameInsertionLabel : InFrameDeletionLabel) + " " +
                                    originalSequence + aminoAcidPosition.ToString() + variantSequence + " " +
                                    v.Chr + ":" + v.OneBasedStart;
                            }
                        }
                    }
                }
                string accession = t.ProteinID;
                int arbitraryNumber = 1;
                while (accessions.Contains(accession))
                {
                    accession = t.ProteinID + "_v" + arbitraryNumber++.ToString();
                }
                t.ProteinAnnotation = String.Join(" ", t.Variants.Select(v => v.Annotation)) + " OS=Homo sapiens GN=" + t.Transcript.Gene.ID;
                Protein newProtein = new Protein(variantAminoAcidSequence.Split('*')[0], accession, null, null, null, null, t.ProteinAnnotation);
                AddProteinIfLessComplex(proteinDictionary, newProtein);
                accessions.Add(newProtein.Accession);
            }
            return proteinDictionary.Values.ToList();
        }

        /// <summary>
        /// This should reduce synonymous variations by eliminating redundancy in favor of simpler explanations.
        /// </summary>
        /// <param name="proteinDictionary"></param>
        /// <param name="newProtein"></param>
        /// <returns></returns>
        public static bool AddProteinIfLessComplex(Dictionary<string, Protein> proteinDictionary, Protein newProtein)
        {
            if (proteinDictionary.TryGetValue(newProtein.BaseSequence, out Protein currentProtein))
            {
                bool currHasFrameShift = currentProtein.FullName.Contains("frameShift");
                bool currHasInFrame = currentProtein.FullName.Contains("inFrame");
                int currSynonymousCount = currentProtein.FullName.Split(new string[] { "variant:synonymous" }, StringSplitOptions.None).Length - 1;
                int currVarCount = currentProtein.FullName.Split(new string[] { "variant:" }, StringSplitOptions.None).Length - currSynonymousCount - 1;

                bool newHasFrameShift = newProtein.FullName.Contains("frameShift");
                bool newHasInFrame = newProtein.FullName.Contains("inFrame");
                int newSynonymousCount = newProtein.FullName.Split(new string[] { "variant:synonymous" }, StringSplitOptions.None).Length - 1;
                int newVarCount = newProtein.FullName.Split(new string[] { "variant:" }, StringSplitOptions.None).Length - newSynonymousCount - 1;

                bool addNewProteinInstead = currHasFrameShift && !newHasFrameShift || currHasInFrame && !newHasInFrame || newVarCount < currVarCount || newSynonymousCount < currSynonymousCount;
                proteinDictionary[newProtein.BaseSequence] = addNewProteinInstead ? newProtein : currentProtein;
                return addNewProteinInstead;
            }
            else
            {
                proteinDictionary.Add(newProtein.BaseSequence, newProtein);
                return true;
            }
        }

        /// <summary>
        /// Transfers likely modifications from a list of proteins to another based on sequence similarity. Returns a list of new objects.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static List<Protein> TransferModifications(List<Protein> source, List<Protein> destination)
        {
            List<Protein> newProteins = new List<Protein>();
            Dictionary<string, Protein> dictDestination = destination.ToDictionary(p => p.BaseSequence, p => p);
            Dictionary<string, Protein> dictSource = source.ToDictionary(p => p.BaseSequence, p => p);

            List<string> commonSeqs = dictDestination.Keys.Intersect(dictDestination.Keys).ToList();
            foreach (string seq in commonSeqs)
            {
                Protein source_protein = dictSource[seq];
                Protein destination_protein = dictDestination[seq];
                newProteins.Add(
                    new Protein(
                        seq,
                        destination_protein.Accession,
                        gene_names: source_protein.GeneNames.ToList(),
                        oneBasedModifications: source_protein.OneBasedPossibleLocalizedModifications,
                        proteolysisProducts: source_protein.ProteolysisProducts.ToList(),
                        name: destination_protein.Name,
                        full_name: destination_protein.FullName,
                        isDecoy: destination_protein.IsDecoy,
                        isContaminant: destination_protein.IsContaminant,
                        databaseReferences: source_protein.DatabaseReferences.ToList(),
                        sequenceVariations: source_protein.SequenceVariations.ToList()));
            }

            List<string> destinationOnly = dictDestination.Keys.Except(dictSource.Keys).ToList();
            List<string> sourceOnly = dictSource.Keys.Except(dictDestination.Keys).ToList();
            return newProteins;
        }

        #endregion Public Methods

    }
}