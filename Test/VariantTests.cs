﻿using Bio.VCF;
using NUnit.Framework;
using Proteogenomics;
using Proteomics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UsefulProteomicsDatabases;
using Bio;

namespace Test
{
    [TestFixture]
    public class VariantTests
    {
        //[Test]
        //public void variants_into_genemodel()
        //{
        //    VCFParser vcf = new VCFParser(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "wgEncodeRep1.Aligned.out.sorted.grouped.marked.split.mapqfixed.realigned.vcf"));
        //    List<VariantContext> variants = vcf.Select(x => x).ToList();
        //    Assert.AreEqual(15574, variants.Count);

        //    Genome genome = new Genome(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr1.fa"));
        //    GeneModel geneModel = new GeneModel(genome, Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr1.gtf"));
        //    geneModel.amend_transcripts(variants);
        //    List<Protein> proteins = geneModel.genes.SelectMany(g => g.transcripts.SelectMany(t => t.translate(false, true))).ToList();
        //    List<Protein> proteins_without_variants = geneModel.genes.SelectMany(g => g.transcripts.SelectMany(t => t.translate(false, false))).ToList();
        //    HashSet<string> variant_seqs = new HashSet<string>(proteins.Select(p => p.BaseSequence));
        //    HashSet<string> seqs = new HashSet<string>(proteins_without_variants.Select(p => p.BaseSequence));
        //    Assert.IsTrue(variant_seqs.Count != seqs.Count);
        //}

