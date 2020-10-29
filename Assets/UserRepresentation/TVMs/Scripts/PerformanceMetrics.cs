using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using System.Text;
using DataProviders;

public class PerformanceMetrics : MonoBehaviour
{
    private static System.Diagnostics.Process process = new System.Diagnostics.Process();
    private string fullPathToExe = "";
    private List<double> deserializeTime = new List<double>();
    private List<double> renderingTime = new List<double>();
    private List<double> deserializeFunctionTime = new List<double>();
    private List<double> deserializeTexturesTime = new List<double>();
    private List<double> deserializeGeometryTime = new List<double>();
    private List<double> deserializeParamsTime = new List<double>();
    private List<double> frameTime = new List<double>();
    private List<string> metric_units = new List<string>() { "KBs", "MBs", "GBs" };
    private long totalTime = 0;
    private int fps = 0;
    private int all_frames = 0;
    private List<int> compressed_buffer_size = new List<int>();
    private List<int> decompressed_buffer_size = new List<int>();
    private List<int> num_vertices = new List<int>();
    private StringBuilder perfomanceMetricsCSV = new StringBuilder();
    public static List<double> usageSamplesCPU = new List<double>();
    public static List<double> usageSamplesGPU = new List<double>();
    public static List<double> usageSamplesRAM = new List<double>();
    public static List<double> usageSamplesBW = new List<double>();


    public void updateReceivingAndDeserializationMetrics(List<System.Diagnostics.Stopwatch> deserializationStopWatches, List<int> tvmSizeData)
    {

        if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
            return;

        all_frames = tvmSizeData[3];
        compressed_buffer_size.Add(tvmSizeData[0]);
        decompressed_buffer_size.Add(tvmSizeData[1]);
        num_vertices.Add(tvmSizeData[2]);
        deserializeTime.Add(deserializationStopWatches[0].ElapsedMilliseconds);
        deserializeFunctionTime.Add(deserializationStopWatches[1].Elapsed.TotalMilliseconds);
        deserializeTexturesTime.Add(deserializationStopWatches[2].Elapsed.TotalMilliseconds);
        deserializeGeometryTime.Add(deserializationStopWatches[3].Elapsed.TotalMilliseconds);
        deserializeParamsTime.Add(deserializationStopWatches[4].Elapsed.TotalMilliseconds);
    }

    public void updateRenderingMetrics(System.Diagnostics.Stopwatch renderingStopWatch, System.Diagnostics.Stopwatch frametime)
    {
        if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
            return;

        ++fps;
        renderingTime.Add(renderingStopWatch.ElapsedMilliseconds);
        frameTime.Add(frametime.ElapsedMilliseconds);
    }

    private double CalculateStdDev(IEnumerable<double> values)
    {
        double ret = 0;
        if (values.Count() > 0)
        {
            //Compute the Average      
            double avg = values.Average();
            //Perform the Sum of (value-avg)_2_2      
            double sum = values.Sum(d => Math.Pow(d - avg, 2));
            //Put it all together      
            ret = Math.Sqrt((sum) / (values.Count() - 1));
        }
        return ret;
    }

    private double changeMetricUnit(double metric, int metric_unit)
    {
        for (int i = 0; i < metric_unit + 1; i++)
            metric /= 1024;

        return metric;
    }

