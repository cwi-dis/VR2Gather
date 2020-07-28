using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using System.Text;

public class PerformanceMetrics : MonoBehaviour
{
    private static System.Diagnostics.Process process = new System.Diagnostics.Process();
    private string fullPathToExe = "";
    private List<long> deserializeTime = new List<long>();
    private List<long> renderingTime = new List<long>();
    private List<long> deserializeFunctionTime = new List<long>();
    private List<long> deserializeTexturesTime = new List<long>();
    private List<long> deserializeGeometryTime = new List<long>();
    private List<long> deserializeParamsTime = new List<long>();
    private List<string> metric_units = new List<string>() { "KBs", "MBs", "GBs" };
    private int fps = 0;
    private int all_frames = 0;
    private int total_compressed_buffer_size = 0;
    private int total_decompressed_buffer_size = 0;
    private int total_num_vertices = 0;
    private StringBuilder perfomanceMetricsCSV = new StringBuilder();
    public static List<float> usageSamplesCPU = new List<float>();
    public static List<float> usageSamplesGPU = new List<float>();
    public static List<float> usageSamplesRAM = new List<float>();
    public static List<float> usageSamplesBW = new List<float>();

    public void updateReceivingAndDeserializationMetrics(List<System.Diagnostics.Stopwatch> deserializationStopWatches, List<int> tvmSizeData)
    {
        if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
            return;

        ++all_frames;
        total_compressed_buffer_size += tvmSizeData[0];
        total_decompressed_buffer_size += tvmSizeData[1];
        total_num_vertices += tvmSizeData[2];
        deserializeTime.Add(deserializationStopWatches[0].ElapsedMilliseconds);
        deserializeFunctionTime.Add(deserializationStopWatches[1].ElapsedMilliseconds);
        deserializeTexturesTime.Add(deserializationStopWatches[2].ElapsedMilliseconds);
        deserializeGeometryTime.Add(deserializationStopWatches[3].ElapsedMilliseconds);
        deserializeParamsTime.Add(deserializationStopWatches[4].ElapsedMilliseconds);
    }

    public void updateRenderingMetrics(System.Diagnostics.Stopwatch renderingStopWatch)
    {
        if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
            return;

        ++fps;
        renderingTime.Add(renderingStopWatch.ElapsedMilliseconds);
    }

    private double CalculateStdDev(IEnumerable<long> values)
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

    private double changeMetricUnit(double metric_int, int metric_unit)
    {
        double metric_double = Convert.ToDouble(metric_int);

        for (int i=0; i < metric_unit + 1; i++)
        {
            metric_double /= 1024;
        }

        return metric_double;
    }

