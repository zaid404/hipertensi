using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;

namespace HypertensionDiagnosisApp
{
    class HypertensionDiagnosis
    {
        private string[,] data;
        private string[][] options;

        public HypertensionDiagnosis()
        {
            // Mount the Google Drive to access the data file (skipped)
            // Load data from prior.csv
            LoadData();
            
            // Define the relevant attributes
            options = new string[][]
            {
                new string[] { "Jenis Kelamin", "Usia", "Pusing", "Berat diTengkuk", "Sesak Nafas", "Jantung Berdebar",
                               "Tekanan Darah Sistolik", "Tekanan Darah Diastolik" },
                new string[] { "Laki-Laki", "Remaja", "Ya", "Ya", "Ya", "Ya", "100 mmHg", "60 mmHg" },
                new string[] { "Perempuan", "Dewasa", "Tidak", "Tidak", "Tidak", "Tidak", "110 mmHg", "70 mmHg" },
                new string[] { "", "Lansia", "", "", "", "", "120 mmHg", "80 mmHg" },
                new string[] { "", "", "", "", "", "", "130 mmHg", "90 mmHg" },
                new string[] { "", "", "", "", "", "", "140 mmHg", "100 mmHg" },
                new string[] { "", "", "", "", "", "", "150 mmHg", "" },
                new string[] { "", "", "", "", "", "", "160 mmHg", "" },
                new string[] { "", "", "", "", "", "", "170 mmHg", "" },
                new string[] { "", "", "", "", "", "", "180 mmHg", "" },
                new string[] { "", "", "", "", "", "", "190 mmHg", "" },
                new string[] { "", "", "", "", "", "", "200 mmHg", "" }
            };
        }

        private void LoadData()
        {
            // Load data from prior.csv (you need to provide the file path correctly)
            string[] lines = File.ReadAllLines("prior.csv");
            int rows = lines.Length;
            int columns = lines[0].Split(',').Length;
            data = new string[rows, columns];

            for (int i = 0; i < rows; i++)
            {
                string[] values = lines[i].Split(',');
                for (int j = 0; j < columns; j++)
                {
                    data[i, j] = values[j];
                }
            }
        }

        private (double, double) CalculateP_X_H(string attribute)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < data.GetLength(0); i++)
            {
                if (data[i, 0] == attribute)
                {
                    indices.Add(i);
                }
            }

            if (indices.Count == 0)
            {
                return (double.NaN, double.NaN);
            }

            double p_x_h_ya = 1.0;
            double p_x_h_tidak = 1.0;

            for (int i = 0; i < indices.Count; i++)
            {
                double value = double.Parse(data[indices[i], 1], CultureInfo.InvariantCulture);
                p_x_h_ya *= value;
                value = double.Parse(data[indices[i], 2], CultureInfo.InvariantCulture);
                p_x_h_tidak *= value;
            }

            return (p_x_h_ya, p_x_h_tidak);
        }

        private (double, double) CalculateProbabilities(string[] selectedOptions)
        {
            List<(double, double)> probabilities = new List<(double, double)>();
            foreach (string attr in selectedOptions)
            {
                (double p_x_h_ya, double p_x_h_tidak) = CalculateP_X_H(attr);
                probabilities.Add((p_x_h_ya, p_x_h_tidak));

                if (double.IsNaN(p_x_h_ya) || double.IsNaN(p_x_h_tidak))
                {
                    Console.WriteLine($"Data for attribute '{attr}' is not found.");
                }
                else
                {
                    Console.WriteLine($"{attr} P(X|H) Ya: {p_x_h_ya:F9}, P(X|H) Tidak: {p_x_h_tidak:F9}");
                }
            }

            double p_x_h_ya_product = probabilities.Aggregate(1.0, (acc, prob) => acc * prob.Item1);
            double p_x_h_tidak_product = probabilities.Aggregate(1.0, (acc, prob) => acc * prob.Item2);
            Console.WriteLine("p_x_h_ya = " + p_x_h_ya_product);
            Console.WriteLine("p_x_h_tidak = " + p_x_h_tidak_product);
            return (p_x_h_ya_product, p_x_h_tidak_product);
        }

        private void Diagnose(double p_x_h_ya_product, double p_x_h_tidak_product, double p_ya, double p_tidak)
        {
            double p_final_ya = p_x_h_ya_product * p_ya;
            double p_final_tidak = p_x_h_tidak_product * p_tidak;

            Console.WriteLine("P(X|Hasil=Ya) * P(Ya) = " + p_final_ya);
            Console.WriteLine("P(X|Hasil=Tidak) * P(Tidak) = " + p_final_tidak);

            if (p_final_ya >= p_final_tidak)
            {
                Console.WriteLine("Hasil dari nilai probabilitas akhir terbesar berada di kelas Ya, maka orang tersebut mengalami penyakit hipertensi.");
            }
            else
            {
                Console.WriteLine("Hasil dari nilai probabilitas akhir terbesar berada di kelas Tidak, maka orang tersebut tidak mengalami penyakit hipertensi.");
            }
        }

        public void RunDiagnosis()
        {
            // Load data from datatest.csv (you need to provide the file path correctly)
            string[] lines = File.ReadAllLines("datatest.csv");
            int totalSamples = lines.Length - 1; // Skip the header line
            string[] actualLabels = new string[totalSamples];
            string[] predictedLabels = new string[totalSamples];

            using (StreamWriter outputFile = new StreamWriter("hasil_uji_datatest.csv"))
            {
                outputFile.WriteLine("No.,Data Hasil,Diagnosis Result,p_x_h_ya_product,p_x_h_tidak_product");

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = lines[i].Split(',');
                    string[] attributeNames = options[0];
                    string[] selectedOptions = attributeNames
                        .Select((attr, index) => $"{attr} {values[index + 1]}")
                        .ToArray();

                    double p_ya = double.Parse(data[0, 1], CultureInfo.InvariantCulture);
                    double p_tidak = double.Parse(data[0, 2], CultureInfo.InvariantCulture);

                    (double p_x_h_ya_product, double p_x_h_tidak_product) = CalculateProbabilities(selectedOptions);

                    string diagnosisResult = p_x_h_ya_product >= p_x_h_tidak_product ? "Ya" : "Tidak";
                    actualLabels[i - 1] = values[values.Length - 1];
                    predictedLabels[i - 1] = diagnosisResult;

                    outputFile.WriteLine($"{values[0]},{values[values.Length - 1]},{diagnosisResult},{p_x_h_ya_product},{p_x_h_tidak_product}");

                    Console.WriteLine($"\nDiagnosis Result for Line: {string.Join(",", values)}");
                    Console.WriteLine($"Hasil Diagnosis: {diagnosisResult}");
                    Console.WriteLine("------------");
                }
            }

            int correctPredictions = actualLabels.Zip(predictedLabels, (actual, pred) => actual == pred ? 1 : 0).Sum();
            double accuracy = (correctPredictions / (double)totalSamples) * 100;
            Console.WriteLine($"\nAccuracy: {accuracy:F2}%");

            // Create a DataFrame for the confusion matrix (you need to use a library like NumSharp or create your own)
            // Plot the confusion matrix as a heatmap (you need to use a library like Matplotlib or create your own)
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            HypertensionDiagnosis diagnosis = new HypertensionDiagnosis();
            diagnosis.RunDiagnosis();
        }
    }
}