    public void printMetrics(System.Diagnostics.Stopwatch stopWatch, string tvmObjName)
    {
        if (!Config.Instance.TVMs.printMetrics)
            return;
        try
        {
            if (!usageSamplesBW.Any() || !usageSamplesGPU.Any() || !usageSamplesCPU.Any() || !usageSamplesRAM.Any())
                return;

            totalTime += stopWatch.ElapsedMilliseconds;

            Debug.Log("sw (ms) = " + totalTime + ": General Statistics (TVM Object Name: " + tvmObjName +
           "): \n                                      Frames per second (number of frames / time): " + (((double)fps / stopWatch.ElapsedMilliseconds) * 1000).ToString("F5") +
           "\n                                      Average Frametime: " + frameTime.Average().ToString("F5") + " ms" + " (Standard deviation: " + CalculateStdDev(frameTime).ToString("F5") + ")" +
           "\n                                      Frames per second (1 / Average Frametime in seconds): " + (1000 / frameTime.Average()).ToString("F5") +
           "\n                                      Number of TVMs received but not rendered: " + ((all_frames - fps) < 0 ? 0 : (all_frames - fps)) +
           "\n                                      Average number of vertices per TVM: " + num_vertices.Average() + " (Standard deviation: " + CalculateStdDev(num_vertices.Select(x => Convert.ToDouble(x))).ToString("F5") + ")");

            Debug.Log("sw (ms) = " + totalTime + ": Compression Module (TVM Object Name: " + tvmObjName +
                                                    "): Average compressed TVM buffer size: " + changeMetricUnit(Convert.ToDouble(compressed_buffer_size.Average()), 0).ToString() + " " + metric_units[0] +
                                                                " (Standard deviation: " + changeMetricUnit(CalculateStdDev(compressed_buffer_size.Select(x => Convert.ToDouble(x))), 0).ToString("F5") + ")" +
                                                    ", Average decompressed TVM buffer size: " + changeMetricUnit(Convert.ToDouble(decompressed_buffer_size.Average()), 1).ToString() + " " + metric_units[1] +
                                                                " (Standard deviation: " + changeMetricUnit(CalculateStdDev(decompressed_buffer_size.Select(x => Convert.ToDouble(x))), 1).ToString("F5") + ")");

            Debug.Log("sw (ms) = " + totalTime + ":  Deserialization-Decompression Module (TVM Object Name: " + tvmObjName +
            "): Average total deserialization-decompression time per TVM: " + deserializeTime.Average().ToString("F5") + " ms (Standard deviation: " + CalculateStdDev(deserializeTime).ToString("F5") + ")" +
            ", Average time of deserialization-decompression function call per TVM: " + deserializeFunctionTime.Average().ToString("F5") + " ms (Standard deviation: " + CalculateStdDev(deserializeFunctionTime).ToString("F5") + ")" +
            ", Average deserialization time of texture data per TVM: " + deserializeTexturesTime.Average().ToString("F5") + " ms (Standard deviation: " + CalculateStdDev(deserializeTexturesTime).ToString("F5") + ")" +
            ", Average deserialization time of geometry (faces, vertices) data per TVM: " + deserializeGeometryTime.Average().ToString("F5") + " ms (Standard deviation: " + CalculateStdDev(deserializeGeometryTime).ToString("F5") + ")" +
            ", Average deserialization time of parameters per TVM: " + deserializeParamsTime.Average().ToString("F5") + " ms (Standard deviation: " + CalculateStdDev(deserializeParamsTime).ToString("F5") + ")");

            Debug.Log("sw (ms) = " + totalTime + ": Rendering Module (TVM Object Name: " + tvmObjName +
            ") Average rendering time per TVM: " + renderingTime.Average().ToString("F5") + " ms (Standard deviation: " + CalculateStdDev(renderingTime).ToString("F5") + ")");

            Debug.Log("sw (ms) = " + totalTime + ": PC Consumptions (TVM Object Name: " + tvmObjName +
            "): Average CPU% usage: " + usageSamplesCPU.Average().ToString("F5") + " (Standard deviation: " + CalculateStdDev(usageSamplesCPU).ToString("F5") + ")" +
            ", Average GPU% usage: " + usageSamplesGPU.Average().ToString("F5") + " (Standard deviation: " + CalculateStdDev(usageSamplesGPU).ToString("F5") + ")" +
            ", Average RAM usage (MBs): " + usageSamplesRAM.Average().ToString("F5") + " (Standard deviation: " + CalculateStdDev(usageSamplesRAM).ToString("F5") + ")" +
            ", Average BW usage (MBps): " + usageSamplesBW.Average().ToString("F5") + " (Standard deviation: " + CalculateStdDev(usageSamplesBW).ToString("F5") + ")");
        }
        catch (Exception e)
        {
            clearAll();
            Debug.Log(e);
            return;
        }
    }