        [Test]
        public void OneTranscriptOneHomozygous()
        {
            VCFParser vcf = new VCFParser(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr_1_one_homozygous_missense.vcf"));
            List<VariantContext> variants = vcf.Select(x => x).ToList();
            Assert.AreEqual(1, variants.Count);

            Genome genome = new Genome(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr1_sample.fa"));
            GeneModel geneModel = new GeneModel(genome, Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData",  "chr1_one_transcript.gtf"));
            geneModel.AmendTranscripts(variants);
            List<Protein> proteins = geneModel.Translate(true, true).ToList();
            List<Protein> proteins_wo_variant = geneModel.Translate(true, false).ToList();
            Assert.AreEqual(1, geneModel.Genes.Count);
            Assert.AreEqual(1, proteins.Count);
            Assert.AreEqual(1, proteins_wo_variant.Count);
            Assert.AreEqual(2, new HashSet<string> { proteins[0].BaseSequence, proteins_wo_variant[0].BaseSequence }.Count);
            Assert.IsTrue(proteins[0].FullName != null);
            Assert.IsTrue(proteins[0].FullName.Contains(ProteinAnnotation.SingleAminoAcidVariantLabel));
            Assert.IsTrue(proteins[0].FullName.Contains("1:69640"));

            string proteinFasta = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr_1_one_homozygous_missense.fasta");
            ProteinDbWriter.WriteFastaDatabase(proteins, proteinFasta, " ");
            string[] proteinFastaLines = File.ReadLines(proteinFasta).ToArray();
            Assert.IsTrue(proteinFastaLines[0].Contains(ProteinAnnotation.SingleAminoAcidVariantLabel));
            Assert.IsTrue(proteinFastaLines[0].Contains("1:69640"));
        }

        [Test]
        public void OneTranscriptOneHeterozygous()
        {
            VCFParser vcf = new VCFParser(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr_1_one_heterozygous_missense.vcf"));
            List<VariantContext> variants = vcf.Select(x => x).ToList();
            Assert.AreEqual(1, variants.Count);

            Genome genome = new Genome(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr1_sample.fa"));
            GeneModel geneModel = new GeneModel(genome, Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr1_one_transcript.gtf"));
            geneModel.AmendTranscripts(variants);
            List<Protein> proteins = geneModel.Translate(true, true).ToList();
            List<Protein> proteins_wo_variant = geneModel.Translate(true, false).ToList();
            Assert.AreEqual(1, geneModel.Genes.Count);
            Assert.AreEqual(2, proteins.Count);
            Assert.AreEqual(1, proteins_wo_variant.Count);
            Assert.AreEqual(2, new HashSet<string> { proteins[0].BaseSequence, proteins[1].BaseSequence, proteins_wo_variant[0].BaseSequence }.Count);
            Assert.IsTrue(proteins.All(p => p.FullName != null));
            Assert.IsTrue(proteins.Any(p => p.FullName.Contains(ProteinAnnotation.SingleAminoAcidVariantLabel)));
            Assert.IsTrue(proteins.Any(p => p.FullName.Contains("1:69640")));

            string proteinFasta = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr_1_one_heterozygous_missense.fasta");
            ProteinDbWriter.WriteFastaDatabase(proteins, proteinFasta, " ");
            string[] proteinFastaLines = File.ReadLines(proteinFasta).ToArray();
            Assert.IsTrue(proteinFastaLines.Any(x => x.Contains(ProteinAnnotation.SingleAminoAcidVariantLabel)));
            Assert.IsTrue(proteinFastaLines.Any(x => x.Contains("1:69640")));
        }

        [Test]
        public void OneTranscriptOneHeterozygousSynonymous()
        {
            VCFParser vcf = new VCFParser(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr_1_one_heterozygous_synonymous.vcf"));
            List<VariantContext> variants = vcf.Select(x => x).ToList();
            Assert.AreEqual(1, variants.Count);

            Genome genome = new Genome(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr1_sample.fa"));
            GeneModel geneModel = new GeneModel(genome, Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr1_one_transcript.gtf"));
            geneModel.AmendTranscripts(variants);
            List<Protein> proteins = geneModel.Translate(true, true).ToList();
            List<Protein> proteins_wo_variant = geneModel.Translate(true, false).ToList();
            Assert.AreEqual(1, geneModel.Genes.Count);
            Assert.AreEqual(1, proteins.Count);
            Assert.AreEqual(1, proteins_wo_variant.Count);
            Assert.AreEqual(1, new HashSet<string> { proteins[0].BaseSequence, proteins_wo_variant[0].BaseSequence }.Count);
            Assert.IsFalse(proteins.Any(p => p.FullName.Contains(ProteinAnnotation.SynonymousVariantLabel)));
            Assert.IsFalse(proteins.Any(p => p.FullName.Contains("1:69666")));

            string proteinFasta = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr_1_one_heterozygous_synonymous.fasta");
            ProteinDbWriter.WriteFastaDatabase(proteins, proteinFasta, " ");
            string[] proteinFastaLines = File.ReadLines(proteinFasta).ToArray();
            Assert.IsFalse(proteinFastaLines[0].Contains(ProteinAnnotation.SynonymousVariantLabel));
            Assert.IsFalse(proteinFastaLines[0].Contains("1:69666"));
        }

        [Test]
        public void OneTranscriptOneHomozygousSynonymous()
        {
            VCFParser vcf = new VCFParser(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr_1_one_homozygous_synonymous.vcf"));
            List<VariantContext> variants = vcf.Select(x => x).ToList();
            Assert.AreEqual(1, variants.Count);

            Genome genome = new Genome(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr1_sample.fa"));
            GeneModel geneModel = new GeneModel(genome, Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr1_one_transcript.gtf"));
            geneModel.AmendTranscripts(variants);
            List<Protein> proteins = geneModel.Translate(true, true).ToList();
            List<Protein> proteins_wo_variant = geneModel.Translate(true, false).ToList();
            Assert.AreEqual(1, geneModel.Genes.Count);
            Assert.AreEqual(1, proteins.Count);
            Assert.AreEqual(1, proteins_wo_variant.Count);
            Assert.AreEqual(1, new HashSet<string> { proteins[0].BaseSequence, proteins_wo_variant[0].BaseSequence }.Count);
            Assert.IsTrue(proteins.All(p => p.FullName != null));
            Assert.IsTrue(proteins.Any(p => p.FullName.Contains(ProteinAnnotation.SynonymousVariantLabel)));
            Assert.IsTrue(proteins.Any(p => p.FullName.Contains("1:69666")));

            string proteinFasta = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "chr_1_one_homozygous_synonymous.fasta");
            ProteinDbWriter.WriteFastaDatabase(proteins, proteinFasta, " ");
            string[] proteinFastaLines = File.ReadLines(proteinFasta).ToArray();
            Assert.IsTrue(proteinFastaLines[0].Contains(ProteinAnnotation.SynonymousVariantLabel));
            Assert.IsTrue(proteinFastaLines[0].Contains("1:69666"));
        }

        [Test]
        public void GetExonSeqsWithNearbyVariants()
        {
            MetadataListItem<List<string>> metadata = new MetadataListItem<List<string>>("something", "somethingAgain");
            metadata.SubItems["strand"] = new List<string> { "+" };
            Exon x = new Exon(new Sequence(Alphabets.DNA, new string(Enumerable.Repeat('A', 250).ToArray())), 0, 249, "1", metadata);
            VCFParser vcf = new VCFParser(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "aFewNearby.vcf"));
            x.Variants = vcf.ToList();
            List<Exon> h = x.GetExonSequences(10, true, 0.9, false, 101);
            Assert.AreEqual(4, h.Count);
        }

        [Test]
        public void GetExonSeqsWithTonsNearby()
        {
            MetadataListItem<List<string>> metadata = new MetadataListItem<List<string>>("something", "somethingAgain");
            metadata.SubItems["strand"] = new List<string> { "+" };
            Exon x = new Exon(new Sequence(Alphabets.DNA, new string(Enumerable.Repeat('A', 300).ToArray())), 0, 299, "1", metadata);
            VCFParser vcf = new VCFParser(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "tonsNearby.vcf"));
            x.Variants = vcf.ToList();
            List<Exon> h = x.GetExonSequences(10, true, 0.9, false, 101);
            Assert.AreEqual(2, h.Count);
        }

        [Test]
        public void GetExonSeqsWithTonsFarApart()
        {
            MetadataListItem<List<string>> metadata = new MetadataListItem<List<string>>("something", "somethingAgain");
            metadata.SubItems["strand"] = new List<string> { "+" };
            Exon x = new Exon(new Sequence(Alphabets.DNA, new string(Enumerable.Repeat('A', 4001).ToArray())), 0, 4000, "1", metadata);
            VCFParser vcf = new VCFParser(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "tonsFarApart.vcf"));
            x.Variants = vcf.ToList();
            List<Exon> h = x.GetExonSequences(10, true, 0.9, false, 101); // would produce 1024 combos without capping max combos
            Assert.AreEqual(8, h.Count);
        }

        // test todo: transcript with zero CodingSequenceExons and try to translate them to check that it doesn fail
        // test todo: multiple transcripts
    }
}