    public void printMetrics(System.Diagnostics.Stopwatch stopWatch)
    {
        if (!Config.Instance.TVMs.printMetrics)
            return;
        try
        {
            Debug.Log("~-----~ Metrics for the last " + ((double)stopWatch.ElapsedMilliseconds / 1000).ToString("F5") + " seconds ~-----~");
            Debug.Log("Frames (TVMs rendered) per second: " + (((double)fps / stopWatch.ElapsedMilliseconds) * 1000).ToString("F5"));
            Debug.Log("Number of frames received but not rendered: " + (all_frames - fps));
            Debug.Log("Average number of vertices per TVM: " + (total_num_vertices / fps));
            Debug.Log("Average compressed TVM buffer size: " + changeMetricUnit(Convert.ToDouble(total_compressed_buffer_size / fps), 0).ToString() + metric_units[0]);
            Debug.Log("Average decompressed TVM buffer size: " + changeMetricUnit(Convert.ToDouble(total_decompressed_buffer_size / fps), 1).ToString() + metric_units[1]);
            Debug.Log("Average total deserialization time per TVM: " + (deserializeTime.Average()).ToString("F5") + " milliseconds (Standard deviation: " + CalculateStdDev(deserializeTime).ToString("F5") + ")");
            Debug.Log("Average time of deserialization function call per TVM: " + (deserializeFunctionTime.Average()).ToString("F5") + " milliseconds (Standard deviation: " + CalculateStdDev(deserializeFunctionTime).ToString("F5") + ")");
            Debug.Log("Average deserialization time of texture data per TVM: " + (deserializeTexturesTime.Average()).ToString("F5") + " milliseconds (Standard deviation: " + CalculateStdDev(deserializeTexturesTime).ToString("F5") + ")");
            Debug.Log("Average deserialization time of geometry (faces, vertices) data per TVM: " + (deserializeGeometryTime.Average()).ToString("F5") + " milliseconds (Standard deviation: " + CalculateStdDev(deserializeGeometryTime).ToString("F5") + ")");
            Debug.Log("Average deserialization time of parameters per TVM: " + (deserializeParamsTime.Average()).ToString("F5") + " milliseconds (Standard deviation: " + CalculateStdDev(deserializeParamsTime).ToString("F5") + ")");
            Debug.Log("Average rendering time per TVM: " + (renderingTime.Average()).ToString("F5") + " (Standard deviation: " + CalculateStdDev(renderingTime).ToString("F5") + ")");
            Debug.Log("Average CPU% usage: " + (usageSamplesCPU.Average()).ToString("F5") + " (Standard deviation: " + CalculateStdDev(usageSamplesCPU.ConvertAll(Convert.ToInt64)).ToString("F5") + ")");
            Debug.Log("Average GPU% usage: " + (usageSamplesGPU.Average()).ToString("F5") + " (Standard deviation: " + CalculateStdDev(usageSamplesGPU.ConvertAll(Convert.ToInt64)).ToString("F5") + ")");
            Debug.Log("Average RAM usage (MBs): " + (usageSamplesRAM.Average()).ToString("F5") + " (Standard deviation: " + CalculateStdDev(usageSamplesRAM.ConvertAll(Convert.ToInt64)).ToString("F5") + ")");
            Debug.Log("Average BW usage (MBps): " + (usageSamplesBW.Average()).ToString("F5") + " (Standard deviation: " + CalculateStdDev(usageSamplesBW.ConvertAll(Convert.ToInt64)).ToString("F5") + ")");
        }
        catch(Exception e)
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
            string metricsLastTen = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25}",
                                                    DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")/*((double)stopWatch.ElapsedMilliseconds / 1000).ToString("F5")*/, (((double)fps / stopWatch.ElapsedMilliseconds) * 1000).ToString("F5"), (all_frames - fps).ToString(),
                                                    (total_num_vertices / fps).ToString(), changeMetricUnit(Convert.ToDouble(total_compressed_buffer_size / fps), 0).ToString(), changeMetricUnit(Convert.ToDouble(total_decompressed_buffer_size / fps), 1).ToString(),
                                                    (deserializeTime.Average()).ToString("F5"), CalculateStdDev(deserializeTime).ToString("F5"),
                                                    (deserializeFunctionTime.Average()).ToString("F5"), CalculateStdDev(deserializeFunctionTime).ToString("F5"),
                                                    (deserializeTexturesTime.Average()).ToString("F5"), CalculateStdDev(deserializeTexturesTime).ToString("F5"),
                                                    (deserializeGeometryTime.Average()).ToString("F5"), CalculateStdDev(deserializeGeometryTime).ToString("F5"),
                                                    (deserializeParamsTime.Average()).ToString("F5"), CalculateStdDev(deserializeParamsTime).ToString("F5"),
                                                    (renderingTime.Average()).ToString("F5"), CalculateStdDev(renderingTime).ToString("F5"),
                                                    (usageSamplesCPU.Average()).ToString("F5"), CalculateStdDev(usageSamplesCPU.ConvertAll(Convert.ToInt64)).ToString("F5"),
                                                    (usageSamplesGPU.Average()).ToString("F5"), CalculateStdDev(usageSamplesGPU.ConvertAll(Convert.ToInt64)).ToString("F5"),
                                                    (usageSamplesRAM.Average()).ToString("F5"), CalculateStdDev(usageSamplesRAM.ConvertAll(Convert.ToInt64)).ToString("F5"),
                                                    (usageSamplesBW.Average()).ToString("F5"), CalculateStdDev(usageSamplesBW.ConvertAll(Convert.ToInt64)).ToString("F5"));

            if (perfomanceMetricsCSV.Length == 0)
            {
                string metricsNames = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25}",
                                                    "Timestamp", "Frames per second", "Missed/Skipped Frames",
                                                    "Average number of vertices per TVM", "Average size of received compressed TVM (" + metric_units[0] + ")", "Average size of decompressed TVM (" + metric_units[1] + ")",
                                                    "Average total deserialization time per TVM (seconds)", "Standard deviation of deserialization time per TVM (seconds)",
                                                    "Average deserialization function execution time per TVM (seconds)", "Standard deviation of deserialization function execution time  per TVM (seconds)",
                                                    "Average marshalling time for the texture data per TVM (seconds)", "Standard deviation of marshalling time for the texture data per TVM (seconds)",
                                                    "Average marshalling time for the geometry data per TVM (seconds)", "Standard deviation of marshalling time for the geometry data per TVM (seconds)",
                                                    "Average marshalling time for the extra parameters per TVM (seconds)", "Standard deviation of marshalling time for the extra parameters per TVM (seconds)",
                                                    "Average rendering time per TVM (seconds)", "Standard deviation of rendering time per TVM (seconds)",
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

    public void saveAndDestroy()
    {
        process.Kill();

        if (Config.Instance.TVMs.saveMetrics)
        {
            string expDirectory = Directory.GetParent(fullPathToExe) + "\\logs\\" + DateTime.Now.ToString("yyyy-MM-dd");
            if (!Directory.Exists(expDirectory))
                Directory.CreateDirectory(expDirectory);

            File.WriteAllText(expDirectory + "\\PerformanceMetrics_" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + ".csv", perfomanceMetricsCSV.ToString());
        }

        perfomanceMetricsCSV.Clear();
    }

    public void clearAll()
    {
        if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
            return;

        fps = 0;
        total_compressed_buffer_size = 0;
        total_decompressed_buffer_size = 0;
        total_num_vertices = 0;
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

        fullPathToExe = Directory.GetParent(Application.dataPath) + "\\PerformanceCounting\\PerfomanceData";

        process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fullPathToExe,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
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
        if(outLine.Data.Length > 0)
            Debug.Log("Error: " + outLine.Data);
    }

    private static void OutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
    {
        //* Do your stuff with the output (write to console/log/StringBuilder)
        if (outLine.Data.Contains("CPU"))
            usageSamplesCPU.Add(float.Parse(outLine.Data.Split(' ')[2]));
        else if (outLine.Data.Contains("GPU"))
            usageSamplesGPU.Add(float.Parse(outLine.Data.Split(' ')[2]));
        else if (outLine.Data.Contains("RAM"))
            usageSamplesRAM.Add(float.Parse(outLine.Data.Split(' ')[2]));
        else if (outLine.Data.Contains("BW"))
            usageSamplesBW.Add(float.Parse(outLine.Data.Split(' ')[1]));
    }
}