    public void saveMetrics(System.Diagnostics.Stopwatch stopWatch)
    {
        if (!Config.Instance.TVMs.saveMetrics)
            return;

        try
        {
            string metricsLastTen = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31}",
                                                    DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss"), (((double)fps / stopWatch.ElapsedMilliseconds) * 1000).ToString("F5"), ((all_frames - fps) < 0 ? 0 : (all_frames - fps)).ToString(),
                                                    frameTime.Average().ToString("F5"), CalculateStdDev(frameTime).ToString("F5"),
                                                    (1000 / frameTime.Average()).ToString("F5"),
                                                    num_vertices.Average().ToString(), CalculateStdDev(num_vertices.Select(x => Convert.ToDouble(x))).ToString("F5"),
                                                    changeMetricUnit(Convert.ToDouble(compressed_buffer_size.Average()), 0).ToString(),
                                                    changeMetricUnit(CalculateStdDev(compressed_buffer_size.Select(x => Convert.ToDouble(x))), 0).ToString("F5"),
                                                    changeMetricUnit(Convert.ToDouble(decompressed_buffer_size.Average()), 1).ToString(),
                                                    changeMetricUnit(CalculateStdDev(decompressed_buffer_size.Select(x => Convert.ToDouble(x))), 1).ToString("F5"),
                                                    deserializeTime.Average().ToString("F5"), CalculateStdDev(deserializeTime).ToString("F5"),
                                                    deserializeFunctionTime.Average().ToString("F5"), CalculateStdDev(deserializeFunctionTime).ToString("F5"),
                                                    deserializeTexturesTime.Average().ToString("F5"), CalculateStdDev(deserializeTexturesTime).ToString("F5"),
                                                    deserializeGeometryTime.Average().ToString("F5"), CalculateStdDev(deserializeGeometryTime).ToString("F5"),
                                                    deserializeParamsTime.Average().ToString("F5"), CalculateStdDev(deserializeParamsTime).ToString("F5"),
                                                    renderingTime.Average().ToString("F5"), CalculateStdDev(renderingTime).ToString("F5"),
                                                    usageSamplesCPU.Average().ToString("F5"), CalculateStdDev(usageSamplesCPU).ToString("F5"),
                                                    usageSamplesGPU.Average().ToString("F5"), CalculateStdDev(usageSamplesGPU).ToString("F5"),
                                                    usageSamplesRAM.Average().ToString("F5"), CalculateStdDev(usageSamplesRAM).ToString("F5"),
                                                    usageSamplesBW.Average().ToString("F5"), CalculateStdDev(usageSamplesBW).ToString("F5"));

