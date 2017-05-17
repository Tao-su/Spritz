﻿using NUnit.Framework;
using Proteogenomics;
using Proteomics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UsefulProteomicsDatabases;

namespace Test
{
    [TestFixture]
    public class TestSequenceSimilarityMethods
    {
        [Test]
        public void test_modification_transfer_exact_sequence_match()
        {
            var nice = new List<Modification>
            {
                new ModificationWithLocation("fayk",null, null,ModificationSites.A,null,  null)
            };

            Dictionary<string, Modification> un;
            List<Protein> ok = ProteinDbLoader.LoadProteinXML(Path.Combine(TestContext.CurrentContext.TestDirectory, @"xml2.xml"), false, nice, false, null, out un);
            List<Protein> destination = new List<Protein> {
                new Protein("MKTCYYELLGVETHASDLELKKAYRKKALQYHPDKNPDNVEEATQKFAVIRAAYEVLSDPQERAWYDSHKEQILNDTPPSTDDYYDYEVDATVTGVTTDELLLFFNSALYTKIDNSAAGIYQIAGKIFAKLAKDEILSGKRLGKFSEYQDDVFEQDINSIGYLKACDNFINKTDKLLYPLFGYSPTDYEYLKHFYKTWSAFNTLKSFSWKDEYMYSKNYDRRTKREVNRRNEKARQQARNEYNKTVKRFVVFIKKLDKRMKEGAKIAEEQRKLKEQQRKNELNNRRKFGNDNNDEEKFHLQSWQTVKEENWDELEKVYDNFGEFENSKNDKEGEVLIYECFICNKTFKSEKQLKNHINTKLHKKNMEEIRKEMEEENITLGLDNLSDLEKFDSADESVKEKEDIDLQALQAELAEIERKLAESSSEDESEDDNLNIEMDIEVEDVSSDENVHVNTKNKKKRKKKKKAKVDTETEESESFDDTKDKRSNELDDLLASLGDKGLQTDDDEDWSTKAKKKKGKQPKKNSKSTKSTPSLSTLPSSMSPTSAIEVCTTCGESFDSRNKLFNHVKIAGHAAVKNVVKRKKVKTKRI",
                "" , new List<Tuple<string, string>>(), new Dictionary<int, List<Modification>>(), null, null, null, "", "", false, false, new List<DatabaseReference>(), new List<SequenceVariation>()) };

            Assert.AreEqual(ok[0].BaseSequence, destination[0].BaseSequence);
            List<Protein> new_proteins = SequenceSimilarity.TransferModifications(ok, destination);

            Assert.AreEqual(ok[0].ProteolysisProducts.Count(), new_proteins[0].ProteolysisProducts.Count());
            Assert.AreEqual(ok[0].OneBasedPossibleLocalizedModifications, new_proteins[0].OneBasedPossibleLocalizedModifications);
            Assert.True(new_proteins[0].OneBasedPossibleLocalizedModifications.Keys.Count == 2);
            Assert.AreEqual(ok[0].DatabaseReferences.Count(), new_proteins[0].DatabaseReferences.Count());
            Assert.AreEqual(ok[0].BaseSequence, new_proteins[0].BaseSequence);
            Assert.AreEqual(1, new_proteins.Count);
        }
    }
}