            if (perfomanceMetricsCSV.Length == 0)
            {
                string metricsNames = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31}",
                                                    "Timestamp", "Frames per second (number of frames / time)", "Missed / Skipped frames",
                                                    "Average frametime per TVM (milliseconds) ", "Standard deviation of average frametime per TVM (milliseconds)",
                                                    "Frames per second (1 / Average frametime in seconds)",
                                                    "Average number of vertices per TVM", "Standard deviation of number of vertices per TVM",
                                                    "Average size of received compressed TVM (" + metric_units[0] + ")", "Standard deviation of received compressed TVM 's size (" + metric_units[0] + ")",
                                                    "Average size of decompressed TVM (" + metric_units[1] + ")", "Standard deviation of decompressed TVM 's size (" + metric_units[1] + ")",
                                                    "Average total deserialization-decompression time per TVM (milliseconds)", "Standard deviation of deserialization-decompression time per TVM (milliseconds)",
                                                    "Average deserialization-decompression function execution time per TVM (milliseconds)", "Standard deviation of deserialization-decompression function execution time  per TVM (milliseconds)",
                                                    "Average marshalling time for the texture data per TVM (milliseconds)", "Standard deviation of marshalling time for the texture data per TVM (milliseconds)",
                                                    "Average marshalling time for the geometry data per TVM (milliseconds)", "Standard deviation of marshalling time for the geometry data per TVM (milliseconds)",
                                                    "Average marshalling time for the extra parameters per TVM (milliseconds)", "Standard deviation of marshalling time for the extra parameters per TVM (milliseconds)",
                                                    "Average rendering time per TVM (milliseconds)", "Standard deviation of rendering time per TVM (milliseconds)",
                                                    "Average CPU % consuption", "Standard deviation of CPU % consuption",
                                                    "Average GPU % consuption", "Standard deviation of GPU % consuption",
                                                    "Average RAM consuption (MBs)", "Standard deviation of RAM consuption (MBs)",
                                                    "Average BW consuption (Mbps)", "Standard deviation of BW consuption (Mbps)");

                perfomanceMetricsCSV.AppendLine(metricsNames);
            }

            perfomanceMetricsCSV.AppendLine(metricsLastTen);
        }
        catch (Exception e)
        {
            clearAll();
            Debug.Log(e);
            return;
        }
    }

    public void saveAndDestroy(string tvmObjName, string firstFrame)
    {
        if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
            return;

        process.Kill();

        if (Config.Instance.TVMs.saveMetrics)
        {
            String[] allLines = perfomanceMetricsCSV.ToString().Split('\n');
            for (int i = 1; i < allLines.Length; i++)
                if (String.IsNullOrEmpty(allLines[i]) || (DateTime.ParseExact(allLines[i].Split(',')[0], "yyyy-MM-dd-hh-mm-ss", null) - DateTime.ParseExact(firstFrame, "yyyy-MM-dd-hh-mm-ss", null)).TotalSeconds < 10)
                    perfomanceMetricsCSV.Remove(i, 1);

            string expDirectory = Directory.GetParent(fullPathToExe) + "\\logs\\" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
            if (!Directory.Exists(expDirectory))
                Directory.CreateDirectory(expDirectory);

            File.WriteAllText(expDirectory + "\\PerformanceMetrics_Player_" + tvmObjName + ".csv", perfomanceMetricsCSV.ToString());
        }

        perfomanceMetricsCSV.Clear();
    }

    public void clearAll()
    {
        if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
            return;

        fps = 0;
        compressed_buffer_size.Clear();
        decompressed_buffer_size.Clear();
        num_vertices.Clear();
        frameTime.Clear();
        all_frames = 0;
        renderingTime.Clear();
        deserializeTime.Clear();
        deserializeParamsTime.Clear();
        deserializeGeometryTime.Clear();
        deserializeTexturesTime.Clear();
        deserializeFunctionTime.Clear();
        usageSamplesCPU.Clear();
        usageSamplesGPU.Clear();
        usageSamplesRAM.Clear();
        usageSamplesBW.Clear();
    }

    public void runCommand()
    {
        if ((!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics) || !string.IsNullOrEmpty(fullPathToExe))
            return;

        fullPathToExe = Application.streamingAssetsPath + "\\PerformanceCounting\\PerfomanceData";

        process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fullPathToExe,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            }
        };
        //* Set your output and error (asynchronous) handlers
        process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(OutputHandler);
        process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(ErrorHandler);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private static void ErrorHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
    {
        if (outLine.Data.Length > 0)
            Debug.Log("Error: " + outLine.Data);
    }

    private static void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
    {
        //* Do your stuff with the output (write to console/log/StringBuilder)
        if (outLine.Data.Contains("CPU"))
            usageSamplesCPU.Add(double.Parse(outLine.Data.Split(' ')[2]));
        else if (outLine.Data.Contains("GPU"))
            usageSamplesGPU.Add(double.Parse(outLine.Data.Split(' ')[2]));
        else if (outLine.Data.Contains("RAM"))
            usageSamplesRAM.Add(double.Parse(outLine.Data.Split(' ')[2]));
        else if (outLine.Data.Contains("BW"))
            usageSamplesBW.Add(double.Parse(outLine.Data.Split(' ')[1]));
    }
}
